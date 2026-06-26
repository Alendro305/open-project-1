// This code is an adaptation of the open-source work by Alexander Ameye
// From a tutorial originally posted here:
// https://alexanderameye.github.io/outlineshader
// Code also available on his Gist account
// https://gist.github.com/AlexanderAmeye

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class DepthNormalsFeature : ScriptableRendererFeature
{
    class DepthNormalsPass : ScriptableRenderPass
    {
        const int kDepthBufferBits = 32;
        const string kTextureName = "_CameraDepthNormalsTexture";
        static readonly int kTextureId = Shader.PropertyToID(kTextureName);

        readonly FilteringSettings m_FilteringSettings;
        readonly ShaderTagId m_ShaderTagId = new ShaderTagId("DepthOnly");

        Material m_DepthNormalsMaterial;
        RTHandle m_DepthNormalsRT;
        RenderTextureDescriptor m_Descriptor;

        class PassData
        {
            public RendererListHandle rendererList;
        }

        public DepthNormalsPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material)
        {
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_DepthNormalsMaterial = material;
            profilingSampler = new ProfilingSampler("DepthNormals Prepass");
        }

        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            m_Descriptor = baseDescriptor;
            m_Descriptor.colorFormat = RenderTextureFormat.ARGB32;
            m_Descriptor.depthBufferBits = kDepthBufferBits;
            m_Descriptor.msaaSamples = 1;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref m_DepthNormalsRT,
                m_Descriptor,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                name: kTextureName);

            TextureHandle destination = renderGraph.ImportTexture(m_DepthNormalsRT);
            if (!destination.IsValid())
                return;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                var sortFlags = cameraData.defaultOpaqueSortFlags;
                var drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagId, renderingData, cameraData, lightData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;
                drawSettings.overrideMaterial = m_DepthNormalsMaterial;

                passData.rendererList = renderGraph.CreateRendererList(
                    new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings));

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(destination, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(destination, kTextureId);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(true, true, Color.black);
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }

        public void Dispose()
        {
            m_DepthNormalsRT?.Release();
        }
    }

    DepthNormalsPass m_DepthNormalsPass;
    Material m_DepthNormalsMaterial;

    public override void Create()
    {
        m_DepthNormalsMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");
        m_DepthNormalsPass = new DepthNormalsPass(RenderQueueRange.opaque, -1, m_DepthNormalsMaterial);
        m_DepthNormalsPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_DepthNormalsPass.Setup(renderingData.cameraData.cameraTargetDescriptor);
        renderer.EnqueuePass(m_DepthNormalsPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_DepthNormalsPass?.Dispose();
        m_DepthNormalsPass = null;
        CoreUtils.Destroy(m_DepthNormalsMaterial);
    }
}
