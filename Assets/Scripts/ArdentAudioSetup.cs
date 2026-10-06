#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ArdentAudioSetup
{
    private const string LibraryPath = "Assets/Resources/ArdentAudioLibrary.asset";
    private const string SettingsScenePath = "Assets/Scenes/UI_Scene.unity";

    private const string MainBgm = "Assets/Audio/Music/MainBGM.wav";
    private const string HardwareBgm = "Assets/Audio/Music/HardwareLibraryBGM.wav";
    private const string QuizBgm = "Assets/Audio/Music/QuizBGM.wav";
    private const string Button = "Assets/Audio/SFX/ButtonPress.wav";
    private const string LessonButton = "Assets/Audio/SFX/LessonButtonPress.wav";
    private const string QuizButton = "Assets/Audio/SFX/QuizAnswerButtonPress.wav";
    private const string Place = "Assets/Audio/SFX/PlaceItemSound.wav";
    private const string Correct = "Assets/Audio/SFX/CorrectItemPlacement.wav";
    private const string Error = "Assets/Audio/SFX/Error.wav";
    private const string Fail = "Assets/Audio/SFX/Fail.wav";
    private const string Success = "Assets/Audio/SFX/QuizARSuccess.wav";
    private const string Subtopic = "Assets/Audio/SFX/SubTopicComplete.wav";

    [MenuItem("ARDENT/Audio/Install Complete Audio System")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("Exit Play Mode and Prefab Mode before installing the audio system.");
            return;
        }
        string[] required = { MainBgm, HardwareBgm, QuizBgm, Button, LessonButton, QuizButton, Place, Correct, Error, Fail, Success, Subtopic };
        var missing = required.Where(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null).ToArray();
        if (missing.Length > 0)
        {
            Debug.LogError("Audio setup is missing these clips:\n" + string.Join("\n", missing));
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string backup = Path.Combine("Library/ARDENTAudioBackups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backup);
        foreach (string path in new[] { LibraryPath, SettingsScenePath })
            if (File.Exists(path)) File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), false);

        ConfigureMusic(MainBgm); ConfigureMusic(HardwareBgm); ConfigureMusic(QuizBgm);
        foreach (string path in new[] { Button, LessonButton, QuizButton, Place, Correct, Error, Fail, Success, Subtopic })
            ConfigureSfx(path);

        EnsureFolder("Assets/Resources");
        var library = AssetDatabase.LoadAssetAtPath<ArdentAudioLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<ArdentAudioLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        Undo.RecordObject(library, "Configure ARDENT audio library");
        library.mainBGM = Clip(MainBgm);
        library.hardwareLibraryBGM = Clip(HardwareBgm);
        library.quizBGM = Clip(QuizBgm);
        library.buttonPress = Clip(Button);
        library.lessonButtonPress = Clip(LessonButton);
        library.quizAnswerButtonPress = Clip(QuizButton);
        library.placeItem = Clip(Place);
        library.correctItemPlacement = Clip(Correct);
        library.error = Clip(Error);
        library.fail = Clip(Fail);
        library.success = Clip(Success);
        library.subtopicComplete = Clip(Subtopic);
        // No dedicated unlock clip was supplied; use the positive subtopic cue.
        library.itemUnlock = Clip(Subtopic);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.OpenScene(SettingsScenePath);
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var panel = all.FirstOrDefault(t => t.name == "settingsPanel");
        var controller = panel != null ? panel.GetComponent<ArdentSettingsPanel>() : null;
        var scroll = panel != null ? panel.GetComponentInChildren<ScrollRect>(true) : null;
        if (controller == null || scroll == null || scroll.content == null)
        {
            Debug.LogError("UI_Scene needs settingsPanel with ArdentSettingsPanel and SettingsScrollView. Audio library was created, but settings controls were not changed.");
            return;
        }

        EnsureSlider(scroll.content, "MusicVolume", "Music volume", .60f, out controller.musicSlider, out controller.musicValue);
        EnsureSlider(scroll.content, "SfxVolume", "Sound effects volume", .80f, out controller.sfxSlider, out controller.sfxValue);
        controller.musicSlider.transform.parent.SetSiblingIndex(0);
        controller.sfxSlider.transform.parent.SetSiblingIndex(1);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = panel.gameObject;
        Debug.Log("ARDENT audio installed: persistent scene music, automatic button sounds, activity notifications, and saved Music/SFX settings. Backup: " + Path.GetFullPath(backup));
    }

    private static AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

    private static void ConfigureMusic(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = .70f;
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        settings.preloadAudioData = false;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = false;
        importer.loadInBackground = true;
        importer.SaveAndReimport();
    }

    private static void ConfigureSfx(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.ADPCM;
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.loadInBackground = false;
        importer.SaveAndReimport();
    }

    private static void EnsureSlider(Transform content, string name, string title, float initial,
        out Slider slider, out TMP_Text valueText)
    {
        var existing = content.Find(name);
        if (existing != null)
        {
            slider = existing.GetComponentInChildren<Slider>(true);
            valueText = existing.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Value");
            if (slider != null && valueText != null) return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var row = Rect(name, content, Vector2.zero, Vector2.one);
        Undo.AddComponent<LayoutElement>(row.gameObject).preferredHeight = 120;
        var heading = Label(row, "Title", title, new Vector2(0, .55f), new Vector2(.78f, 1));
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        valueText = Label(row, "Value", Mathf.RoundToInt(initial * 100) + "%", new Vector2(.78f, .55f), Vector2.one);
        var sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
        Undo.RegisterCreatedObjectUndo(sliderObject, "Create audio volume slider");
        sliderObject.name = name + "Slider";
        var rect = sliderObject.GetComponent<RectTransform>();
        rect.SetParent(row, false); rect.anchorMin = new Vector2(.02f, .05f); rect.anchorMax = new Vector2(.98f, .45f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false; slider.value = initial;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create audio settings UI");
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text Label(Transform parent, string name, string text, Vector2 min, Vector2 max)
    {
        var label = Undo.AddComponent<TextMeshProUGUI>(Rect(name, parent, min, max).gameObject);
        label.text = text; label.fontSize = 28; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/'); string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
