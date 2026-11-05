Shader "Hidden/RenderingBugs/UIBlur"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // 読む範囲(片側のテクセル数)。σ の 3 倍まで読めれば重みの取りこぼしはほぼない
        #define BLUR_RADIUS 12

        float _BlurSigma; // ぼかしの強さ(テクセル単位の標準偏差)

        // 方向 dir に沿ったガウスぼかし。1 テクセルずつ読み、重みはその場で計算する
        half4 Blur(float2 uv, float2 dir)
        {
            float2 stepUV = dir * _BlitTexture_TexelSize.xy;
            half4 sum = 0;
            float weightSum = 0;
            for (int i = -BLUR_RADIUS; i <= BLUR_RADIUS; i++)
            {
                float w = exp(-(i * i) / (2.0 * _BlurSigma * _BlurSigma));
                sum += SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + stepUV * i, 0) * w;
                weightSum += w;
            }
            return sum / weightSum;
        }
        ENDHLSL

        // 0: 縮小。バイリニアで 2x2 ずつ平均した 4 点をさらに平均し、1/4 なら 4x4、1/2 なら周囲を含む 4x4 を読む
        Pass
        {
            Name "UIBlur_Downsample"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 t = _BlitTexture_TexelSize.xy;
                half4 c = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-1, -1) * t, 0)
                        + SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( 1, -1) * t, 0)
                        + SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2(-1,  1) * t, 0)
                        + SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv + float2( 1,  1) * t, 0);
                return c * 0.25;
            }
            ENDHLSL
        }

        // 1: 横方向
        Pass
        {
            Name "UIBlur_Horizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return Blur(input.texcoord, float2(1, 0));
            }
            ENDHLSL
        }

        // 2: 縦方向
        Pass
        {
            Name "UIBlur_Vertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return Blur(input.texcoord, float2(0, 1));
            }
            ENDHLSL
        }
    }
}
