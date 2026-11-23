using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity;

/// Выбор с таймером (день 15). В Yarn перед вариантами пишется <<timed_choice 8>>,
/// и следующий набор вариантов ждёт ответа не дольше 8 секунд.
/// Когда время вышло, выделение пробегает по вариантам («рулетка») и останавливается на случайном.
/// Если выключить Random On Timeout, выбирается вариант с тегом #timeout, а без тега — последний.
///
///   <<timed_choice 8>>
///   -> Открыть дверь
///   -> Замереть и не дышать
///
/// Таймер стоит на паузе, пока открыта история реплик или игра на паузе (Time.timeScale = 0).
/// Вешается автоматически (AstraAutoStyle) на объект с DialogueRunner.
public class TimedChoice : MonoBehaviour
{
    public const string TimeoutTag = "timeout";

    public static TimedChoice Instance { get; private set; }

    [Tooltip("Сколько секунд даётся, если в команде указано 0 или меньше")]
    [SerializeField] private float defaultSeconds = 8f;
    [Tooltip("Таймер не короче этого (чтобы успеть прочитать варианты)")]
    [SerializeField] private float minSeconds = 3f;

    [Header("Когда время вышло")]
    [Tooltip("Выбрать случайный вариант. Выключено: вариант с тегом #timeout или последний")]
    [SerializeField] private bool randomOnTimeout = true;
    [Tooltip("Сколько секунд выделение бегает по вариантам перед случайным выбором (0 — сразу)")]
    [SerializeField] private float rouletteTime = 0.9f;

    /// Время пошло (длительность)
    public event Action<float> OnTimerStarted;
    /// Игрок успел выбрать сам
    public event Action OnAnswered;
    /// Время вышло, вариант выбран за игрока
    public event Action OnExpired;

    public bool IsRunning { get; private set; }
    /// Время вышло, выделение бегает по вариантам
    public bool IsRolling { get; private set; }
    public float Duration { get; private set; }
    public float Remaining { get; private set; }
    public float Remaining01 => Duration > 0f ? Mathf.Clamp01(Remaining / Duration) : 0f;

    private static float pendingSeconds = -1f;

    private OptionsPresenter presenter;
    private CanvasGroup group;
    private DialogueExtensions extensions;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        presenter = FindFirstObjectByType<OptionsPresenter>(FindObjectsInactive.Include);
        if (presenter != null)
        {
            group = presenter.GetComponent<CanvasGroup>();
            if (group == null) group = presenter.GetComponentInChildren<CanvasGroup>(true);
        }
        extensions = FindFirstObjectByType<DialogueExtensions>(FindObjectsInactive.Include);
        if (presenter == null) Debug.LogWarning("[TimedChoice] OptionsPresenter не найден, выборы с таймером не работают");
    }

    // ---------------- Команда для Yarn ----------------

    [YarnCommand("timed_choice")]
    public static void Arm(float seconds)
    {
        pendingSeconds = seconds;
    }

    /// Взвести таймер из кода (например, для скримера)
    public void ArmNext(float seconds) => Arm(seconds);

    // ---------------- Работа таймера ----------------

    private bool OptionsShown =>
        presenter != null && presenter.isActiveAndEnabled &&
        group != null && group.interactable && group.alpha > 0.99f;

    private void Update()
    {
        bool shown = OptionsShown;

        if (!IsRunning)
        {
            if (shown && pendingSeconds >= 0f) StartTimer();
            return;
        }

        if (!shown)
        {
            // варианты исчезли раньше, чем вышло время: игрок выбрал сам
            IsRunning = false;
            OnAnswered?.Invoke();
            return;
        }

        bool paused = extensions != null && extensions.IsBacklogOpen;
        if (!paused) Remaining -= Time.deltaTime;
        if (Remaining <= 0f) Expire();
    }

    private void StartTimer()
    {
        float s = pendingSeconds > 0f ? pendingSeconds : defaultSeconds;
        pendingSeconds = -1f;
        Duration = Remaining = Mathf.Max(minSeconds, s);
        IsRunning = true;
        Debug.Log($"[TimedChoice] На выбор {Duration:0.#} с");
        OnTimerStarted?.Invoke(Duration);
    }

    private void Expire()
    {
        Remaining = 0f;
        IsRunning = false;
        OnExpired?.Invoke();

        var available = Available();
        if (available.Count == 0) return;

        if (randomOnTimeout)
        {
            var pick = available[UnityEngine.Random.Range(0, available.Count)];
            Debug.Log($"[TimedChoice] Время вышло, случайный вариант: {LabelOf(pick)}");
            StartCoroutine(Roulette(available, pick));
        }
        else
        {
            var pick = PickTagged(available);
            Debug.Log($"[TimedChoice] Время вышло, выбран вариант: {LabelOf(pick)}");
            Choose(pick);
        }
    }

    /// Выделение бегает по вариантам и замедляется, потом выбор фиксируется
    private IEnumerator Roulette(List<OptionItem> items, OptionItem pick)
    {
        IsRolling = true;
        if (group != null) group.blocksRaycasts = false; // время вышло: мышью уже не выбрать
        if (rouletteTime > 0f && items.Count > 1)
        {
            int index = UnityEngine.Random.Range(0, items.Count);
            float elapsed = 0f;
            while (elapsed < rouletteTime)
            {
                if (!OptionsShown) { IsRolling = false; yield break; } // игрок успел нажать сам
                index = (index + 1) % items.Count;
                Highlight(items[index]);
                float step = Mathf.Lerp(0.05f, 0.18f, elapsed / rouletteTime);
                elapsed += step;
                yield return new WaitForSecondsRealtime(step);
            }
            Highlight(pick);
            yield return new WaitForSecondsRealtime(0.25f);
        }
        IsRolling = false;
        if (OptionsShown) Choose(pick);
    }

    private static void Highlight(OptionItem item)
    {
        if (item == null) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(item.gameObject);
        else item.Select();
    }

    private static void Choose(OptionItem item)
    {
        if (item == null || !item.isActiveAndEnabled) return;
        Highlight(item);
        item.InvokeOptionSelected();
    }

    private static string LabelOf(OptionItem item)
    {
        if (item == null) return "нет";
        try { return item.Option.Line.TextWithoutCharacterName.Text; }
        catch (NullReferenceException) { return item.name; }
    }

    /// Варианты, которые сейчас можно выбрать
    private List<OptionItem> Available()
    {
        var list = new List<OptionItem>();
        if (presenter == null) return list;
        foreach (var item in presenter.GetComponentsInChildren<OptionItem>(false))
            if (item.isActiveAndEnabled && item.interactable) list.Add(item);
        return list;
    }

    /// Вариант с тегом #timeout, иначе последний доступный
    private OptionItem PickTagged(List<OptionItem> items)
    {
        OptionItem last = null;
        foreach (var item in items)
        {
            last = item;
            try
            {
                var tags = item.Option.Line.Metadata;
                if (tags == null) continue;
                foreach (var tag in tags)
                    if (string.Equals(tag.TrimStart('#'), TimeoutTag, StringComparison.OrdinalIgnoreCase))
                        return item;
            }
            catch (NullReferenceException) { }
        }
        return last;
    }
}
