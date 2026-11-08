using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Yarn.Unity;
using System.Reflection;

public class DialogueExtensions : MonoBehaviour
{
    [Header("Ссылки на компоненты Yarn Spinner")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Настройки бэклога (Backlog)")]
    [SerializeField] private GameObject backlogPanel;
    [SerializeField] private TextMeshProUGUI backlogText;
    [SerializeField] private int maxHistoryLines = 50;

    private List<string> dialogueHistory = new List<string>();

    private void Awake()
    {
        if (backlogPanel != null)
            backlogPanel.SetActive(false);
    }

    // Сохранение реплики в историю (Бэклог)
    public void AddLineToHistory(string speaker, string text)
    {
        string entry = string.IsNullOrEmpty(speaker) ? text : $"<b>{speaker}:</b> {text}";
        dialogueHistory.Add(entry);

        if (dialogueHistory.Count > maxHistoryLines)
            dialogueHistory.RemoveAt(0);

        UpdateBacklogUI();
    }

    // Пробел ТОЛЬКО мгновенно завершает анимацию печати текста (без переключения реплики)
    public void OnSkipText(InputValue value)
    {
        if (!value.isPressed || dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
            return;

        var linePresenter = UnityEngine.Object.FindFirstObjectByType<LinePresenter>();
        if (linePresenter != null)
        {
            // Явно указываем UnityEngine.Component, чтобы избежать путаницы с System
            foreach (UnityEngine.Component comp in linePresenter.GetComponentsInChildren<UnityEngine.Component>())
            {
                var type = comp.GetType();
                var method = type.GetMethod("Complete") ?? type.GetMethod("Skip") ?? type.GetMethod("ShowAll");
                if (method != null)
                {
                    method.Invoke(comp, null);
                    break;
                }
            }
        }
    }

    // Обработка колеса мыши для Бэклога
    public void OnScrollBacklog(InputValue value)
    {
        Vector2 scroll = value.Get<Vector2>();

        if (scroll.y > 0 && backlogPanel != null && !backlogPanel.activeSelf)
        {
            OpenBacklog();
        }
        else if (scroll.y < 0 && backlogPanel != null && backlogPanel.activeSelf)
        {
            CloseBacklog();
        }
    }

    public void OpenBacklog()
    {
        if (backlogPanel != null)
        {
            UpdateBacklogUI();
            backlogPanel.SetActive(true);
        }
    }

    public void CloseBacklog()
    {
        if (backlogPanel != null)
            backlogPanel.SetActive(false);
    }

    private void UpdateBacklogUI()
    {
        if (backlogText != null)
            backlogText.text = string.Join("\n\n", dialogueHistory);
    }
}