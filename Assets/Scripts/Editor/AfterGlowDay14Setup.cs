using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Меню AfterGlow → «День 14: подключить хоррор-эффекты».
/// Делает через API Unity то, что иначе пришлось бы накликивать руками:
///  1) добавляет GlitchRendererFeature в Renderer2D.asset;
///  2) добавляет оверрайд AfterGlow/Glitch в HorrorProfile и включает цвет виньетки;
///  3) вешает HorrorVolumeController на Volume_Horror (если открыта GameplayScene).
/// Повторный запуск безопасен: уже сделанные шаги пропускаются.
public static class AfterGlowDay14Setup
{
    private const string RendererPath = "Assets/Settings/Renderer2D.asset";
    private const string HorrorProfilePath = "Assets/Scenes/GameplayScene/HorrorProfile.asset";
    private const string ShaderName = "AfterGlow/GlitchFullscreen";
    private const string HorrorVolumeName = "Volume_Horror";

    [MenuItem("AfterGlow/День 14: подключить хоррор-эффекты")]
    private static void Run()
    {
        var log = new StringBuilder();
        SetupRendererFeature(log);
        SetupHorrorProfile(log);
        SetupScene(log);
        AssetDatabase.SaveAssets();

        Debug.Log("[AfterGlow] День 14:\n" + log);
        EditorUtility.DisplayDialog("AfterGlow — день 14", log.ToString(), "Готово");
    }

    private static void SetupRendererFeature(StringBuilder log)
    {
        var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
        if (data == null)
        {
            log.AppendLine($"✗ Не найден {RendererPath}");
            return;
        }

        if (data.rendererFeatures.Exists(f => f is GlitchRendererFeature))
        {
            log.AppendLine("• Glitch Renderer Feature уже есть в Renderer2D");
            return;
        }

        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            log.AppendLine($"✗ Шейдер {ShaderName} не найден (есть ли Assets/Shaders/GlitchFullscreen.shader и нет ли ошибок компиляции?)");
            return;
        }

        var feature = ScriptableObject.CreateInstance<GlitchRendererFeature>();
        feature.name = "AfterGlow Glitch";
        feature.GlitchShader = shader;
        AssetDatabase.AddObjectToAsset(feature, data);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

        // так же, как это делает инспектор URP при «Add Renderer Feature»
        var so = new SerializedObject(data);
        var features = so.FindProperty("m_RendererFeatures");
        var map = so.FindProperty("m_RendererFeatureMap");
        features.arraySize++;
        features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
        map.arraySize++;
        map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
        so.ApplyModifiedProperties();

        data.SetDirty();
        EditorUtility.SetDirty(data);
        log.AppendLine("✓ Glitch Renderer Feature добавлена в Renderer2D");
    }

    private static void SetupHorrorProfile(StringBuilder log)
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(HorrorProfilePath);
        if (profile == null)
        {
            log.AppendLine($"✗ Не найден {HorrorProfilePath}");
            return;
        }

        if (!profile.TryGet(out GlitchVolume glitch))
        {
            glitch = profile.Add<GlitchVolume>(false);
            glitch.name = nameof(GlitchVolume);
            glitch.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(glitch, profile);

            // стартовые значения «полного» хоррора; при weight 0 их не видно
            glitch.intensity.Override(0.6f);
            glitch.blockCount.Override(24f);
            glitch.colorSplit.Override(0.006f);
            glitch.maxShift.Override(0.04f);
            glitch.speed.Override(10f);
            EditorUtility.SetDirty(glitch);
            log.AppendLine("✓ В HorrorProfile добавлен оверрайд AfterGlow/Glitch");
        }
        else
        {
            log.AppendLine("• Оверрайд AfterGlow/Glitch уже есть в HorrorProfile");
        }

        // цвет виньетки был выбран, но галочка Override не стояла — виньетка оставалась чёрной
        if (profile.TryGet(out Vignette vignette) && !vignette.color.overrideState)
        {
            vignette.color.overrideState = true;
            EditorUtility.SetDirty(vignette);
            log.AppendLine("✓ Включён цвет виньетки в HorrorProfile");
        }

        EditorUtility.SetDirty(profile);
    }

    private static void SetupScene(StringBuilder log)
    {
        var go = GameObject.Find(HorrorVolumeName);
        if (go == null || go.GetComponent<Volume>() == null)
        {
            log.AppendLine($"• {HorrorVolumeName} не найден: откройте GameplayScene и запустите пункт меню ещё раз");
            return;
        }

        if (go.GetComponent<HorrorVolumeController>() != null)
        {
            log.AppendLine("• HorrorVolumeController уже висит на " + HorrorVolumeName);
            return;
        }

        Undo.AddComponent<HorrorVolumeController>(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
        log.AppendLine("✓ HorrorVolumeController добавлен на " + HorrorVolumeName + " (сохраните сцену: Ctrl+S)");
    }
}
