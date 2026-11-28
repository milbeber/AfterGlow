using UnityEngine;

/// Держит коллайдер на объекте фоновой картинки (BG_Art).
/// Картинка — UI и закрывает экран, поэтому зона считается в долях кадра
/// и каждый кадр переносится на плоскость камеры, куда попадает луч мыши.
public class BackgroundAnchor : MonoBehaviour
{
    [Tooltip("Прямоугольник на фоне: x,y — левый нижний угол, width/height — размер. Все значения от 0 до 1.")]
    [SerializeField] private Rect normalized = new Rect(0.4f, 0.4f, 0.2f, 0.2f);

    public Rect Normalized
    {
        get => normalized;
        set => normalized = value;
    }

    public void Apply(RectTransform image, Camera cam)
    {
        if (image == null || cam == null) return;

        var corners = new Vector3[4];
        image.GetWorldCorners(corners);
        Vector2 bl = cam.WorldToScreenPoint(corners[0]);
        Vector2 tr = cam.WorldToScreenPoint(corners[2]);

        float u0 = normalized.xMin;
        float v0 = normalized.yMin;
        float u1 = normalized.xMax;
        float v1 = normalized.yMax;
        var s0 = new Vector2(Mathf.Lerp(bl.x, tr.x, u0), Mathf.Lerp(bl.y, tr.y, v0));
        var s1 = new Vector2(Mathf.Lerp(bl.x, tr.x, u1), Mathf.Lerp(bl.y, tr.y, v1));

        float depth = cam.WorldToScreenPoint(Vector3.zero).z;
        Vector3 w0 = cam.ScreenToWorldPoint(new Vector3(s0.x, s0.y, depth));
        Vector3 w1 = cam.ScreenToWorldPoint(new Vector3(s1.x, s1.y, depth));

        var center = (w0 + w1) * 0.5f;
        center.z = 0f;
        transform.position = center;

        var size = new Vector2(Mathf.Abs(w1.x - w0.x), Mathf.Abs(w1.y - w0.y));
        var col = GetComponent<BoxCollider2D>();
        if (col != null) col.size = size;
    }
}
