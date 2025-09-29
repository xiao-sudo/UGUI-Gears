Shader "Custom_UIRectMask"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
        _Rect("Rect In Screen (Center, Size)", Vector) = (0, 0, 1000, 1000)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIRectMask"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment pixel
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                half4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _Rect;
            CBUFFER_END

            v2f vert(appdata_t v)
            {
                v2f OUT;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);


                OUT.worldPosition = v.vertex;
                OUT.vertex = TransformObjectToHClip(v.vertex.xyz);

                float2 pixelSize = OUT.vertex.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                OUT.mask = half4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                                     0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.color = v.color * _Color;
                return OUT;
            }

            // This is a overridable method for calculate UI image color
            // if you want make some effects for UI image, please override this method.
            #ifndef INITIALIZE_UI_IMAGE
            #define INITIALIZE_UI_IMAGE InitializeUIImage

            void InitializeUIImage(v2f IN, inout float4 color)
            {
                color = IN.color * (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord) + _TextureSampleAdd);
            }
            #endif

            half _IsInUICamera;

            float4 pixel(v2f IN) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0 / alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision) * invAlphaPrecision;

                half4 color;
                INITIALIZE_UI_IMAGE(IN, color);

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                // Guaranteeing that your UI texture is selected "sRGB (Color Texture)" in "(Texture 2D) Import Setting".
                color.rgb = lerp(color.rgb, LinearToSRGB(color.rgb), _IsInUICamera);

                float2 center = _Rect.xy;
                float2 halfSize = _Rect.zw * 0.5;
                float2 distanceToCenter = abs(IN.worldPosition.xy - center);
                
                float2 inRect = step(distanceToCenter, halfSize);
                float xyAllInRect = inRect.x * inRect.y;
                color.a *= 1 - xyAllInRect;

                // Guaranteeing that your UI shader is in mode of "One OneMinusSrcAlpha".
                color.rgb *= color.a;

                return color;
            }
            ENDHLSL
        }
    }
}