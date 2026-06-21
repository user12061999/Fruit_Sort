Shader "FruitSort/SpriteFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 0 = rỗng, 1 = đầy. Phần dưới mức này hiện rõ, phần trên mờ.
        _FillAmount ("Fill Amount", Range(0,1)) = 0
        // Alpha của phần CHƯA đầy (để thấy outline thùng). 0 = ẩn hẳn.
        _EmptyAlpha ("Empty Alpha", Range(0,1)) = 0.25
        // Độ mềm của mép fill (theo UV.y).
        _EdgeSoftness ("Edge Softness", Range(0,0.5)) = 0.02
        // Hướng fill: 0 = dưới->trên, 1 = trên->dưới, 2 = trái->phải, 3 = phải->trái.
        [Enum(Bottom,0,Top,1,Left,2,Right,3)] _FillDirection ("Fill Direction", Float) = 0

        // Cho phép dùng như sprite thường (blend, cull...).
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localuv  : TEXCOORD1; // uv 0..1 trong sprite, dùng để cắt fill
            };

            fixed4 _Color;
            float _FillAmount;
            float _EmptyAlpha;
            float _EdgeSoftness;
            float _FillDirection;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                // localuv: vị trí trong quad sprite (giả định sprite full-rect 0..1).
                OUT.localuv = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // Toạ độ dùng để so với mức fill, theo hướng đã chọn.
                float axis;
                if (_FillDirection < 0.5)        axis = IN.localuv.y;        // bottom -> top
                else if (_FillDirection < 1.5)   axis = 1.0 - IN.localuv.y;  // top -> bottom
                else if (_FillDirection < 2.5)   axis = IN.localuv.x;        // left -> right
                else                             axis = 1.0 - IN.localuv.x;  // right -> left

                // filled = 1 ở vùng đã đầy, mờ dần ở mép theo _EdgeSoftness.
                float edge = max(_EdgeSoftness, 1e-4);
                float filled = smoothstep(_FillAmount + edge, _FillAmount - edge, axis);

                float a = lerp(_EmptyAlpha, 1.0, filled);
                c.rgb *= c.a; // premultiplied (khớp Blend One OneMinusSrcAlpha)
                c *= a;
                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
