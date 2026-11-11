using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Стиль «Академия Астра»: розовая бумага с кружевом + хоррор-вставки (кровь, глитч).
/// Все спрайты рисуются кодом — импортировать картинки не нужно.
public static class UIStyleKit
{
    // ---- Милая сторона ----
    public static readonly Color Paper      = Hex("F8DCE3"); // розовая бумага
    public static readonly Color Lace       = Hex("FDF2F5"); // кружево
    public static readonly Color RoseLine   = Hex("D99AAD"); // линии, рамки, орнамент
    public static readonly Color RoseDeep   = Hex("B23A5A"); // акцент (имена, выделение)
    public static readonly Color ButtonPink = Hex("EAAFBF"); // кнопки
    public static readonly Color Ink        = Hex("2B1B20"); // заголовки
    public static readonly Color InkSoft    = Hex("4A3238"); // основной текст
    public static readonly Color Placeholder= Hex("D48CA2"); // подсказки

    // ---- Хоррор-сторона ----
    public static readonly Color Blood      = Hex("8E0F1D");
    public static readonly Color BloodBright= Hex("E2263B");
    public static readonly Color Void       = Hex("140508");
    public static readonly Color Wine       = Hex("2A0710");
    public static readonly Color GlitchCyan = Hex("5FE3F2");

    // ---- Мрачная сторона (диалоги): та же роза, но в темноте ----
    public static readonly Color GloomLace   = Hex("8E4A60"); // кружево, потемневшее
    public static readonly Color GloomPaper  = Hex("1C0B12"); // почти чёрная винная бумага
    public static readonly Color GloomPlate  = Hex("2A0F1A"); // плашки вариантов
    public static readonly Color GloomStitch = Hex("B5546F"); // строчка
    public static readonly Color GloomTag    = Hex("6E1D36"); // ярлычок имени, выбранный вариант
    public static readonly Color GloomText   = Hex("F2D6DF"); // текст
    public static readonly Color GloomMuted  = Hex("B88A9A"); // второстепенный текст
    public static readonly Color GloomAccent = Hex("E0577F"); // сердечки, имена

    /// Масштаб кружева: текстура рисуется в 2x, multiplier 1 = крупное, 2 = мелкое
    public const float LaceInset = 40f; // где в текстуре кружева начинается бумага (px)

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        c.a = alpha;
        return c;
    }

    public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }

    // =====================================================================
    // Спрайты
    // =====================================================================

    /// Скруглённый прямоугольник для 9-slice (border > 0 — обводка, fillAlpha — заливка внутри)
    public static Sprite Rounded(int radius, int border = 0, float fillAlpha = 1f)
    {
        string key = $"r{radius}_{border}_{fillAlpha}";
        if (TryCache(key, out var s)) return s;

        int size = radius * 2 + 6;
        var px = new Color32[size * size];
        float half = size / 2f, ext = half - 1f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float qx = Mathf.Abs(x + 0.5f - half) - (ext - radius);
            float qy = Mathf.Abs(y + 0.5f - half) - (ext - radius);
            float d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude
                      + Mathf.Min(Mathf.Max(qx, qy), 0) - radius; // <0 внутри

            float shape = Mathf.Clamp01(0.5f - d);
            float a = border > 0 ? Mathf.Lerp(fillAlpha, 1f, Mathf.Clamp01(border + 0.5f + d)) : fillAlpha;
            px[y * size + x] = White(shape * a);
        }

        float b = radius + 2;
        return Store(key, MakeSprite(px, size, size, new Vector4(b, b, b, b)));
    }

    /// Кружевная рамка с фестонами и «дырочками». Использовать с Image.Type.Tiled.
    public static Sprite LaceFrame()
    {
        const string key = "lace";
        if (TryCache(key, out var s)) return s;

        const int S = 192;          // текстура
        const float period = 32f;   // шаг фестонов
        const float bandStart = 20f, scallopInset = 18f, scallopR = 13f;
        const float eyeletR = 3.0f, holeInset = 29f, holeR = 2.6f;

        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            // (глубина от края, координата вдоль края) для 4 сторон
            float[] depth = { fx, S - fx, fy, S - fy };
            float[] along = { fy, fy, fx, fx };

            float inset = Mathf.Min(Mathf.Min(depth[0], depth[1]), Mathf.Min(depth[2], depth[3]));
            float shape = Mathf.Clamp01(inset - bandStart + 0.5f); // сплошная лента
            float hole = 0f;

            for (int e = 0; e < 4; e++)
            {
                float u = along[e];
                float cu = Mathf.Floor(u / period) * period + period / 2f;
                float dist = new Vector2(u - cu, depth[e] - scallopInset).magnitude;
                shape = Mathf.Max(shape, Mathf.Clamp01(scallopR + 0.5f - dist));
                hole = Mathf.Max(hole, Mathf.Clamp01(eyeletR + 0.5f - dist));

                // второй ряд мелких дырочек — между фестонами
                float cu2 = Mathf.Round(u / period) * period;
                float dist2 = new Vector2(u - cu2, depth[e] - holeInset).magnitude;
                if (depth[e] < 40f) hole = Mathf.Max(hole, Mathf.Clamp01(holeR + 0.5f - dist2));
            }

            px[y * S + x] = White(shape * (1f - hole));
        }

        return Store(key, MakeSprite(px, S, S, new Vector4(64, 64, 64, 64), TextureWrapMode.Clamp));
    }

    /// Мягкий круг (свечение, пятна крови)
    public static Sprite SoftCircle()
    {
        const string key = "soft";
        if (TryCache(key, out var s)) return s;
        const int n = 128;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            px[y * n + x] = White(t * t);
        }
        return Store(key, MakeSprite(px, n, n, Vector4.zero));
    }

    /// Сердечко
    public static Sprite Heart()
    {
        const string key = "heart";
        if (TryCache(key, out var s)) return s;
        const int n = 64, ss = 4;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int hits = 0;
            for (int sy = 0; sy < ss; sy++)
            for (int sx = 0; sx < ss; sx++)
            {
                float hx = ((x + (sx + 0.5f) / ss) / n * 2f - 1f) * 1.3f;
                float hy = ((y + (sy + 0.5f) / ss) / n * 2f - 1f) * 1.3f + 0.15f;
                float a = hx * hx + hy * hy - 1f;
                if (a * a * a - hx * hx * hy * hy * hy <= 0f) hits++;
            }
            px[y * n + x] = White(hits / (float)(ss * ss));
        }
        return Store(key, MakeSprite(px, n, n, Vector4.zero));
    }

    /// Виньетка: прозрачный центр, тёмные края
    public static Sprite Vignette()
    {
        const string key = "vignette";
        if (TryCache(key, out var s)) return s;
        const int n = 256;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
            px[y * n + x] = White(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, r)));
        }
        return Store(key, MakeSprite(px, n, n, Vector4.zero));
    }

    /// Сканлайны (для Image.Type.Tiled)
    public static Sprite Scanlines()
    {
        const string key = "scan";
        if (TryCache(key, out var s)) return s;
        var px = new Color32[4];
        px[0] = White(1f); px[1] = White(0.35f); px[2] = White(0f); px[3] = White(0f);
        return Store(key, MakeSprite(px, 1, 4, Vector4.zero, TextureWrapMode.Repeat, FilterMode.Point));
    }

    /// Линия, мягко тающая к обоим концам
    public static Sprite FadeLine()
    {
        const string key = "fadeline";
        if (TryCache(key, out var s)) return s;
        const int w = 256;
        var px = new Color32[w];
        for (int x = 0; x < w; x++)
            px[x] = White(Mathf.Pow(Mathf.Sin(Mathf.PI * (x + 0.5f) / w), 0.5f));
        return Store(key, MakeSprite(px, w, 1, Vector4.zero));
    }

    /// Вертикальный градиент: colors[0] — низ
    public static Sprite VerticalGradient(params Color[] colors)
    {
        const int h = 256;
        var px = new Color32[h];
        for (int y = 0; y < h; y++)
        {
            float f = y / (h - 1f) * (colors.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(f), colors.Length - 2);
            px[y] = colors.Length == 1 ? colors[0] : Color.Lerp(colors[i], colors[i + 1], f - i);
        }
        return MakeSprite(px, 1, h, Vector4.zero);
    }

    // =====================================================================
    // Готовые элементы
    // =====================================================================

    /// Кружевная панель: кружево → бумага → тонкая розовая рамка.
    /// laceScale: 1 — крупное кружево (окна), 2 — мелкое (кнопки, ярлычки).
    /// Возвращает бумагу (Image), поверх которой можно класть контент.
    public static Image LacePanel(Transform parent, string name, float laceScale, Color paper, int siblingIndex = -1)
        => LacePanel(parent, name, laceScale, paper, Lace, RoseLine.WithAlpha(0.8f), siblingIndex);

    /// То же, но со своими цветами кружева и строчки (для мрачного варианта)
    public static Image LacePanel(Transform parent, string name, float laceScale, Color paper, Color laceColor, Color stitchColor, int siblingIndex = -1)
    {
        var root = Child(parent, name, siblingIndex);
        Stretch(root);

        var lace = root.gameObject.AddComponent<Image>();
        lace.sprite = LaceFrame();
        lace.type = Image.Type.Tiled;
        lace.pixelsPerUnitMultiplier = laceScale;
        lace.color = laceColor;
        lace.raycastTarget = false;

        float inset = LaceInset / laceScale;
        var paperRt = Child(root, "Paper");
        Stretch(paperRt, inset);
        var paperImg = paperRt.gameObject.AddComponent<Image>();
        paperImg.sprite = Rounded(10);
        paperImg.type = Image.Type.Sliced;
        paperImg.color = paper;
        paperImg.raycastTarget = false;

        var lineRt = Child(paperRt, "Stitch");
        Stretch(lineRt, 6f);
        var line = lineRt.gameObject.AddComponent<Image>();
        line.sprite = Rounded(8, 2, 0f);
        line.type = Image.Type.Sliced;
        line.color = stitchColor;
        line.raycastTarget = false;

        return paperImg;
    }

    /// Разделитель-орнамент: линия + ромбик + две точки
    public static RectTransform Divider(Transform parent, Vector2 pos, float width)
        => Divider(parent, pos, width, RoseLine, Paper);

    public static RectTransform Divider(Transform parent, Vector2 pos, float width, Color lineColor, Color gapColor)
    {
        var root = Child(parent, "Divider");
        Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(width, 18f));

        var line = Child(root, "Line");
        Place(line, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 2f));
        AddImage(line, FadeLine(), lineColor);

        var bg = Child(root, "Gap");
        Place(bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 18f));
        AddImage(bg, Rounded(4), gapColor);

        var diamond = Child(root, "Diamond");
        Place(diamond, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11f, 11f));
        diamond.localRotation = Quaternion.Euler(0, 0, 45f);
        AddImage(diamond, Rounded(2), lineColor);

        for (int s = -1; s <= 1; s += 2)
        {
            var dot = Child(root, "Dot");
            Place(dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f * s, 0f), new Vector2(5f, 5f));
            AddImage(dot, SoftCircle(), lineColor);
        }
        return root;
    }

    public static Image AddImage(RectTransform rt, Sprite sprite, Color color, Image.Type type = Image.Type.Simple)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = type;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI MakeText(Transform parent, string name, string value, TMP_FontAsset font,
        float size, Color color, TextAlignmentOptions align)
    {
        var rt = Child(parent, name);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = value;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    /// Цветок сакуры (5 лепестков с вырезом на кончиках)
    public static Sprite Sakura()
    {
        const string key = "sakura";
        if (TryCache(key, out var s)) return s;
        const int n = 96, ss = 3;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int hits = 0;
            for (int sy = 0; sy < ss; sy++)
            for (int sx = 0; sx < ss; sx++)
            {
                float dx = (x + (sx + 0.5f) / ss) / n * 2f - 1f;
                float dy = (y + (sy + 0.5f) / ss) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float th = Mathf.Atan2(dy, dx) + Mathf.PI / 2f;
                float lobe = Mathf.Abs(Mathf.Cos(2.5f * th));
                float edge = 0.5f + 0.45f * Mathf.Pow(lobe, 0.7f) - 0.14f * Mathf.Pow(lobe, 60f); // вырез на кончике
                if (r <= edge * 0.98f) hits++;
            }
            px[y * n + x] = White(hits / (float)(ss * ss));
        }
        return Store(key, MakeSprite(px, n, n, Vector4.zero));
    }

    /// Фон экрана: арт (или тёмный градиент) + красная виньетка + сканлайны.
    /// target — Image, в которую положить арт; overlaysParent — куда добавить слои поверх.
    public static void Backdrop(Image target, Sprite art, Transform overlaysParent, int siblingIndex)
    {
        if (art == null) art = DefaultBackground();
        target.raycastTarget = false;
        if (art != null)
        {
            target.sprite = art;
            target.type = Image.Type.Simple;
            target.color = new Color(0.92f, 0.82f, 0.86f);
            var fit = GetOrAdd<AspectRatioFitter>(target.gameObject);
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = art.rect.width / art.rect.height;
        }
        else
        {
            target.sprite = VerticalGradient(Void, Wine, Hex("4A1426"));
            target.color = Color.white;
        }

        var vig = Child(overlaysParent, "BG_Vignette", siblingIndex);
        Stretch(vig);
        AddImage(vig, Vignette(), Wine.WithAlpha(0.9f));

        var scan = Child(overlaysParent, "BG_Scanlines", siblingIndex + 1);
        Stretch(scan);
        var s = AddImage(scan, Scanlines(), Color.black.WithAlpha(0.12f), Image.Type.Tiled);
        s.pixelsPerUnitMultiplier = 0.5f;
    }

    /// Уменьшает шрифт, если текст не влезает в maxWidth (считается точно, по метрикам шрифта)
    public static void FitWidth(TMP_Text t, float maxWidth, float maxSize, float minSize = 14f)
    {
        if (t == null) return;
        t.enableAutoSizing = false;
        t.fontSize = maxSize;
        float w = t.GetPreferredValues(t.text, float.PositiveInfinity, float.PositiveInfinity).x;
        if (w > maxWidth && w > 0f)
            t.fontSize = Mathf.Max(minSize, Mathf.Floor(maxSize * maxWidth / w));
    }

    /// Фон по умолчанию: Resources/fon, а в редакторе — прямо Assets/Sprites/fon.jpg
    public static Sprite DefaultBackground()
    {
        var s = Resources.Load<Sprite>("fon");
        if (s != null) return s;
#if UNITY_EDITOR
        foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/fon.jpg"))
            if (o is Sprite sp) return sp;
#endif
        return null;
    }

    // =====================================================================
    // Хелперы
    // =====================================================================

    public static RectTransform Child(Transform parent, string name, int siblingIndex = -1)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        if (siblingIndex >= 0) rt.SetSiblingIndex(siblingIndex);
        return rt;
    }

    public static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    public static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static Color32 White(float a) => new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));

    private static bool TryCache(string key, out Sprite s) => cache.TryGetValue(key, out s) && s != null;

    private static Sprite Store(string key, Sprite s) { cache[key] = s; return s; }

    private static Sprite MakeSprite(Color32[] px, int w, int h, Vector4 border,
        TextureWrapMode wrap = TextureWrapMode.Clamp, FilterMode filter = FilterMode.Bilinear)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = filter };
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
