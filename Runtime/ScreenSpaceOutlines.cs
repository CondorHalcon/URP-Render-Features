using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CondorHalcon.URPRenderFeatures
{
    public class ScreenSpaceOutlines : ScriptableRendererFeature
    {
        #region Settings Types
        [System.Serializable]
        private class ViewSpaceNormalsTextureSettings
        {
            [SerializeField] internal RenderTextureFormat colorFormat = RenderTextureFormat.Default;
            [SerializeField] internal int depthBufferBits = 8;
            [SerializeField] internal FilterMode filterMode = FilterMode.Point;
            [SerializeField] internal Color backgroundColor = Color.white;
            [SerializeField] internal FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        }
        [System.Serializable]
        private class ScreenSpaceOutlinesSettings
        {
            [SerializeField] internal Color outlineColor = Color.black;
            [SerializeField, Range(0, 5)] internal float outlineScale = 1f;
            [SerializeField] internal float robertsCrossMultiplier = 1;
            [SerializeField, Range(0, 1)] internal float depthThreshold = .1f;
            [SerializeField, Range(0, 1)] internal float normalThreshold = .5f;
            [SerializeField] internal float steepAngleThreshold = .1f;
            [SerializeField] internal float steepAngleMultiplier = 1;
        }
        #endregion

        #region Normals Texture Pass
        private class ViewSpaceNormalsTexturePass : ScriptableRenderPass
        {
            private ViewSpaceNormalsTextureSettings settings;
            private readonly List<ShaderTagId> shaderTagIdList;
            private readonly RenderTargetHandle normals;
            private readonly Material normalsMaterial;
            private FilteringSettings filteringSettings;
            private readonly Material occluderMaterial;
            private FilteringSettings occluderFilteringSettings;

            public ViewSpaceNormalsTexturePass(RenderPassEvent renderPassEvent, LayerMask layerMask, ViewSpaceNormalsTextureSettings settings, Shader shader, Shader occluderShader)
            {
                this.shaderTagIdList = new List<ShaderTagId>() {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward"),
                new ShaderTagId("SRPDefaultUnlit"),
            };
                this.renderPassEvent = renderPassEvent;
                this.settings = settings;
                this.normals.Init("_SceneViewSpaceNormals");
                this.normalsMaterial = new Material(shader);
                this.filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
                this.occluderMaterial = new Material(occluderShader);
                this.occluderFilteringSettings = new FilteringSettings(RenderQueueRange.opaque, (LayerMask)int.MaxValue);
            }
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor normalsTextureDescriptor = cameraTextureDescriptor;
                normalsTextureDescriptor.colorFormat = settings.colorFormat;
                normalsTextureDescriptor.depthBufferBits = settings.depthBufferBits;

                cmd.GetTemporaryRT(normals.id, normalsTextureDescriptor, settings.filterMode);
                ConfigureTarget(normals.Identifier());
                ConfigureClear(ClearFlag.All, settings.backgroundColor);
            }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!normalsMaterial) { return; }
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, new ProfilingSampler("SceneViewSpaceNormalsTexture")))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    //  normal draw
                    DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                    drawingSettings.overrideMaterial = normalsMaterial;
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                    // occlusion draw
                    DrawingSettings occluderSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                    occluderSettings.overrideMaterial = occluderMaterial;
                    //context.DrawRenderers(renderingData.cullResults, ref occluderSettings, ref occluderFilteringSettings);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(normals.id);
            }
        }
        #endregion

        #region OutlinesPass
        private class ScreenSpaceOutlinesPass : ScriptableRenderPass
        {
            private ScreenSpaceOutlinesSettings settings;
            private readonly Material outlineMaterial;
            private RenderTargetIdentifier cameraColorTarget;
            private RenderTargetIdentifier temporaryBuffer;
            private int temporaryBufferID = Shader.PropertyToID("_TemporaryBuffer");
            public ScreenSpaceOutlinesPass(RenderPassEvent renderPassEvent, ScreenSpaceOutlinesSettings settings, Shader shader)
            {
                this.renderPassEvent = renderPassEvent;
                this.outlineMaterial = new Material(shader);
                this.outlineMaterial.SetColor("_OutlineColor", settings.outlineColor);
                this.outlineMaterial.SetFloat("_OutlineScale", settings.outlineScale);
                this.outlineMaterial.SetFloat("_RobertsCrossMultiplier", settings.robertsCrossMultiplier);
                this.outlineMaterial.SetFloat("_DepthThreshold", settings.depthThreshold);
                this.outlineMaterial.SetFloat("_NormalThreshold", settings.normalThreshold);
                this.outlineMaterial.SetFloat("_SteepAngleThreshold", settings.steepAngleThreshold);
                this.outlineMaterial.SetFloat("_SteepAngleMultiplier", settings.steepAngleMultiplier);
            }
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;

                RenderTextureDescriptor temporaryTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                cmd.GetTemporaryRT(temporaryBufferID, temporaryTextureDescriptor, FilterMode.Point);
                temporaryBuffer = new RenderTargetIdentifier(temporaryBufferID);
            }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (!outlineMaterial) { return; }

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, new ProfilingSampler("ScreenSpaceOutlines")))
                {
                    Blit(cmd, cameraColorTarget, temporaryBuffer);
                    Blit(cmd, temporaryBuffer, cameraColorTarget, outlineMaterial);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                cmd.ReleaseTemporaryRT(temporaryBufferID);
            }
        }
        #endregion

        #region Main
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        [SerializeField] private LayerMask outlinesLayerMask = (LayerMask)int.MaxValue;
        [SerializeField] private ViewSpaceNormalsTextureSettings normalsSettings;
        [SerializeField] private ScreenSpaceOutlinesSettings outlinesSettings;
        [Header("Shaders")] // shaders used in the feature
        [SerializeField] private Shader viewSpaceNormalShader;
        [SerializeField] private Shader viewSpaceOccluderShader;
        [SerializeField] private Shader screenSpaceOutlineShader;

        // pass instances
        private ViewSpaceNormalsTexturePass viewSpaceNormalsTexturePass;
        private ScreenSpaceOutlinesPass screenSpaceOutlinePass;

        public override void Create()
        {
            viewSpaceNormalsTexturePass = new ViewSpaceNormalsTexturePass(renderPassEvent, outlinesLayerMask, normalsSettings, viewSpaceNormalShader, viewSpaceOccluderShader);
            screenSpaceOutlinePass = new ScreenSpaceOutlinesPass(renderPassEvent, outlinesSettings, screenSpaceOutlineShader);
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(viewSpaceNormalsTexturePass);
            renderer.EnqueuePass(screenSpaceOutlinePass);
        }

        #endregion
    }
}
