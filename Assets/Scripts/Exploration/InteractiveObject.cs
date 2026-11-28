using TMPro;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Интерактивная зона на фоне локации (день 16).
/// Повесьте на пустой GameObject вместе с Collider2D (Box/Polygon),
/// расположенный поверх нужного места на ИИ-фоне.
/// По клику запускает Yarn-узел с текстовым комментарием.
/// На зоне только сердечко. При наведении от него расходится розовый свет и тает к краям.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[DefaultExecutionOrder(50)]
public class InteractiveObject : MonoBehaviour
{
    [Header("Yarn Spinner")]
    [Tooltip("Имя узла (title:) в .yarn файле, например Obj_Window")]
    [SerializeField] private string yarnNode = "Obj_Example";

    [Header("Подсветка")]
    [SerializeField] private SpriteRenderer highlightSprite;
    [SerializeField] private TMP_Text hoverLabel;

    [Header("Отладка")]
    [SerializeField] private bool drawGizmo = false;

    private SpriteRenderer halo;
    private SpriteRenderer heart;
    private bool hovered;
    private float hoverBlend;
    private float presence;
    private float phase;

    public string YarnNode => yarnNode;

    private void Awake()
    {
        phase = (name.GetHashCode() & 1023) / 1023f * Mathf.PI * 2f;
        EnsureVisuals();
    }

    /// <summary>Курсор наведён / ушёл (вызывает InteractionRaycaster).</summary>
    public void SetHover(bool isHovered) => hovered = isHovered;

    private void LateUpdate()
    {
        var col = GetComponent<BoxCollider2D>();
        Vector2 size = col != null ? col.size : Vector2.one;
        if (size.x < 0.05f || size.y < 0.05f) return;

        bool show = InteractionRaycaster.HeartsVisible;
        if (!show) hovered = false;

        float dt = Time.unscaledDeltaTime;
        presence = Mathf.MoveTowards(presence, show ? 1f : 0f, dt * 3.2f);
        hoverBlend = Mathf.MoveTowards(hoverBlend, hovered ? 1f : 0f, dt * 4.5f);

        // Пока курсор на зоне, волна гоняет свет: он растёт и одновременно тает.
        float wave = Mathf.Sin(Time.unscaledTime * 1.6f + phase) * 0.5f + 0.5f;
        float pool = Mathf.Min(size.x, size.y);
        float glow = hoverBlend * presence;

        float haloSpread = Mathf.Lerp(0.2f, Mathf.Lerp(1.05f, 1.75f, wave), glow);
        float coreSpread = Mathf.Lerp(0.08f, Mathf.Lerp(0.45f, 0.8f, wave), glow);
        float haloAlpha = glow * Mathf.Lerp(0.34f, 0.05f, wave);
        float coreAlpha = glow * Mathf.Lerp(0.58f, 0.16f, wave);

        Paint(halo, Vector2.one * pool * haloSpread, UIStyleKit.GloomAccent, haloAlpha);
        Paint(highlightSprite, Vector2.one * pool * coreSpread, Color.Lerp(UIStyleKit.GloomAccent, UIStyleKit.Paper, 0.55f), coreAlpha);

        if (heart != null)
        {
            float heartSize = Mathf.Lerp(0.36f, 0.44f, hoverBlend) * presence;
            float unit = heart.sprite != null ? heart.sprite.bounds.size.x : 1f;
            float s = heartSize / Mathf.Max(0.01f, unit);
            heart.transform.localScale = new Vector3(s, s, 1f);
            var c = Color.Lerp(UIStyleKit.GloomText, UIStyleKit.Lace, hoverBlend);
            c.a = presence;
            heart.color = c;
        }
    }

    private static void Paint(SpriteRenderer renderer, Vector2 scale, Color color, float alpha)
    {
        if (renderer == null) return;
        renderer.transform.localScale = new Vector3(Mathf.Max(scale.x, 0.05f), Mathf.Max(scale.y, 0.05f), 1f);
        color.a = alpha;
        renderer.color = color;
    }

    private void EnsureVisuals()
    {
        var glow = GlowSprite();

        if (highlightSprite == null)
        {
            var existing = transform.Find("Highlight");
            if (existing != null) highlightSprite = existing.GetComponent<SpriteRenderer>();
        }
        if (highlightSprite == null)
            highlightSprite = CreateLayer("Highlight", glow, 6);
        else
        {
            highlightSprite.sprite = glow;
            highlightSprite.sortingOrder = 6;
        }

        var haloTransform = transform.Find("Halo");
        halo = haloTransform != null ? haloTransform.GetComponent<SpriteRenderer>() : CreateLayer("Halo", glow, 5);
        halo.sprite = glow;
        halo.sortingOrder = 5;

        var heartTransform = transform.Find("Heart");
        heart = heartTransform != null ? heartTransform.GetComponent<SpriteRenderer>() : CreateLayer("Heart", UIStyleKit.Heart(), 7);
        heart.sprite = UIStyleKit.Heart();
        heart.sortingOrder = 7;
        var hidden = heart.color;
        hidden.a = 0f;
        heart.color = hidden;
        if (highlightSprite != null) highlightSprite.color = hidden;
        if (halo != null) halo.color = hidden;

        if (hoverLabel != null)
            hoverLabel.gameObject.SetActive(false);
        var leftoverLabel = transform.Find("Label");
        if (leftoverLabel != null)
            leftoverLabel.gameObject.SetActive(false);
    }

    private SpriteRenderer CreateLayer(string layerName, Sprite sprite, int order)
    {
        var go = new GameObject(layerName);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static Sprite glowSprite;

    /// Мягкое пятно: в центре плотнее, к краю гаснет по гауссиане — свет, а не прямоугольник.
    private static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;

        const int n = 128;
        var px = new Color32[n * n];
        const float sigma = 0.38f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float r2 = dx * dx + dy * dy;
                float a = r2 >= 1f ? 0f : Mathf.Exp(-r2 / (2f * sigma * sigma));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
        }

        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        tex.SetPixels32(px);
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return glowSprite;
    }

    /// <summary>Клик по зоне: запускаем комментарий через Yarn Spinner.</summary>
    public void Interact(DialogueRunner runner)
    {
        if (runner == null)
        {
            Debug.LogError($"[InteractiveObject] DialogueRunner не назначен ({name}).", this);
            return;
        }
        if (string.IsNullOrWhiteSpace(yarnNode))
        {
            Debug.LogWarning($"[InteractiveObject] Не указан узел Yarn ({name}).", this);
            return;
        }

        Debug.Log($"[InteractiveObject] Клик по '{name}' -> узел '{yarnNode}'");
        runner.StartDialogue(yarnNode);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
