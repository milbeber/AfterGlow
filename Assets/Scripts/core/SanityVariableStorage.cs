using UnityEngine;
using Yarn.Unity;

public class SanityVariableStorage : InMemoryVariableStorage
{
    private const string SanityVar = "$sanity";
    private bool isSyncingFromStats = false;

    private void OnEnable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnSanityChanged += HandleStatsChanged;
    }

    private void Start()
    {
        // На случай если PlayerStats инициализировался позже OnEnable
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnSanityChanged -= HandleStatsChanged;
            PlayerStats.Instance.OnSanityChanged += HandleStatsChanged;
            SyncFromPlayerStats(PlayerStats.Instance.CurrentSanity);
        }
    }

    private void OnDisable()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnSanityChanged -= HandleStatsChanged;
    }

    // Перехватываем <<set $sanity to ...>> из Yarn-скрипта
    public override void SetValue(string variableName, float floatValue)
    {
        if (variableName == SanityVar && !isSyncingFromStats)
        {
            float clamped = Mathf.Clamp(floatValue, 0f, 100f);
            base.SetValue(variableName, clamped);

            if (PlayerStats.Instance != null)
                PlayerStats.Instance.SetSanity(clamped);

            return;
        }

        base.SetValue(variableName, floatValue);
    }

    // Если рассудок изменили из C# кода (не из Yarn), обновляем $sanity в Yarn
    private void HandleStatsChanged(float newSanity, float delta)
    {
        SyncFromPlayerStats(newSanity);
    }

    private void SyncFromPlayerStats(float value)
    {
        isSyncingFromStats = true;
        base.SetValue(SanityVar, Mathf.Clamp(value, 0f, 100f));
        isSyncingFromStats = false;
    }
}