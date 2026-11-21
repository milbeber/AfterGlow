using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Индикатор рассудка в мрачном стиле «Астра»: кружевная плашка в левом верхнем углу,
/// сердце-пульс (чем ниже рассудок, тем чаще бьётся), шкала с «призрачным» следом потери,
/// отметки порогов, статус через «//» и красная виньетка по краям экрана.
/// Подключается автоматически (AstraAutoStyle) на корневой Canvas игровой сцены.
public class SanityHUD : MonoBehaviour
{
    [Header("Шрифты (необязательно)")]
    [SerializeField] private TMP_FontAsset headingFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    [Header("Рисованная рамка (Assets/Resources/AfterGlowUI/SanityFrame.png)")]
    [Tooltip("Медальон с мозгом и шкала из арта. Выключите — вернётся процедурная плашка")]
    [SerializeField] private bool useArtFrame = true;
    [SerializeField] private Sprite artFrame;
    [SerializeField] private Vector2 artPosition = new Vector2(10f, -6f);

    [Header("Положение (процедурная плашка)")]
    [SerializeField] private Vector2 position = new Vector2(34f, -30f);
    [SerializeField] private Vector2 size = new Vector2(444f, 132f);

    [Header("Виньетка по краям экрана")]
    [SerializeField] private bool screenVignette = true;
    [Range(0f, 1f)] [SerializeField] private float warningVignette = 0.25f;
    [Range(0f, 1f)] [SerializeField] private float criticalVignette = 0.5f;

    [Header("Подписи")]
    [SerializeField] private string title = "РАССУДОК";
    [SerializeField] private string normalStatus = "ПСИХИКА // СТАБИЛЬНА";
    [SerializeField] private string warningStatus = "ПСИХИКА // ТРЕВОГА";
    [SerializeField] private string criticalStatus = "ОШИБКА // НЕ ВЕРЬ ГЛАЗАМ";
    [SerializeField] private string depletedStatus = "ПУСТО // ОСТАНЬСЯ СО МНОЙ";

    private PlayerStats stats;
    private RectTransform root, heart, fill, ghost, bar, barInner;
    private Image heartImg, halo, fillImg, ghostImg, flash, vignette, stitch;
    private TextMeshProUGUI titleText, valueText, statusText;
    private GlitchText titleGlitch;
    private CanvasGroup group;

    private float shown, ghostValue, ghostHold, shake, flashAlpha, vignetteKick, appear;
    private float beatPhase;
    private const float BarInset = 3f;

    // =====================================================================

    private void Start()
    {
        stats = PlayerStats.Instance;
        if (stats == null) { Debug.LogWarning("[SanityHUD] PlayerStats не найден — индикатор не построен"); enabled = false; return; }

        Build();

        shown = ghostValue = stats.NormalizedSanity;
        ApplyState(stats.CurrentState);

        stats.OnSanityChanged += HandleChanged;
        stats.OnSanityStateChanged += HandleState;
    }

    private void OnDestroy()
    {
        if (stats == null) return;
        stats.OnSanityChanged -= HandleChanged;
        stats.OnSanityStateChanged -= HandleState;
    }

    // ---------------- Построение ----------------

    private void Build()
    {
        var canvasRoot = (RectTransform)transform;

        // индекс сразу над фоном игровой сцены (под диалогом и историей)
        int index = 0;
        var bgTop = canvasRoot.Find("BG_Scanlines") ?? canvasRoot.Find("BG_Vignette") ?? canvasRoot.Find("BG_Art");
        if (bgTop != null) index = bgTop.GetSiblingIndex() + 1;

        if (screenVignette)
        {
            var v = UIStyleKit.Child(canvasRoot, "Sanity_Vignette", index++);
            UIStyleKit.Stretch(v);
            vignette = UIStyleKit.AddImage(v, UIStyleKit.Vignette(), UIStyleKit.BloodBright.WithAlpha(0f));
        }

        root = UIStyleKit.Child(canvasRoot, "SanityHUD", index);
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.alpha = 0f;

        if (useArtFrame && artFrame == null) artFrame = Resources.Load<Sprite>(ArtFramePath);
        if (useArtFrame && artFrame != null) { BuildArt(); return; }

        UIStyleKit.Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);

        // тень
        var shadow = UIStyleKit.Child(root, "Shadow");
        UIStyleKit.Stretch(shadow, -26f);
        UIStyleKit.AddImage(shadow, UIStyleKit.SoftCircle(), Color.black.WithAlpha(0.5f));

        // кружевная плашка
        var paper = UIStyleKit.LacePanel(root, "Lace", 2.2f, UIStyleKit.GloomPaper.WithAlpha(0.92f),
            UIStyleKit.GloomLace, UIStyleKit.GloomStitch.WithAlpha(0.55f));
        stitch = paper.transform.Find("Stitch")?.GetComponent<Image>();
        paper.gameObject.AddComponent<RectMask2D>();

        var ember = UIStyleKit.Child(paper.rectTransform, "Ember");
        UIStyleKit.Place(ember, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(220f, 200f));
        UIStyleKit.AddImage(ember, UIStyleKit.SoftCircle(), UIStyleKit.Blood.WithAlpha(0.35f));

        var scan = UIStyleKit.Child(paper.rectTransform, "Scanlines");
        UIStyleKit.Stretch(scan, 2f);
        var sc = UIStyleKit.AddImage(scan, UIStyleKit.Scanlines(), Color.black.WithAlpha(0.2f), Image.Type.Tiled);
        sc.pixelsPerUnitMultiplier = 0.6f;

        // красная вспышка при потере рассудка
        var flashRt = UIStyleKit.Child(paper.rectTransform, "Flash");
        UIStyleKit.Stretch(flashRt);
        flash = UIStyleKit.AddImage(flashRt, UIStyleKit.Rounded(10), UIStyleKit.BloodBright.WithAlpha(0f), Image.Type.Sliced);

        // сердце
        heart = UIStyleKit.Child(root, "Heart");
        UIStyleKit.Place(heart, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 2f), new Vector2(44f, 44f));
        var haloRt = UIStyleKit.Child(heart, "Halo");
        UIStyleKit.Stretch(haloRt, -22f);
        halo = UIStyleKit.AddImage(haloRt, UIStyleKit.SoftCircle(), UIStyleKit.BloodBright.WithAlpha(0.35f));
        var heartRt = UIStyleKit.Child(heart, "Icon");
        UIStyleKit.Stretch(heartRt);
        heartImg = UIStyleKit.AddImage(heartRt, UIStyleKit.Heart(), UIStyleKit.GloomAccent);

        const float left = 108f, right = 30f;

        // заголовок
        titleText = UIStyleKit.MakeText(root, "Title", title, headingFont, 19f, UIStyleKit.GloomMuted, TextAlignmentOptions.Left);
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 8f;
        TopRow(titleText.rectTransform, left, right + 70f, 26f, 28f);
        titleGlitch = titleText.gameObject.AddComponent<GlitchText>();
        titleGlitch.target = titleText;
        titleGlitch.minInterval = 1.5f;
        titleGlitch.maxInterval = 4f;
        titleGlitch.strength = 4f;
        titleGlitch.lingerChance = 0.3f;
        titleGlitch.enabled = false;

        // число
        valueText = UIStyleKit.MakeText(root, "Value", "100", headingFont, 32f, UIStyleKit.GloomText, TextAlignmentOptions.Right);
        valueText.fontStyle = FontStyles.Bold;
        TopRow(valueText.rectTransform, root.sizeDelta.x - right - 90f, right, 22f, 36f);
        Glow(valueText, UIStyleKit.BloodBright.WithAlpha(0.5f));

        // шкала
        bar = UIStyleKit.Child(root, "Bar");
        TopRow(bar, left, right, 62f, 18f);
        UIStyleKit.AddImage(bar, UIStyleKit.Rounded(8), UIStyleKit.Void, Image.Type.Sliced);
        barInner = UIStyleKit.Child(bar, "Inner");
        UIStyleKit.Stretch(barInner, BarInset);

        ghost = BarFill("Ghost", out ghostImg);
        ghostImg.color = UIStyleKit.BloodBright.WithAlpha(0.55f);
        fill = BarFill("Fill", out fillImg);

        // отметки порогов
        Tick(stats.WarningThreshold);
        Tick(stats.CriticalThreshold);

        var outline = UIStyleKit.Child(bar, "Outline");
        UIStyleKit.Stretch(outline);
        UIStyleKit.AddImage(outline, UIStyleKit.Rounded(8, 2, 0f), UIStyleKit.GloomStitch.WithAlpha(0.6f), Image.Type.Sliced);

        // статус
        statusText = UIStyleKit.MakeText(root, "Status", normalStatus, bodyFont, 15f, UIStyleKit.GloomMuted, TextAlignmentOptions.Left);
        statusText.characterSpacing = 4f;
        TopRow(statusText.rectTransform, left, right, 88f, 22f);
    }

    // ---------------- Рисованный вариант: медальон с мозгом + шкала в раме ----------------

    public const string ArtFramePath = "AfterGlowUI/SanityFrame";

    // Координаты внутри SanityFrame.png (px от левого верхнего угла)
    private static readonly Rect ArtSlot = new Rect(189f, 55f, 320f, 33f);   // прорезь шкалы
    private static readonly Vector2 ArtBrain = new Vector2(110f, 68f);       // центр медальона
    private static readonly Color Neon = UIStyleKit.Hex("FF4FA3");
    private bool art;

    private void BuildArt()
    {
        art = true;
        position = artPosition;
        var sz = artFrame.rect.size;
        UIStyleKit.Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), position, sz);

        // шкала — ПОД рамкой: края прорези аккуратно её обрезают
        bar = UIStyleKit.Child(root, "Bar");
        UIStyleKit.Place(bar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(ArtSlot.x, -ArtSlot.y), ArtSlot.size);
        UIStyleKit.AddImage(bar, null, UIStyleKit.Hex("0A0207"));
        barInner = UIStyleKit.Child(bar, "Inner");
        UIStyleKit.Stretch(barInner);

        ghost = BarFill("Ghost", out ghostImg);
        ghostImg.sprite = null;
        ghostImg.type = Image.Type.Simple;
        ghostImg.color = UIStyleKit.BloodBright.WithAlpha(0.6f);
        fill = BarFill("Fill", out fillImg);
        fillImg.sprite = UIStyleKit.VerticalGradient(UIStyleKit.Hex("8E1450"), Neon, UIStyleKit.Hex("FFB3D9"));
        fillImg.type = Image.Type.Simple;

        // блик по верху шкалы
        var gloss = UIStyleKit.Child(fill, "Gloss");
        gloss.anchorMin = new Vector2(0f, 0.55f);
        gloss.anchorMax = new Vector2(1f, 0.85f);
        gloss.offsetMin = gloss.offsetMax = Vector2.zero;
        UIStyleKit.AddImage(gloss, UIStyleKit.FadeLine(), Color.white.WithAlpha(0.25f));

        Tick(stats.WarningThreshold);
        Tick(stats.CriticalThreshold);

        var flashRt = UIStyleKit.Child(bar, "Flash");
        UIStyleKit.Stretch(flashRt);
        flash = UIStyleKit.AddImage(flashRt, null, UIStyleKit.BloodBright.WithAlpha(0f));

        // сама рамка
        var frameRt = UIStyleKit.Child(root, "Frame");
        UIStyleKit.Stretch(frameRt);
        UIStyleKit.AddImage(frameRt, artFrame, Color.white);

        // пульс мозга: неоновое свечение поверх медальона
        heart = UIStyleKit.Child(root, "BrainPulse");
        UIStyleKit.Place(heart, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(ArtBrain.x, -ArtBrain.y), new Vector2(150f, 150f));
        halo = UIStyleKit.AddImage(heart, UIStyleKit.SoftCircle(), Neon.WithAlpha(0f));
    }

    private RectTransform BarFill(string name, out Image img)
    {
        var rt = UIStyleKit.Child(barInner, name);
        UIStyleKit.Stretch(rt);
        rt.pivot = new Vector2(0f, 0.5f);
        img = UIStyleKit.AddImage(rt, UIStyleKit.Rounded(5), UIStyleKit.GloomAccent, Image.Type.Sliced);
        return rt;
    }

    private void Tick(float threshold)
    {
        float x = Mathf.InverseLerp(stats.MinSanity, stats.MaxSanity, threshold);
        var t = UIStyleKit.Child(barInner, "Tick");
        t.anchorMin = new Vector2(x, 0f);
        t.anchorMax = new Vector2(x, 1f);
        t.pivot = new Vector2(0.5f, 0.5f);
        t.sizeDelta = new Vector2(2f, 6f);
        t.anchoredPosition = Vector2.zero;
        UIStyleKit.AddImage(t, null, UIStyleKit.GloomLace.WithAlpha(0.9f));
    }

    /// Строка, прибитая к верху плашки: отступы слева/справа, отступ сверху, высота
    private static void TopRow(RectTransform rt, float left, float right, float top, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void Glow(TextMeshProUGUI t, Color color)
    {
        var m = t.fontMaterial;
        m.EnableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", color);
        m.SetFloat("_UnderlaySoftness", 0.6f);
        m.SetFloat("_UnderlayDilate", 0f);
        t.fontMaterial = m;
        t.UpdateMeshPadding();
    }

    // ---------------- Реакция на изменения ----------------

    private void HandleChanged(float value, float delta)
    {
        if (delta < 0f)
        {
            float k = Mathf.Clamp01(-delta / 30f);
            shake = Mathf.Max(shake, 6f + 10f * k);
            flashAlpha = Mathf.Max(flashAlpha, 0.25f + 0.35f * k);
            vignetteKick = Mathf.Max(vignetteKick, 0.2f + 0.3f * k);
            ghostHold = 0.45f; // красный след задерживается, потом «стекает»
            if (titleGlitch != null && titleGlitch.isActiveAndEnabled) titleGlitch.Trigger(0.3f);
        }
        SpawnDelta(delta);
    }

    private void HandleState(PlayerStats.SanityState s)
    {
        ApplyState(s);
        if (s >= PlayerStats.SanityState.Critical)
        {
            vignetteKick = 0.6f;
            shake = Mathf.Max(shake, 16f);
            if (titleGlitch != null) titleGlitch.Trigger(0.6f);
        }
    }

    private void ApplyState(PlayerStats.SanityState s)
    {
        if (statusText == null) return; // рисованный вариант: состояние видно по цвету и пульсу
        switch (s)
        {
            case PlayerStats.SanityState.Normal:
                statusText.text = normalStatus; statusText.color = UIStyleKit.GloomMuted; break;
            case PlayerStats.SanityState.Warning:
                statusText.text = warningStatus; statusText.color = UIStyleKit.GloomAccent; break;
            case PlayerStats.SanityState.Critical:
                statusText.text = criticalStatus; statusText.color = UIStyleKit.BloodBright; break;
            default:
                statusText.text = depletedStatus; statusText.color = UIStyleKit.BloodBright; break;
        }
        titleGlitch.enabled = s >= PlayerStats.SanityState.Critical;
        titleText.color = s >= PlayerStats.SanityState.Critical ? UIStyleKit.BloodBright : UIStyleKit.GloomMuted;
    }

    /// «-15» / «+10», всплывает над шкалой и тает
    private void SpawnDelta(float delta)
    {
        if (Mathf.Abs(delta) < 0.5f) return;
        var t = UIStyleKit.MakeText(root, "Delta", delta > 0 ? $"+{delta:0}" : $"-{-delta:0}", headingFont, 24f,
            delta > 0 ? UIStyleKit.GloomText : UIStyleKit.BloodBright, TextAlignmentOptions.Right);
        t.fontStyle = FontStyles.Bold;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(100f, 30f);
        rt.anchoredPosition = art ? new Vector2(-40f, -40f) : new Vector2(-110f, -38f);
        t.gameObject.AddComponent<SanityDeltaFloat>();
    }

    // ---------------- Анимация ----------------

    private void Update()
    {
        if (stats == null || root == null) return;
        float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;
        float target = stats.NormalizedSanity;
        var state = stats.CurrentState;

        appear = Mathf.MoveTowards(appear, 1f, dt * 2f);
        group.alpha = appear;

        // шкала догоняет значение, красный след — с задержкой
        shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-dt * 10f));
        if (ghostHold > 0f) ghostHold -= dt;
        else ghostValue = Mathf.MoveTowards(ghostValue, shown, dt * 0.5f);
        if (ghostValue < shown) ghostValue = shown;

        SetWidth(fill, shown);
        SetWidth(ghost, ghostValue);
        if (valueText != null)
            valueText.text = Mathf.RoundToInt(Mathf.Lerp(stats.MinSanity, stats.MaxSanity, shown)).ToString();

        // цвет шкалы: розовый → кровь; на критическом мерцает
        Color c = art
            ? Color.Lerp(UIStyleKit.Hex("FF8A9A"), Color.white, Mathf.InverseLerp(0.3f, 0.6f, target)) // градиент даёт неон, тут — оттенок
            : Color.Lerp(UIStyleKit.BloodBright, UIStyleKit.GloomAccent, Mathf.InverseLerp(0.3f, 0.6f, target));
        if (state >= PlayerStats.SanityState.Critical)
            c = Color.Lerp(UIStyleKit.Blood, UIStyleKit.BloodBright, 0.5f + 0.5f * Mathf.Sin(t * 9f)) * (Random.value < 0.03f ? 0.4f : 1f);
        c.a = 1f;
        fillImg.color = c;
        if (valueText != null)
            valueText.color = state >= PlayerStats.SanityState.Critical ? UIStyleKit.BloodBright : UIStyleKit.GloomText;

        // сердце: от 60 до 150 ударов в минуту, двойной «тук-тук»
        float bpm = Mathf.Lerp(150f, 60f, target);
        beatPhase = (beatPhase + dt * bpm / 60f) % 1f;
        float beat = Pulse(beatPhase, 0.0f) + 0.6f * Pulse(beatPhase, 0.18f);
        if (art)
        {
            // мозг светится в такт; чем хуже рассудок, тем ярче и краснее
            heart.localScale = Vector3.one * (0.9f + 0.15f * beat);
            Color glow = Color.Lerp(UIStyleKit.BloodBright, Neon, Mathf.InverseLerp(0.3f, 0.7f, target));
            halo.color = glow.WithAlpha(0.08f + (0.22f + 0.25f * (1f - target)) * beat);
        }
        else
        {
            heart.localScale = Vector3.one * (1f + 0.2f * beat);
            heartImg.color = Color.Lerp(UIStyleKit.BloodBright, UIStyleKit.GloomAccent, Mathf.InverseLerp(0.3f, 0.7f, target));
            halo.color = UIStyleKit.BloodBright.WithAlpha(0.2f + 0.35f * beat * (1.4f - target));
        }

        // встряска и вспышка
        shake = Mathf.MoveTowards(shake, 0f, dt * 40f);
        float jitter = state >= PlayerStats.SanityState.Critical && Random.value < 0.02f ? 3f : 0f;
        float s = shake + jitter;
        root.anchoredPosition = position + (s > 0f ? new Vector2(Random.Range(-s, s), Random.Range(-s, s) * 0.5f) : Vector2.zero);
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 1.2f);
        flash.color = UIStyleKit.BloodBright.WithAlpha(flashAlpha);

        if (stitch != null)
        {
            float a = 0.45f + 0.12f * Mathf.Sin(t * 1.3f);
            if (Random.value < (state >= PlayerStats.SanityState.Critical ? 0.03f : 0.004f)) a = 0.95f;
            stitch.color = (state >= PlayerStats.SanityState.Critical ? UIStyleKit.BloodBright : UIStyleKit.GloomStitch).WithAlpha(a);
        }

        // виньетка по краям экрана
        if (vignette != null)
        {
            float baseA = 0f;
            if (state == PlayerStats.SanityState.Warning) baseA = warningVignette;
            else if (state >= PlayerStats.SanityState.Critical) baseA = criticalVignette * (0.8f + 0.2f * beat);
            vignetteKick = Mathf.MoveTowards(vignetteKick, 0f, dt * 0.8f);
            float a = Mathf.Clamp01(baseA + vignetteKick);
            vignette.color = Color.Lerp(UIStyleKit.BloodBright, UIStyleKit.Blood, a).WithAlpha(a);
        }
    }

    private static float Pulse(float phase, float at)
    {
        float d = phase - at;
        return d < 0f || d > 0.15f ? 0f : Mathf.Sin(d / 0.15f * Mathf.PI);
    }

    private static void SetWidth(RectTransform rt, float v)
    {
        v = Mathf.Clamp01(v);
        rt.anchorMax = new Vector2(v, 1f);
        rt.offsetMax = Vector2.zero;
        rt.gameObject.SetActive(v > 0.005f);
    }
}

/// Всплывающая цифра «-15» над индикатором: поднимается и тает
public class SanityDeltaFloat : MonoBehaviour
{
    private float life;
    private TextMeshProUGUI text;
    private RectTransform rt;
    private Vector2 start;

    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        rt = (RectTransform)transform;
        start = rt.anchoredPosition;
    }

    private void Update()
    {
        life += Time.unscaledDeltaTime;
        float k = life / 1.3f;
        rt.anchoredPosition = start + new Vector2(0f, 34f * Mathf.Sqrt(Mathf.Clamp01(k)));
        text.alpha = 1f - Mathf.Clamp01((k - 0.4f) / 0.6f);
        if (k >= 1f) Destroy(gameObject);
    }
}
