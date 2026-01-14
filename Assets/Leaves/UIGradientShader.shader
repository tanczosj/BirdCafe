    Shader "Custom/UIGradientShader"
    {
        Properties
        {
            [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
            _Color ("Left Color", Color) = (1,1,1,1)
            _Color2 ("Right Color", Color) = (1,1,1,1)
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
                    float2 texcoord : TEXCOORD0;
                };

                fixed4 _Color;
                fixed4 _Color2;
                sampler2D _MainTex;

                v2f vert(appdata_t IN)
                {
                    v2f OUT;
                    OUT.vertex = UnityObjectToClipPos(IN.vertex);
                    OUT.texcoord = IN.texcoord;
                    OUT.color = IN.color; // Pass vertex color if needed, or set to white
                    return OUT;
                }

                fixed4 frag(v2f IN) : SV_Target
                {
                    fixed4 texColor = tex2D(_MainTex, IN.texcoord);
                    fixed4 gradientColor = lerp(_Color, _Color2, IN.texcoord.x); // Interpolate based on X-coordinate for horizontal
                    return texColor * gradientColor * IN.color; // Apply gradient and original vertex color
                }
                ENDCG
            }
        }
    }