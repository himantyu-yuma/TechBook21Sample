using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderingBugs
{
    // UI カメラ用 Renderer にだけ追加する。UI を描く前のカメラ画像をぼかし、グローバルテクスチャとして公開する。
    public class UIBlurFeature : ScriptableRendererFeature
    {
        [Tooltip("縮小・横方向・縦方向のぼかしを持つシェーダー(Hidden/RenderingBugs/UIBlur)")]
        public Shader shader;

        // 縮小のパスが元の画素をすべて読めるのは 1/2 と 1/4 のときだけなので、この2つに限る
        public enum Downsample
        {
            Half = 2,
            Quarter = 4,
        }

        [Tooltip("縮小率。Quarter なら縦横 1/4")]
        public Downsample downsample = Downsample.Quarter;

        [Tooltip("ぼかしの強さ。縮小後のテクスチャでのテクセル単位の標準偏差(σ)")]
        [Range(0.5f, 4f)] public float blurSigma = 4f;

        [Tooltip("ぼかしの結果を公開するグローバルテクスチャの名前")]
        public string globalTextureName = "_UIBlurTexture";

        Material m_Material;
        UIBlurPass m_Pass;

        public override void Create()
        {
            m_Pass = new UIBlurPass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Scene ビューやプレビューのカメラには適用しない
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;

            if (shader == null)
            {
                Debug.LogWarning($"{name}: shader is null and will be skipped.");
                return;
            }

            if (m_Material == null || m_Material.shader != shader)
            {
                CoreUtils.Destroy(m_Material);
                m_Material = CoreUtils.CreateEngineMaterial(shader);
            }

            m_Pass.Setup(m_Material, (int)downsample, blurSigma, Shader.PropertyToID(globalTextureName));
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}
