using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RenderingBugs
{
    // 縮小 → 横方向 → 縦方向 + 公開 の3パス。Frame Debugger のイベント一覧で見分けられる名前を付ける。
    class UIBlurPass : ScriptableRenderPass
    {
        const int k_DownsamplePass = 0;
        const int k_HorizontalPass = 1;
        const int k_VerticalPass = 2;

        static readonly int s_BlurSigmaId = Shader.PropertyToID("_BlurSigma");

        Material m_Material;
        int m_Downsample;
        int m_GlobalTextureId;

        class PassData
        {
            public TextureHandle source;
            public Material material;
            public int shaderPass;
        }

        public void Setup(Material material, int downsample, float blurSigma, int globalTextureId)
        {
            m_Material = material;
            m_Downsample = Mathf.Max(1, downsample);
            m_GlobalTextureId = globalTextureId;
            m_Material.SetFloat(s_BlurSigmaId, blurSigma);

            // カメラ色を入力に読むため中間テクスチャが必要。バックバッファは入力にできない。
            // カメラスタック全体が常に中間テクスチャに描かれるので、HDR や MSAA の設定によらず条件が揃う。
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError("UIBlurPass requires an intermediate color texture. Skipping.");
                return;
            }

            var desc = cameraData.cameraTargetDescriptor;
            desc.width = Mathf.Max(1, desc.width / m_Downsample);
            desc.height = Mathf.Max(1, desc.height / m_Downsample);
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            var small = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_UIBlurDownsampled", false, FilterMode.Bilinear);
            var horizontal = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_UIBlurHorizontal", false, FilterMode.Bilinear);
            var vertical = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_UIBlurVertical", false, FilterMode.Bilinear);

            AddBlurPass(renderGraph, "UIBlur_Downsample", resourceData.activeColorTexture, small, k_DownsamplePass, false);
            AddBlurPass(renderGraph, "UIBlur_Horizontal", small, horizontal, k_HorizontalPass, false);
            AddBlurPass(renderGraph, "UIBlur_Vertical", horizontal, vertical, k_VerticalPass, true);
        }

        void AddBlurPass(RenderGraph renderGraph, string passName, TextureHandle source, TextureHandle destination, int shaderPass, bool publish)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                passData.source = source;
                passData.material = m_Material;
                passData.shaderPass = shaderPass;

                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0);

                if (publish)
                {
                    // 結果はグローバル経由でしか読まれないので、パスカリングで消されないようにする
                    builder.AllowPassCulling(false);
                    builder.SetGlobalTextureAfterPass(destination, m_GlobalTextureId);
                }

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, data.shaderPass);
                });
            }
        }
    }
}
