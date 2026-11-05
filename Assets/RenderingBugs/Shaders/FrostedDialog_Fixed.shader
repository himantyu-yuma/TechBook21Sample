Shader "RenderingBugs/FrostedDialog_Fixed"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {} // uGUI の Image が設定するため宣言しておく
        _TintColor ("Tint", Color) = (1, 1, 1, 0.2)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha // 角丸のスプライトの透明な部分を抜く
        ZTest [unity_GUIZTestMode]

        Pass
        {
            Name "FrostedDialog"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_UIBlurTexture);
            SAMPLER(sampler_UIBlurTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 clipPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.clipPos = output.positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                half3 blur = SAMPLE_TEXTURE2D(_UIBlurTexture, sampler_UIBlurTexture, screenUV).rgb;
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                return half4(lerp(blur, _TintColor.rgb, _TintColor.a), alpha);
            }
            ENDHLSL
        }
    }
}
