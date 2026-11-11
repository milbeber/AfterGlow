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

    [Header("Бэклог (Backlog / История)")]
    [SerializeField] private GameObject backlogPanel;
    [SerializeField] private TextMeshProUGUI backlogText;
    [SerializeField] private ScrollRect backlogScrollRect;
    [SerializeField] private int maxHistoryLines = 50;
    [Tooltip("Цвет имени говорящего в истории")]
    [SerializeField] private Color speakerColor = new Color32(0xB2, 0x3A, 0x5A, 0xFF);

    /// Цвет имён в истории (BacklogStyler ставит светлый, под тёмное окно)
    public Color SpeakerColor { get => speakerColor; set => speakerColor = value; }
    [Tooltip("Защита от дребезга колеса: пауза после открытия/закрытия (сек)")]
    [SerializeField] private float toggleCooldown = 0.2f;

    private readonly List<string> dialogueHistory = new List<string>();
    private Yarn.Unity.LineAdvancerInput.KeyCodes[] yarnKeys;
    private float lastToggleTime = -10f;

    public bool IsBacklogOpen => backlogPanel != null && backlogPanel.activeSelf;

    private void Awake()
    {
        if (backlogPanel != null)
            backlogPanel.SetActive(false);

        if (typewriter == null)
            typewriter = FindFirstObjectByType<CustomTypewriter>();

        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        if (backlogScrollRect == null && backlogPanel != null)
            backlogScrollRect = backlogPanel.GetComponentInChildren<ScrollRect>(true);

        yarnKeys = FindObjectsByType<Yarn.Unity.LineAdvancerInput.KeyCodes>(FindObjectsSortMode.None);
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
        // Правая кнопка мыши — альтернативный способ закрыть бэклог
        if (IsBacklogOpen && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            CloseBacklog();
    }

    #region Input System Callbacks (Send Messages)

    // ВАЖНО: в режиме Send Messages PlayerInput НЕ присылает отпускание
    // Button-действий. Поэтому "зажатие" через value.isPressed отследить нельзя —
    // старый isSkipHeld навсегда оставался true, и диалог сам проматывался до развилки.
    // Теперь Skip — одиночное нажатие: мгновенно допечатать текущую строку, и всё.
    public void OnSkip(InputValue value)
    {
        if (!value.isPressed || IsBacklogOpen) return;
        if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning) return;

        dialogueRunner.RequestHurryUpLine();
    }

    public void OnScrollBacklog(InputValue value)
    {
        float y = value.Get<Vector2>().y;

        // PassThrough присылает и нулевые значения — игнорируем
        if (Mathf.Abs(y) < 0.05f || backlogPanel == null) return;
        if (Time.unscaledTime - lastToggleTime < toggleCooldown) return;

        if (y > 0f && !IsBacklogOpen)
        {
            OpenBacklog();
        }
        else if (y < 0f && IsBacklogOpen && IsScrolledToBottom())
        {
            // Колесо вниз закрывает окно, когда история долистана до конца
            CloseBacklog();
        }
    }

    #endregion

    #region History

    public void AddLineToHistory(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        string entry = string.IsNullOrEmpty(speaker)
            ? text.Trim()
            : $"<b><color=#{ColorUtility.ToHtmlStringRGB(speakerColor)}>{speaker}:</color></b> {text.Trim()}";

        dialogueHistory.Add(entry);

        while (dialogueHistory.Count > maxHistoryLines)
            dialogueHistory.RemoveAt(0);

        if (IsBacklogOpen)
            UpdateBacklogUI();
    }

    private void OnTypewriterLineStarted(string text)
    {
        // Если у реплики нет говорящего, Yarn прячет поле имени, но старое имя в нём остаётся
        string speaker = speakerText != null && speakerText.gameObject.activeInHierarchy
            ? speakerText.text
            : string.Empty;
        AddLineToHistory(speaker, text);
    }

    #endregion

    #region Backlog UI

    public void OpenBacklog()
    {
        if (backlogPanel == null) return;

        UpdateBacklogUI();
        backlogPanel.SetActive(true);
        SetYarnKeysEnabled(false); // Space/Esc не должны листать диалог под окном
        lastToggleTime = Time.unscaledTime;

        StartCoroutine(ScrollToBottomRoutine());
    }

    // Можно повесить на кнопку "Закрыть" в BacklogPanel
    public void CloseBacklog()
    {
        if (backlogPanel == null) return;

        backlogPanel.SetActive(false);
        SetYarnKeysEnabled(true);
        lastToggleTime = Time.unscaledTime;
    }

    private bool IsScrolledToBottom()
    {
        if (backlogScrollRect == null || backlogScrollRect.content == null) return true;

        RectTransform viewport = backlogScrollRect.viewport != null
            ? backlogScrollRect.viewport
            : (RectTransform)backlogScrollRect.transform;

        // Весь текст помещается — листать нечего
        if (backlogScrollRect.content.rect.height <= viewport.rect.height + 1f) return true;

        return backlogScrollRect.verticalNormalizedPosition <= 0.01f;
    }

    private void SetYarnKeysEnabled(bool enabled)
    {
        if (yarnKeys == null) return;
        foreach (var keys in yarnKeys)
            if (keys != null) keys.enabled = enabled;
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
            Canvas.ForceUpdateCanvases();
            backlogScrollRect.verticalNormalizedPosition = 0f;
            backlogScrollRect.velocity = Vector2.zero;
        }
    }

    #endregion
}
