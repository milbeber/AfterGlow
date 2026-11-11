using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

public class CustomTypewriter : MonoBehaviour
{
    public enum SkipMode
    {
        Accelerate, // Сверхбыстрая печать
        Instant     // Моментальное появление
    }

    [Header("UI Ссылки")]
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private Button continueButton;

    [Header("Скорость печати")]
    [Tooltip("Задержка между символами при обычной печати (сек)")]
    [FormerlySerializedAs("typingSpeed")]
    [SerializeField] private float normalTypingSpeed = 0.035f;
    [Tooltip("Задержка между символами при ускорении (сек)")]
    [SerializeField] private float acceleratedTypingSpeed = 0.002f;
    [SerializeField] private SkipMode skipMode = SkipMode.Instant;

    public event Action<string> LineStarted;
    public event Action LineFinished;

    private string lastProcessedText = "";
    private string currentFullText = "";
    private bool isTyping;
    private bool isSkipHeld;
    private Coroutine typingCoroutine;

    public bool IsTyping => isTyping;
    public SkipMode CurrentSkipMode => skipMode;

    private void Awake()
    {
        if (textDisplay == null)
            textDisplay = GetComponent<TextMeshProUGUI>();

        if (continueButton == null)
            continueButton = GetComponent<Button>();
    }

    private void Update()
    {
        if (textDisplay == null) return;

        // Отслеживаем поступление нового текста от системы диалогов
        if (!isTyping && !string.IsNullOrEmpty(textDisplay.text) && textDisplay.text != lastProcessedText)
        {
            StartTypingLine(textDisplay.text);
        }
    }

    public void SetSkipHeld(bool held)
    {
        isSkipHeld = held;

        // Если режим моментальный и Skip зажат во время печати — мгновенно завершаем
        if (isSkipHeld && isTyping && skipMode == SkipMode.Instant)
        {
            CompleteLineInstantly();
        }
    }

    public void StartTypingLine(string fullText)
    {
        currentFullText = fullText;
        lastProcessedText = currentFullText;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeTextRoutine());
    }

    public void CompleteLineInstantly()
    {
        if (!isTyping) return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        textDisplay.maxVisibleCharacters = textDisplay.textInfo.characterCount;
        FinishTyping();
    }

    private IEnumerator TypeTextRoutine()
    {
        isTyping = true;
        if (continueButton != null)
            continueButton.interactable = false;

        LineStarted?.Invoke(currentFullText);

        // Устанавливаем полный текст и обновляем геометрию TMP, чтобы узнать реальное число символов (без тегов)
        textDisplay.text = currentFullText;
        textDisplay.maxVisibleCharacters = 0;
        textDisplay.ForceMeshUpdate();

        int totalVisibleCharacters = textDisplay.textInfo.characterCount;

        if (isSkipHeld && skipMode == SkipMode.Instant)
        {
            textDisplay.maxVisibleCharacters = totalVisibleCharacters;
            FinishTyping();
            yield break;
        }

        int currentVisible = 0;
        while (currentVisible < totalVisibleCharacters)
        {
            if (isSkipHeld)
            {
                if (skipMode == SkipMode.Instant)
                {
                    textDisplay.maxVisibleCharacters = totalVisibleCharacters;
                    break;
                }

                // В режиме Accelerate печатаем пачками за кадр для максимальной скорости
                int charsThisFrame = Mathf.Max(1, Mathf.CeilToInt(Time.unscaledDeltaTime / Mathf.Max(acceleratedTypingSpeed, 0.0001f)));
                currentVisible = Mathf.Min(totalVisibleCharacters, currentVisible + charsThisFrame);
                textDisplay.maxVisibleCharacters = currentVisible;
                yield return null;
                continue;
            }

            currentVisible++;
            textDisplay.maxVisibleCharacters = currentVisible;

            yield return new WaitForSecondsRealtime(normalTypingSpeed);
        }

        FinishTyping();
    }

    private void FinishTyping()
    {
        isTyping = false;
        typingCoroutine = null;

        if (continueButton != null)
            continueButton.interactable = true;

        LineFinished?.Invoke();
    }
}