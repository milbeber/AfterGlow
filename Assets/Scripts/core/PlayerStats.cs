using System;
using UnityEngine;
using UnityEngine.Events;

/// Рассудок героя (0–100) — синглтон, живёт между сценами.
/// Любое изменение проходит через SetSanity: значение жёстко зажимается в [min; max],
/// а при пересечении порогов автоматически рассылаются события (C# и UnityEvent в инспекторе).
/// Из Yarn управляется через <<set $sanity to ...>>, <<change_sanity -10>>, <<set_sanity 50>> (см. SanityYarnBridge).
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    public enum SanityState
    {
        Normal,   
        Warning,  
        Critical, 
        Depleted 
    }

    [Header("Настройки рассудка")]
    [SerializeField] private float minSanity = 0f;
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float startSanity = 100f;

    [Header("Критические пороги")]
    [SerializeField] private float warningThreshold = 60f;
    [SerializeField] private float criticalThreshold = 30f;

    [Header("Триггеры (можно подписать объекты прямо в инспекторе)")]
    public UnityEvent<float> onSanityChanged = new UnityEvent<float>();
    public UnityEvent onWarningReached = new UnityEvent();
    public UnityEvent onCriticalReached = new UnityEvent();
    public UnityEvent onDepleted = new UnityEvent();
    public UnityEvent onRecovered = new UnityEvent();

    // События для кода: HUD, эффекты (дни 14–15), GameManager
    public event Action<float, float> OnSanityChanged;          // (новое значение, изменение)
    public event Action<SanityState> OnSanityStateChanged;
    public event Action OnCriticalSanityReached;
    public event Action OnSanityDepleted;

    private float currentSanity;
    private SanityState currentState = SanityState.Normal;

    public float CurrentSanity => currentSanity;
    public float MinSanity => minSanity;
    public float MaxSanity => maxSanity;
    public float WarningThreshold => warningThreshold;
    public float CriticalThreshold => criticalThreshold;
    public float NormalizedSanity => Mathf.InverseLerp(minSanity, maxSanity, currentSanity);
    public SanityState CurrentState => currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (transform.parent != null) transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);

        currentSanity = Mathf.Clamp(startSanity, minSanity, maxSanity);
        currentState = Evaluate(currentSanity);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        // пороги не могут выйти за шкалу и перепутаться местами
        maxSanity = Mathf.Max(maxSanity, minSanity + 1f);
        startSanity = Mathf.Clamp(startSanity, minSanity, maxSanity);
        criticalThreshold = Mathf.Clamp(criticalThreshold, minSanity, maxSanity);
        warningThreshold = Mathf.Clamp(warningThreshold, criticalThreshold, maxSanity);
    }

  
    public void SetSanity(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            Debug.LogWarning($"[PlayerStats] Некорректное значение рассудка ({value}) — игнорирую");
            return;
        }

        float clamped = Mathf.Clamp(value, minSanity, maxSanity);
        if (!Mathf.Approximately(clamped, value))
            Debug.Log($"[PlayerStats] {value:0.#} вне границ [{minSanity:0}; {maxSanity:0}] → {clamped:0.#}");

        if (Mathf.Approximately(clamped, currentSanity)) return;

        float delta = clamped - currentSanity;
        currentSanity = clamped;

        Debug.Log($"[PlayerStats] Рассудок: {currentSanity:0.#} ({delta:+0.#;-0.#})");
        OnSanityChanged?.Invoke(currentSanity, delta);
        onSanityChanged.Invoke(currentSanity);

        UpdateState();
    }

    /// Изменить рассудок на delta (отрицательное — потеря, положительное — восстановление).
    public void ModifySanity(float delta) => SetSanity(currentSanity + delta);

    /// Сброс к стартовому значению (новая игра).
    public void ResetSanity() => SetSanity(startSanity);

    private SanityState Evaluate(float v)
    {
        if (v <= minSanity) return SanityState.Depleted;
        if (v <= criticalThreshold) return SanityState.Critical;
        if (v <= warningThreshold) return SanityState.Warning;
        return SanityState.Normal;
    }

    private void UpdateState()
    {
        var old = currentState;
        var now = Evaluate(currentSanity);
        if (now == old) return;

        currentState = now;
        Debug.Log($"[PlayerStats] Состояние психики: {old} → {now}");
        OnSanityStateChanged?.Invoke(now);

        if (now > old)
        {
            if (old < SanityState.Warning && now >= SanityState.Warning) onWarningReached.Invoke();
            if (old < SanityState.Critical && now >= SanityState.Critical)
            {
                OnCriticalSanityReached?.Invoke();
                onCriticalReached.Invoke();
            }
            if (now == SanityState.Depleted)
            {
                OnSanityDepleted?.Invoke();
                onDepleted.Invoke();
            }
        }
        else if (now == SanityState.Normal)
        {
            onRecovered.Invoke();
        }
    }
}
