using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class RenderMetaballsScreenSpace : ScriptableRendererFeature
{
    class RenderMetaballsDepthPass : ScriptableRenderPass
    {
        const string MetaballDepthRTId = "_MetaballDepthRT";
        int _metaballDepthRTId;
        public Material WriteDepthMaterial;

        RenderTargetIdentifier _metaballDepthRT;
        RenderStateBlock _renderStateBlock;
        RenderQueueType _renderQueueType;
        FilteringSettings _filteringSettings;
        ProfilingSampler _profilingSampler;
        List<ShaderTagId> _shaderTagIdList = new List<ShaderTagId>();

        public RenderMetaballsDepthPass(string profilerTag, RenderPassEvent renderPassEvent,
            string[] shaderTags, RenderQueueType renderQueueType, int layerMask)
        {
            profilingSampler = new ProfilingSampler(nameof(RenderObjectsPass));

            _profilingSampler = new ProfilingSampler(profilerTag);
            this.renderPassEvent = renderPassEvent;
            this._renderQueueType = renderQueueType;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            _filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    _shaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                _shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                _shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                _shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
                _shaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            }

            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            _metaballDepthRTId = Shader.PropertyToID(MetaballDepthRTId);
            cmd.GetTemporaryRT(_metaballDepthRTId, blitTargetDescriptor);
            _metaballDepthRT = new RenderTargetIdentifier(_metaballDepthRTId);
            ConfigureTarget(_metaballDepthRT);
            ConfigureClear(ClearFlag.All, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (_renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings =
                CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                //Write Depth
                drawingSettings.overrideMaterial = WriteDepthMaterial;
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings,
                    ref _renderStateBlock);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    class RenderMetaballsScreenSpacePass : ScriptableRenderPass
    {
        const string MetaballRTId = "_MetaballRT";
        const string MetaballRT2Id = "_MetaballRT2";
        const string MetaballDepthRTId = "_MetaballDepthRT";

        int _metaballRTId;
        int _metaballRT2Id;
        int _metaballDepthRTId;

        public Material BlitMaterial;
        Material _blurMaterial;
        Material _blitCopyDepthMaterial;

        public int BlurPasses;
        public float BlurDistance;

        RenderTargetIdentifier _metaballRT;
        RenderTargetIdentifier _metaballRT2;
        RenderTargetIdentifier _metaballDepthRT;
        RenderTargetIdentifier _cameraTargetId;
        RenderTargetIdentifier _cameraDepthTargetId;

        RenderQueueType _renderQueueType;
        FilteringSettings _filteringSettings;
        ProfilingSampler _profilingSampler;

        List<ShaderTagId> ShaderTagIdList = new List<ShaderTagId>();

        RenderStateBlock _renderStateBlock;

        public RenderMetaballsScreenSpacePass(string profilerTag, RenderPassEvent renderPassEvent,
            string[] shaderTags,
            RenderQueueType renderQueueType, int layerMask)
        {
            profilingSampler = new ProfilingSampler(nameof(RenderObjectsPass));

            _profilingSampler = new ProfilingSampler(profilerTag);
            this.renderPassEvent = renderPassEvent;
            this._renderQueueType = renderQueueType;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            _filteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    ShaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
                ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            }

            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            _blitCopyDepthMaterial = new Material(Shader.Find("Hidden/BlitToDepth"));
            _blurMaterial = new Material(Shader.Find("Hidden/KawaseBlur"));
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
            _metaballDepthRTId = Shader.PropertyToID(MetaballDepthRTId);

            cmd.GetTemporaryRT(_metaballRTId, blitTargetDescriptor, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_metaballRT2Id, blitTargetDescriptor, FilterMode.Bilinear);

            _metaballRT = new RenderTargetIdentifier(_metaballRTId);
            _metaballRT2 = new RenderTargetIdentifier(_metaballRT2Id);
            _metaballDepthRT = new RenderTargetIdentifier(_metaballDepthRTId);

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
            SortingCriteria sortingCriteria = (_renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings =
                CreateDrawingSettings(ShaderTagIdList, ref renderingData, sortingCriteria);

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                //Clear small RT
                cmd.ClearRenderTarget(true, true, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //Blit Camera Depth Texture
                Blit(cmd, _cameraDepthTargetId, _metaballRT, _blitCopyDepthMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //Draw to RT
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings,
                    ref _renderStateBlock);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //Blur
                cmd.SetGlobalTexture("_BlurDepthTex", _metaballDepthRT);
                cmd.SetGlobalFloat("_BlurDistance", BlurDistance);
                float offset = 1.5f;
                cmd.SetGlobalFloat("_Offset", offset);
                Blit(cmd, _metaballRT, _metaballRT2, _blurMaterial);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                var tmpRT = _metaballRT;
                _metaballRT = _metaballRT2;
                _metaballRT2 = tmpRT;

                for (int i = 1; i < BlurPasses; ++i)
                {
                    offset += 1.0f;
                    cmd.SetGlobalFloat("_Offset", offset);
                    Blit(cmd, _metaballRT, _metaballRT2, _blurMaterial);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    tmpRT = _metaballRT;
                    _metaballRT = _metaballRT2;
                    _metaballRT2 = tmpRT;
                }

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
            cmd.ReleaseTemporaryRT(_metaballDepthRTId);
        }
    }

    public string PassTag = "RenderMetaballsScreenSpace";
    public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

    public RenderObjects.FilterSettings FilterSettings = new RenderObjects.FilterSettings();

    public Material BlitMaterial;
    public Material WriteDepthMaterial;

    RenderMetaballsDepthPass _renderMetaballsDepthPass;
    RenderMetaballsScreenSpacePass _scriptableMetaballsScreenSpacePass;

    [Range(1, 15)]
    public int BlurPasses = 1;

    [Range(0f, 1f)]
    public float BlurDistance = 0.5f;

    /// <inheritdoc/>
    public override void Create()
    {
        _renderMetaballsDepthPass = new RenderMetaballsDepthPass(PassTag, Event, FilterSettings.PassNames,
            FilterSettings.RenderQueueType, FilterSettings.LayerMask)
        {
            WriteDepthMaterial = WriteDepthMaterial
        };

        _scriptableMetaballsScreenSpacePass = new RenderMetaballsScreenSpacePass(PassTag, Event,
            FilterSettings.PassNames, FilterSettings.RenderQueueType, FilterSettings.LayerMask)
        {
            BlitMaterial = BlitMaterial,
            BlurPasses = BlurPasses,
            BlurDistance = BlurDistance
        };
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_renderMetaballsDepthPass);
        renderer.EnqueuePass(_scriptableMetaballsScreenSpacePass);
    }
}