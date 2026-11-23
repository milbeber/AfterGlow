using TMPro;
using UnityEngine;

/// Сбой текста, который не мешает читать: буквы чуть дрожат, а одна иногда «соскальзывает» вниз
/// и на миг краснеет. Сам текст не меняется (только вершины меша), поэтому печатная машинка,
/// бэклог и Yarn ничего не замечают. Силу задаёт HorrorEffectsManager через GlobalIntensity.
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class SanityTextDistortion : MonoBehaviour
{
    /// 0 = текст спокоен, 1 = максимум (задаёт HorrorEffectsManager)
    public static float GlobalIntensity;

    [Tooltip("Множитель общей силы для этого текста")]
    [Range(0f, 2f)] public float multiplier = 1f;
    [Tooltip("Дрожание букв в пикселях при полной силе")]
    public float maxJitter = 1.6f;
    [Tooltip("На сколько пикселей «соскальзывает» буква")]
    public float maxSlip = 7f;
    [Tooltip("С какой силы начинается дрожание (до неё только редкие соскальзывания)")]
    [Range(0f, 1f)] public float jitterFrom = 0.3f;
    public Color slipColor = new Color(0.886f, 0.149f, 0.231f);

    private TMP_Text text;
    private bool dirty;
    private int slipIndex = -1;
    private float slipUntil, nextSlip;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnDisable()
    {
        if (dirty && text != null) text.ForceMeshUpdate();
        dirty = false;
    }

    private void LateUpdate()
    {
        float k = Mathf.Clamp01(GlobalIntensity * multiplier);
        if (k < 0.01f || string.IsNullOrEmpty(text.text))
        {
            if (dirty) { text.ForceMeshUpdate(); dirty = false; }
            return;
        }

        text.ForceMeshUpdate();
        var info = text.textInfo;
        int count = info.characterCount;
        if (count == 0) return;

        float t = Time.unscaledTime;
        if (t >= nextSlip)
        {
            slipIndex = Random.Range(0, count);
            slipUntil = t + Random.Range(0.08f, 0.25f);
            nextSlip = t + Mathf.Lerp(4f, 0.6f, k) * Random.Range(0.6f, 1.4f);
        }
        if (t > slipUntil) slipIndex = -1;

        float jitter = maxJitter * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(jitterFrom, 1f, k));
        Color32 slip = slipColor;

        for (int i = 0; i < count; i++)
        {
            var ch = info.characterInfo[i];
            if (!ch.isVisible) continue;

            var mesh = info.meshInfo[ch.materialReferenceIndex];
            int v = ch.vertexIndex;

            Vector3 off = Vector3.zero;
            if (jitter > 0f)
            {
                off.x = (Mathf.PerlinNoise(i * 0.37f, t * 7f) - 0.5f) * 2f * jitter;
                off.y = (Mathf.PerlinNoise(t * 7f, i * 0.37f) - 0.5f) * 2f * jitter;
            }
            if (i == slipIndex) off.y -= maxSlip * Mathf.Lerp(0.4f, 1f, k);

            for (int j = 0; j < 4; j++)
            {
                mesh.vertices[v + j] += off;
                if (i == slipIndex)
                {
                    byte a = mesh.colors32[v + j].a;
                    mesh.colors32[v + j] = new Color32(slip.r, slip.g, slip.b, a);
                }
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        dirty = true;
    }
}
