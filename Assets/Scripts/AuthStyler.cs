using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Окна входа и регистрации в стиле «Академия Астра».
/// Повесить на AuthCanvas (рядом с AuthUI). Объекты ищутся по именам из префаба.
public class AuthStyler : MonoBehaviour
{
    [Header("Фон (перетащите сюда fon из Assets/Sprites)")]
    [SerializeField] private Sprite backgroundArt;

    [Header("Шрифты (необязательно)")]
    [SerializeField] private TMP_FontAsset headingFont;
    [SerializeField] private TMP_FontAsset bodyFont;

    [Header("Тексты")]
    [SerializeField] private string loginTitle = "АКАДЕМИЯ «АСТРА» // КЛУБНЫЙ КАБИНЕТ";
    [SerializeField] private string registerTitle = "АКАДЕМИЯ «АСТРА» // ЗАЧИСЛЕНИЕ";
    [SerializeField] private string creepyWhisper = "Не уходи. Останься со мной.";

    private TMP_FontAsset hFont, bFont;
    private TextMeshProUGUI loginError, regError;

    private void Awake()
    {
        if (backgroundArt == null) backgroundArt = UIStyleKit.DefaultBackground();
        // шрифт по умолчанию — тот, что уже стоит в префабе
        var anyText = GetComponentInChildren<TextMeshProUGUI>(true);
        TMP_FontAsset baseFont = anyText != null ? anyText.font : TMP_Settings.defaultFontAsset;
        hFont = headingFont != null ? headingFont : baseFont;
        bFont = bodyFont != null ? bodyFont : baseFont;

        // Фон: арт + виньетка + сканлайны, строго позади окон
        var bg = Find<Image>("Background");
        if (bg != null)
        {
            bg.transform.SetAsFirstSibling();
            UIStyleKit.Stretch(bg.rectTransform);
            UIStyleKit.Backdrop(bg, backgroundArt, transform, 1);
        }

        StyleLogin();
        StyleRegister();
    }

    // =====================================================================
    // Вход
    // =====================================================================

    private void StyleLogin()
    {
        var win = UIStyleKit.FindDeep(transform, "LoginWindow");
        if (win == null) return;

        var size = new Vector2(900f, 720f);
        float top = size.y / 2f;
        Card(win, size);

        Header(Find<TextMeshProUGUI>(win, "Text (TMP)"), win, loginTitle, top);

        Field(win, "Login_UsernameInput", "Логин / ID студента", "«Введите ваш ID…»", top - 196f, UIStyleKit.Sakura());
        Field(win, "Login_PasswordInput", "Пароль", "«Пароль безопасности…»", top - 326f, UIStyleKit.Heart());

        loginError = ErrorText(win, "Text_ErrorLog", top - 414f);

        var submit = Find<Button>(win, "Button_SubmitLogin");
        if (submit != null) CuteButton(submit, new Vector2(-190f, top - 514f), new Vector2(390f, 96f), "Войти в академию");

        CreepyPlate(win, new Vector2(205f, top - 514f), new Vector2(370f, 116f));

        var toReg = Find<Button>(win, "Button_ToRegister");
        if (toReg != null) LinkButton(toReg, new Vector2(0f, top - 628f), "Нет пропуска? Записаться в клуб");
    }

    // =====================================================================
    // Регистрация
    // =====================================================================

    private void StyleRegister()
    {
        var win = UIStyleKit.FindDeep(transform, "RegisterWindow");
        if (win == null) return;

        var size = new Vector2(900f, 840f);
        float top = size.y / 2f;
        Card(win, size);

        Header(Find<TextMeshProUGUI>(win, "Reg_Title"), win, registerTitle, top);

        Field(win, "Reg_UsernameInput", "Логин / ID студента", "«Придумайте ID студента…»", top - 196f, UIStyleKit.Sakura());
        Field(win, "Reg_EmailInput", "Почта", "«Адрес для писем клуба…»", top - 318f, UIStyleKit.Sakura());
        Field(win, "Reg_PasswordInput", "Пароль", "«Придумайте пароль…»", top - 440f, UIStyleKit.Heart());

        regError = ErrorText(win, "Text_RegErrorLog", top - 530f);

        var submit = Find<Button>(win, "Button_SubmitRegister");
        if (submit != null) CuteButton(submit, new Vector2(0f, top - 628f), new Vector2(500f, 96f), "Получить пропуск");

        var toLogin = Find<Button>(win, "Button_ToLogin");
        if (toLogin != null) LinkButton(toLogin, new Vector2(0f, top - 736f), "Уже есть пропуск? Войти");
    }

    // =====================================================================
    // Детали
    // =====================================================================

    private void Card(Transform win, Vector2 size)
    {
        var card = UIStyleKit.Child(win, "Card", 0);
        UIStyleKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        UIStyleKit.LacePanel(card, "Lace", 1f, UIStyleKit.Paper);
    }

    private void Header(TextMeshProUGUI t, Transform win, string text, float top)
    {
        if (t == null) t = UIStyleKit.MakeText(win, "Title", text, hFont, 36, UIStyleKit.Ink, TextAlignmentOptions.Center);
        t.font = hFont;
        t.text = text;
        t.fontSize = 36;
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 1f;
        t.color = UIStyleKit.Ink;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        UIStyleKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, top - 74f), new Vector2(740f, 56f));
        UIStyleKit.FitWidth(t, 720f, 36f, 18f);

        UIStyleKit.Divider(win, new Vector2(0f, top - 116f), 740f);
    }

    /// Поле ввода: подпись сверху, иконка слева, кружевная рамка, плейсхолдер в «кавычках»
    private void Field(Transform win, string name, string label, string placeholder, float y, Sprite icon)
    {
        var input = Find<TMP_InputField>(win, name);
        if (input == null) return;
        var rt = (RectTransform)input.transform;

        const float fieldX = 40f, fieldW = 600f, fieldH = 84f, lace = 2.4f;
        UIStyleKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(fieldX, y - 26f), new Vector2(fieldW, fieldH));

        // подпись
        var cap = UIStyleKit.MakeText(win, name + "_Label", label, bFont, 26, UIStyleKit.InkSoft, TextAlignmentOptions.Left);
        UIStyleKit.Place(cap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(fieldX - fieldW / 2f + 30f, y + 36f), new Vector2(fieldW, 36f));
        UIStyleKit.FitWidth(cap, fieldW - 40f, 26f);

        // иконка со свечением
        var glow = UIStyleKit.Child(win, name + "_IconGlow");
        UIStyleKit.Place(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-322f, y - 26f), new Vector2(110f, 110f));
        UIStyleKit.AddImage(glow, UIStyleKit.SoftCircle(), UIStyleKit.ButtonPink.WithAlpha(0.8f));
        var ic = UIStyleKit.Child(win, name + "_Icon");
        UIStyleKit.Place(ic, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-322f, y - 26f), new Vector2(60f, 60f));
        ic.localRotation = Quaternion.Euler(0, 0, -12f);
        UIStyleKit.AddImage(ic, icon, icon == UIStyleKit.Heart() ? UIStyleKit.RoseDeep : UIStyleKit.ButtonPink);
        if (icon == UIStyleKit.Sakura())
        {
            var dot = UIStyleKit.Child(ic, "Core");
            UIStyleKit.Place(dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
            UIStyleKit.AddImage(dot, UIStyleKit.SoftCircle(), UIStyleKit.RoseDeep);
        }

        // рамка: кружево + светлая бумага + строчка
        var frame = input.GetComponent<Image>();
        if (frame != null)
        {
            frame.sprite = UIStyleKit.LaceFrame();
            frame.type = Image.Type.Tiled;
            frame.pixelsPerUnitMultiplier = lace;
            frame.color = UIStyleKit.Lace;
        }
        var fillRt = UIStyleKit.Child(rt, "Fill", 0);
        UIStyleKit.Stretch(fillRt, UIStyleKit.LaceInset / lace);
        var fill = UIStyleKit.AddImage(fillRt, UIStyleKit.Rounded(10), UIStyleKit.Hex("FFF6F8"), Image.Type.Sliced);
        var stitch = UIStyleKit.Child(fillRt, "Stitch");
        UIStyleKit.Stretch(stitch, 4f);
        UIStyleKit.AddImage(stitch, UIStyleKit.Rounded(8, 2, 0f), UIStyleKit.RoseLine.WithAlpha(0.7f), Image.Type.Sliced);

        input.targetGraphic = fill;
        var cb = input.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.98f, 0.95f, 0.96f);
        cb.selectedColor = Color.white;
        cb.pressedColor = new Color(0.96f, 0.92f, 0.93f);
        cb.fadeDuration = 0.1f;
        input.colors = cb;

        input.customCaretColor = true;
        input.caretColor = UIStyleKit.RoseDeep;
        input.caretWidth = 2;
        input.selectionColor = UIStyleKit.ButtonPink.WithAlpha(0.6f);

        if (input.textViewport != null)
        {
            input.textViewport.SetAsLastSibling();
            input.textViewport.anchorMin = Vector2.zero;
            input.textViewport.anchorMax = Vector2.one;
            input.textViewport.offsetMin = new Vector2(UIStyleKit.LaceInset / lace + 22f, UIStyleKit.LaceInset / lace + 4f);
            input.textViewport.offsetMax = new Vector2(-(UIStyleKit.LaceInset / lace + 18f), -(UIStyleKit.LaceInset / lace + 4f));
        }

        if (input.textComponent is TextMeshProUGUI txt)
        {
            txt.font = bFont;
            txt.fontSize = 25;
            txt.color = UIStyleKit.Ink;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
        }
        input.pointSize = 25;

        if (input.placeholder is TextMeshProUGUI ph)
        {
            ph.font = bFont;
            ph.text = placeholder;
            ph.fontSize = 25;
            ph.fontStyle = FontStyles.Italic;
            ph.color = UIStyleKit.Placeholder;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            ph.textWrappingMode = TextWrappingModes.NoWrap;
            UIStyleKit.FitWidth(ph, fieldW - 2f * (UIStyleKit.LaceInset / lace) - 50f, 25f, 16f);
        }
    }

    private TextMeshProUGUI ErrorText(Transform win, string name, float y)
    {
        var t = Find<TextMeshProUGUI>(win, name);
        if (t == null) return null;
        t.font = bFont;
        t.fontSize = 22;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        UIStyleKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(760f, 40f));
        return t;
    }

    /// Розовая кнопка в кружеве
    private void CuteButton(Button btn, Vector2 pos, Vector2 size, string text)
    {
        var rt = (RectTransform)btn.transform;
        UIStyleKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        const float lace = 2.2f;

        var root = btn.GetComponent<Image>();
        root.sprite = UIStyleKit.LaceFrame();
        root.type = Image.Type.Tiled;
        root.pixelsPerUnitMultiplier = lace;
        root.color = UIStyleKit.Lace;

        var fillRt = UIStyleKit.Child(rt, "Fill", 0);
        UIStyleKit.Stretch(fillRt, UIStyleKit.LaceInset / lace);
        var fill = UIStyleKit.AddImage(fillRt, UIStyleKit.Rounded(10), UIStyleKit.ButtonPink, Image.Type.Sliced);
        var shine = UIStyleKit.Child(fillRt, "Shine");
        UIStyleKit.Stretch(shine, 4f);
        UIStyleKit.AddImage(shine, UIStyleKit.Rounded(8, 2, 0f), Color.white.WithAlpha(0.55f), Image.Type.Sliced);

        btn.targetGraphic = fill;
        var cb = btn.colors;
        cb.normalColor = new Color(0.95f, 0.95f, 0.95f);
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
            label.font = bFont;
            label.text = text;
            label.fontSize = 28;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            UIStyleKit.Stretch(label.rectTransform);
            label.transform.SetAsLastSibling();
            UIStyleKit.FitWidth(label, size.x - 2f * (UIStyleKit.LaceInset / lace) - 40f, 28f);

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

    /// Текстовая ссылка (переход между окнами)
    private void LinkButton(Button btn, Vector2 pos, string text)
    {
        var rt = (RectTransform)btn.transform;
        UIStyleKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(560f, 44f));

        var img = btn.GetComponent<Image>();
        if (img != null) { img.sprite = null; img.color = new Color(1, 1, 1, 0); }

        var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null) return;
        label.font = bFont;
        label.text = text;
        label.fontSize = 23;
        label.fontStyle = FontStyles.Underline;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        UIStyleKit.Stretch(label.rectTransform);
        UIStyleKit.FitWidth(label, 540f, 23f);

        btn.targetGraphic = label;
        var cb = btn.colors;
        cb.normalColor = UIStyleKit.RoseDeep;
        cb.highlightedColor = UIStyleKit.Blood;
        cb.pressedColor = UIStyleKit.Ink;
        cb.selectedColor = UIStyleKit.RoseDeep;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    /// «НЕ ВХОДИ / ОСТАВАЙСЯ СО МНОЙ» — кнопка, которая ничего не делает. Только сбоит.
    private void CreepyPlate(Transform win, Vector2 pos, Vector2 size)
    {
        var rt = UIStyleKit.Child(win, "Button_DontEnter");
        UIStyleKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        rt.localRotation = Quaternion.Euler(0, 0, 1.5f);

        var bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = UIStyleKit.Rounded(6);
        bg.type = Image.Type.Sliced;
        bg.color = UIStyleKit.Void;

        var stains = UIStyleKit.Child(rt, "Stains");
        UIStyleKit.Stretch(stains, 4f);
        stains.gameObject.AddComponent<RectMask2D>();
        var rng = new System.Random(1934);
        for (int i = 0; i < 8; i++)
        {
            var s = UIStyleKit.Child(stains, "Stain");
            float d = 30f + (float)rng.NextDouble() * 70f;
            UIStyleKit.Place(s, new Vector2((float)rng.NextDouble(), (float)rng.NextDouble()),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d * 1.3f, d));
            UIStyleKit.AddImage(s, UIStyleKit.SoftCircle(), UIStyleKit.Blood.WithAlpha(0.55f + (float)rng.NextDouble() * 0.35f));
        }

        var scanRt = UIStyleKit.Child(rt, "Scanlines");
        UIStyleKit.Stretch(scanRt, 4f);
        var scan = UIStyleKit.AddImage(scanRt, UIStyleKit.Scanlines(), UIStyleKit.BloodBright.WithAlpha(0.1f), Image.Type.Tiled);
        scan.pixelsPerUnitMultiplier = 0.6f;

        var rimRt = UIStyleKit.Child(rt, "Rim");
        UIStyleKit.Stretch(rimRt);
        var rim = UIStyleKit.AddImage(rimRt, UIStyleKit.Rounded(6, 3, 0f), UIStyleKit.BloodBright, Image.Type.Sliced);

        var main = UIStyleKit.MakeText(rt, "Main", "НЕ ВХОДИ", hFont, 40, Color.white, TextAlignmentOptions.Center);
        UIStyleKit.Place(main.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(size.x - 20f, 50f));
        main.fontStyle = FontStyles.Bold;
        main.characterSpacing = 3f;
        UIStyleKit.FitWidth(main, size.x - 50f, 40f);

        var cap = UIStyleKit.MakeText(rt, "Caption", "ОШИБКА // СБОЙ", hFont, 14, UIStyleKit.BloodBright, TextAlignmentOptions.Center);
        UIStyleKit.Place(cap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(size.x - 20f, 18f));
        cap.fontStyle = FontStyles.Bold;
        cap.characterSpacing = 6f;
        UIStyleKit.FitWidth(cap, size.x - 50f, 14f, 9f);

        var sub = UIStyleKit.MakeText(rt, "Sub", "ОСТАВАЙСЯ СО МНОЙ", hFont, 26, Color.white, TextAlignmentOptions.Center);
        UIStyleKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -32f), new Vector2(size.x - 20f, 34f));
        sub.fontStyle = FontStyles.Bold;
        UIStyleKit.FitWidth(sub, size.x - 50f, 26f);

        var glitch = main.gameObject.AddComponent<GlitchText>();
        glitch.target = main;
        glitch.minInterval = 2.5f;
        glitch.maxInterval = 7f;
        glitch.strength = 8f;
        glitch.lingerChance = 0.5f; // «НЕ ВХЭДИ» иногда задерживается на пару секунд

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        var cb = btn.colors;
        cb.highlightedColor = new Color(1f, 0.85f, 0.85f);
        cb.pressedColor = new Color(0.7f, 0.5f, 0.5f);
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            glitch.Trigger(0.6f);
            StopAllCoroutines();
            StartCoroutine(Whisper());
        });

        var fx = rt.gameObject.AddComponent<HorrorButtonFx>();
        fx.glitch = glitch;
        fx.scanlines = scan;
        fx.rim = rim;
    }

    private IEnumerator Whisper()
    {
        if (loginError == null) yield break;
        loginError.text = creepyWhisper;
        loginError.color = UIStyleKit.Blood;
        yield return new WaitForSecondsRealtime(2.2f);
        if (loginError.text == creepyWhisper) loginError.text = "";
    }

    // AuthUI красит сообщения в чистый красный/зелёный — переводим в палитру
    private void LateUpdate()
    {
        Recolor(loginError);
        Recolor(regError);
    }

    private static void Recolor(TextMeshProUGUI t)
    {
        if (t == null) return;
        var c = t.color;
        if (c.r > 0.9f && c.g < 0.1f && c.b < 0.1f) t.color = UIStyleKit.Blood;                 // ошибка
        else if (c.g > 0.9f && c.r < 0.1f && c.b < 0.1f) t.color = UIStyleKit.Hex("3F7F5F");     // успех
    }

    private T Find<T>(string name) where T : Component => Find<T>(transform, name);

    private static T Find<T>(Transform root, string name) where T : Component
    {
        var tr = UIStyleKit.FindDeep(root, name);
        if (tr == null) { Debug.LogWarning($"[AuthStyler] Не найден объект {name}"); return null; }
        return tr.GetComponent<T>();
    }
}
