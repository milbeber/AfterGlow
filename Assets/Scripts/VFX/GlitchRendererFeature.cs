using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// Renderer Feature для Renderer2D: выводит шейдер AfterGlow/GlitchFullscreen на экран.
/// Параметры берёт из оверрайда GlitchVolume в активных Volume, поэтому глитч
/// включается и выключается вместе с Volume_Horror (через его weight).
/// Хранит копию прошлого кадра — из неё берутся «застрявшие» блоки датамоша.
public class GlitchRendererFeature : ScriptableRendererFeature
{
    private const string DefaultShaderName = "AfterGlow/GlitchFullscreen";

    [SerializeField] private Shader shader;
    [Tooltip("До постобработки: виньетка и аберрация URP ложатся поверх глитча")]
    [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    private Material material;
    private GlitchPass pass;

    public Shader GlitchShader
    {
        get => shader;
        set => shader = value;
    }

    public override void Create()
    {
        if (shader == null) shader = Shader.Find(DefaultShaderName);

        pass?.Dispose();
        CoreUtils.Destroy(material);
        material = shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;

        pass = new GlitchPass(material) { renderPassEvent = injectionPoint };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || pass == null) return;

        var glitch = VolumeManager.instance.stack.GetComponent<GlitchVolume>();
        if (glitch == null || !glitch.IsActive())
        {
            // при следующем включении прошлый кадр устарел — не показываем его
            pass.InvalidateHistory();
            return;
        }

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;
        CoreUtils.Destroy(material);
        material = null;
    }

    private class GlitchPass : ScriptableRenderPass
    {
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int BlockCountId = Shader.PropertyToID("_BlockCount");
        private static readonly int ColorSplitId = Shader.PropertyToID("_ColorSplit");
        private static readonly int MaxShiftId = Shader.PropertyToID("_MaxShift");
        private static readonly int SpeedId = Shader.PropertyToID("_Speed");
        private static readonly int CorruptionId = Shader.PropertyToID("_Corruption");
        private static readonly int MacroBlocksId = Shader.PropertyToID("_MacroBlocks");
        private static readonly int ColorCrushId = Shader.PropertyToID("_ColorCrush");
        private static readonly int DatamoshId = Shader.PropertyToID("_Datamosh");
        private static readonly int DrownId = Shader.PropertyToID("_Drown");
        private static readonly int VoidColorId = Shader.PropertyToID("_VoidColor");
        private static readonly int BloodColorId = Shader.PropertyToID("_BloodColor");
        private static readonly int BloodBrightColorId = Shader.PropertyToID("_BloodBrightColor");
        private static readonly int NeonColorId = Shader.PropertyToID("_NeonColor");
        private static readonly int PaleColorId = Shader.PropertyToID("_PaleColor");
        private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
        private static readonly int SafeZoneHeightId = Shader.PropertyToID("_SafeZoneHeight");
        private static readonly int SafeZoneStrengthId = Shader.PropertyToID("_SafeZoneStrength");
        private static readonly int SafeZoneFlipId = Shader.PropertyToID("_SafeZoneFlip");
        private static readonly int HistoryValidId = Shader.PropertyToID("_HistoryValid");
        private static readonly int HistoryTexId = Shader.PropertyToID("_GlitchHistoryTex");

        private readonly Material material;
        private RTHandle history;
        private bool historyValid;

        private class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle history;
        }

        public GlitchPass(Material material)
        {
            this.material = material;
            requiresIntermediateTexture = true;
        }

        public void InvalidateHistory() => historyValid = false;

        public void Dispose()
        {
            history?.Release();
            history = null;
            historyValid = false;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType != CameraType.Game || !cameraData.postProcessEnabled) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            var glitch = VolumeManager.instance.stack.GetComponent<GlitchVolume>();
            if (glitch == null || !glitch.IsActive()) return;

            // текстура прошлого кадра под размер экрана; при смене разрешения пересоздаётся
            var historyDesc = cameraData.cameraTargetDescriptor;
            historyDesc.depthBufferBits = 0;
            historyDesc.msaaSamples = 1;
            bool reallocated = RenderingUtils.ReAllocateHandleIfNeeded(ref history, historyDesc,
                FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_AfterGlowGlitchHistory");
            if (reallocated) historyValid = false;

            material.SetFloat(IntensityId, glitch.intensity.value);
            material.SetFloat(BlockCountId, glitch.blockCount.value);
            material.SetFloat(ColorSplitId, glitch.colorSplit.value);
            material.SetFloat(MaxShiftId, glitch.maxShift.value);
            material.SetFloat(SpeedId, glitch.speed.value);
            material.SetFloat(CorruptionId, glitch.corruption.value);
            material.SetFloat(MacroBlocksId, glitch.macroBlocks.value);
            material.SetFloat(ColorCrushId, glitch.colorCrush.value);
            material.SetFloat(DatamoshId, glitch.datamosh.value);
            material.SetFloat(DrownId, glitch.drown.value);
            // SetColor сам переводит цвет из sRGB в линейное пространство проекта
            material.SetColor(VoidColorId, glitch.voidColor.value);
            material.SetColor(BloodColorId, glitch.bloodColor.value);
            material.SetColor(BloodBrightColorId, glitch.bloodBrightColor.value);
            material.SetColor(NeonColorId, glitch.neonColor.value);
            material.SetColor(PaleColorId, glitch.paleColor.value);
            material.SetColor(AccentColorId, glitch.accentColor.value);
            material.SetFloat(SafeZoneHeightId, glitch.safeZoneHeight.value);
            material.SetFloat(SafeZoneStrengthId, glitch.safeZoneStrength.value);
            material.SetFloat(SafeZoneFlipId, glitch.flipSafeZone.value ? 1f : 0f);
            material.SetFloat(HistoryValidId, historyValid ? 1f : 0f);

            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle historyHandle = renderGraph.ImportTexture(history);

            TextureDesc desc = renderGraph.GetTextureDesc(source);
            desc.name = "_AfterGlowGlitchTarget";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("AfterGlow Glitch", out var data))
            {
                data.material = material;
                data.source = source;
                data.history = historyHandle;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(historyHandle, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    RTHandle historyRT = d.history;
                    d.material.SetTexture(HistoryTexId, historyRT);
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), d.material, 0);
                });
            }

            // запоминаем получившийся кадр — следующий кадр возьмёт из него застрявшие блоки
            renderGraph.AddBlitPass(destination, historyHandle, Vector2.one, Vector2.zero,
                passName: "AfterGlow Glitch History");
            historyValid = true;

            resourceData.cameraColor = destination;
        }
    }
}
