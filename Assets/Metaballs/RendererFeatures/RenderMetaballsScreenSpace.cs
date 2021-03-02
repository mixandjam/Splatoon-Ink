using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderMetaballsScreenSpace : ScriptableRendererFeature
{
    class RenderMetaballsScreenSpacePass : ScriptableRenderPass
    {
        const string MetaballRTId = "_MetaballRT";
        const string MetaballRT2Id = "_MetaballRT2";

        int _metaballRTId;
        int _metaballRT2Id;

        public Material BlitMaterial;
        public Material BlurMaterial;
        public Material BlitCopyDepthMaterial;

        RenderTargetIdentifier _metaballRT;
        RenderTargetIdentifier _metaballRT2;
        RenderTargetIdentifier _cameraTargetId;
        RenderTargetIdentifier _cameraDepthTargetId;

        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        RenderObjects.CustomCameraSettings m_CameraSettings;
        ProfilingSampler m_ProfilingSampler;

        public Material overrideMaterial { get; set; }
        public int overrideMaterialPassIndex { get; set; }

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        RenderStateBlock m_RenderStateBlock;

        public RenderMetaballsScreenSpacePass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags,
            RenderQueueType renderQueueType, int layerMask, RenderObjects.CustomCameraSettings cameraSettings)
        {
            profilingSampler = new ProfilingSampler(nameof(RenderObjectsPass));

            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
                m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            }

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_CameraSettings = cameraSettings;

            BlitCopyDepthMaterial = new Material(Shader.Find("Hidden/BlitToDepth"));
            BlurMaterial = new Material(Shader.Find("Hidden/KawaseBlur"));
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            blitTargetDescriptor.colorFormat = RenderTextureFormat.ARGB32;

            var renderer = renderingData.cameraData.renderer;

            _metaballRTId = Shader.PropertyToID(MetaballRTId);
            _metaballRT2Id = Shader.PropertyToID(MetaballRT2Id);

            cmd.GetTemporaryRT(_metaballRTId, blitTargetDescriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_metaballRT2Id, blitTargetDescriptor, FilterMode.Bilinear);

            _metaballRT = new RenderTargetIdentifier(_metaballRTId);
            _metaballRT2 = new RenderTargetIdentifier(_metaballRT2Id);

            ConfigureTarget(_metaballRT);

            _cameraTargetId = renderer.cameraColorTarget;
            _cameraDepthTargetId = new RenderTargetIdentifier("_CameraDepthTexture"); // renderer.cameraDepthTarget;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings =
                CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;

            ref CameraData cameraData = ref renderingData.cameraData;

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                //Clear small RT
                cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //Blit Camera Depth Texture
                Blit(cmd, _cameraDepthTargetId, _metaballRT, BlitCopyDepthMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //Draw to RT
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings,
                    ref m_RenderStateBlock);

                if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera)
                {
                    RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.GetViewMatrix(),
                        cameraData.GetGPUProjectionMatrix(), false);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //Blur
                cmd.SetGlobalVector("_Offsets", new Vector4(1.5f, 2.0f, 2.5f, 3.0f));
                Blit(cmd, _metaballRT, _metaballRT2, BlurMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Blit(cmd, _metaballRT2, _metaballRT, BlurMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Blit(cmd, _metaballRT, _metaballRT2, BlurMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Blit(cmd, _metaballRT2, _metaballRT, BlurMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //Draw to Camera Target
                Blit(cmd, _metaballRT, _cameraTargetId, BlitMaterial);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_metaballRTId);
            cmd.ReleaseTemporaryRT(_metaballRT2Id);
        }
    }

    public Material blitMaterial;
    RenderMetaballsScreenSpacePass _scriptableMetaballsScreenSpacePass;
    public RenderObjects.RenderObjectsSettings renderObjectsSettings = new RenderObjects.RenderObjectsSettings();

    /// <inheritdoc/>
    public override void Create()
    {
        RenderObjects.FilterSettings filter = renderObjectsSettings.filterSettings;
        _scriptableMetaballsScreenSpacePass = new RenderMetaballsScreenSpacePass(renderObjectsSettings.passTag,
            renderObjectsSettings.Event,
            filter.PassNames, filter.RenderQueueType, filter.LayerMask, renderObjectsSettings.cameraSettings)
        {
            BlitMaterial = blitMaterial
        };
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_scriptableMetaballsScreenSpacePass);
    }
}