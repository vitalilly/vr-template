Shader "Custom/CabinWater"
{
    Properties
    {
        _WaterColor ("Water Color (RGBA)", Color) = (0.02, 0.15, 0.2, 0.75)
        _RippleTex ("Ripple Normal / Noise Texture", 2D) = "bump" {}
        _Speed ("Scroll Speed", Float) = 0.3
        _Distortion ("Surface Shimmer", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off // Renders both top and bottom (crucial when player's head goes underwater)

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

            fixed4 _WaterColor;
            sampler2D _RippleTex;
            float _Speed;
            float _Distortion;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Scroll UV coordinates over time
                float2 scrolledUV = i.uv * 4.0 + float2(_Time.y * _Speed * 0.05, _Time.y * _Speed * 0.08);
                float noise = tex2D(_RippleTex, scrolledUV).r;

                // Factor in Unity's ambient light so the water goes dark when lights are off
                float3 ambientLight = UNITY_LIGHTMODEL_AMBIENT.rgb;
                
                // Base water color multiplied by ambient lighting
                float3 finalRgb = _WaterColor.rgb * max(ambientLight, 0.05); // 0.05 prevents total pitch black

                fixed4 col;
                col.rgb = finalRgb;
                col.a = saturate(_WaterColor.a + (noise - 0.5) * _Distortion);

                return col;
            }
            ENDCG
        }
    }
}