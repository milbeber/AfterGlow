using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using Yarn.Markup;
using Yarn.Unity;

/// Кастомная печатная машинка, подключённая к Yarn Spinner как Custom Typewriter.
/// Теперь текст печатает ТОЛЬКО она (раньше параллельно работала встроенная
/// машинка Yarn — они дрались за maxVisibleCharacters, отсюда мерцание).
/// В Line Presenter: Typewriter Style = Custom, Custom Typewriter = этот компонент.
public class CustomTypewriter : MonoBehaviour, IAsyncTypewriter
{
    [Header("UI Ссылки")]
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private Button continueButton;

    [Header("Скорость печати")]
    [Tooltip("Задержка между символами (сек)")]
    [FormerlySerializedAs("typingSpeed")]
    [SerializeField] private float normalTypingSpeed = 0.035f;

    /// Вызывается, когда начинается новая реплика (используется бэклогом)
    public event Action<string> LineStarted;
    public event Action LineFinished;

    public bool IsTyping { get; private set; }

    // --- IAsyncTypewriter ---
    public List<IActionMarkupHandler> ActionMarkupHandlers { get; } = new List<IActionMarkupHandler>();

    public TMP_Text TextElement
    {
        get => textDisplay;
        set { if (value is TextMeshProUGUI ugui) textDisplay = ugui; }
    }

    private void Awake()
    {
        if (textDisplay == null) textDisplay = GetComponentInChildren<TextMeshProUGUI>();
        if (continueButton == null) continueButton = GetComponent<Button>();
    }

    // Yarn вызывает это перед показом реплики
    public void PrepareForContent(MarkupParseResult line)
    {
        if (textDisplay == null) return;

        textDisplay.maxVisibleCharacters = 0;
        textDisplay.text = line.Text;

        foreach (var handler in ActionMarkupHandlers)
            handler.OnPrepareForLine(line, textDisplay);
    }

    // Yarn вызывает это для печати. cancellationToken = "HurryUp" (скип):
    // как только он отменён — мгновенно показываем строку целиком.
    public async YarnTask RunTypewriter(MarkupParseResult line, CancellationToken cancellationToken)
    {
        if (textDisplay == null) return;

        IsTyping = true;
        if (continueButton != null) continueButton.interactable = false;

        textDisplay.maxVisibleCharacters = 0;
        textDisplay.text = line.Text;
        LineStarted?.Invoke(line.Text);

        foreach (var handler in ActionMarkupHandlers)
            handler.OnLineDisplayBegin(line, textDisplay);

        // Реальное число видимых символов (без rich-text тегов)
        int total = textDisplay.GetTextInfo(line.Text).characterCount;
        float delay = Mathf.Max(0f, normalTypingSpeed);
        float budget = delay; // первый символ появляется сразу

        for (int i = 0; i < total; i++)
        {
            while (!cancellationToken.IsCancellationRequested && budget < delay)
            {
                await YarnTask.Yield();
                budget += Time.unscaledDeltaTime;
            }

            if (cancellationToken.IsCancellationRequested) break; // скип

            foreach (var handler in ActionMarkupHandlers)
                await handler.OnCharacterWillAppear(i, line, cancellationToken).SuppressCancellationThrow();

            textDisplay.maxVisibleCharacters = i + 1; // абсолютное значение, без "+="
            budget -= delay;
        }

        // Скип или конец — показываем всю строку, и только её
        textDisplay.maxVisibleCharacters = total;

        foreach (var handler in ActionMarkupHandlers)
            handler.OnLineDisplayComplete();

        IsTyping = false;
        if (continueButton != null) continueButton.interactable = true;
        LineFinished?.Invoke();
    }

    public void ContentWillDismiss()
    {
        foreach (var handler in ActionMarkupHandlers)
            handler.OnLineWillDismiss();
    }

    public void ContentDidDismiss()
    {
        if (textDisplay != null) textDisplay.maxVisibleCharacters = 0;
    }
}
