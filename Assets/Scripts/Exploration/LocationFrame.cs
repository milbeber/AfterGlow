using UnityEngine;

/// Подгоняет фон локации под камеру, не обрезая кликабельные зоны.
/// Зоны — дочерние объекты, поэтому остаются на своих местах рисунка.
public class LocationFrame : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backdrop;

    public SpriteRenderer Backdrop
    {
        get => backdrop;
        set => backdrop = value;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying) return;
        Fit(Camera.main);
    }

    public void Fit(Camera cam)
    {
        if (cam == null || backdrop == null || backdrop.sprite == null) return;

        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;
        Vector2 size = backdrop.sprite.bounds.size;
        if (size.x <= 0.001f || size.y <= 0.001f) return;

        float scale = Mathf.Min(worldW / size.x, worldH / size.y);
        transform.localScale = new Vector3(scale, scale, 1f);

        Vector3 camPos = cam.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, 0f);
    }
}
