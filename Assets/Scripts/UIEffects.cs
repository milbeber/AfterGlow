using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Глитч текста: редкие короткие сбои — красная и голубая «тени» разъезжаются,
/// буквы на миг подменяются. Добавляется стайлерами автоматически.
public class GlitchText : MonoBehaviour
{
    public TextMeshProUGUI target;
    public float minInterval = 4f;
    public float maxInterval = 10f;
    public float burstDuration = 0.22f;
    public float strength = 6f;
    [Range(0f, 1f)] public float corruptChance = 0.35f;

    [Tooltip("Шанс, что после сбоя одна «неправильная» буква останется (НЕ ВХЭДИ)")]
    [Range(0f, 1f)] public float lingerChance = 0f;
    public Vector2 lingerTime = new Vector2(2f, 5f);

    private TextMeshProUGUI red, cyan;
    private RectTransform targetRt;
    private Vector2 basePos;
    private float nextBurst, burstEnd;
    private string original;
    private bool lingering;
    private float healAt;
    private const string Junk = "ЖЫЭЮЪ#%/\\§01";

    private void Start()
    {
        if (target == null) target = GetComponent<TextMeshProUGUI>();
        if (target == null) { enabled = false; return; }

        targetRt = target.rectTransform;
        basePos = targetRt.anchoredPosition;
        red = MakeGhost("GlitchRed", UIStyleKit.BloodBright);
        cyan = MakeGhost("GlitchCyan", UIStyleKit.GlitchCyan);
        Schedule();
    }

    private TextMeshProUGUI MakeGhost(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(targetRt.parent, false);
        rt.SetSiblingIndex(targetRt.GetSiblingIndex()); // за основным текстом
        rt.anchorMin = targetRt.anchorMin;
        rt.anchorMax = targetRt.anchorMax;
        rt.pivot = targetRt.pivot;
        rt.sizeDelta = targetRt.sizeDelta;
        rt.anchoredPosition = basePos;

        var t = go.AddComponent<TextMeshProUGUI>();
        t.font = target.font;
        t.fontSize = target.fontSize;
        t.fontStyle = target.fontStyle;
        t.characterSpacing = target.characterSpacing;
        t.alignment = target.alignment;
        t.textWrappingMode = target.textWrappingMode;
        t.margin = target.margin;
        t.color = color.WithAlpha(0.85f);
        t.raycastTarget = false;
        t.enabled = false;
        return t;
    }

    /// Запустить сбой прямо сейчас (например, при наведении)
    public void Trigger(float duration = -1f)
    {
        if (target == null || red == null) return;
        float end = Time.unscaledTime + (duration > 0 ? duration : burstDuration);

        // Уже сбоит — только продлеваем сбой. Иначе запомнили бы «испорченный» текст как настоящий
        if (red.enabled) { burstEnd = Mathf.Max(burstEnd, end); return; }

        if (!lingering) original = target.text; // во время «застрявшей» буквы настоящий текст уже сохранён
        lingering = false;
        red.text = cyan.text = original;
        red.margin = cyan.margin = target.margin;
        red.enabled = cyan.enabled = true;
        burstEnd = end;
    }

    private void Schedule() => nextBurst = Time.unscaledTime + Random.Range(minInterval, maxInterval);

    private void Update()
    {
        if (red == null) return;
        float now = Time.unscaledTime;

        if (lingering && now >= healAt)
        {
            lingering = false;
            target.text = original; // тихо «заживает»
        }

        if (!red.enabled && now >= nextBurst) Trigger();

        if (red.enabled)
        {
            if (now >= burstEnd) { EndBurst(); return; }

            float s = strength;
            red.rectTransform.anchoredPosition = basePos + new Vector2(Random.Range(-s, s), Random.Range(-s, s) * 0.3f);
            cyan.rectTransform.anchoredPosition = basePos + new Vector2(Random.Range(-s, s), Random.Range(-s, s) * 0.3f);
            targetRt.anchoredPosition = basePos + new Vector2(Random.Range(-s, s) * 0.4f, 0f);

            target.text = Random.value < corruptChance ? Corrupt(original) : original;
        }
    }

    private void EndBurst()
    {
        red.enabled = cyan.enabled = false;
        targetRt.anchoredPosition = basePos;
        if (original != null)
        {
            if (Random.value < lingerChance)
            {
                target.text = CorruptOne(original); // одна буква «застревает»
                lingering = true;
                healAt = Time.unscaledTime + Random.Range(lingerTime.x, lingerTime.y);
            }
            else target.text = original;
        }
        Schedule();
    }

    private static string CorruptOne(string s)
    {
        // предпочитаем гласную: «НЕ ВХЭДИ», «ВЫЙТЫ» — читается, но явно «не так»
        const string vowels = "АОЕИУЫЭЯЮ";
        const string swaps  = "ЭЭЁЫЮИЕАУ";
        var idx = new System.Collections.Generic.List<int>();
        for (int i = 0; i < s.Length; i++)
            if (vowels.IndexOf(char.ToUpper(s[i])) >= 0) idx.Add(i);
        if (idx.Count == 0) return Corrupt(s);

        int at = idx[Random.Range(0, idx.Count)];
        char c = swaps[vowels.IndexOf(char.ToUpper(s[at]))];
        if (char.IsLower(s[at])) c = char.ToLower(c);
        var sb = new StringBuilder(s);
        sb[at] = c;
        return sb.ToString();
    }

    private void OnDisable()
    {
        bool wasGlitching = lingering || (red != null && red.enabled);
        if (red != null && red.enabled) { red.enabled = cyan.enabled = false; targetRt.anchoredPosition = basePos; }
        if (wasGlitching && original != null && target != null) target.text = original;
        lingering = false;
    }

    private static string Corrupt(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s);
        int n = Mathf.Max(1, s.Length / 6);
        for (int i = 0; i < n; i++)
        {
            int idx = Random.Range(0, sb.Length);
            if (sb[idx] == '<' || sb[idx] == '>' || char.IsWhiteSpace(sb[idx])) continue; // не ломаем теги
            sb[idx] = Junk[Random.Range(0, Junk.Length)];
        }
        return sb.ToString();
    }
}

/// Розовая кнопка: при наведении чуть увеличивается, текст гаснет у недоступных кнопок.
public class LaceButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private TextMeshProUGUI label;
    private Color labelColor;
    private bool hovered;
    private float t;

    public void Init(Button b, TextMeshProUGUI l)
    {
        button = b;
        label = l;
        if (label != null) labelColor = label.color;
    }

    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    private void OnDisable() { hovered = false; t = 0f; transform.localScale = Vector3.one; }

    private void Update()
    {
        bool active = button == null || button.interactable;
        t = Mathf.MoveTowards(t, hovered && active ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        float e = t * t * (3f - 2f * t);
        transform.localScale = Vector3.one * (1f + 0.035f * e);

        if (label != null)
        {
            var c = labelColor;
            c.a = active ? labelColor.a : labelColor.a * 0.45f;
            label.color = c;
        }
    }
}

/// «Неправильная» кнопка: при наведении сразу сбоит и мигает сканлайнами.
public class HorrorButtonFx : MonoBehaviour, IPointerEnterHandler
{
    public GlitchText glitch;
    public Image scanlines;
    public Image rim;
    private float flash;

    public void OnPointerEnter(PointerEventData e)
    {
        if (glitch != null) glitch.Trigger(0.35f);
        flash = 1f;
    }

    private void Update()
    {
        flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.5f);
        // лёгкое «дыхание» рамки
        float pulse = 0.65f + 0.2f * Mathf.Sin(Time.unscaledTime * 2.2f) + 0.35f * flash;
        if (rim != null) rim.color = UIStyleKit.BloodBright.WithAlpha(Mathf.Clamp01(pulse));
        if (scanlines != null) scanlines.color = UIStyleKit.BloodBright.WithAlpha(0.1f + 0.25f * flash * Random.value);
    }
}
