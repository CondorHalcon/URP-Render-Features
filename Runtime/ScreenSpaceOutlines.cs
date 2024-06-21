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

            public ViewSpaceNormalsTexturePass(RenderPassEvent renderPassEvent, LayerMask layerMask, ViewSpaceNormalsTextureSettings settings, Shader shader, Shader occluderShader) { }
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) { }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
            public override void OnCameraCleanup(CommandBuffer cmd) { }
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
            public ScreenSpaceOutlinesPass(RenderPassEvent renderPassEvent, ScreenSpaceOutlinesSettings settings, Shader shader) { }
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }
            public override void OnCameraCleanup(CommandBuffer cmd) { }
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
