Shader "Custom/SpriteGridFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Columns ("Columns", Float) = 5
        _Rows ("Rows", Float) = 4
        _FillAmount ("Fill Amount", Range(0,1)) = 1
        _CellGap ("Cell Gap", Range(0,0.45)) = 0.02
        [HideInInspector] _LocalBounds ("Local Bounds", Vector) = (-0.5,-0.5,1,1)
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0

        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float4 _LocalBounds;
            float _Columns;
            float _Rows;
            float _FillAmount;
            float _CellGap;
        CBUFFER_END

        float2 GetGridUV(float2 positionOS, float2 spriteFlip)
        {
            float2 size = max(_LocalBounds.zw, float2(0.00001, 0.00001));
            float2 uv = (positionOS - _LocalBounds.xy) / size;

            // Giữ thứ tự trái->phải, dưới->trên theo hình đang nhìn khi SpriteRenderer bị flip.
            if (spriteFlip.x < 0.0) uv.x = 1.0 - uv.x;
            if (spriteFlip.y < 0.0) uv.y = 1.0 - uv.y;
            return uv;
        }

        half GetGridMask(float2 gridUV)
        {
            float columns = max(1.0, floor(_Columns + 0.5));
            float rows = max(1.0, floor(_Rows + 0.5));
            float2 gridSize = float2(columns, rows);

            // Tránh UV=1 tạo cell giả ở biên phải/trên.
            float2 safeUV = min(saturate(gridUV), 0.999999);
            float2 scaled = safeUV * gridSize;
            float2 cell = floor(scaled);
            float2 cellUV = frac(scaled);
            float cellIndex = cell.y * columns + cell.x;

            float progress = saturate(_FillAmount) * columns * rows;
            float cellProgress = saturate(progress - cellIndex);

            // step riêng thứ hai loại bỏ vạch 1 pixel khi Fill Amount = 0.
            half fillMask = step(cellUV.x, cellProgress) * step(0.00001, cellProgress);
            float2 edgeDistance = min(cellUV, 1.0 - cellUV);
            half gapMask = step(saturate(_CellGap), min(edgeDistance.x, edgeDistance.y));
            return fillMask * gapMask;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
                float2 gridUV : TEXCOORD4;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                float2 unflippedPosition = input.positionOS.xy;
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.gridUV = GetGridUV(unflippedPosition, unity_SpriteProps.xy);
                return output;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 color = CommonLitFragment(input, input.color);
                half gridMask = GetGridMask(input.gridUV);
                color.rgb *= gridMask;
                color.a *= gridMask;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
                float2 gridUV : TEXCOORD4;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            Varyings NormalsVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                float2 unflippedPosition = input.positionOS.xy;
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonNormalsVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.gridUV = GetGridUV(unflippedPosition, unity_SpriteProps.xy);
                return output;
            }

            half4 NormalsFragment(Varyings input) : SV_Target
            {
                half4 color = CommonNormalsFragment(input, input.color);
                half gridMask = GetGridMask(input.gridUV);
                color.rgb *= gridMask;
                color.a *= gridMask;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 gridUV : TEXCOORD4;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                float2 unflippedPosition = input.positionOS.xy;
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.gridUV = GetGridUV(unflippedPosition, unity_SpriteProps.xy);
                return output;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                half4 color = CommonUnlitFragment(input, input.color);
                half gridMask = GetGridMask(input.gridUV);
                color.rgb *= gridMask;
                color.a *= gridMask;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
