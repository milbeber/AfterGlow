using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        foreach (var sr in Object.FindObjectsByType<ScrollRect>(inc, none))
            if (sr.name == "BacklogPanel")
                UIStyleKit.GetOrAdd<BacklogStyler>(sr.gameObject);
    }
}
