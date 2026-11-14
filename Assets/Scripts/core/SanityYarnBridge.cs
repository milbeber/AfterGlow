using UnityEngine;
using Yarn.Unity;

public class SanityYarnBridge : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;

    private void Awake()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        if (dialogueRunner != null)
        {
            // Регистрация кастомной функции для условий в Yarn: <<if sanity() <= 30>>
            dialogueRunner.AddFunction("sanity", () =>
                PlayerStats.Instance != null ? PlayerStats.Instance.CurrentSanity : 100f);
        }
    }

    // Кастомная команда: <<change_sanity -20>>
    [YarnCommand("change_sanity")]
    public static void ChangeSanity(float amount)
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.ModifySanity(amount);
    }

    // Кастомная команда: <<set_sanity 50>>
    [YarnCommand("set_sanity")]
    public static void SetSanity(float value)
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.SetSanity(value);
    }
}