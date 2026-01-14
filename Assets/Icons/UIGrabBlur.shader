Shader "UI/GrabBlur"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Size ("Blur Size", Range(0, 10)) = 2
        _Color ("Tint", Color) = (1,1,1,0.6)
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

        // Capture the screen behind this UI element
        GrabPass { "_GrabTexture" }

        Pass
        {
            Name "GrabBlur"
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
                float4 vertex  : SV_POSITION;
                fixed4 color   : COLOR;
                float2 uv      : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
            };

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            fixed4 _Color;
            float _Size;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex  = UnityObjectToClipPos(v.vertex);
                o.uv      = v.texcoord;
                o.color   = v.color * _Color;
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 SampleScene(float2 uv)
            {
                return tex2D(_GrabTexture, uv);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Screen UV from GrabPass
                float2 uv = i.grabPos.xy / i.grabPos.w;

                // Offset per-tap based on blur size
                float2 offset = _GrabTexture_TexelSize.xy * _Size;

                // Simple 5-tap blur (you can expand to 9/13 taps if you want it smoother)
                fixed4 c = SampleScene(uv) * 0.4;
                c += SampleScene(uv + float2( offset.x,  0)) * 0.15;
                c += SampleScene(uv + float2(-offset.x,  0)) * 0.15;
                c += SampleScene(uv + float2( 0,  offset.y)) * 0.15;
                c += SampleScene(uv + float2( 0, -offset.y)) * 0.15;

                // Apply tint & alpha from UI color
                c *= i.color;
                return c;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
