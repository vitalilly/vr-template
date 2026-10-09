Shader "Custom/SonarCRTDisplay"
{
    Properties
    {
        _SonarColor ("Sonar Phosphor Color", Color) = (0, 1, 0.3, 1)
        _SweepAngle ("Sweep Angle (Degrees)", Float) = 0
        _SweepWidth ("Trail Width (Degrees)", Float) = 60
        _MaxDistance ("Max Sonar Range (World Units)", Float) = 500

        [Header(Depth and Attenuation Settings)]
        _SubDepth ("Submarine Current Depth (Meters)", Float) = 50.0
        _MaxDepth ("Trench Max Depth (Meters)", Float) = 1200.0
        _AcousticAttenuation ("Acoustic Attenuation Strength", Range(0, 5)) = 1.5

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
            #pragma target 3.0
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

            // Uniforms
            sampler2D _TerrainMap;
            float4 _MapSizeWorld;
            float4 _SubmarinePos;
            float _SubmarineHeading;

            float4 _SonarColor;
            float _SweepAngle;
            float _SweepWidth;
            float _MaxDistance;

            float _SubDepth;
            float _MaxDepth;
            float _AcousticAttenuation;

            float _Curvature;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _ScanlineIntensity;
            float _VignettePower;
            float _GlowMultiplier;

            // Contacts Array Uniforms
            float4 _Contacts[16]; 
            int _ContactCount;

            float RandomNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float2 CurveUVs(float2 uv, float bend)
            {
                float2 centered = uv - 0.5;
                float r2 = centered.x * centered.x + centered.y * centered.y;
                centered *= 1.0 + r2 * bend;
                return centered + 0.5;
            }

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

            float4 frag (v2f i) : SV_Target
            {
                // 1. CRT Screen Barrel Curvature
                float2 crtUV = CurveUVs(i.uv, _Curvature);

                if (crtUV.x < 0.0 || crtUV.x > 1.0 || crtUV.y < 0.0 || crtUV.y > 1.0)
                    return float4(0, 0, 0, 0);

                float2 polarUV = (crtUV - 0.5) * 2.0;
                float radius = length(polarUV);
                if (radius > 1.0) return float4(0, 0, 0, 0);

                float angle = atan2(polarUV.y, polarUV.x) * (180.0 / 3.14159265);
                if (angle < 0.0) angle += 360.0;

                // 2. Depth Attenuation Factor
                float safeMaxDepth = max(_MaxDepth, 1.0);
                float depthRatio = saturate(_SubDepth / safeMaxDepth);
                float signalDecay = exp(-radius * depthRatio * _AcousticAttenuation * 2.0);

                // 3. Polar Range Rings & Radar Sweep Line
                float ring = frac(radius * 4.0);
                float grid = smoothstep(0.04, 0.01, abs(ring - 0.02)) * 0.25;
                grid *= lerp(1.0, 0.4, depthRatio);

                float width = max(_SweepWidth, 10.0);
                float angleDiff = _SweepAngle - angle;
                if (angleDiff < 0.0) angleDiff += 360.0;

                float phosphorTrail = 0.0;
                if (angleDiff <= width)
                {
                    phosphorTrail = 1.0 - (angleDiff / width); 
                }
                phosphorTrail *= signalDecay;

                // 4. Terrain Map World Sampling
                float2 worldOffset = polarUV * (_MaxDistance * 0.5);
                worldOffset = RotateVector(worldOffset, _SubmarineHeading);

                float2 terrainUV = ((_SubmarinePos.xy + worldOffset) / max(_MapSizeWorld.xy, float2(1,1))) + 0.5;
                
                float terrainGlow = 0.0;
                if (terrainUV.x >= 0.0 && terrainUV.x <= 1.0 && terrainUV.y >= 0.0 && terrainUV.y <= 1.0)
                {
                    float terrainValue = tex2D(_TerrainMap, terrainUV).r;
                    terrainGlow = terrainValue * phosphorTrail * 2.5 * signalDecay;
                }

                // 5. Dynamic Contact Blips Array
                float blipGlow = 0.0;
                int count = clamp(_ContactCount, 0, 16);
                for (int c = 0; c < count; c++)
                {
                    float targetDist = _Contacts[c].x / max(_MaxDistance, 1.0);
                    float targetBearing = _Contacts[c].y;

                    float distDiff = abs(radius - targetDist);
                    float bDiff = abs(angle - targetBearing);
                    if (bDiff > 180.0) bDiff = 360.0 - bDiff;

                    if (distDiff < 0.2 && bDiff < 6.0 && angleDiff <= width)
                    {
                        blipGlow += smoothstep(0.2, 0.00, distDiff) * phosphorTrail * 2.0 * signalDecay;
                    }
                }

                // Submarine Position Marker
                float centerSubBlip = smoothstep(0.03, 0.00, radius) * 1.5;

                // 6. Static / Grain Artifacts
                float staticNoise = RandomNoise(crtUV + float2(_Time.x, _Time.y)) * depthRatio * 0.15;

                // 7. Combine Signal Layers
                float baseIntensity = grid + (phosphorTrail * 0.3) + terrainGlow + blipGlow + centerSubBlip + staticNoise;

                float scanline = sin((crtUV.y + _Time.y * _ScanlineSpeed * 0.01) * _ScanlineCount * 3.14159265);
                scanline = lerp(1.0, (scanline * 0.5 + 0.5), _ScanlineIntensity);

                float vignettePowerAdjusted = _VignettePower + (depthRatio * 1.5);
                float vignette = pow(saturate(1.0 - radius * 0.5), vignettePowerAdjusted);

                float finalIntensity = baseIntensity * scanline * vignette;
                float3 finalColor = _SonarColor.rgb * finalIntensity * _GlowMultiplier;

                return float4(finalColor, saturate(finalIntensity + 0.1));
            }
            ENDCG
        }
    }
}