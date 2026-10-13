Shader "Custom/SonarCRTDisplay"
{
    Properties
    {
        _SonarColor ("Sonar Phosphor Color", Color) = (0, 1, 0.3, 1)
        _ContactColor ("Contact Blip Color", Color) = (0.4, 0.8, 1.0, 1.0) // Light Blue
        _SweepAngle ("Sweep Angle (Degrees)", Float) = 0
        _SweepWidth ("Trail Width (Degrees)", Float) = 60
        _MaxDistance ("Max Sonar Range (World Units)", Float) = 1000

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
            float4 _ContactColor;
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
                float2 crtUV = CurveUVs(i.uv, _Curvature);

                if (crtUV.x < 0.0 || crtUV.x > 1.0 || crtUV.y < 0.0 || crtUV.y > 1.0)
                    return float4(0, 0, 0, 0);

                float2 polarUV = (crtUV - 0.5) * 2.0;
                polarUV.x = -polarUV.x;
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
                float blipGlow = 0.;

                // Hardcoded color palette mapped to contact slots 0 through 15
                float3 beaconPalette[16] = {
                    float3(0.4, 0.8, 1.0), // Slot 0: Light Blue
                    float3(1.0, 0.6, 0.6), // Slot 1: Red
                    float3(0.2, 1.0, 0.4), // Slot 2: Green
                    float3(1.0, 0.9, 0.2), // Slot 3: Yellow
                    float3(1.0, 0.5, 0.0), // Slot 4: Orange
                    float3(0.8, 0.3, 1.0), // Slot 5: Purple
                    float3(0.0, 1.0, 1.0), // Slot 6: Cyan
                    float3(1.0, 0.4, 0.7), // Slot 7: Pink
                    float3(0.4, 0.8, 1.0), float3(0.4, 0.8, 1.0), 
                    float3(0.4, 0.8, 1.0), float3(0.4, 0.8, 1.0),
                    float3(0.4, 0.8, 1.0), float3(0.4, 0.8, 1.0), 
                    float3(0.4, 0.8, 1.0), float3(0.4, 0.8, 1.0)  // Fallbacks for slots 8-15
                };
                
                int count = clamp(_ContactCount, 0, 16);
                float3 totalContactColor = float3(0, 0, 0);
                for (int c = 0; c < count; c++)
                {
                    float2 localPos = _Contacts[c].xy; // Local X (right) and Z (forward) from submarine
                    float rawDist = _Contacts[c].z;

                    // Match the terrain map's exact scaling factor (_MaxDistance * 0.5)
                    // This places a 4m beacon at 0.8 radius on a 10m max range scope.
                    float2 normalizedContactPos = localPos / (_MaxDistance * 0.5);
                    //circular clamping
                    float contactDist = length(normalizedContactPos);
                    bool isOOB = contactDist > 1.0;
                    if (isOOB) {
                        normalizedContactPos = normalize(normalizedContactPos) * 0.98;
                    }

                    // Distance from center of CRT screen (polarUV ranges from -1 to 1)
                    float distToContact = distance(polarUV, normalizedContactPos);

                    float threshold = isOOB ? 0.025:0.05;

                    // Check if within blip radius threshold (e.g., 0.05 screen units)
                    if (distToContact < threshold)
                    {
                        float intensityMod = isOOB ? 1.9:3.5;
                        blipGlow += smoothstep(0.05, 0.005, distToContact) * intensityMod;
                        totalContactColor += beaconPalette[c] * blipGlow;
                    }
                }

                // Submarine Position Marker
                float centerSubBlip = smoothstep(0.03, 0.00, radius) * 1.5;

                // 6. Static / Grain Artifacts
                float staticNoise = RandomNoise(crtUV + float2(_Time.x, _Time.y)) * depthRatio * 0.15;

                // 7. Combine Signal Layers (Separating Base Sonar from Blips)
                float baseIntensity = grid + (phosphorTrail * 0.3) + terrainGlow + centerSubBlip + staticNoise;

                float scanline = sin((crtUV.y + _Time.y * _ScanlineSpeed * 0.01) * _ScanlineCount * 3.14159265);
                scanline = lerp(1.0, (scanline * 0.5 + 0.5), _ScanlineIntensity);

                float vignettePowerAdjusted = _VignettePower + (depthRatio * 1.5);
                float vignette = pow(saturate(1.0 - radius * 0.5), vignettePowerAdjusted);

                // Apply scanlines and vignette independently
                float finalBaseIntensity = baseIntensity * scanline * vignette;
                float finalBlipIntensity = blipGlow * scanline * vignette;

                // Combine colors: Phosphor green for the scope, light blue for contacts
                float3 baseColor = _SonarColor.rgb * finalBaseIntensity * _GlowMultiplier;
                float3 contactColor = totalContactColor * finalBlipIntensity * _GlowMultiplier;
                float3 finalColor = baseColor + contactColor;

                float totalAlpha = saturate((finalBaseIntensity + finalBlipIntensity) + 0.1);

                return float4(finalColor, totalAlpha);
            }
            ENDCG
        }
    }
}