using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class ToonStylizeRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class ToonStylizeSettings
    {
        [Range(2, 16)]
        public int posterizeSteps = 4;
        [Range(0f, 3f)]
        public float outlineThickness = 1.2f;
        [Range(0f, 5f)]
        public float outlineStrength = 1f;
        public Shader shader;
    }

    public ToonStylizeSettings settings = new ToonStylizeSettings();

    class ToonStylizePass : ScriptableRenderPass
    {
        Material mat;
        ToonStylizeSettings settings;

        public ToonStylizePass(ToonStylizeSettings settings)
        {
            this.settings = settings;
        }

        // Modern Render Graph entry point
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData res = frameData.Get<UniversalResourceData>();

            // Lazy material setup
            if (mat == null && settings.shader != null)
                mat = CoreUtils.CreateEngineMaterial(settings.shader);

            if (mat == null) return;

            mat.SetFloat("_Steps", settings.posterizeSteps);
            mat.SetFloat("_Thickness", settings.outlineThickness);
            mat.SetFloat("_Strength", settings.outlineStrength);

            // Render Graph pattern: out param, then SetRenderFunc
            var pass = renderGraph.AddRenderPass<PassData>("ToonStylize", out var passData);

            pass.SetRenderFunc((PassData data, RenderGraphContext ctx) =>
            {
                Blitter.BlitCameraTexture(ctx.cmd, res.activeColorTexture, res.activeColorTexture, mat, 0);
            });
        }

        // Add this blank Execute override to silence Unity's warning (optional)
        [System.Obsolete("Use RecordRenderGraph for Render Graph pipeline in URP 17+")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

        class PassData { }
    }

    ToonStylizePass pass;

    public override void Create()
    {
        pass = new ToonStylizePass(settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
}
