using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// Менеджер эффектов (день 15). Связывает рассудок, выборы с таймером и хоррор-слой дня 14:
///  - чем ниже рассудок, тем сильнее Volume_Horror (через кривую: до порога тревоги почти ничего);
///  - каждая потеря рассудка даёт всплеск, сильная (20 и больше) — полный удар;
///  - на критическом уровне случаются короткие «припадки»;
///  - во время выбора с таймером сбой нарастает к концу и бьёт в такт сердцу;
///  - текст остаётся читаемым: интерфейс диалога выводится поверх пост-эффектов,
///    а буквы только слегка дрожат (SanityTextDistortion).
/// Вешается автоматически (AstraAutoStyle) в игровой сцене.
/// В редакторе для проверки: F7 — рассудок +20, F8 — рассудок -20.
[DisallowMultipleComponent]
public class HorrorEffectsManager : MonoBehaviour
{
    public static HorrorEffectsManager Instance { get; private set; }

    public enum ReadabilityMode
    {
        OverlayUI,     // окно диалога и выборы поверх пост-эффектов; глитч рвёт только фон
        ProtectedZone  // всё под эффектами, вес ограничен, текст спасает защищённая зона шейдера
    }

    [Header("Рассудок → хоррор")]
    [Tooltip("X: потеря рассудка (0 = рассудок 100, 1 = рассудок 0). Y: вес Volume_Horror")]
    [SerializeField] private AnimationCurve horrorByLoss = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.4f, 0.08f), new Keyframe(0.7f, 0.5f), new Keyframe(1f, 0.9f));
    [Tooltip("X: потеря рассудка. Y: сила дрожания текста")]
    [SerializeField] private AnimationCurve textByLoss = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.4f, 0f), new Keyframe(0.7f, 0.45f), new Keyframe(1f, 1f));

    [Header("Удары по рассудку")]
    [Tooltip("Потеря, начиная с которой всплеск максимальный")]
    [SerializeField] private float strongDrop = 20f;
    [SerializeField, Range(0f, 1f)] private float weakPulse = 0.35f;
    [SerializeField, Range(0f, 1f)] private float strongPulse = 1f;
    [SerializeField] private float pulseDuration = 0.45f;

    [Header("Припадки на критическом уровне")]
    [SerializeField] private bool seizures = true;
    [SerializeField] private Vector2 seizureInterval = new Vector2(4f, 9f);
    [SerializeField, Range(0f, 1f)] private float seizureStrength = 0.75f;

    [Header("Выбор с таймером")]
    [Tooltip("Добавка к хоррору в самом конце таймера")]
    [SerializeField, Range(0f, 1f)] private float timerPressure = 0.3f;
    [Tooltip("За сколько секунд до конца начинаются удары сердца")]
    [SerializeField] private float heartbeatFrom = 3f;
    [SerializeField, Range(0f, 1f)] private float heartbeatPulse = 0.45f;
    [SerializeField, Range(0f, 1f)] private float expirePulse = 1f;

    [Header("Читаемость текста")]
    [SerializeField] private ReadabilityMode readability = ReadabilityMode.OverlayUI;
    [Tooltip("Потолок веса в режиме ProtectedZone")]
    [SerializeField, Range(0f, 1f)] private float protectedZoneCap = 0.6f;
    [SerializeField] private bool distortText = true;
    [SerializeField, Range(0f, 1f)] private float maxTextDistortion = 1f;

    /// Объекты фона, которые остаются под пост-эффектами в режиме OverlayUI
    private static readonly string[] SceneLayers = { "BG_Art", "BG_Vignette", "BG_Scanlines", "Sanity_Vignette" };

    private PlayerStats stats;
    private TimedChoice timer;
    private float nextSeizure;
    private float lastBeat;

    public float CurrentLevel { get; private set; }
    public ReadabilityMode Readability => readability;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SanityTextDistortion.GlobalIntensity = 0f;
        Unsubscribe();
    }

    private IEnumerator Start()
    {
        stats = PlayerStats.Instance;
        if (stats != null) stats.OnSanityChanged += HandleSanityChanged;
        else Debug.LogWarning("[HorrorEffectsManager] PlayerStats не найден, хоррор от рассудка не работает");

        timer = TimedChoice.Instance;
        if (timer != null)
        {
            timer.OnTimerStarted += HandleTimerStarted;
            timer.OnExpired += HandleTimerExpired;
        }

        if (HorrorVolumeController.Instance == null)
            Debug.LogWarning("[HorrorEffectsManager] HorrorVolumeController не найден (Volume_Horror). Запустите AfterGlow → День 14");

        // при входе в сцену сразу нужный уровень, без «наплыва» с нуля
        CurrentLevel = TargetLevel();
        HorrorVolumeController.Instance?.SetHorrorImmediate(Cap(CurrentLevel));
        ScheduleSeizure();

        // ждём, пока стайлеры и HUD достроят интерфейс, потом разводим слои
        yield return null;
        yield return null;
        if (readability == ReadabilityMode.OverlayUI) LiftDialogueUI();
    }

    private void Unsubscribe()
    {
        if (stats != null) stats.OnSanityChanged -= HandleSanityChanged;
        if (timer != null)
        {
            timer.OnTimerStarted -= HandleTimerStarted;
            timer.OnExpired -= HandleTimerExpired;
        }
    }

    // ---------------- Каждый кадр ----------------

    private void Update()
    {
        CurrentLevel = TargetLevel();
        var horror = HorrorVolumeController.Instance;
        if (horror != null) horror.SetHorror(Cap(CurrentLevel));

        float loss = Loss();
        float text = distortText ? textByLoss.Evaluate(loss) + TimerFactor() * 0.35f : 0f;
        SanityTextDistortion.GlobalIntensity = Mathf.Clamp(text, 0f, maxTextDistortion);

        if (seizures && stats != null && stats.CurrentState >= PlayerStats.SanityState.Critical && Time.unscaledTime >= nextSeizure)
        {
            Pulse(seizureStrength * Random.Range(0.6f, 1f), Random.Range(0.2f, 0.5f));
            ScheduleSeizure();
        }

        Heartbeat();

#if UNITY_EDITOR
        var keyboard = Keyboard.current;
        if (keyboard != null && stats != null)
        {
            if (keyboard.f7Key.wasPressedThisFrame) stats.ModifySanity(20f);
            if (keyboard.f8Key.wasPressedThisFrame) stats.ModifySanity(-20f);
        }
#endif
    }

    private float Loss() => stats != null ? 1f - stats.NormalizedSanity : 0f;

    /// 0 — таймера нет или он только начался, 1 — время на исходе
    private float TimerFactor()
    {
        if (timer == null || !timer.IsRunning) return 0f;
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0f, timer.Remaining01));
    }

    private float TargetLevel()
    {
        float level = horrorByLoss.Evaluate(Loss()) + timerPressure * TimerFactor();
        return Mathf.Clamp01(level);
    }

    private float Cap(float level) =>
        readability == ReadabilityMode.ProtectedZone ? Mathf.Min(level, protectedZoneCap) : level;

    private void Heartbeat()
    {
        if (timer == null || !timer.IsRunning || timer.Remaining > heartbeatFrom) return;

        // удары чаще к концу: раз в секунду → два раза в секунду
        float interval = Mathf.Lerp(0.5f, 1f, timer.Remaining / Mathf.Max(0.01f, heartbeatFrom));
        if (Time.unscaledTime - lastBeat < interval) return;
        lastBeat = Time.unscaledTime;
        Pulse(heartbeatPulse, 0.22f);
    }

    private void ScheduleSeizure() =>
        nextSeizure = Time.unscaledTime + Random.Range(seizureInterval.x, seizureInterval.y);

    // ---------------- События ----------------

    private void HandleSanityChanged(float value, float delta)
    {
        if (delta >= 0f) return; // восстановление: уровень сам плавно опустится

        float k = Mathf.Clamp01(-delta / Mathf.Max(1f, strongDrop));
        Pulse(Mathf.Lerp(weakPulse, strongPulse, k), pulseDuration * Mathf.Lerp(0.6f, 1f, k));
    }

    private void HandleTimerStarted(float duration) => lastBeat = Time.unscaledTime;

    private void HandleTimerExpired() => Pulse(expirePulse, 0.6f);

    /// Всплеск с учётом режима читаемости
    public void Pulse(float strength, float duration)
    {
        HorrorVolumeController.Instance?.Pulse(Cap(strength), duration);
    }

    // ---------------- Читаемость ----------------

    /// Canvas диалога уходит в Screen Space – Overlay (пост-эффекты его не трогают),
    /// а фон переезжает в отдельный Canvas на камере и продолжает ломаться.
    private void LiftDialogueUI()
    {
        var typewriter = FindFirstObjectByType<CustomTypewriter>(FindObjectsInactive.Include);
        var canvas = typewriter != null ? typewriter.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null) return;
        canvas = canvas.rootCanvas;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return;

        var go = new GameObject("AfterGlow_SceneCanvas", typeof(RectTransform));
        go.layer = canvas.gameObject.layer;
        var sceneCanvas = go.AddComponent<Canvas>();
        sceneCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        sceneCanvas.worldCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        sceneCanvas.planeDistance = canvas.planeDistance;
        sceneCanvas.sortingLayerID = canvas.sortingLayerID;
        sceneCanvas.sortingOrder = canvas.sortingOrder - 1;

        var srcScaler = canvas.GetComponent<CanvasScaler>();
        var scaler = go.AddComponent<CanvasScaler>();
        if (srcScaler != null)
        {
            scaler.uiScaleMode = srcScaler.uiScaleMode;
            scaler.referenceResolution = srcScaler.referenceResolution;
            scaler.screenMatchMode = srcScaler.screenMatchMode;
            scaler.matchWidthOrHeight = srcScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = srcScaler.referencePixelsPerUnit;
        }

        int moved = 0;
        foreach (var name in SceneLayers)
        {
            var layer = canvas.transform.Find(name);
            if (layer == null) continue;
            layer.SetParent(go.transform, false);
            moved++;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Debug.Log($"[HorrorEffectsManager] Интерфейс диалога поверх пост-эффектов, слоёв фона под эффектами: {moved}");
    }
}
