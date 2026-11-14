using System;
using Debug = UnityEngine.Debug;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Настройки рассудка")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float minSanity = 0f;
    [SerializeField] private float startSanity = 100f;

    [Header("Критические пороги")]
    [SerializeField] private float warningThreshold = 60f;
    [SerializeField] private float criticalThreshold = 30f;

    private float currentSanity;
    private SanityState currentState = SanityState.Normal;

    public float CurrentSanity => currentSanity;
    public float NormalizedSanity => currentSanity / maxSanity;
    public SanityState CurrentState => currentState;

    // События для систем эффектов (Дни 14-15), UI и GameManager
    public event Action<float, float> OnSanityChanged; // (новое значение, изменение)
    public event Action<SanityState> OnSanityStateChanged;
    public event Action OnCriticalSanityReached;
    public event Action OnSanityDepleted;

    public enum SanityState
    {
        Normal,
        Warning,
        Critical,
        Depleted
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetSanity(startSanity, silent: true);
    }

    /// <summary>
    /// Устанавливает рассудок с жесткой валидацией [0; 100].
    /// </summary>
    public void SetSanity(float value, bool silent = false)
    {
        float clamped = Mathf.Clamp(value, minSanity, maxSanity);
        float delta = clamped - currentSanity;

        // Если значение реально не изменилось (например, уже 0 и пытаемся уйти в минус)
        if (Mathf.Approximately(clamped, currentSanity) && !silent)
            return;

        currentSanity = clamped;

        if (!silent)
        {
            Debug.Log($"[PlayerStats] Рассудок изменен: {currentSanity:F1} (delta: {delta:+0.0;-0.0;0})");
            OnSanityChanged?.Invoke(currentSanity, delta);
        }

        UpdateSanityState(silent);
    }

    /// <summary>
    /// Изменяет рассудок на указанную величину (отрицательную или положительную).
    /// </summary>
    public void ModifySanity(float delta)
    {
        SetSanity(currentSanity + delta);
    }

    private void UpdateSanityState(bool silent)
    {
        SanityState newState;
        if (currentSanity <= minSanity)
            newState = SanityState.Depleted;
        else if (currentSanity <= criticalThreshold)
            newState = SanityState.Critical;
        else if (currentSanity <= warningThreshold)
            newState = SanityState.Warning;
        else
            newState = SanityState.Normal;

        if (newState == currentState) return;

        currentState = newState;

        if (silent) return;

        Debug.Log($"[PlayerStats] Состояние психики: {currentState}");
        OnSanityStateChanged?.Invoke(currentState);

        if (currentState == SanityState.Critical)
            OnCriticalSanityReached?.Invoke();
        else if (currentState == SanityState.Depleted)
            OnSanityDepleted?.Invoke();
    }
}