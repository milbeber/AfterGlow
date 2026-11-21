using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// Точка управления хоррор-слоем из кода (день 14, основа для менеджера эффектов дня 15).
/// Меняет только Volume.weight у Volume_Horror: аберрация, глитч, искажение линзы и виньетка
/// из HorrorProfile нарастают вместе. Сам профиль не трогается, поэтому ассет не портится в Play Mode.
///
/// API:  SetHorror(0..1)        — плавно прийти к уровню
///       SetHorrorImmediate()   — выставить сразу
///       Pulse(сила, длительность) — короткий всплеск (скример, удар по рассудку)
/// В редакторе для проверки: F9 — переключить 0/1, F10 — всплеск.
[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public class HorrorVolumeController : MonoBehaviour
{
    public static HorrorVolumeController Instance { get; private set; }

    [Header("Уровень хоррора")]
    [Tooltip("Целевой вес Volume_Horror. Можно двигать в инспекторе во время игры")]
    [SerializeField, Range(0f, 1f)] private float targetLevel = 0f;
    [Tooltip("Скорость изменения веса (единиц в секунду)")]
    [SerializeField, Min(0.01f)] private float fadeSpeed = 1.5f;
    [Tooltip("Потолок веса — чтобы эффекты никогда не мешали читать текст")]
    [SerializeField, Range(0f, 1f)] private float maxWeight = 1f;

    private Volume horrorVolume;
    private float currentLevel;
    private float pulseStrength;
    private float pulseDuration;
    private float pulseTime;

    public float TargetLevel => targetLevel;
    public float CurrentWeight => horrorVolume != null ? horrorVolume.weight : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[HorrorVolumeController] В сцене уже есть контроллер, этот отключён", this);
            enabled = false;
            return;
        }
        Instance = this;

        horrorVolume = GetComponent<Volume>();
        currentLevel = targetLevel;
        Apply();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetHorror(float level)
    {
        targetLevel = Mathf.Clamp01(level);
    }

    public void SetHorrorImmediate(float level)
    {
        targetLevel = Mathf.Clamp01(level);
        currentLevel = targetLevel;
        Apply();
    }

    public void Pulse(float strength = 1f, float duration = 0.4f)
    {
        pulseStrength = Mathf.Clamp01(strength);
        pulseDuration = Mathf.Max(0.05f, duration);
        pulseTime = 0f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        currentLevel = Mathf.MoveTowards(currentLevel, targetLevel, fadeSpeed * dt);

        if (pulseStrength > 0f)
        {
            pulseTime += dt;
            if (pulseTime >= pulseDuration) pulseStrength = 0f;
        }

#if UNITY_EDITOR
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f9Key.wasPressedThisFrame) SetHorror(targetLevel > 0.5f ? 0f : 1f);
            if (keyboard.f10Key.wasPressedThisFrame) Pulse();
        }
#endif

        Apply();
    }

    private void Apply()
    {
        if (horrorVolume == null) return;

        float pulse = 0f;
        if (pulseStrength > 0f)
        {
            // быстрый удар и затухание
            float k = 1f - pulseTime / pulseDuration;
            pulse = pulseStrength * k * k;
        }

        horrorVolume.weight = Mathf.Clamp01(Mathf.Max(currentLevel, pulse)) * maxWeight;
    }
}
