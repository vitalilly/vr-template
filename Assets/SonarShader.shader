Shader "Custom/SonarCRTDisplay"
{
    Properties
    {
        _SonarColor ("Sonar Phosphor Color", Color) = (0, 1, 0.3, 1)
        _SweepAngle ("Sweep Angle (Degrees)", Float) = 0
        _SweepWidth ("Trail Width (Degrees)", Float) = 60
        _MaxDistance ("Max Sonar Range (World Units)", Float) = 500

        [Header(Terrain Map Input)]
        _TerrainMap ("Terrain Mask (White = Land)", 2D) = "black" {}
        _MapSizeWorld ("Terrain Map World Size (Width, Height)", Vector) = (1000, 1000, 0, 0)
        _SubmarinePos ("Submarine World Position", Vector) = (0, 0, 0, 0)
        _SubmarineHeading ("Submarine Heading (Rad)", Float) = 0.0
        
        [Header(CRT Artifact Settings)]
        _Curvature ("Screen Curvature", Range(0, 1)) = 0.15
        _ScanlineCount ("Scanline Count", Float) = 150.0
        _ScanlineSpeed ("Scanline Scroll Speed", Float) = 2.0
        _ScanlineIntensity ("Scanline Darkening", Range(0, 1)) = 0.35
        _VignettePower ("Vignette Edge Darkness", Range(0, 5)) = 2.0
        _GlowMultiplier ("Bloom Output Multiplier", Range(1, 5)) = 2.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Global Shader Uniforms
            sampler2D _TerrainMap;
            float4 _MapSizeWorld;
            float4 _SubmarinePos;
            float _SubmarineHeading;

            fixed4 _SonarColor;
            float _SweepAngle;
            float _SweepWidth;
            float _MaxDistance;

            float _Curvature;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _ScanlineIntensity;
            float _VignettePower;
            float _GlowMultiplier;

            // Target Contact Blips Array (x = World Distance, y = World Bearing Angle in Deg)
            float4 _Contacts[16]; 
            int _ContactCount;

            // --- 1. CRT Screen Barrel Curvature Helper ---
            float2 CurveUVs(float2 uv, float bend)
            {
                float2 centered = uv - 0.5;
                float r2 = centered.x * centered.x + centered.y * centered.y;
                centered *= 1.0 + r2 * bend;
                return centered + 0.5;
            }

            // --- 2. 2D Vector Rotation Helper ---
            float2 RotateVector(float2 v, float angleRad)
            {
                float s = sin(angleRad);
                float c = cos(angleRad);
                return float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Apply CRT Screen Barrel Curvature
                float2 crtUV = CurveUVs(i.uv, _Curvature);

                // Clip pixels distorted beyond screen bezel bounds [0, 1]
                if (crtUV.x < 0.0 || crtUV.x > 1.0 || crtUV.y < 0.0 || crtUV.y > 1.0)
                    return fixed4(0, 0, 0, 0);

                // Center coordinates to [-1.0, 1.0] domain (Submarine locked at center 0,0)
                float2 polarUV = (crtUV - 0.5) * 2.0;
                float radius = length(polarUV);
                if (radius > 1.0) return fixed4(0, 0, 0, 0); // Clip outer circular display frame

                // Calculate pixel polar bearing angle [0, 360] degrees
                float angle = atan2(polarUV.y, polarUV.x) * (180.0 / 3.14159265);
                if (angle < 0.0) angle += 360.0;

                // --- 2. Polar Range Rings & Radar Sweep Line ---
                float ring = frac(radius * 4.0);
                float grid = smoothstep(0.04, 0.01, abs(ring - 0.02)) * 0.25;

                float width = (_SweepWidth > 0.0) ? _SweepWidth : 60.0;
                float angleDiff = _SweepAngle - angle;
                if (angleDiff < 0.0) angleDiff += 360.0;

                float phosphorTrail = 0.0;
                if (angleDiff <= width)
                {
                    phosphorTrail = 1.0 - (angleDiff / width); 
                }

                // --- 3. Terrain Map World Sampling ---
                // Convert screen radius & polar vector to real-world offset distance relative to sub
                float2 worldOffset = polarUV * (_MaxDistance * 0.5);
                
                // Rotate offset vector by sub's heading angle so the display remains Head-Up
                worldOffset = RotateVector(worldOffset, _SubmarineHeading);

                // Translate to absolute world position and normalize into [0.0, 1.0] terrain UV space
                // Adding 0.5 centers the map origin (0,0) to the middle of the texture
                float2 terrainUV = ((_SubmarinePos.xy + worldOffset) / _MapSizeWorld.xy) + 0.5;
                
                float terrainGlow = 0.0;
                // Sample terrain map only when within valid texture coordinates
                if (terrainUV.x >= 0.0 && terrainUV.x <= 1.0 && terrainUV.y >= 0.0 && terrainUV.y <= 1.0)
                {
                    half terrainValue = tex2D(_TerrainMap, terrainUV).r;
                    // Illuminate land/obstacle tiles bright green when hit by the sweep beam
                    terrainGlow = terrainValue * phosphorTrail * 2.5;
                }

                // --- 4. Dynamic Contact Blips Array ---
                float blipGlow = 0.0;
                for (int c = 0; c < _ContactCount; c++)
                {
                    float targetDist = _Contacts[c].x / _MaxDistance;
                    float targetBearing = _Contacts[c].y;

                    float distDiff = abs(radius - targetDist);
                    float bDiff = abs(angle - targetBearing);
                    if (bDiff > 180.0) bDiff = 360.0 - bDiff;

                    if (distDiff < 0.04 && bDiff < 3.0 && angleDiff <= width)
                    {
                        blipGlow += smoothstep(0.04, 0.00, distDiff) * phosphorTrail * 2.0;
                    }
                }

                // Submarine Position Marker (Always illuminated at screen center 0,0)
                float centerSubBlip = smoothstep(0.03, 0.00, radius) * 1.5;

                // --- 5. Combine Signal Layers ---
                float baseIntensity = grid + (phosphorTrail * 0.3) + terrainGlow + blipGlow + centerSubBlip;

                // Animated CRT Scanline Overlay
                float scanline = sin((crtUV.y + _Time.y * _ScanlineSpeed * 0.01) * _ScanlineCount * 3.14159265);
                scanline = lerp(1.0, (scanline * 0.5 + 0.5), _ScanlineIntensity);

                // Lens Edge Vignette Shadow
                float vignette = radius;
                vignette = pow(1.0 - vignette * 0.5, _VignettePower);

                // Final Color Synthesis with HDR Glow Multiplier
                float finalIntensity = baseIntensity * scanline * vignette;
                fixed3 finalColor = _SonarColor.rgb * finalIntensity * _GlowMultiplier;

                return fixed4(finalColor, clamp(finalIntensity + 0.1, 0.0, 1.0));
            }
            ENDCG
        }
    }
}