using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Yarn.Unity;

/// Меню AfterGlow → «День 16: локация и клики».
/// Расставляет зоны InteractiveObject по фону Assets/Sprites/fon.jpg
/// (его на экран выводит DialogueStyler). Повторный запуск пересобирает зоны.
public static class AfterGlowDay16Setup
{
    private const string ScenePath = "Assets/Scenes/GameplayScene.unity";
    private const string Folder = "Assets/Sprites/Locations";
    private const string CorridorPath = Folder + "/Corridor.png";
    private const string HotspotPath = Folder + "/Hotspot.png";
    private const int Width = 1600;
    private const int Height = 900;
    private const float Ppu = 100f;

    private struct Zone
    {
        public string Name;
        public string Node;
        public Rect Normalized;
        public Color Tint;
    }

    [MenuItem("AfterGlow/День 16: локация и клики")]
    public static void Run()
    {
        var log = new StringBuilder();

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath);
            log.AppendLine("• Открыта GameplayScene");
        }

        var corridor = CreateSprite(CorridorPath, PaintCorridor(), Ppu);
        var hotspot = CreateSprite(HotspotPath, PaintHotspot(), 64f);
        if (corridor == null || hotspot == null)
        {
            Debug.LogError("[AfterGlow] День 16: не удалось создать спрайты");
            return;
        }

        BuildLocation(corridor, hotspot, log);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[AfterGlow] День 16:\n" + log);
    }

    private static void BuildLocation(Sprite corridor, Sprite hotspot, StringBuilder log)
    {
        var oldLocation = GameObject.Find("Location");
        if (oldLocation != null) Object.DestroyImmediate(oldLocation);
        var oldController = GameObject.Find("ExplorationController");
        if (oldController != null) Object.DestroyImmediate(oldController);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.075f, 0.09f, 1f);
        }

        var root = new GameObject("Location");
        var backdropGo = new GameObject("Backdrop");
        backdropGo.transform.SetParent(root.transform, false);
        var backdrop = backdropGo.AddComponent<SpriteRenderer>();
        backdrop.sprite = corridor;
        backdrop.sortingOrder = 0;

        root.AddComponent<HotspotLayout>();
        var frame = root.AddComponent<LocationFrame>();
        frame.Backdrop = backdrop;
        frame.enabled = false;
        backdrop.enabled = false;

        foreach (var zone in Zones())
            CreateZone(root.transform, zone, hotspot);

        var controller = new GameObject("ExplorationController");
        var raycaster = controller.AddComponent<InteractionRaycaster>();
        var raySo = new SerializedObject(raycaster);
        raySo.FindProperty("cam").objectReferenceValue = cam;
        raySo.FindProperty("dialogueRunner").objectReferenceValue = Object.FindFirstObjectByType<DialogueRunner>();
        raySo.ApplyModifiedPropertiesWithoutUndo();

        log.AppendLine("✓ 5 зон по фону fon.jpg (дверь, окно, люстра, картина, чашка)");
        log.AppendLine("✓ ExplorationController с InteractionRaycaster");
    }

    private static Zone[] Zones()
    {
        // Доли кадра fon.jpg: x,y — левый нижний угол, width/height — размер.
        return new[]
        {
            new Zone { Name = "Zone_Door", Node = "Obj_Door", Normalized = new Rect(0.305f, 0.40f, 0.10f, 0.38f), Tint = new Color(0.75f, 0.28f, 0.32f) },
            new Zone { Name = "Zone_DarkHall", Node = "Obj_DarkHall", Normalized = new Rect(0.56f, 0.42f, 0.18f, 0.38f), Tint = new Color(0.85f, 0.35f, 0.5f) },
            new Zone { Name = "Zone_Lamp", Node = "Obj_Lamp", Normalized = new Rect(0.48f, 0.92f, 0.12f, 0.075f), Tint = new Color(0.9f, 0.75f, 0.4f) },
            new Zone { Name = "Zone_Wall", Node = "Obj_Wall", Normalized = new Rect(0.045f, 0.56f, 0.15f, 0.22f), Tint = new Color(0.7f, 0.7f, 0.75f) },
            new Zone { Name = "Zone_Continue", Node = "ContinueStory", Normalized = new Rect(0.675f, 0.345f, 0.10f, 0.13f), Tint = new Color(0.85f, 0.82f, 0.7f) },
        };
    }

    private static void CreateZone(Transform parent, Zone zone, Sprite hotspot)
    {
        var go = new GameObject(zone.Name);
        go.transform.SetParent(parent, false);
        var anchor = go.AddComponent<BackgroundAnchor>();
        anchor.Normalized = zone.Normalized;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var highlightGo = new GameObject("Highlight");
        highlightGo.transform.SetParent(go.transform, false);
        var highlight = highlightGo.AddComponent<SpriteRenderer>();
        highlight.sprite = hotspot;
        highlight.color = new Color(zone.Tint.r, zone.Tint.g, zone.Tint.b, 0f);
        highlight.sortingOrder = 5;

        var interactive = go.AddComponent<InteractiveObject>();
        var so = new SerializedObject(interactive);
        so.FindProperty("yarnNode").stringValue = zone.Node;
        so.FindProperty("highlightSprite").objectReferenceValue = highlight;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite CreateSprite(string path, Color32[] pixels, float ppu)
    {
        int height = path.EndsWith("Hotspot.png") ? 64 : Height;
        int width = path.EndsWith("Hotspot.png") ? 64 : Width;

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();

        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Folder));
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Color32[] PaintHotspot()
    {
        var px = new Color32[64 * 64];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        return px;
    }

    private static Color32[] PaintCorridor()
    {
        var px = new Color32[Width * Height];
        var lamp = new Vector2(0.1f, 3.15f);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float lx = x / Ppu - 8f;
                float ly = y / Ppu - 4.5f;
                float n = Hash(x / 4, y / 4);
                float v = 0.075f + n * 0.045f;

                if (ly < -1.6f) v *= 0.72f;

                float glow = Vector2.Distance(new Vector2(lx, ly), lamp);
                float warm = Mathf.Clamp01(1f - glow / 3.4f);
                warm *= warm;

                float edgeX = Mathf.Abs(lx) / 8f;
                float edgeY = Mathf.Abs(ly) / 4.5f;
                float vig = Mathf.Clamp01(Mathf.Max(edgeX, edgeY));
                vig = vig * vig * 0.45f;

                float r = v * (1f - vig) + warm * 0.28f;
                float g = v * (1f - vig) + warm * 0.2f;
                float b = (v + 0.02f) * (1f - vig) + warm * 0.08f;
                px[x + y * Width] = ToColor(r, g, b);
            }
        }

        // Тёмный проём слева.
        FillRect(px, -6.7f, -2.9f, -2.5f, 3.3f, new Color(0.015f, 0.016f, 0.02f), 0.92f);
        FillRect(px, -5.6f, -2.1f, -3.4f, 2.5f, new Color(0.0f, 0.0f, 0.0f), 0.8f);

        // Железная дверь справа: рама и полотно без ручки.
        FillRect(px, 2.7f, -2.55f, 5.9f, 2.9f, new Color(0.16f, 0.17f, 0.19f), 1f);
        FillRect(px, 3.05f, -2.25f, 5.55f, 2.55f, new Color(0.22f, 0.24f, 0.27f), 1f);
        for (float scratch = -1.6f; scratch < 2.2f; scratch += 0.55f)
            FillRect(px, 3.2f, scratch, 5.3f, scratch + 0.035f, new Color(0.1f, 0.1f, 0.11f), 0.7f);

        // Корпус лампы.
        FillRect(px, -0.55f, 3.55f, 0.75f, 3.85f, new Color(0.12f, 0.11f, 0.1f), 1f);
        FillRect(px, -0.15f, 2.7f, 0.35f, 3.55f, new Color(0.78f, 0.62f, 0.32f), 1f);

        // Трещины по центру стены.
        DrawCrack(px, new Vector2(-0.4f, -0.9f), new Vector2(0.2f, 1.6f));
        DrawCrack(px, new Vector2(0.2f, 1.6f), new Vector2(-0.15f, 1.7f));
        DrawCrack(px, new Vector2(0.15f, 0.4f), new Vector2(0.9f, -0.7f));

        // Плита на полу.
        FillRect(px, -1.6f, -3.25f, 1.6f, -2.15f, new Color(0.2f, 0.19f, 0.17f), 1f);
        FillRect(px, -1.45f, -3.12f, 1.45f, -2.28f, new Color(0.28f, 0.26f, 0.23f), 1f);

        return px;
    }

    private static void FillRect(Color32[] px, float x0, float y0, float x1, float y1, Color color, float alpha)
    {
        int left = Mathf.Clamp(Mathf.RoundToInt((x0 + 8f) * Ppu), 0, Width - 1);
        int right = Mathf.Clamp(Mathf.RoundToInt((x1 + 8f) * Ppu), 0, Width - 1);
        int bottom = Mathf.Clamp(Mathf.RoundToInt((y0 + 4.5f) * Ppu), 0, Height - 1);
        int top = Mathf.Clamp(Mathf.RoundToInt((y1 + 4.5f) * Ppu), 0, Height - 1);
        if (left > right) (left, right) = (right, left);
        if (bottom > top) (bottom, top) = (top, bottom);

        for (int y = bottom; y <= top; y++)
        {
            for (int x = left; x <= right; x++)
            {
                int i = x + y * Width;
                var dst = px[i];
                px[i] = new Color32(
                    (byte)Mathf.Lerp(dst.r, color.r * 255f, alpha),
                    (byte)Mathf.Lerp(dst.g, color.g * 255f, alpha),
                    (byte)Mathf.Lerp(dst.b, color.b * 255f, alpha),
                    255);
            }
        }
    }

    private static void DrawCrack(Color32[] px, Vector2 a, Vector2 b)
    {
        Vector2 delta = b - a;
        int steps = Mathf.Max(8, Mathf.CeilToInt(delta.magnitude * Ppu));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = a + delta * (i / (float)steps);
            float j = (Hash(i, 3) - 0.5f) * 0.08f;
            FillRect(px, p.x + j - 0.03f, p.y - 0.03f, p.x + j + 0.03f, p.y + 0.03f, new Color(0.02f, 0.02f, 0.025f), 1f);
        }
    }

    private static Color32 ToColor(float r, float g, float b)
    {
        return new Color32(
            (byte)Mathf.Clamp(r * 255f, 0f, 255f),
            (byte)Mathf.Clamp(g * 255f, 0f, 255f),
            (byte)Mathf.Clamp(b * 255f, 0f, 255f),
            255);
    }

    private static float Hash(int x, int y)
    {
        int n = x * 374761393 + y * 668265263;
        n = (n ^ (n >> 13)) * 1274126177;
        n ^= n >> 16;
        return (n & 255) / 255f;
    }
}
