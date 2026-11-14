using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Главное меню в стиле «Академия Астра». Повесить на MainMenuCanvas.
/// Объекты ищутся по именам из сцены (Button_NewGame, Text_GameTitle и т.д.).
public class MainMenuStyler : MonoBehaviour
{
    [Header("Фон (перетащите сюда fon из Assets/Sprites)")]
    [SerializeField] private Sprite backgroundArt;

    [Header("Шрифты (необязательно — те же, что в окне входа)")]
    [SerializeField] private TMP_FontAsset headingFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    [Header("Тексты")]
    [SerializeField] private string title = "AFTERGLOW";
    [SerializeField] private string subtitle = "Академия «Астра» // клубная комната";
    [SerializeField] private string exitCaption = "ОШИБКА // НЕ УХОДИ";
    [SerializeField] private string exitLabel = "ВЫЙТИ";

    [Header("Окно меню")]
    [SerializeField] private Vector2 cardPosition = new Vector2(-430f, -10f);
    [SerializeField] private Vector2 cardSize = new Vector2(700f, 760f);

    [Header("Ярлычок «Вы вошли как»")]
    [Tooltip("Прозрачность в покое (0 — не видно, 1 — как было)")]
    [SerializeField, Range(0f, 1f)] private float profileAlpha = 0.6f;
    [Tooltip("Прозрачность при наведении мыши")]
    [SerializeField, Range(0f, 1f)] private float profileHoverAlpha = 0.9f;
    [Tooltip("Насколько заметен прямоугольник под текстом")]
    [SerializeField, Range(0f, 1f)] private float profilePlateAlpha = 0.35f;
    [SerializeField] private float profileFontSize = 18f;

    private static readonly string[] CuteButtons = { "Button_NewGame", "Button_LoadGame", "Button_Settings" };

    private RectTransform profileTag;
    private TextMeshProUGUI welcome;
    private CanvasGroup profileGroup;
    private Canvas rootCanvas;

    private void Awake()
    {
        BuildBackground();

        var window = UIStyleKit.FindDeep(transform, "MainMenuWindow") as RectTransform;
        if (window == null) { Debug.LogWarning("[MainMenuStyler] Не найден MainMenuWindow"); return; }

        // Окно-карточка с кружевом
        var card = UIStyleKit.Child(window, "Card", 0);
        UIStyleKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), cardPosition, cardSize);
        UIStyleKit.LacePanel(card, "Lace", 1f, UIStyleKit.Paper);

        float top = cardPosition.y + cardSize.y / 2f;
        float x = cardPosition.x;

        StyleTitle(window, x, top);
        UIStyleKit.Divider(window, new Vector2(x, top - 238f), cardSize.x - 180f);

        float y = top - 320f;
        foreach (var name in CuteButtons)
        {
            var btn = Find<Button>(name);
            if (btn == null) continue;
            StyleCuteButton(btn, new Vector2(x, y));
            y -= 96f;
        }

        var exit = Find<Button>("Button_Exit");
        if (exit != null) StyleHorrorButton(exit, new Vector2(x, y - 34f));

        StyleProfile();
        StartCoroutine(Intro(window));
    }

    // ---------------- Фон ----------------

    private void BuildBackground()
    {
        var bg = UIStyleKit.Child(transform, "BG_Art", 0);
        UIStyleKit.Stretch(bg);
        var art = bg.gameObject.AddComponent<Image>();
        UIStyleKit.Backdrop(art, backgroundArt, transform, 1); // пусто → возьмётся Assets/Sprites/fon
    }

    // ---------------- Заголовок ----------------

    private void StyleTitle(Transform window, float x, float top)
    {
        var t = Find<TextMeshProUGUI>("Text_GameTitle");
        if (t == null) return;

        if (headingFont != null) t.font = headingFont;
        UIStyleKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, top - 130f), new Vector2(cardSize.x - 120f, 110f));
        t.text = title;
        t.fontSize = 88;
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 10f;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableVertexGradient = false;
        t.color = UIStyleKit.Ink;
        UIStyleKit.FitWidth(t, cardSize.x - 160f, 88f, 40f);

        var glitch = t.gameObject.AddComponent<GlitchText>();
        glitch.target = t;
        glitch.minInterval = 7f;
        glitch.maxInterval = 16f;
        glitch.strength = 7f;

        var sub = UIStyleKit.MakeText(window, "Text_Subtitle", subtitle, bodyFont ? bodyFont : t.font,
            26, UIStyleKit.RoseDeep.WithAlpha(0.85f), TextAlignmentOptions.Center);
        UIStyleKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(x, top - 196f), new Vector2(cardSize.x - 120f, 40f));
        sub.fontStyle = FontStyles.Italic;
        UIStyleKit.FitWidth(sub, cardSize.x - 160f, 26f);
    }

    // ---------------- Милые кнопки ----------------

    private void StyleCuteButton(Button btn, Vector2 pos)
    {
        var rt = (RectTransform)btn.transform;
        UIStyleKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(470f, 80f));

        // Корень кнопки = кружево, поверх него — розовая заливка
        var lace = btn.GetComponent<Image>();
        lace.sprite = UIStyleKit.LaceFrame();
        lace.type = Image.Type.Tiled;
        lace.pixelsPerUnitMultiplier = 2.2f;
        lace.color = UIStyleKit.Lace;

        var fillRt = UIStyleKit.Child(rt, "Fill", 0);
        UIStyleKit.Stretch(fillRt, UIStyleKit.LaceInset / 2.2f);
        var fill = UIStyleKit.AddImage(fillRt, UIStyleKit.Rounded(10), UIStyleKit.ButtonPink, Image.Type.Sliced);

        var shine = UIStyleKit.Child(fillRt, "Shine");
        UIStyleKit.Stretch(shine, 4f);
        UIStyleKit.AddImage(shine, UIStyleKit.Rounded(8, 2, 0f), Color.white.WithAlpha(0.55f), Image.Type.Sliced);

        btn.targetGraphic = fill;
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.normalColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        cb.highlightedColor = Color.white;
        cb.pressedColor = UIStyleKit.RoseLine;
        cb.selectedColor = cb.normalColor;
        cb.disabledColor = new Color(0.85f, 0.8f, 0.82f, 0.7f);
        cb.fadeDuration = 0.12f;
        btn.colors = cb;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            if (bodyFont != null) label.font = bodyFont;
            label.text = label.text.Trim();
            label.fontSize = 30;
            label.fontStyle = FontStyles.Normal;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            UIStyleKit.Stretch(label.rectTransform);
            label.transform.SetAsLastSibling();
            UIStyleKit.FitWidth(label, 470f - 80f, 30f);

            // мягкая розовая тень, чтобы белый текст читался на розовом
            var mat = label.fontMaterial;
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", UIStyleKit.RoseDeep.WithAlpha(0.55f));
            mat.SetFloat("_UnderlayOffsetY", -0.6f);
            mat.SetFloat("_UnderlaySoftness", 0.35f);
            label.fontMaterial = mat;
            label.UpdateMeshPadding();
        }

        btn.gameObject.AddComponent<LaceButtonFx>().Init(btn, label);
    }

    // ---------------- Хоррор-кнопка «Выйти» ----------------

    private void StyleHorrorButton(Button btn, Vector2 pos)
    {
        var rt = (RectTransform)btn.transform;
        UIStyleKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(470f, 104f));
        rt.localRotation = Quaternion.Euler(0, 0, -1.2f); // чуть «криво висит»

        var bg = btn.GetComponent<Image>();
        bg.sprite = UIStyleKit.Rounded(6);
        bg.type = Image.Type.Sliced;
        bg.color = UIStyleKit.Void;

        // пятна «крови»
        var stains = UIStyleKit.Child(rt, "Stains", 0);
        UIStyleKit.Stretch(stains, 4f);
        stains.gameObject.AddComponent<RectMask2D>();
        var rng = new System.Random(1934);
        for (int i = 0; i < 7; i++)
        {
            var s = UIStyleKit.Child(stains, "Stain");
            float size = 30f + (float)rng.NextDouble() * 70f;
            UIStyleKit.Place(s, new Vector2((float)rng.NextDouble(), (float)rng.NextDouble()),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 1.3f, size));
            UIStyleKit.AddImage(s, UIStyleKit.SoftCircle(), UIStyleKit.Blood.WithAlpha(0.55f + (float)rng.NextDouble() * 0.35f));
        }

        var scanRt = UIStyleKit.Child(rt, "Scanlines", 1);
        UIStyleKit.Stretch(scanRt, 4f);
        var scan = UIStyleKit.AddImage(scanRt, UIStyleKit.Scanlines(), UIStyleKit.BloodBright.WithAlpha(0.1f), Image.Type.Tiled);
        scan.pixelsPerUnitMultiplier = 0.6f;

        var rimRt = UIStyleKit.Child(rt, "Rim", 2);
        UIStyleKit.Stretch(rimRt);
        var rim = UIStyleKit.AddImage(rimRt, UIStyleKit.Rounded(6, 3, 0f), UIStyleKit.BloodBright, Image.Type.Sliced);

        btn.targetGraphic = bg;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.85f, 0.85f);
        cb.pressedColor = new Color(0.7f, 0.5f, 0.5f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        TMP_FontAsset font = headingFont;
        var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            if (font == null) font = label.font;
            label.font = font;
            label.text = exitLabel;
            label.fontSize = 40;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 4f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            UIStyleKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f), new Vector2(440f, 56f));
            label.transform.SetAsLastSibling();
            UIStyleKit.FitWidth(label, 400f, 40f);
        }

        var caption = UIStyleKit.MakeText(rt, "Caption", exitCaption, font, 16, UIStyleKit.BloodBright, TextAlignmentOptions.Center);
        UIStyleKit.Place(caption.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 26f), new Vector2(440f, 22f));
        caption.fontStyle = FontStyles.Bold;
        caption.characterSpacing = 6f;
        UIStyleKit.FitWidth(caption, 400f, 16f, 10f);

        GlitchText glitch = null;
        if (label != null)
        {
            glitch = label.gameObject.AddComponent<GlitchText>();
            glitch.target = label;
            glitch.minInterval = 3f;
            glitch.maxInterval = 8f;
            glitch.strength = 8f;
            glitch.lingerChance = 0.4f; // «ВЫЙТЫ» иногда задерживается
        }

        var fx = btn.gameObject.AddComponent<HorrorButtonFx>();
        fx.glitch = glitch;
        fx.scanlines = scan;
        fx.rim = rim;
    }

    // ---------------- Ярлычок профиля ----------------

    private void StyleProfile()
    {
        var panel = UIStyleKit.FindDeep(transform, "PlayerProfilePanel") as RectTransform;
        welcome = Find<TextMeshProUGUI>("Text_WelcomeUser");
        if (panel == null || welcome == null) return;

        UIStyleKit.Place(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -40f), new Vector2(520f, 76f));

        profileTag = UIStyleKit.Child(panel, "Tag", 0);
        profileTag.anchorMin = profileTag.anchorMax = new Vector2(1f, 0.5f);
        profileTag.pivot = new Vector2(1f, 0.5f);
        profileTag.anchoredPosition = Vector2.zero;
        profileTag.sizeDelta = new Vector2(400f, 44f);
        profileGroup = profileTag.gameObject.AddComponent<CanvasGroup>();
        profileGroup.alpha = profileAlpha;
        profileGroup.blocksRaycasts = false; // не мешает кликам
        rootCanvas = profileTag.GetComponentInParent<Canvas>();
        // без кружева: просто еле видный тёмный прямоугольник
        var plate = UIStyleKit.AddImage(profileTag, UIStyleKit.Rounded(8), UIStyleKit.GloomPaper.WithAlpha(profilePlateAlpha), Image.Type.Sliced);
        plate.raycastTarget = false;

        if (bodyFont != null) welcome.font = bodyFont;
        welcome.rectTransform.SetParent(profileTag, false);
        UIStyleKit.Stretch(welcome.rectTransform);
        welcome.fontSize = profileFontSize;
        welcome.alignment = TextAlignmentOptions.Center;
        welcome.textWrappingMode = TextWrappingModes.NoWrap;
        welcome.margin = Vector4.zero;

        StartCoroutine(FixWelcomeColor());
    }

    // MainMenuUI красит текст в белый/красный — приводим к палитре (светлый текст на тёмной плашке)
    private IEnumerator FixWelcomeColor()
    {
        yield return null;
        if (welcome == null) yield break;
        bool blocked = welcome.color.g < 0.5f && welcome.color.r > 0.5f;
        welcome.color = blocked ? UIStyleKit.BloodBright : UIStyleKit.GloomText;
        if (blocked)
        {
            var g = welcome.gameObject.AddComponent<GlitchText>();
            g.target = welcome;
            g.strength = 4f;
        }
    }

    private void LateUpdate()
    {
        // ярлычок подстраивается под длину имени
        if (profileTag != null && welcome != null)
            profileTag.sizeDelta = new Vector2(welcome.preferredWidth + 40f, profileTag.sizeDelta.y);

        // в покое ярлычок полупрозрачный, при наведении мыши плавно проявляется
        if (profileGroup != null)
        {
            bool hover = false;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
                hover = RectTransformUtility.RectangleContainsScreenPoint(profileTag, mouse.position.ReadValue(), cam);
            }
            float target = hover ? profileHoverAlpha : profileAlpha;
            profileGroup.alpha = Mathf.MoveTowards(profileGroup.alpha, target, Time.unscaledDeltaTime * 3f);
        }
    }

    // ---------------- Появление ----------------

    private IEnumerator Intro(RectTransform window)
    {
        var group = UIStyleKit.GetOrAdd<CanvasGroup>(window.gameObject);
        Vector3 baseScale = window.localScale;
        const float dur = 0.7f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / dur, 3f);
            group.alpha = k;
            window.localScale = baseScale * Mathf.Lerp(0.97f, 1f, k);
            yield return null;
        }
        group.alpha = 1f;
        window.localScale = baseScale;
    }

    private T Find<T>(string name) where T : Component
    {
        var tr = UIStyleKit.FindDeep(transform, name);
        if (tr == null) { Debug.LogWarning($"[MainMenuStyler] Не найден объект {name}"); return null; }
        return tr.GetComponent<T>();
    }
}
