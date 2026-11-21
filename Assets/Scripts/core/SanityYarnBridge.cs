using System;
using UnityEngine;
using Yarn.Unity;

/// Связывает рассудок (PlayerStats) со скриптами Yarn Spinner в обе стороны.
/// Вешается автоматически (AstraAutoStyle) на объект с DialogueRunner.
///
/// В .yarn доступно:
///   <<declare $sanity = 100>>               — объявить один раз (уже есть в StartStory.yarn)
///   <<set $sanity to $sanity - 15>>         — обычная переменная, зажимается в [0; 100]
///   <<change_sanity -15>>                    — команда: изменить на величину
///   <<set_sanity 50>>                        — команда: установить значение
///   <<if sanity() <= 30>> ... <<endif>>      — функция: текущее значение
///   <<if sanity_state() == "Critical">>      — функция: Normal / Warning / Critical / Depleted
public class SanityYarnBridge : MonoBehaviour
{
    public const string Variable = "$sanity";

    [SerializeField] private DialogueRunner dialogueRunner;

    private VariableStorageBehaviour storage;
    private PlayerStats stats;
    private IDisposable listener;
    private bool pushing; // мы сами пишем в Yarn — не отражать обратно

    private void Awake()
    {
        if (dialogueRunner == null) dialogueRunner = GetComponent<DialogueRunner>();
        if (dialogueRunner == null) dialogueRunner = FindFirstObjectByType<DialogueRunner>();
    }

    private void Start()
    {
        stats = PlayerStats.Instance;
        if (dialogueRunner == null || stats == null)
        {
            Debug.LogWarning("[SanityYarnBridge] Нет DialogueRunner или PlayerStats — связь с Yarn не установлена");
            enabled = false;
            return;
        }

        storage = dialogueRunner.VariableStorage;

        // Yarn → C#: ловим любое <<set $sanity to ...>>
        listener = storage.AddChangeListener(OnYarnVariableChanged);

        // C# → Yarn: изменения из кода (предметы, события) видны в скриптах
        stats.OnSanityChanged += OnStatsChanged;

        Push(stats.CurrentSanity); // стартовая синхронизация
    }

    private void OnDestroy()
    {
        listener?.Dispose();
        if (stats != null) stats.OnSanityChanged -= OnStatsChanged;
    }

    private void OnYarnVariableChanged(string name, object value)
    {
        if (pushing || name != Variable || stats == null) return;
        if (!(value is float f)) return;

        stats.SetSanity(f); 

        if (!Mathf.Approximately(f, stats.CurrentSanity)) Push(stats.CurrentSanity);
    }

    private void OnStatsChanged(float value, float delta) => Push(value);

    private void Push(float value)
    {
        if (storage == null) return;
        if (storage.TryGetValue<float>(Variable, out var current) && Mathf.Approximately(current, value)) return;

        pushing = true;
        try { storage.SetValue(Variable, value); }
        finally { pushing = false; }
    }

    // ---------------- Команды и функции для Yarn ----------------

    [YarnCommand("change_sanity")]
    public static void ChangeSanity(float amount)
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.ModifySanity(amount);
        else Debug.LogWarning("[SanityYarnBridge] change_sanity: PlayerStats не найден");
    }

    [YarnCommand("set_sanity")]
    public static void SetSanity(float value)
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.SetSanity(value);
        else Debug.LogWarning("[SanityYarnBridge] set_sanity: PlayerStats не найден");
    }

    [YarnFunction("sanity")]
    public static float GetSanity()
        => PlayerStats.Instance != null ? PlayerStats.Instance.CurrentSanity : 100f;

    [YarnFunction("sanity_state")]
    public static string GetSanityState()
        => PlayerStats.Instance != null ? PlayerStats.Instance.CurrentState.ToString() : "Normal";
}
