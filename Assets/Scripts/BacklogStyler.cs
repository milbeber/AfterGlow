using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Окно истории в стиле «Академия Астра». Повесить на BacklogPanel.
/// Оформление строится при первом открытии окна.
[RequireComponent(typeof(ScrollRect))]
public class BacklogStyler : MonoBehaviour
{
    [Header("Шрифты (необязательно)")]
    [SerializeField] private TMP_FontAsset headingFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    [Header("Тексты")]
    [SerializeField] private string title = "ДНЕВНИК КЛУБА // ИСТОРИЯ";
    [SerializeField] private string hint = "Закрыть: правая кнопка мыши или колесо вниз";

    [SerializeField] private Vector2 cardSize = new Vector2(1240f, 840f);

    [Header("Рисованная рамка (Assets/Resources/AfterGlowUI/BacklogFrame.png)")]
    [Tooltip("Окно «История» из арта с розами. Выключите — вернётся процедурное кружево")]
    [SerializeField] private bool useArtFrame = true;
    [SerializeField] private Sprite artFrame;

    public const string ArtFramePath = "AfterGlowUI/BacklogFrame";

    // Координаты внутри BacklogFrame.png (px от левого верхнего угла)
    private static readonly Rect ArtViewport = new Rect(167f, 168f, 1073f, 577f);
    private static readonly Rect ArtScrollbar = new Rect(1268f, 270f, 14f, 465f);
    private static readonly Vector2 ArtClose = new Vector2(1297f, 103f);

    // куда ставить область прокрутки и скроллбар (центр карточки = 0,0)
    private Vector2 vpPos, vpSize, sbPos, sbSize;

    private void Awake()
    {
        var scroll = GetComponent<ScrollRect>();
        var text = scroll.content != null ? scroll.content.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        TMP_FontAsset baseFont = text != null ? text.font : TMP_Settings.defaultFontAsset;
        TMP_FontAsset hFont = headingFont != null ? headingFont : baseFont;
        TMP_FontAsset bFont = bodyFont != null ? bodyFont : baseFont;

        // 1. Затемнение всего экрана (оно же ловит колесо мыши)
        var dim = GetComponent<Image>();
        if (dim != null)
        {
            dim.sprite = UIStyleKit.Vignette();
            dim.color = UIStyleKit.Wine.WithAlpha(0.92f);
            dim.raycastTarget = true;
        }
        var dimFlat = UIStyleKit.Child(transform, "Dim", 0);
        UIStyleKit.Stretch(dimFlat);
        UIStyleKit.AddImage(dimFlat, null, UIStyleKit.Void.WithAlpha(0.45f));

        if (useArtFrame && artFrame == null) artFrame = Resources.Load<Sprite>(ArtFramePath);
        if (useArtFrame && artFrame != null) BuildArtCard(hFont, bFont);
        else BuildLaceCard(hFont, bFont);

        // 4. Область прокрутки внутри карточки, текст мягко тает сверху и снизу
        var viewport = scroll.viewport;
        if (viewport != null)
        {
            var mask = viewport.GetComponent<Mask>();
            if (mask != null) mask.enabled = false;
            var vpImg = viewport.GetComponent<Image>();
            if (vpImg != null) vpImg.enabled = false;
            UIStyleKit.GetOrAdd<RectMask2D>(viewport.gameObject).softness = new Vector2Int(0, 36);

            UIStyleKit.Place(viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), vpPos, vpSize);
            viewport.SetAsLastSibling();
        }

        // 5. Контент растёт по высоте текста
        var content = scroll.content;
        if (content != null)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = UIStyleKit.GetOrAdd<VerticalLayoutGroup>(content.gameObject);
            layout.padding = new RectOffset(8, 24, 30, 36);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = UIStyleKit.GetOrAdd<ContentSizeFitter>(content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // 6. Текст истории
        if (text != null)
        {
            var extra = text.GetComponent<VerticalLayoutGroup>();
            if (extra != null) extra.enabled = false;

            text.font = bFont;
            text.fontSize = 29;
            text.color = UIStyleKit.GloomText;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 10f;
            text.paragraphSpacing = 16f;
            text.margin = Vector4.zero;
            text.raycastTarget = false;
        }

        // 7. Скроллбар
        if (scroll.verticalScrollbar != null)
            StyleScrollbar(scroll.verticalScrollbar, viewport);

        if (scroll.horizontalScrollbar != null)
        {
            scroll.horizontalScrollbar.gameObject.SetActive(false);
            scroll.horizontalScrollbar = null;
        }

        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;
        scroll.scrollSensitivity = 40f;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    // ---------------- Процедурная кружевная карточка (прежний вид) ----------------

    private void BuildLaceCard(TMP_FontAsset hFont, TMP_FontAsset bFont)
    {
        // 2. Кружевная карточка
        var card = UIStyleKit.Child(transform, "Card", 1);
        UIStyleKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, cardSize);
        var paper = UIStyleKit.LacePanel(card, "Lace", 1f, UIStyleKit.GloomPaper.WithAlpha(0.96f),
            UIStyleKit.GloomLace, UIStyleKit.GloomStitch.WithAlpha(0.55f));
        var scan = UIStyleKit.Child(paper.rectTransform, "Scanlines");
        UIStyleKit.Stretch(scan, 2f);
        var sc = UIStyleKit.AddImage(scan, UIStyleKit.Scanlines(), Color.black.WithAlpha(0.18f), Image.Type.Tiled);
        sc.pixelsPerUnitMultiplier = 0.6f;

        float top = cardSize.y / 2f;

        // 3. Заголовок, орнамент, подсказка
        var head = UIStyleKit.MakeText(card, "Title", title, hFont, 40, UIStyleKit.GloomText, TextAlignmentOptions.Center);
        UIStyleKit.Place(head.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, top - 92f), new Vector2(cardSize.x - 200f, 56f));
        head.fontStyle = FontStyles.Bold;
        head.characterSpacing = 3f;
        UIStyleKit.FitWidth(head, cardSize.x - 260f, 40f, 20f);

        UIStyleKit.Divider(card, new Vector2(0f, top - 136f), cardSize.x - 240f, UIStyleKit.GloomStitch, UIStyleKit.GloomPaper);

        var hintText = UIStyleKit.MakeText(card, "Hint", hint, bFont, 20, UIStyleKit.GloomMuted, TextAlignmentOptions.Center);
        UIStyleKit.Place(hintText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -top + 72f), new Vector2(cardSize.x - 200f, 30f));
        hintText.fontStyle = FontStyles.Italic;

        // Крестик
        var close = UIStyleKit.MakeText(card, "Close", "×", hFont, 54, UIStyleKit.GloomMuted, TextAlignmentOptions.Center);
        UIStyleKit.Place(close.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(cardSize.x / 2f - 86f, top - 90f), new Vector2(60f, 60f));
        close.raycastTarget = true;
        var closeBtn = close.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = close;
        closeBtn.navigation = new Navigation { mode = Navigation.Mode.None };
        var cb = closeBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = UIStyleKit.GloomAccent;
        cb.pressedColor = UIStyleKit.BloodBright;
        cb.selectedColor = Color.white;
        closeBtn.colors = cb;
        closeBtn.onClick.AddListener(() =>
        {
            var ext = FindFirstObjectByType<DialogueExtensions>();
            if (ext != null) ext.CloseBacklog(); else gameObject.SetActive(false);
        });

        const float vpTop = 170f, vpBottom = 110f;
        vpPos = new Vector2(-20f, (vpBottom - vpTop) / 2f);
        vpSize = new Vector2(cardSize.x - 240f, cardSize.y - vpTop - vpBottom);
        sbPos = new Vector2(cardSize.x / 2f - 100f, vpPos.y);
        sbSize = new Vector2(18f, vpSize.y - 20f);
    }

    // ---------------- Рисованная карточка: розы, банты, «История», крестик-ромб ----------------

    private void BuildArtCard(TMP_FontAsset hFont, TMP_FontAsset bFont)
    {
        var size = artFrame.rect.size;
        var card = UIStyleKit.Child(transform, "Card", 1);
        UIStyleKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

        var shadow = UIStyleKit.Child(card, "Shadow");
        UIStyleKit.Stretch(shadow, -60f);
        UIStyleKit.AddImage(shadow, UIStyleKit.SoftCircle(), Color.black.WithAlpha(0.6f));

        var frameRt = UIStyleKit.Child(card, "Frame");
        UIStyleKit.Stretch(frameRt);
        UIStyleKit.AddImage(frameRt, artFrame, Color.white);

        // перевод «px от левого верхнего угла картинки» → координаты от центра карточки
        Vector2 C(float x, float y) => new Vector2(x - size.x / 2f, size.y / 2f - y);

        vpPos = C(ArtViewport.center.x, ArtViewport.center.y);
        vpSize = ArtViewport.size;
        sbPos = C(ArtScrollbar.center.x, ArtScrollbar.center.y);
        sbSize = ArtScrollbar.size;

        // подсказка — под рамкой
        var hintText = UIStyleKit.MakeText(card, "Hint", hint, bFont, 20, UIStyleKit.GloomMuted, TextAlignmentOptions.Center);
        UIStyleKit.Place(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(size.x - 200f, 30f));
        hintText.fontStyle = FontStyles.Italic;

        // крестик нарисован в ромбе; поверх — невидимая кнопка с розовым свечением при наведении
        var close = UIStyleKit.Child(card, "Close");
        UIStyleKit.Place(close, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), C(ArtClose.x, ArtClose.y), new Vector2(96f, 96f));
        var glow = UIStyleKit.AddImage(close, UIStyleKit.SoftCircle(), UIStyleKit.Hex("FF4FA3").WithAlpha(0.6f));
        glow.raycastTarget = true;
        var closeBtn = close.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = glow;
        closeBtn.navigation = new Navigation { mode = Navigation.Mode.None };
        var cb = closeBtn.colors;
        cb.normalColor = new Color(1f, 1f, 1f, 0f);
        cb.highlightedColor = Color.white;
        cb.pressedColor = UIStyleKit.BloodBright;
        cb.selectedColor = new Color(1f, 1f, 1f, 0f);
        cb.fadeDuration = 0.12f;
        closeBtn.colors = cb;
        closeBtn.onClick.AddListener(CloseFromButton);
    }

    private void CloseFromButton()
    {
        var ext = FindFirstObjectByType<DialogueExtensions>();
        if (ext != null) ext.CloseBacklog(); else gameObject.SetActive(false);
    }

    /// Скроллбар: розовая «ленточка» с кружевной дорожкой и сердечком на ручке
    private void StyleScrollbar(Scrollbar bar, RectTransform viewport)
    {
        var rt = (RectTransform)bar.transform;
        UIStyleKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sbPos, sbSize);
        bar.direction = Scrollbar.Direction.BottomToTop;
        bar.navigation = new Navigation { mode = Navigation.Mode.None };
        rt.SetAsLastSibling();

        // Дорожка: светлая полоска с розовым контуром
        var track = bar.GetComponent<Image>();
        if (track != null)
        {
            track.sprite = UIStyleKit.Rounded(9);
            track.type = Image.Type.Sliced;
            track.color = UIStyleKit.GloomPlate;
        }
        var outline = UIStyleKit.Child(rt, "Outline", 0);
        UIStyleKit.Stretch(outline);
        UIStyleKit.AddImage(outline, UIStyleKit.Rounded(9, 2, 0f), UIStyleKit.GloomStitch.WithAlpha(0.6f), Image.Type.Sliced);

        if (bar.handleRect != null && bar.handleRect.parent is RectTransform area)
        {
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;
            area.SetAsLastSibling();
            bar.handleRect.offsetMin = new Vector2(2f, 2f);
            bar.handleRect.offsetMax = new Vector2(-2f, -2f);

            var handle = bar.handleRect.GetComponent<Image>();
            if (handle != null)
            {
                handle.sprite = UIStyleKit.Rounded(7);
                handle.type = Image.Type.Sliced;
                handle.color = Color.white;
                bar.targetGraphic = handle;
            }

            // сердечко по центру ручки
            var heart = UIStyleKit.Child(bar.handleRect, "Heart");
            UIStyleKit.Place(heart, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));
            UIStyleKit.AddImage(heart, UIStyleKit.Heart(), UIStyleKit.GloomPaper);
        }

        var c = bar.colors;
        c.normalColor = UIStyleKit.GloomLace;
        c.highlightedColor = UIStyleKit.GloomStitch;
        c.pressedColor = UIStyleKit.GloomAccent;
        c.selectedColor = UIStyleKit.GloomLace;
        c.fadeDuration = 0.12f;
        bar.colors = c;
    }
}
