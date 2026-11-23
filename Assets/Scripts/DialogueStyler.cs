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

    [Header("Рисованная рамка (Assets/Resources/AfterGlowUI/DialogueFrame.png)")]
    [Tooltip("Окно из нарисованного арта с розами. Выключите — вернётся процедурное кружево")]
    [SerializeField] private bool useArtFrame = true;
    [SerializeField] private Sprite artFrame;
    [SerializeField] private float artSideMargin = 4f;
    [SerializeField] private float artBottomMargin = 6f;

    [Header("Варианты ответа")]
    [Tooltip("Рисованная панель выбора: рамка с розами и неоновые плашки. Выключите — вернётся процедурное кружево")]
    [SerializeField] private bool useArtOptions = true;
    [SerializeField] private float optionsWidth = 1100f;
    [SerializeField] private float optionsY = 70f;
    [SerializeField] private float artOptionsWidth = 1360f;
    [Tooltip("Желаемый центр панели выбора по высоте (от центра экрана). Если панель высокая, она сама опустится ниже индикатора рассудка")]
    [SerializeField] private float artOptionsY = 0f;
    [Tooltip("Сколько места сверху оставить под индикатор рассудка, если его не удалось измерить")]
    [SerializeField] private float hudReserve = 235f;
    [Tooltip("Минимальный отступ панели от краёв экрана")]
    [SerializeField] private float screenMargin = 16f;
    [Tooltip("Непрозрачность фона внутри рамки выбора: 1 — сплошной, 0 — сцену видно насквозь")]
    [Range(0f, 1f)] [SerializeField] private float optionsFillOpacity = 0.72f;
    [Tooltip("Показывать последнюю реплику над вариантами")]
    [SerializeField] private bool showLastLine = true;

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
        public Image plateOn;
        public RectTransform sparkle, visual;
        public float padLeft, padRight, padY, minHeight, phase, glow;
    }

    private bool artOptions;
    private float artMinHeight;
    private Image optionsFill;
    private RectTransform sanityHud;
    private VerticalLayoutGroup optionsLayout;
    private RectTransform timerRoot, timerFill;
    private Image timerFillImg, timerGlowImg;
    private Sprite plateSprite, plateOnSprite, sparkleSprite, heartSprite;

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
        if (useArtFrame && artFrame == null) artFrame = Resources.Load<Sprite>(ArtFramePath);
        bool art = useArtFrame && artFrame != null;
        panel.sizeDelta = art
            ? new Vector2(0f, artFrame.rect.height + artBottomMargin)
            : new Vector2(0f, boxHeight + bottomMargin + 40f);

        var panelImg = GetComponent<Image>();
        if (panelImg != null) { panelImg.sprite = null; panelImg.color = new Color(0, 0, 0, 0); }

        if (art)
        {
            BuildArtBox(panel);
            BuildArtNameTag();
        }
        else
        {
            BuildBox(panel);
            BuildNameTag(panel);
        }

        options = FindFirstObjectByType<OptionsPresenter>(FindObjectsInactive.Include);

        // день 15: при низком рассудке буквы дрожат и «соскальзывают», но текст остаётся читаемым
        if (dialogueText != null) UIStyleKit.GetOrAdd<SanityTextDistortion>(dialogueText.gameObject);
        if (nameText != null) UIStyleKit.GetOrAdd<SanityTextDistortion>(nameText.gameObject).multiplier = 0.5f;

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

    // ---------------- Рисованное окно (розы, банты, чётки) ----------------

    public const string ArtFramePath = "AfterGlowUI/DialogueFrame";

    // Координаты внутри DialogueFrame.png (px от краёв картинки)
    private const float ArtTextLeft = 150f, ArtTextRight = 290f, ArtTextTop = 142f, ArtTextBottom = 74f;
    private static readonly Vector2 ArtNameCenter = new Vector2(286f, 86f);   // от левого верхнего угла
    public const string ArtHeartPath = "AfterGlowUI/NextHeart";
    private static readonly Vector2 ArtHeartPos = new Vector2(-128f, 102.5f);  // центр сердечка от правого нижнего угла

    private void BuildArtBox(RectTransform panel)
    {
        float h = artFrame.rect.height;
        box = UIStyleKit.Child(panel, "Box", 0);
        box.anchorMin = new Vector2(0f, 0f);
        box.anchorMax = new Vector2(1f, 0f);
        box.pivot = new Vector2(0.5f, 0f);
        box.offsetMin = new Vector2(artSideMargin, artBottomMargin);
        box.offsetMax = new Vector2(-artSideMargin, artBottomMargin + h);
        boxGroup = box.gameObject.AddComponent<CanvasGroup>();
        boxGroup.blocksRaycasts = false;

        // мягкая тень, чтобы рамка «отрывалась» от фона
        var shadow = UIStyleKit.Child(box, "Shadow", 0);
        UIStyleKit.Stretch(shadow, -30f);
        UIStyleKit.AddImage(shadow, UIStyleKit.SoftCircle(), Color.black.WithAlpha(0.55f));

        var frameRt = UIStyleKit.Child(box, "Frame");
        UIStyleKit.Stretch(frameRt);
        var frame = UIStyleKit.AddImage(frameRt, artFrame, Color.white, Image.Type.Sliced);
        frame.fillCenter = true;

        if (dialogueText != null)
        {
            if (bodyFont != null) dialogueText.font = bodyFont;
            var rt = dialogueText.rectTransform;
            rt.SetParent(box, false);
            rt.SetAsLastSibling();
            UIStyleKit.Stretch(rt);
            rt.offsetMin = new Vector2(ArtTextLeft, ArtTextBottom);
            rt.offsetMax = new Vector2(-ArtTextRight, -ArtTextTop);
            dialogueText.fontSize = 31;
            dialogueText.color = UIStyleKit.GloomText;
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.overflowMode = TextOverflowModes.Overflow;
            dialogueText.lineSpacing = 6f;
            dialogueText.margin = Vector4.zero;
            dialogueText.raycastTarget = false;
            Glow(dialogueText, Color.black.WithAlpha(0.85f), 0.4f, -0.5f);
        }

        // нарисованное сердечко остаётся на рамке всегда; когда реплика допечатана —
        // поверх него «оживает» его точная копия: бьётся и светится
        var heartArt = Resources.Load<Sprite>(ArtHeartPath);
        if (heartArt != null)
        {
            heart = UIStyleKit.Child(box, "NextHeart");
            UIStyleKit.Place(heart, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), ArtHeartPos, heartArt.rect.size);
            var halo = UIStyleKit.Child(heart, "Halo");
            UIStyleKit.Place(halo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, 2f), new Vector2(90f, 90f));
            UIStyleKit.AddImage(halo, UIStyleKit.SoftCircle(), UIStyleKit.Hex("FF4FA3").WithAlpha(0.4f));
            var icon = UIStyleKit.Child(heart, "Icon");
            UIStyleKit.Stretch(icon);
            UIStyleKit.AddImage(icon, heartArt, Color.white);
        }
    }

    /// Имя — прямо в нарисованной табличке со звёздочками
    private void BuildArtNameTag()
    {
        if (nameText == null) return;

        nameTag = UIStyleKit.Child(box, "NameTag");
        UIStyleKit.Place(nameTag, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(ArtNameCenter.x, -ArtNameCenter.y), new Vector2(ArtNameWidth, 52f));

        if (headingFont != null) nameText.font = headingFont;
        var rt = nameText.rectTransform;
        rt.SetParent(nameTag, false);
        UIStyleKit.Stretch(rt);
        nameText.fontSize = 26;
        nameText.fontStyle = FontStyles.Bold;
        nameText.characterSpacing = 3f;
        nameText.color = UIStyleKit.Hex("FFE3F1");
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.margin = Vector4.zero;
        nameText.raycastTarget = false;
        Glow(nameText, UIStyleKit.Hex("FF2E8A").WithAlpha(0.55f), 0.6f, 0f);
        artNameTag = true;
    }

    private const float ArtNameWidth = 190f; // между звёздочками таблички
    private bool artNameTag;
    private string lastName;

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
            if (hasName && !artNameTag) nameTag.sizeDelta = new Vector2(Mathf.Max(200f, nameText.preferredWidth + 90f), nameTag.sizeDelta.y);
            else if (hasName && nameText.text != lastName)
            {
                lastName = nameText.text;
                UIStyleKit.FitWidth(nameText, ArtNameWidth, 26f, 16f); // длинное имя ужимается под табличку
            }
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
            if (o.plateOn != null) AnimateArtOption(o, on);
            else o.fill.color = Color.Lerp(o.fill.color, on ? UIStyleKit.GloomTag : UIStyleKit.GloomPlate, Time.unscaledDeltaTime * 14f);

            // высота плашки — по тексту, чтобы длинные варианты помещались целиком
            float w = ((RectTransform)o.item.transform).rect.width - o.padLeft - o.padRight;
            if (w > 10f && o.label != null)
            {
                float h = o.label.GetPreferredValues(o.label.text, w, float.PositiveInfinity).y + 2f * o.padY;
                o.layout.preferredHeight = Mathf.Max(o.minHeight, h);
            }
        }

        if (artOptions)
        {
            if (optionsFill != null) optionsFill.color = Color.white.WithAlpha(optionsFillOpacity);
            UpdateTimer(optionsVisible);
            PlaceArtOptions();
        }
    }

    /// Панель выбора не заезжает на индикатор рассудка в левом верхнем углу и не уходит за экран
    private void PlaceArtOptions()
    {
        var rt = (RectTransform)options.transform;
        var parent = rt.parent as RectTransform;
        if (parent == null) return;

        float halfScreen = parent.rect.height * 0.5f;
        float half = rt.rect.height * 0.5f;
        float top = halfScreen - hudReserve;

        if (sanityHud == null)
        {
            var hud = UIStyleKit.FindDeep(parent.root, "SanityHUD");
            if (hud != null) sanityHud = hud as RectTransform;
        }
        if (sanityHud != null && sanityHud.gameObject.activeInHierarchy)
        {
            var corners = new Vector3[4];
            sanityHud.GetWorldCorners(corners);
            float hudBottom = parent.InverseTransformPoint(corners[0]).y;
            float hudRight = parent.InverseTransformPoint(corners[2]).x;
            float panelLeft = rt.anchoredPosition.x - rt.rect.width * 0.5f;
            if (hudRight > panelLeft) top = hudBottom - 10f;   // по горизонтали пересекаются — уходим ниже
        }

        float y = Mathf.Min(artOptionsY, top - half);
        y = Mathf.Max(y, -halfScreen + screenMargin + half);
        if (!Mathf.Approximately(rt.anchoredPosition.y, y))
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
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
        if (optionsGroup == null) optionsGroup = options.GetComponentInChildren<CanvasGroup>(true);

        if (useArtOptions)
        {
            var frame = LoadSliced(ArtOptionsFramePath, new Vector4(460f, 285f, 390f, 440f));
            plateSprite = LoadSliced(ArtPlatePath, new Vector4(40f, 26f, 40f, 26f));
            plateOnSprite = LoadSliced(ArtPlateOnPath, new Vector4(40f, 26f, 40f, 26f));
            sparkleSprite = Resources.Load<Sprite>(ArtSparklePath);
            heartSprite = Resources.Load<Sprite>(ArtOptionHeartPath);
            if (frame != null && plateSprite != null && plateOnSprite != null)
            {
                artOptions = true;
                StyleArtOptionsPanel(rt, frame);
                return;
            }
            Debug.LogWarning("[DialogueStyler] Нет картинок AfterGlowUI/Options*, панель выбора в процедурном стиле");
        }

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
        if (artOptions) return StyleArtOption(item);

        var rt = (RectTransform)item.transform;
        var o = new Option { item = item, padLeft = OptionPadX, padRight = OptionPadX, padY = OptionPadY, minHeight = 84f };

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

    // =====================================================================
    // Рисованная панель выбора (рамка с розами, неоновые плашки)
    // =====================================================================

    public const string ArtOptionsFramePath = "AfterGlowUI/OptionsFrame";
    public const string ArtPlatePath = "AfterGlowUI/OptionPlate";
    public const string ArtPlateOnPath = "AfterGlowUI/OptionPlateSelected";
    public const string ArtSparklePath = "AfterGlowUI/OptionSparkle";
    public const string ArtOptionHeartPath = "AfterGlowUI/OptionHeart";

    // OptionsFrame.png рисуется уменьшенной в ArtFrameScale раз (картинка 2170×749).
    // Поля внутри неё (уже в единицах экрана): слева бант и чётки, справа крест, снизу кружево
    private const float ArtFrameScale = 1.6f;
    private const float ArtPanelLeft = 188f, ArtPanelRight = 141f, ArtPanelTop = 134f, ArtPanelBottom = 138f;
    // Прозрачная середина рамки, под ней лежит полупрозрачная заливка
    private const float ArtFillLeft = 111f, ArtFillRight = 86f, ArtFillTop = 100f, ArtFillBottom = 112f;
    private const float ArtTimerSpace = 30f;      // место под полоску таймера
    private const float ArtPlateGlow = 14f;        // ореол вокруг плашки в OptionPlate.png
    private const float ArtOptionHeight = 78f;
    private const float ArtLabelLeft = 104f, ArtLabelRight = 86f, ArtLabelY = 12f;

    private void StyleArtOptionsPanel(RectTransform rt, Sprite frame)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, artOptionsY);
        rt.sizeDelta = new Vector2(artOptionsWidth, rt.sizeDelta.y);

        optionsLayout = options.GetComponent<VerticalLayoutGroup>();
        if (optionsLayout != null)
        {
            optionsLayout.padding = new RectOffset((int)ArtPanelLeft, (int)ArtPanelRight, (int)ArtPanelTop, (int)ArtPanelBottom);
            optionsLayout.spacing = 18f;
            optionsLayout.childAlignment = TextAnchor.MiddleCenter;
            optionsLayout.childControlWidth = true;
            optionsLayout.childControlHeight = true;
            optionsLayout.childForceExpandWidth = true;
            optionsLayout.childForceExpandHeight = false;
        }
        var fitter = UIStyleKit.GetOrAdd<ContentSizeFitter>(options.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // фон панели: рисованная рамка вместо чёрного прямоугольника
        var bg = UIStyleKit.FindDeep(rt, "Background");
        RectTransform frameRt;
        if (bg != null)
        {
            var bgImg = bg.GetComponent<Image>();
            if (bgImg != null) bgImg.enabled = false;
            frameRt = UIStyleKit.Child(bg, "ArtFrame");
        }
        else
        {
            frameRt = UIStyleKit.Child(rt, "ArtFrame", 0);
            UIStyleKit.GetOrAdd<LayoutElement>(frameRt.gameObject).ignoreLayout = true;
        }
        UIStyleKit.Stretch(frameRt);

        var shadow = UIStyleKit.Child(frameRt, "Shadow");
        UIStyleKit.Stretch(shadow, -40f);
        UIStyleKit.AddImage(shadow, UIStyleKit.SoftCircle(), Color.black.WithAlpha(0.3f * optionsFillOpacity));

        // полупрозрачный фон внутри рамки (в самой картинке середина прозрачная)
        var fill = UIStyleKit.Child(frameRt, "Fill");
        UIStyleKit.Stretch(fill);
        fill.offsetMin = new Vector2(ArtFillLeft, ArtFillBottom);
        fill.offsetMax = new Vector2(-ArtFillRight, -ArtFillTop);
        optionsFill = UIStyleKit.AddImage(fill, UIStyleKit.VerticalGradient(UIStyleKit.Hex("16040D"), UIStyleKit.Hex("2E0B1F")),
            Color.white.WithAlpha(optionsFillOpacity));

        var art = UIStyleKit.Child(frameRt, "Frame");
        UIStyleKit.Stretch(art);
        var artImg = UIStyleKit.AddImage(art, frame, Color.white, Image.Type.Sliced);
        artImg.fillCenter = true;
        artImg.pixelsPerUnitMultiplier = ArtFrameScale;
        artMinHeight = (frame.border.y + frame.border.w) / ArtFrameScale;
        UIStyleKit.GetOrAdd<LayoutElement>(options.gameObject).minHeight = artMinHeight;

        BuildTimer(frameRt);

        // последняя реплика — курсивом над вариантами
        var last = FindText(rt, "Last Line");
        if (last != null)
        {
            var container = last.transform.parent != null && last.transform.parent != rt ? last.transform.parent.gameObject : last.gameObject;
            if (!showLastLine) { container.SetActive(false); SetPresenterLastLine(false); return; }
            if (bodyFont != null) last.font = bodyFont;
            last.enableAutoSizing = false;
            last.fontSize = 25;
            last.fontStyle = FontStyles.Italic;
            last.color = UIStyleKit.Hex("E7B7C9");
            last.alignment = TextAlignmentOptions.Center;
            last.textWrappingMode = TextWrappingModes.Normal;
            last.margin = new Vector4(30f, -6f, 30f, 8f);
            last.raycastTarget = false;
            Glow(last, Color.black.WithAlpha(0.85f), 0.4f, -0.5f);
            UIStyleKit.GetOrAdd<SanityTextDistortion>(last.gameObject).multiplier = 0.6f;
        }
    }

    private void SetPresenterLastLine(bool on)
    {
        var f = typeof(OptionsPresenter).GetField("showsLastLine", Any);
        f?.SetValue(options, on);
    }

    /// Неоновая полоска под вариантами: тает к центру, пока идёт время на выбор
    private void BuildTimer(RectTransform frameRt)
    {
        timerRoot = UIStyleKit.Child(frameRt, "Timer");
        timerRoot.anchorMin = new Vector2(0f, 0f);
        timerRoot.anchorMax = new Vector2(1f, 0f);
        timerRoot.pivot = new Vector2(0.5f, 0.5f);
        timerRoot.offsetMin = new Vector2(ArtPanelLeft + 60f, 0f);
        timerRoot.offsetMax = new Vector2(-(ArtPanelRight + 60f), 0f);
        timerRoot.anchoredPosition = new Vector2(0f, ArtPanelBottom + 4f);
        timerRoot.sizeDelta = new Vector2(timerRoot.sizeDelta.x, 8f);

        var track = UIStyleKit.Child(timerRoot, "Track");
        UIStyleKit.Stretch(track);
        UIStyleKit.AddImage(track, UIStyleKit.Rounded(4), UIStyleKit.Hex("2A0A18").WithAlpha(0.9f), Image.Type.Sliced);

        timerFill = UIStyleKit.Child(timerRoot, "Fill");
        UIStyleKit.Stretch(timerFill);
        var glow = UIStyleKit.Child(timerFill, "Glow");
        UIStyleKit.Stretch(glow, -16f);
        timerGlowImg = UIStyleKit.AddImage(glow, UIStyleKit.SoftCircle(), UIStyleKit.Hex("FF3FA0").WithAlpha(0.5f), Image.Type.Simple);
        var line = UIStyleKit.Child(timerFill, "Line");
        UIStyleKit.Stretch(line);
        timerFillImg = UIStyleKit.AddImage(line, UIStyleKit.Rounded(4), UIStyleKit.Hex("FF8CC8"), Image.Type.Sliced);
        timerRoot.gameObject.SetActive(false);
    }

    private void UpdateTimer(bool optionsVisible)
    {
        var timer = TimedChoice.Instance;
        bool running = optionsVisible && timer != null && timer.IsRunning;
        if (timerRoot != null && timerRoot.gameObject.activeSelf != running) timerRoot.gameObject.SetActive(running);

        // под полоску нужно место: нижний отступ панели растёт, пока идёт таймер
        if (optionsLayout != null)
        {
            int bottom = (int)(ArtPanelBottom + (running ? ArtTimerSpace : 0f));
            if (optionsLayout.padding.bottom != bottom)
            {
                optionsLayout.padding.bottom = bottom;
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)options.transform);
            }
        }
        if (!running || timerFill == null) return;

        float left = timer.Remaining01;
        timerFill.anchorMin = new Vector2(0.5f - left * 0.5f, 0f);
        timerFill.anchorMax = new Vector2(0.5f + left * 0.5f, 1f);
        timerFill.offsetMin = timerFill.offsetMax = Vector2.zero;

        // к концу полоска наливается кровью и мигает в такт сердцу
        float danger = Mathf.InverseLerp(0.45f, 0.05f, left);
        Color c = Color.Lerp(UIStyleKit.Hex("FF8CC8"), UIStyleKit.BloodBright, danger);
        float beat = danger > 0f ? Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * Mathf.Lerp(5f, 11f, danger))), 8f) : 0f;
        timerFillImg.color = Color.Lerp(c, Color.white, beat * 0.35f);
        timerGlowImg.color = c.WithAlpha(0.35f + 0.45f * beat);
        timerRoot.anchoredPosition = new Vector2(danger > 0.6f ? Random.Range(-1.5f, 1.5f) * danger : 0f, ArtPanelBottom + 4f);
    }

    private Option StyleArtOption(OptionItem item)
    {
        var rt = (RectTransform)item.transform;
        var o = new Option
        {
            item = item, padLeft = ArtLabelLeft, padRight = ArtLabelRight, padY = ArtLabelY,
            minHeight = ArtOptionHeight, phase = Random.value * 10f
        };

        var oldText = GetField<TextMeshProUGUI>(item, "text") ?? item.GetComponentInChildren<TextMeshProUGUI>(true);
        var oldSelection = GetField<Image>(item, "selectionImage");
        if (oldSelection != null) oldSelection.gameObject.SetActive(false);
        var oldBg = UIStyleKit.FindDeep(rt, "Option Background");
        if (oldBg != null) oldBg.gameObject.SetActive(false);
        var rootImg = item.GetComponent<Image>();
        if (rootImg != null) rootImg.color = new Color(0, 0, 0, 0);
        item.transition = Selectable.Transition.None;

        // всё рисованное — в одном объекте, чтобы плашку можно было слегка «приподнять» при выборе
        o.visual = UIStyleKit.Child(rt, "AstraPlate", 0);
        UIStyleKit.Stretch(o.visual);

        // плашка (она же ловит мышь) и её «горящая» версия поверх
        var plate = UIStyleKit.Child(o.visual, "Plate");
        UIStyleKit.Stretch(plate, -ArtPlateGlow);
        o.fill = UIStyleKit.AddImage(plate, plateSprite, Color.white, Image.Type.Sliced);
        o.fill.raycastTarget = true;
        var on = UIStyleKit.Child(o.visual, "PlateSelected");
        UIStyleKit.Stretch(on, -ArtPlateGlow);
        o.plateOn = UIStyleKit.AddImage(on, plateOnSprite, Color.white.WithAlpha(0f), Image.Type.Sliced);

        // сердечко выбранного варианта (им управляет сам OptionItem через selectionImage)
        var mark = UIStyleKit.Child(o.visual, "Heart");
        UIStyleKit.Place(mark, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 0f), new Vector2(38f, 38f));
        var markImg = UIStyleKit.AddImage(mark, heartSprite != null ? heartSprite : UIStyleKit.Heart(), Color.white);
        markImg.preserveAspect = true;

        // звёздочка справа — есть всегда, у выбранного мерцает ярче
        if (sparkleSprite != null)
        {
            o.sparkle = UIStyleKit.Child(o.visual, "Sparkle");
            UIStyleKit.Place(o.sparkle, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50f, 0f), new Vector2(34f, 34f));
            UIStyleKit.AddImage(o.sparkle, sparkleSprite, Color.white.WithAlpha(0.85f));
        }

        var labelRt = UIStyleKit.Child(rt, "AstraLabel");
        UIStyleKit.Stretch(labelRt);
        labelRt.offsetMin = new Vector2(ArtLabelLeft, ArtLabelY);
        labelRt.offsetMax = new Vector2(-ArtLabelRight, -ArtLabelY);
        var label = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = bodyFont != null ? bodyFont : (oldText != null ? oldText.font : TMP_Settings.defaultFontAsset);
        label.text = oldText != null ? oldText.text : "";
        label.fontSize = 28;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.color = UIStyleKit.GloomText;
        Glow(label, Color.black.WithAlpha(0.8f), 0.35f, -0.4f);
        labelRt.gameObject.AddComponent<SanityTextDistortion>().multiplier = 0.7f;
        o.label = label;

        if (oldText != null)
        {
            if (oldText.gameObject == item.gameObject) oldText.enabled = false;
            else oldText.gameObject.SetActive(false);
        }
        SetField(item, "text", label);
        SetField(item, "selectionImage", markImg);

        SetAppearance(item, "normal", UIStyleKit.Hex("F2D6DF"), null);
        SetAppearance(item, "selected", Color.white, markImg.sprite);
        SetAppearance(item, "disabled", UIStyleKit.GloomMuted.WithAlpha(0.6f), null);
        markImg.gameObject.SetActive(false);

        o.layout = UIStyleKit.GetOrAdd<LayoutElement>(item.gameObject);
        o.layout.minHeight = ArtOptionHeight;
        o.layout.preferredHeight = ArtOptionHeight;
        o.layout.flexibleHeight = 0f;
        return o;
    }

    private void AnimateArtOption(Option o, bool on)
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;
        o.glow = Mathf.MoveTowards(o.glow, on ? 1f : 0f, dt * 7f);

        float breath = 0.9f + 0.1f * Mathf.Sin(t * 2.4f + o.phase);
        o.plateOn.color = Color.white.WithAlpha(o.glow * breath);
        o.fill.color = Color.white.WithAlpha(o.item.interactable ? 1f : 0.55f);
        if (o.visual != null) o.visual.localScale = Vector3.one * (1f + 0.012f * o.glow);

        if (o.sparkle != null)
        {
            float tw = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 1.7f + o.phase)), 6f);
            float k = Mathf.Lerp(0.75f, 1f, o.glow) + 0.2f * tw;
            o.sparkle.localScale = Vector3.one * k;
            o.sparkle.localRotation = Quaternion.Euler(0f, 0f, 45f * tw * o.glow);
        }
    }

    /// Спрайт с 9-slice. Если .meta потерялся и границ нет — делаем срез сами.
    private static Sprite LoadSliced(string path, Vector4 border)
    {
        var s = Resources.Load<Sprite>(path);
        if (s == null)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
        if (s.border != Vector4.zero) return s;
        return Sprite.Create(s.texture, s.rect, new Vector2(0.5f, 0.5f), s.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
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
