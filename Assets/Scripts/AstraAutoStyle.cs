using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;

/// Подключает стайлеры автоматически при загрузке любой сцены —
/// вешать компоненты руками не нужно. Если компонент уже висит (с настройками), он не дублируется.
public static class AstraAutoStyle
{
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (!subscribed)
        {
            SceneManager.sceneLoaded += (scene, mode) => Apply();
            subscribed = true;
        }
        Apply(); // первая сцена
    }

    private static void Apply()
    {
        const FindObjectsInactive inc = FindObjectsInactive.Include;
        const FindObjectsSortMode none = FindObjectsSortMode.None;

        foreach (var ui in Object.FindObjectsByType<AuthUI>(inc, none))
            UIStyleKit.GetOrAdd<AuthStyler>(ui.gameObject);

        foreach (var ui in Object.FindObjectsByType<MainMenuUI>(inc, none))
            UIStyleKit.GetOrAdd<MainMenuStyler>(ui.gameObject);

        foreach (var tw in Object.FindObjectsByType<CustomTypewriter>(inc, none))
            UIStyleKit.GetOrAdd<DialogueStyler>(tw.gameObject);

        // День 13: рассудок — только там, где идёт диалог (игровая сцена)
        var runners = Object.FindObjectsByType<DialogueRunner>(inc, none);
        if (runners.Length > 0)
        {
            if (PlayerStats.Instance == null && Object.FindFirstObjectByType<PlayerStats>(inc) == null)
                new GameObject("PlayerStats").AddComponent<PlayerStats>();

            foreach (var runner in runners)
                UIStyleKit.GetOrAdd<SanityYarnBridge>(runner.gameObject);

            // День 15: выборы с таймером и менеджер хоррор-эффектов
            if (Object.FindFirstObjectByType<TimedChoice>(inc) == null)
                runners[0].gameObject.AddComponent<TimedChoice>();
            if (Object.FindFirstObjectByType<HorrorEffectsManager>(inc) == null)
                runners[0].gameObject.AddComponent<HorrorEffectsManager>();

            foreach (var tw in Object.FindObjectsByType<CustomTypewriter>(inc, none))
            {
                var canvas = tw.GetComponentInParent<Canvas>(true);
                if (canvas != null) UIStyleKit.GetOrAdd<SanityHUD>(canvas.rootCanvas.gameObject);
            }
        }

        foreach (var sr in Object.FindObjectsByType<ScrollRect>(inc, none))
            if (sr.name == "BacklogPanel")
                UIStyleKit.GetOrAdd<BacklogStyler>(sr.gameObject);
    }
}
