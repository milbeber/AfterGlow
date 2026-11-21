using System;
using UnityEngine;
using UnityEngine.Rendering;

/// Оверрайд «AfterGlow/Glitch» для Volume-профиля.
/// Значения смешиваются URP так же, как у встроенных эффектов: при Volume_Horror.weight = 0
/// интенсивность равна 0 и полноэкранный проход вообще не выполняется.
[Serializable]
[VolumeComponentMenu("AfterGlow/Glitch")]
public sealed class GlitchVolume : VolumeComponent
{
    [Header("Общее")]
    [Tooltip("Сила сбоя: 0 — эффекта нет, 1 — максимум. Управляется весом Volume_Horror")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Сколько раз в секунду перестраивается рисунок сбоя")]
    public ClampedFloatParameter speed = new ClampedFloatParameter(10f, 0f, 30f);

    [Header("Полосы")]
    [Tooltip("Количество горизонтальных полос, на которые режется экран")]
    public ClampedFloatParameter blockCount = new ClampedFloatParameter(24f, 4f, 64f);

    [Tooltip("Расщепление каналов RGB (доля ширины экрана)")]
    public ClampedFloatParameter colorSplit = new ClampedFloatParameter(0.004f, 0f, 0.03f);

    [Tooltip("Максимальный сдвиг сорвавшейся полосы (доля ширины экрана)")]
    public ClampedFloatParameter maxShift = new ClampedFloatParameter(0.05f, 0f, 0.2f);

    [Header("Битое видео")]
    [Tooltip("Сколько макроблоков разваливается. Растёт нелинейно: при 1 и полной интенсивности — сплошное месиво")]
    public ClampedFloatParameter corruption = new ClampedFloatParameter(0.7f, 0f, 1f);

    [Tooltip("Число рядов макроблоков по высоте экрана (меньше — крупнее квадраты)")]
    public ClampedFloatParameter macroBlocks = new ClampedFloatParameter(12f, 4f, 40f);

    [Tooltip("Порча цвета: перестановка каналов, инверсия, постеризация, кислотная палитра")]
    public ClampedFloatParameter colorCrush = new ClampedFloatParameter(0.6f, 0f, 1f);

    [Tooltip("Доля блоков, которые застревают в прошлом кадре и ползут")]
    public ClampedFloatParameter datamosh = new ClampedFloatParameter(0.6f, 0f, 1f);

    [Tooltip("Насколько весь кадр тонет в палитре игры при сильном сбое")]
    public ClampedFloatParameter drown = new ClampedFloatParameter(0.35f, 0f, 1f);

    [Header("Палитра сбоя (из UIStyleKit)")]
    public ColorParameter voidColor = new ColorParameter(Hex(0x14, 0x05, 0x08), false, false, true);
    public ColorParameter bloodColor = new ColorParameter(Hex(0x8E, 0x0F, 0x1D), false, false, true);
    public ColorParameter bloodBrightColor = new ColorParameter(Hex(0xE2, 0x26, 0x3B), false, false, true);
    public ColorParameter neonColor = new ColorParameter(Hex(0xFF, 0x4F, 0xA3), false, false, true);
    public ColorParameter paleColor = new ColorParameter(Hex(0xFF, 0xE3, 0xF1), false, false, true);
    [Tooltip("Голубая тень сбоя")]
    public ColorParameter accentColor = new ColorParameter(Hex(0x5F, 0xE3, 0xF2), false, false, true);

    [Header("Читаемость текста")]
    [Tooltip("Высота защищённой зоны снизу экрана (окно диалога), доля высоты")]
    public ClampedFloatParameter safeZoneHeight = new ClampedFloatParameter(0.33f, 0f, 0.6f);

    [Tooltip("Насколько ослабляется сбой в защищённой зоне: 1 — там его нет совсем")]
    public ClampedFloatParameter safeZoneStrength = new ClampedFloatParameter(0.8f, 0f, 1f);

    [Tooltip("Включить, если защищённая зона оказалась сверху экрана, а не снизу")]
    public BoolParameter flipSafeZone = new BoolParameter(false);

    private static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    public bool IsActive() => active && intensity.value > 0.001f;
}
