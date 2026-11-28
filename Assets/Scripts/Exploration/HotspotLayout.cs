using UnityEngine;

/// Расставляет зоны клика по реальному фону сцены (картинка BG_Art из DialogueStyler).
public class HotspotLayout : MonoBehaviour
{
    private RectTransform background;
    private BackgroundAnchor[] anchors;
    private SpriteRenderer backdrop;

    private void Awake()
    {
        anchors = GetComponentsInChildren<BackgroundAnchor>(true);
        backdrop = GetComponentInChildren<SpriteRenderer>();
        if (backdrop != null && backdrop.gameObject.name == "Backdrop")
            backdrop.enabled = false;

        transform.localScale = Vector3.one;
        transform.position = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (background == null)
        {
            var go = GameObject.Find("BG_Art");
            if (go != null) background = go.GetComponent<RectTransform>();
        }
        if (background == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] != null) anchors[i].Apply(background, cam);
        }
    }
}
