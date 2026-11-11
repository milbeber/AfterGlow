using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Yarn.Unity;

public class DialogueExtensions : MonoBehaviour
{
    [Header("Yarn Spinner и Машинка")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private CustomTypewriter typewriter;
    [SerializeField] private TMP_Text speakerText;

    [Header("Параметры Пропуска (Skip)")]
    [Tooltip("Пауза между репликами при зажатом Skip (сек)")]
    [SerializeField] private float skipLineAutoAdvanceDelay = 0.15f;

    [Header("Бэклог (Backlog / История)")]
    [SerializeField] private GameObject backlogPanel;
    [SerializeField] private TextMeshProUGUI backlogText;
    [SerializeField] private ScrollRect backlogScrollRect;
    [SerializeField] private int maxHistoryLines = 50;

    private readonly List<string> dialogueHistory = new List<string>();
    private bool isSkipHeld;
    private float nextLineTimer;

    public bool IsBacklogOpen => backlogPanel != null && backlogPanel.activeSelf;

    private void Awake()
    {
        if (backlogPanel != null)
            backlogPanel.SetActive(false);

        if (typewriter == null)
            typewriter = FindFirstObjectByType<CustomTypewriter>();

        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
    }

    private void OnEnable()
    {
        if (typewriter != null)
            typewriter.LineStarted += OnTypewriterLineStarted;
    }

    private void OnDisable()
    {
        if (typewriter != null)
            typewriter.LineStarted -= OnTypewriterLineStarted;
    }

    private void Update()
    {
        // Передаем состояние удержания кнопки в машинку (при открытом бэклоге скип блокируется)
        if (typewriter != null)
            typewriter.SetSkipHeld(isSkipHeld && !IsBacklogOpen);

        // Логика авто-пропуска диалогов при удержании Skip
        HandleSkipAdvancement();
    }

    private void HandleSkipAdvancement()
    {
        if (!isSkipHeld || IsBacklogOpen || dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
            return;

        // Если машинка еще печатает — ждем завершения строки
        if (typewriter != null && typewriter.IsTyping)
        {
            nextLineTimer = 0f;
            return;
        }

        // Если реплика напечатана, выжидаем таймер и запрашиваем следующую
        nextLineTimer += Time.unscaledDeltaTime;
        if (nextLineTimer >= skipLineAutoAdvanceDelay)
        {
            nextLineTimer = 0f;
            AdvanceDialogue();
        }
    }

    private void AdvanceDialogue()
    {
        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.RequestNextLine();
        }
    }

    public void AddLineToHistory(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        string entry = string.IsNullOrEmpty(speaker)
            ? text.Trim()
            : $"<b><color=#E0BB68>{speaker}:</color></b> {text.Trim()}";

        dialogueHistory.Add(entry);

        while (dialogueHistory.Count > maxHistoryLines)
            dialogueHistory.RemoveAt(0);

        if (IsBacklogOpen)
            UpdateBacklogUI();
    }

    #region Input System Callbacks (Send Messages)

    public void OnSkip(InputValue value)
    {
        isSkipHeld = value.isPressed;
        if (!isSkipHeld)
            nextLineTimer = 0f;
    }

    public void OnScrollBacklog(InputValue value)
    {
        Vector2 scroll = value.Get<Vector2>();

        if (scroll.y > 0.05f && !IsBacklogOpen)
        {
            OpenBacklog();
        }
    }

    public void OnCancel(InputValue value)
    {
        if (value.isPressed && IsBacklogOpen)
        {
            CloseBacklog();
        }
    }

    #endregion

    #region Backlog UI

    public void OpenBacklog()
    {
        if (backlogPanel == null) return;

        UpdateBacklogUI();
        backlogPanel.SetActive(true);

        StartCoroutine(ScrollToBottomRoutine());
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

    private IEnumerator ScrollToBottomRoutine()
    {
        yield return null;
        if (backlogScrollRect != null)
        {
            backlogScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    #endregion

    private void OnTypewriterLineStarted(string text)
    {
        string speaker = speakerText != null ? speakerText.text : string.Empty;
        AddLineToHistory(speaker, text);
    }
}