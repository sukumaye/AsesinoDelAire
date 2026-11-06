Shader "Custom/CRT_Screen"
{
    Properties
    {
        _MainTex ("Map Texture", 2D) = "white" {}
        _Curvature ("Screen Curvature", Range(0.0, 0.25)) = 0.06
        _ScanlineIntensity ("Scanline Intensity", Range(0.0, 1.0)) = 0.30
        _ScanlineCount ("Scanline Count", Float) = 240.0
        _ScanlineSpeed ("Scanline Roll Speed", Float) = 0.15
        _VignetteStrength ("Vignette Strength", Range(0.1, 1.5)) = 0.35
        _ChromaticAberration ("Chromatic Aberration", Range(0.0, 0.02)) = 0.0035
        _PhosphorGlow ("Phosphor Glow", Range(0.8, 2.0)) = 1.12
        _FlickerStrength ("CRT Flicker", Range(0.0, 0.1)) = 0.015
        _ScreenNoise ("Static Noise", Range(0.0, 0.1)) = 0.02
        _ColorTint ("Phosphor Color Tint", Color) = (0.95, 1.0, 0.98, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Curvature;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ScanlineSpeed;
            float _VignetteStrength;
            float _ChromaticAberration;
            float _PhosphorGlow;
            float _FlickerStrength;
            float _ScreenNoise;
            float4 _ColorTint;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Deformación esférica de tubo CRT (Barrel Distortion)
            float2 CurveScreen(float2 uv)
            {
                float2 centered = uv * 2.0 - 1.0;
                float2 offset = centered.yx / float2(3.2, 2.4);
                centered = centered + centered * offset * offset * (_Curvature * 10.0);
                return centered * 0.5 + 0.5;
            }

            // Pseudo-random noise
            float RandomNoise(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = CurveScreen(i.uv);

                // Bordes negros fuera del cristal curvado
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return fixed4(0.04, 0.05, 0.04, 1.0);
                }

                // Aberración cromática RGB en los bordes de la lente
                float2 distFromCenter = uv - 0.5;
                float r = tex2D(_MainTex, uv + distFromCenter * _ChromaticAberration).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - distFromCenter * _ChromaticAberration).b;
                float3 col = float3(r, g, b);

                // Líneas de escaneo horizontales (Scanlines) con desplazamiento suave
                float scanline = sin((uv.y * _ScanlineCount) + (_Time.y * _ScanlineSpeed * 10.0));
                float scanFactor = 1.0 - (1.0 - scanline * scanline) * _ScanlineIntensity;
                col *= scanFactor;

                // Viñeteado de esquinas curvas del tubo de rayos catódicos
                float2 vigUV = uv * (1.0 - uv.yx);
                float vig = vigUV.x * vigUV.y * 16.0;
                vig = saturate(pow(vig, _VignetteStrength));
                col *= vig;

                // Ruido estático analógico y micro-parpadeo del fósforo
                float noise = RandomNoise(uv + _Time.xy) * _ScreenNoise;
                float flicker = 1.0 + sin(_Time.y * 55.0) * _FlickerStrength;
                col = (col + noise) * flicker * _PhosphorGlow;

                // Tinte de fósforo del monitor
                col *= _ColorTint.rgb;

                return fixed4(saturate(col), 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
