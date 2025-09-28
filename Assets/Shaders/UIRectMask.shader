Shader "Custom_UIRectMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" { }
        _Color ("Tint", Color) = (1.000000,1.000000,1.000000,1.000000)
        _StencilComp ("Stencil Comparison", Float) = 8.000000
        _Stencil ("Stencil ID", Float) = 0.000000
        _StencilOp ("Stencil Operation", Float) = 0.000000
        _StencilWriteMask ("Stencil Write Mask", Float) = 255.000000
        _StencilReadMask ("Stencil Read Mask", Float) = 255.000000
        _ColorMask ("Color Mask", Float) = 15.000000
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0.000000

        _Center("Center",Vector) = (0,0,0,0)
        _Size("Size",Vector) = (1000, 1000, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType"="Plane"
        }
        Pass
        {
            Tags
            {
                "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType"="Plane"
            }
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            Cull Off
            Stencil
            {
                Ref [_Stencil]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilOp]
            }
            Blend One OneMinusSrcAlpha
            ColorMask [_ColorMask]

            LOD 100

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
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _Center;
            float4 _Size;

            v2f vert(appdata v)
            {
                v2f o;
                o.screenPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;

                float2 distanceToCenter = abs(i.screenPos - _Center.xy);
                float2 halfSize = float2(_Size.x / 2, _Size.y / 2);

                // 判断是否在透明的矩形区域内
                float2 insideRect = step(distanceToCenter, halfSize);
                float isTransparent = insideRect.x * insideRect.y;

                // 如果在矩形区域内，透明度设为 0；否则保持原始颜色
                col.a *= 1.0 - isTransparent;
                
                return col;
            }
            ENDCG
        }
    }
}