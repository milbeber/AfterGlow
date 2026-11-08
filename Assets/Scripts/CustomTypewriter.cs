using System.Collections;
using UnityEngine;
using TMPro;

public class CustomTypewriter : MonoBehaviour
{
    [Header("Настройки кастомного текста")]
    [SerializeField] private TextMeshProUGUI textDisplay;
    [SerializeField] private float typingSpeed = 0.04f;

    private string lastProcessedText = "";
    private string currentFullText = "";
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private UnityEngine.UI.Button panelButton;

    private void Awake()
    {
      
        panelButton = GetComponent<UnityEngine.UI.Button>();
    }

    private void Update()
    {
        if (textDisplay == null) return;

        if (!isTyping && textDisplay.text != "" && textDisplay.text != lastProcessedText)
        {
            currentFullText = textDisplay.text;
            lastProcessedText = currentFullText;

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeText());
        }
    }

    private IEnumerator TypeText()
    {
        isTyping = true;

        if (panelButton != null) panelButton.interactable = false;

        textDisplay.text = "";

        foreach (char letter in currentFullText.ToCharArray())
        {
            textDisplay.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        textDisplay.text = currentFullText;
        isTyping = false;

        if (panelButton != null) panelButton.interactable = true;
    }
}
