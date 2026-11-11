using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

/// Диалоговое окно, варианты ответа и фон игровой сцены — мрачный вариант стиля «Астра».
/// Подключается автоматически (AstraAutoStyle), но можно повесить и вручную на DialoguePanel.
public class DialogueStyler : MonoBehaviour
{
    [Header("Ссылки (если пусто — найдутся по именам)")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Шрифты (необязательно)")]
    [SerializeField] private TMP_FontAsset headingFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    [Header("Фон игровой сцены (пусто — возьмётся Assets/Sprites/fon)")]
    [SerializeField] private bool addBackground = true;
    [SerializeField] private Sprite backgroundArt;

    [Header("Размеры окна")]
    [Tooltip("Отступ окна от краёв экрана (0 — вплотную)")]
    [SerializeField] private float edgeMargin = 14f;
    [Tooltip("Непрозрачность бумаги окна: 1 — сплошная, 0 — прозрачная")]
    [Range(0f, 1f)] [SerializeField] private float paperOpacity = 0.78f;
    [SerializeField] private float bottomMargin = 26f;
    [SerializeField] private float boxHeight = 230f;

    [Header("Варианты ответа")]
    [SerializeField] private float optionsWidth = 1100f;
    [SerializeField] private float optionsY = 70f;

    private RectTransform box, nameTag, heart;
    private CanvasGroup boxGroup;
    private Image stitch;
    private CustomTypewriter typewriter;
    private OptionsPresenter options;
    private CanvasGroup optionsGroup;
    private bool optionsStyled;
    private readonly List<Option> styled = new List<Option>();
    private float scanTimer;

    private class Option
    {
        public OptionItem item;
        public Image fill;
        public TextMeshProUGUI label;
        public LayoutElement layout;
    }

    // =====================================================================

    private void Awake()
    {
        var panel = (RectTransform)transform;
        typewriter = GetComponent<CustomTypewriter>();
        if (dialogueText == null) dialogueText = FindText(transform, "DialogueText");
        if (nameText == null) nameText = FindText(transform, "CharacterNameText");

        if (addBackground) BuildBackground();

        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(0f, boxHeight + bottomMargin + 40f);

        var panelImg = GetComponent<Image>();
        if (panelImg != null) { panelImg.sprite = null; panelImg.color = new Color(0, 0, 0, 0); }

        BuildBox(panel);
        BuildNameTag(panel);

        options = FindFirstObjectByType<OptionsPresenter>(FindObjectsInactive.Include);

        // имена в истории — светло-розовые, под тёмное окно
        var ext = FindFirstObjectByType<DialogueExtensions>(FindObjectsInactive.Include);
        if (ext != null) ext.SpeakerColor = UIStyleKit.Hex("E88BA8");
    }

    // ---------------- Окно реплики ----------------

    private void BuildBox(RectTransform panel)
    {
        box = UIStyleKit.Child(panel, "Box", 0);
        box.anchorMin = new Vector2(0f, 0f);
        box.anchorMax = new Vector2(1f, 0f);
        box.pivot = new Vector2(0.5f, 0f);
        box.offsetMin = new Vector2(edgeMargin, bottomMargin);
        box.offsetMax = new Vector2(-edgeMargin, bottomMargin + boxHeight);
        boxGroup = box.gameObject.AddComponent<CanvasGroup>();
        boxGroup.blocksRaycasts = false;

        // мягкая тень под окном
        var shadow = UIStyleKit.Child(box, "Shadow", 0);
        UIStyleKit.Stretch(shadow, -40f);
        UIStyleKit.AddImage(shadow, UIStyleKit.SoftCircle(), Color.black.WithAlpha(0.45f * paperOpacity));

        var paper = UIStyleKit.LacePanel(box, "Lace", 1.4f, UIStyleKit.GloomPaper.WithAlpha(paperOpacity),
            UIStyleKit.GloomLace.WithAlpha(Mathf.Lerp(0.6f, 1f, paperOpacity)), UIStyleKit.GloomStitch.WithAlpha(0.55f));
        stitch = paper.transform.Find("Stitch")?.GetComponent<Image>();

        // красноватое свечение снизу — будто от свечи
        var ember = UIStyleKit.Child(paper.rectTransform, "Ember");
        UIStyleKit.Place(ember, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 160f));
        UIStyleKit.AddImage(ember, UIStyleKit.SoftCircle(), UIStyleKit.Blood.WithAlpha(0.35f));
        paper.gameObject.AddComponent<RectMask2D>(); // свечение не вылезает за бумагу

        // сканлайны — «старая плёнка»
        var scan = UIStyleKit.Child(paper.rectTransform, "Scanlines");
        UIStyleKit.Stretch(scan, 2f);
        var sc = UIStyleKit.AddImage(scan, UIStyleKit.Scanlines(), Color.black.WithAlpha(0.18f), Image.Type.Tiled);
        sc.pixelsPerUnitMultiplier = 0.6f;

        if (dialogueText != null)
        {
            if (bodyFont != null) dialogueText.font = bodyFont;
            var rt = dialogueText.rectTransform;
            rt.SetParent(box, false);
            rt.SetAsLastSibling();
            UIStyleKit.Stretch(rt);
            rt.offsetMin = new Vector2(110f, 50f);
            rt.offsetMax = new Vector2(-150f, -58f);
            dialogueText.fontSize = 31;
            dialogueText.color = UIStyleKit.GloomText;
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.lineSpacing = 8f;
            dialogueText.margin = Vector4.zero;
            dialogueText.raycastTarget = false;
            Glow(dialogueText, Color.black.WithAlpha(0.8f), 0.4f, -0.5f);
        }

        // увядшая сакура в углу
        var stamp = UIStyleKit.Child(box, "Sakura");
        UIStyleKit.Place(stamp, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-72f, -64f), new Vector2(44f, 44f));
        stamp.localRotation = Quaternion.Euler(0, 0, 14f);
        UIStyleKit.AddImage(stamp, UIStyleKit.Sakura(), UIStyleKit.GloomLace.WithAlpha(0.8f));
        var core = UIStyleKit.Child(stamp, "Core");
        UIStyleKit.Place(core, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f, 12f));
        UIStyleKit.AddImage(core, UIStyleKit.SoftCircle(), UIStyleKit.Blood);

        // сердечко «можно дальше» — бьётся, как пульс
        heart = UIStyleKit.Child(box, "NextHeart");
        UIStyleKit.Place(heart, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-64f, 52f), new Vector2(24f, 24f));
        UIStyleKit.AddImage(heart, UIStyleKit.Heart(), UIStyleKit.GloomAccent);
        var halo = UIStyleKit.Child(heart, "Halo", 0);
        UIStyleKit.Stretch(halo, -18f);
        UIStyleKit.AddImage(halo, UIStyleKit.SoftCircle(), UIStyleKit.BloodBright.WithAlpha(0.35f));
    }

    // ---------------- Ярлычок с именем ----------------

    private void BuildNameTag(RectTransform panel)
    {
        if (nameText == null) return;

        nameTag = UIStyleKit.Child(panel, "NameTag");
        nameTag.anchorMin = nameTag.anchorMax = new Vector2(0f, 0f);
        nameTag.pivot = new Vector2(0f, 0.5f);
        nameTag.anchoredPosition = new Vector2(edgeMargin + 90f, bottomMargin + boxHeight - 4f);
        nameTag.sizeDelta = new Vector2(300f, 74f);

        const float lace = 2.2f;
        var l = UIStyleKit.AddImage(nameTag, UIStyleKit.LaceFrame(), UIStyleKit.GloomLace, Image.Type.Tiled);
        l.pixelsPerUnitMultiplier = lace;
        var fill = UIStyleKit.Child(nameTag, "Fill");
        UIStyleKit.Stretch(fill, UIStyleKit.LaceInset / lace);
        UIStyleKit.AddImage(fill, UIStyleKit.Rounded(10), UIStyleKit.GloomTag, Image.Type.Sliced);
        var st = UIStyleKit.Child(fill, "Stitch");
        UIStyleKit.Stretch(st, 4f);
        UIStyleKit.AddImage(st, UIStyleKit.Rounded(8, 2, 0f), UIStyleKit.GloomStitch.WithAlpha(0.7f), Image.Type.Sliced);

        if (headingFont != null) nameText.font = headingFont;
        var rt = nameText.rectTransform;
        rt.SetParent(nameTag, false);
        UIStyleKit.Stretch(rt);
        nameText.fontSize = 27;
        nameText.fontStyle = FontStyles.Bold;
        nameText.characterSpacing = 3f;
        nameText.color = UIStyleKit.GloomText;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.margin = Vector4.zero;
        nameText.raycastTarget = false;
        Glow(nameText, UIStyleKit.BloodBright.WithAlpha(0.45f), 0.6f, 0f);
    }

    // ---------------- Фон ----------------

    private void BuildBackground()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var root = canvas.rootCanvas.transform;
        if (root.Find("BG_Art") != null) return;

        var bg = UIStyleKit.Child(root, "BG_Art", 0);
        UIStyleKit.Stretch(bg);
        var img = bg.gameObject.AddComponent<Image>();
        UIStyleKit.Backdrop(img, backgroundArt, root, 1);
    }

    // =====================================================================

    private void LateUpdate()
    {
        float t = Time.unscaledTime;

        // ярлычок прячется вместе с именем
        if (nameTag != null && nameText != null)
        {
            bool hasName = nameText.gameObject.activeSelf && !string.IsNullOrWhiteSpace(nameText.text);
            if (nameTag.gameObject.activeSelf != hasName) nameTag.gameObject.SetActive(hasName);
            if (hasName) nameTag.sizeDelta = new Vector2(Mathf.Max(200f, nameText.preferredWidth + 90f), nameTag.sizeDelta.y);
        }

        // сердце-пульс, когда реплика допечатана
        if (heart != null)
        {
            bool show = typewriter == null || (!typewriter.IsTyping && dialogueText != null && !string.IsNullOrEmpty(dialogueText.text));
            heart.gameObject.SetActive(show);
            if (show)
            {
                float beat = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 5f)), 12f); // «тук»
                heart.localScale = Vector3.one * (1f + 0.22f * beat);
            }
        }

        // строчка окна чуть «дышит», иногда вздрагивает
        if (stitch != null)
        {
            float a = 0.45f + 0.12f * Mathf.Sin(t * 1.3f);
            if (Random.value < 0.004f) a = 0.95f;
            stitch.color = UIStyleKit.GloomStitch.WithAlpha(a);
        }

        UpdateOptions();
    }

    // =====================================================================
    // Варианты ответа
    // =====================================================================

    private void UpdateOptions()
    {
        if (options == null) return;
        if (!optionsStyled) StyleOptionsPanel();

        // пока показаны варианты — окно реплики уходит в тень (последняя реплика видна в панели вариантов)
        bool optionsVisible = optionsGroup != null && optionsGroup.alpha > 0.01f && options.gameObject.activeInHierarchy;
        if (boxGroup != null)
            boxGroup.alpha = Mathf.MoveTowards(boxGroup.alpha, optionsVisible ? 0f : 1f, Time.unscaledDeltaTime * 6f);
        if (nameTag != null && optionsVisible) nameTag.gameObject.SetActive(false);

        scanTimer -= Time.unscaledDeltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = 0.05f;
            foreach (var item in options.GetComponentsInChildren<OptionItem>(true))
                if (!styled.Exists(o => o.item == item)) styled.Add(StyleOption(item));
        }
        styled.RemoveAll(o => o.item == null);

        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        foreach (var o in styled)
        {
            if (!o.item.gameObject.activeInHierarchy) continue;

            bool on = o.item.gameObject == selected && o.item.interactable;
            o.fill.color = Color.Lerp(o.fill.color, on ? UIStyleKit.GloomTag : UIStyleKit.GloomPlate, Time.unscaledDeltaTime * 14f);

            // высота плашки — по тексту, чтобы длинные варианты помещались целиком
            float w = ((RectTransform)o.item.transform).rect.width - 2f * OptionPadX;
            if (w > 10f && o.label != null)
            {
                float h = o.label.GetPreferredValues(o.label.text, w, float.PositiveInfinity).y + 2f * OptionPadY;
                o.layout.preferredHeight = Mathf.Max(84f, h);
            }
        }
    }

    private const float OptionLace = 2.4f;
    private static float OptionPadX => UIStyleKit.LaceInset / OptionLace + 46f;
    private static float OptionPadY => UIStyleKit.LaceInset / OptionLace + 12f;

    /// Панель вариантов: тёмное кружевное окно по центру экрана
    private void StyleOptionsPanel()
    {
        optionsStyled = true;
        var rt = (RectTransform)options.transform;
        optionsGroup = options.GetComponent<CanvasGroup>();

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, optionsY);
        rt.sizeDelta = new Vector2(optionsWidth, rt.sizeDelta.y);

        var vlg = options.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.padding = new RectOffset(80, 80, 70, 70);
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        // фон панели: был чёрный прямоугольник — делаем тёмное кружево
        var bg = UIStyleKit.FindDeep(rt, "Background");
        if (bg != null)
        {
            var bgImg = bg.GetComponent<Image>();
            if (bgImg != null) bgImg.enabled = false;
            var bgRt = (RectTransform)bg;
            var panel = UIStyleKit.LacePanel(bgRt, "Lace", 1.4f, UIStyleKit.GloomPaper.WithAlpha(0.95f),
                UIStyleKit.GloomLace, UIStyleKit.GloomStitch.WithAlpha(0.55f));
            var scan = UIStyleKit.Child(panel.rectTransform, "Scanlines");
            UIStyleKit.Stretch(scan, 2f);
            var sc = UIStyleKit.AddImage(scan, UIStyleKit.Scanlines(), Color.black.WithAlpha(0.18f), Image.Type.Tiled);
            sc.pixelsPerUnitMultiplier = 0.6f;
        }

        // последняя реплика над вариантами
        var last = FindText(rt, "Last Line");
        if (last != null)
        {
            if (bodyFont != null) last.font = bodyFont;
            last.enableAutoSizing = false;
            last.fontSize = 30;
            last.fontStyle = FontStyles.Italic;
            last.color = UIStyleKit.GloomMuted;
            last.alignment = TextAlignmentOptions.Left;
            last.textWrappingMode = TextWrappingModes.Normal;
            last.margin = new Vector4(10f, 0f, 10f, 12f);
            last.raycastTarget = false;
        }
    }

    /// Плашка варианта. Текст переносится в свой дочерний объект, иначе фон ложится поверх текста.
    private Option StyleOption(OptionItem item)
    {
        var rt = (RectTransform)item.transform;
        var o = new Option { item = item };

        // исходный текст (в префабе Yarn он на самом корне) и старые украшения
        var oldText = GetField<TextMeshProUGUI>(item, "text") ?? item.GetComponentInChildren<TextMeshProUGUI>(true);
        var oldSelection = GetField<Image>(item, "selectionImage");
        if (oldSelection != null) oldSelection.gameObject.SetActive(false);
        var oldBg = UIStyleKit.FindDeep(rt, "Option Background");
        if (oldBg != null) oldBg.gameObject.SetActive(false);
        var rootImg = item.GetComponent<Image>();
        if (rootImg != null) rootImg.color = new Color(0, 0, 0, 0);
        item.transition = Selectable.Transition.None;

        // кружевная рамка (она же ловит мышь) → заливка → строчка → текст
        var frame = UIStyleKit.Child(rt, "AstraFrame", 0);
        UIStyleKit.Stretch(frame);
        var frameImg = UIStyleKit.AddImage(frame, UIStyleKit.LaceFrame(), UIStyleKit.GloomLace, Image.Type.Tiled);
        frameImg.pixelsPerUnitMultiplier = OptionLace;
        frameImg.raycastTarget = true;

        var fillRt = UIStyleKit.Child(frame, "Fill");
        UIStyleKit.Stretch(fillRt, UIStyleKit.LaceInset / OptionLace);
        o.fill = UIStyleKit.AddImage(fillRt, UIStyleKit.Rounded(10), UIStyleKit.GloomPlate, Image.Type.Sliced);
        var st = UIStyleKit.Child(fillRt, "Stitch");
        UIStyleKit.Stretch(st, 4f);
        UIStyleKit.AddImage(st, UIStyleKit.Rounded(8, 2, 0f), UIStyleKit.GloomStitch.WithAlpha(0.5f), Image.Type.Sliced);

        // сердечко-указатель выбранного варианта
        var mark = UIStyleKit.Child(frame, "Mark");
        UIStyleKit.Place(mark, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(UIStyleKit.LaceInset / OptionLace + 24f, 0f), new Vector2(20f, 20f));
        var markImg = UIStyleKit.AddImage(mark, UIStyleKit.Heart(), UIStyleKit.GloomAccent);

        // новый текст
        var labelRt = UIStyleKit.Child(rt, "AstraLabel");
        UIStyleKit.Stretch(labelRt);
        labelRt.offsetMin = new Vector2(OptionPadX, OptionPadY);
        labelRt.offsetMax = new Vector2(-OptionPadX, -OptionPadY);
        var label = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = bodyFont != null ? bodyFont : (oldText != null ? oldText.font : TMP_Settings.defaultFontAsset);
        label.text = oldText != null ? oldText.text : "";
        label.fontSize = 27;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.color = UIStyleKit.GloomText;
        o.label = label;

        // старый текст прячем, но Yarn теперь пишет в новый
        if (oldText != null)
        {
            if (oldText.gameObject == item.gameObject) oldText.enabled = false;
            else oldText.gameObject.SetActive(false);
        }
        SetField(item, "text", label);
        SetField(item, "selectionImage", markImg);

        // цвета и значок для состояний (в префабах они прозрачные/серые)
        SetAppearance(item, "normal", UIStyleKit.GloomText, null);
        SetAppearance(item, "selected", Color.white, UIStyleKit.Heart());
        SetAppearance(item, "disabled", UIStyleKit.GloomMuted.WithAlpha(0.6f), null);
        markImg.gameObject.SetActive(false);

        o.layout = UIStyleKit.GetOrAdd<LayoutElement>(item.gameObject);
        o.layout.minHeight = 84f;
        o.layout.preferredHeight = 84f;
        o.layout.flexibleHeight = 0f;
        return o;
    }

    // ---------------- Рефлексия для приватных полей OptionItem ----------------

    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static T GetField<T>(OptionItem item, string name) where T : class
    {
        var f = typeof(OptionItem).GetField(name, Any);
        var v = f?.GetValue(item) as T;
        return v is Object uo && uo == null ? null : v;
    }

    private static void SetField(OptionItem item, string name, object value)
    {
        var f = typeof(OptionItem).GetField(name, Any);
        f?.SetValue(item, value);
    }

    private static void SetAppearance(OptionItem item, string fieldName, Color colour, Sprite sprite)
    {
        var field = typeof(OptionItem).GetField(fieldName, Any);
        if (field == null) return;
        object boxed = field.GetValue(item);
        boxed.GetType().GetField("colour", Any)?.SetValue(boxed, colour);
        boxed.GetType().GetField("sprite", Any)?.SetValue(boxed, sprite);
        field.SetValue(item, boxed);
    }

    // ---------------- Мелочи ----------------

    private static void Glow(TextMeshProUGUI t, Color color, float softness, float offsetY)
    {
        var mat = t.fontMaterial;
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", color);
        mat.SetFloat("_UnderlaySoftness", softness);
        mat.SetFloat("_UnderlayOffsetY", offsetY);
        t.fontMaterial = mat;
        t.UpdateMeshPadding();
    }

    private static TextMeshProUGUI FindText(Transform root, string name)
    {
        var t = UIStyleKit.FindDeep(root, name);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }
}
