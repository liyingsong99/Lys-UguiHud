
Shader "UI/RichText"
{
    Properties
    {
        _MainTex("Font Texture", 2D) = "white" {}
        _SpriteTex("Sprite Texture", 2D) = "white" {}
        _Color("Text Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Lighting Off
        Cull Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RichTextPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            // URP Core includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // SRP Batcher compatible: Material properties in CBUFFER
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _SpriteTex_ST;
                half4 _Color;
            CBUFFER_END

            // Textures and samplers outside CBUFFER
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_SpriteTex);
            SAMPLER(sampler_SpriteTex);

            struct appdata_t
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Transform to clip space
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);

                // Transform UVs
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.uv1 = TRANSFORM_TEX(v.uv1, _SpriteTex);

                // Pass through color
                o.color = v.color;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Sample textures using URP macros
                half4 mainTexSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv0);
                half4 spriteTexSample = SAMPLE_TEXTURE2D(_SpriteTex, sampler_SpriteTex, i.uv0);

                // Original blending logic
                half4 result = i.color * i.uv1.x;
                result.a *= mainTexSample.a;
                result += i.uv1.y * i.color * spriteTexSample;

                return result;
            }
            ENDHLSL
        }
    }

    // Fallback for older pipelines
    Fallback "UI/Default"
}
