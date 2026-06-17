Shader "UI/HealthBarFluidFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _FillAmount ("Fill Amount", Range(0,1)) = 1

        _LeftColor ("Left Color", Color) = (0.08, 0.55, 0.18, 1)
        _RightColor ("Right Color", Color) = (0.18, 0.95, 0.35, 1)
        _EdgeColor ("Edge Color", Color) = (0.75, 1.0, 0.82, 1)

        _GradientPower ("Gradient Power", Range(0.2, 3)) = 1.0

        _WaveAmplitude1 ("Wave Amplitude 1", Range(0, 0.08)) = 0.015
        _WaveFrequency1 ("Wave Frequency 1", Range(1, 40)) = 10
        _WaveSpeed1 ("Wave Speed 1", Range(0, 10)) = 2.2

        _WaveAmplitude2 ("Wave Amplitude 2", Range(0, 0.06)) = 0.010
        _WaveFrequency2 ("Wave Frequency 2", Range(1, 40)) = 18
        _WaveSpeed2 ("Wave Speed 2", Range(0, 10)) = 3.6

        _EdgeWidth ("Edge Width", Range(0.001, 0.15)) = 0.035
        _EdgeGlow ("Edge Glow", Range(0, 2)) = 0.75
        _Softness ("Softness", Range(0.0001, 0.05)) = 0.003
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;

            float _FillAmount;

            fixed4 _LeftColor;
            fixed4 _RightColor;
            fixed4 _EdgeColor;

            float _GradientPower;

            float _WaveAmplitude1;
            float _WaveFrequency1;
            float _WaveSpeed1;

            float _WaveAmplitude2;
            float _WaveFrequency2;
            float _WaveSpeed2;

            float _EdgeWidth;
            float _EdgeGlow;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, i.uv);

                float t = _Time.y;

                // Wave runs vertically so the right edge wiggles up/down.
                float wave1 = sin(i.uv.y * _WaveFrequency1 + t * _WaveSpeed1) * _WaveAmplitude1;
                float wave2 = sin(i.uv.y * _WaveFrequency2 - t * _WaveSpeed2) * _WaveAmplitude2;
                float wave = wave1 + wave2;

                // Live edge of the health bar.
                float edgeX = saturate(_FillAmount + wave);

                // Filled region mask.
                float fillMask = 1.0 - smoothstep(edgeX, edgeX + _Softness, i.uv.x);

                // Gradient across the visible filled portion.
                float gradientT = saturate(i.uv.x / max(_FillAmount, 0.0001));
                gradientT = pow(gradientT, _GradientPower);

                fixed3 baseRgb = lerp(_LeftColor.rgb, _RightColor.rgb, gradientT);

                // Glow band hugging the live edge.
                float edgeDistance = abs(i.uv.x - edgeX);
                float edgeMask = 1.0 - smoothstep(0.0, _EdgeWidth, edgeDistance);
                edgeMask *= fillMask;

                fixed3 finalRgb = lerp(baseRgb, _EdgeColor.rgb, edgeMask);
                finalRgb += edgeMask * _EdgeGlow * 0.08;

                fixed4 finalCol;
                // Use sprite alpha only, not sprite rgb.
                finalCol.rgb = finalRgb * i.color.rgb;
                finalCol.a = sprite.a * i.color.a * fillMask;

                return finalCol;
            }
            ENDCG
        }
    }
}