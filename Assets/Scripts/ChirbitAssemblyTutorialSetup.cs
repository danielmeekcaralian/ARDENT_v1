#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChirbitAssemblyTutorialSetup
{
    [MenuItem("ARDENT/Chirbit/Connect Assembly Tutorial")]
    public static void Connect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "ARScene")
        { Debug.LogWarning("Open ARScene outside Play Mode first."); return; }
        RectTransform root = null;
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        { root = Find<RectTransform>(go.transform, "ChirbitTutorialRoot"); if (root != null) break; }
        if (root == null || root.parent == null) { Debug.LogError("ChirbitTutorialRoot was not found beneath a Canvas."); return; }
        var canvas = root.GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("Place ChirbitTutorialRoot under the ARScene Canvas."); return; }
        var dialogue = Find<RectTransform>(root, "DialoguePanel");
        var top = Find<Image>(root, "TopShade"); var bottom = Find<Image>(root, "BottomShade");
        var left = Find<Image>(root, "LeftShade"); var right = Find<Image>(root, "RightShade");
        var message = Find<TMP_Text>(root, "MessageText"); var step = Find<TMP_Text>(root, "StepText");
        var back = Find<Button>(root, "BackButton"); var next = Find<Button>(root, "NextButton"); var skip = Find<Button>(root, "SkipButton");
        var inventory = Find<Button>(canvas.transform, "InventoryButton");
        var inventoryPanel = Find<Transform>(canvas.transform, "InventoryPanel");
        var completion = Find<Button>(canvas.transform, "ViewCompletionButton");
        var completionPanel = Find<Transform>(canvas.transform, "ARCompletionPanel");
        var instructions = Find<TMP_Text>(canvas.transform, "InstructionsText");
        var progress = Find<TMP_Text>(canvas.transform, "ProgressText");
        var title = Find<TMP_Text>(canvas.transform, "StepTitleText");
        var assembly = Object.FindFirstObjectByType<ARAssemblyManager>();
        var placement = Object.FindFirstObjectByType<ARPlacementManager>();
        var missing = new System.Collections.Generic.List<string>();
        if (dialogue == null) missing.Add("DialoguePanel"); if (top == null) missing.Add("TopShade");
        if (bottom == null) missing.Add("BottomShade"); if (left == null) missing.Add("LeftShade"); if (right == null) missing.Add("RightShade");
        if (message == null) missing.Add("MessageText"); if (step == null) missing.Add("StepText");
        if (back == null) missing.Add("BackButton"); if (next == null) missing.Add("NextButton"); if (skip == null) missing.Add("SkipButton");
        if (inventory == null) missing.Add("InventoryButton"); if (inventoryPanel == null) missing.Add("InventoryPanel");
        if (completion == null) missing.Add("ViewCompletionButton"); if (completionPanel == null) missing.Add("ARCompletionPanel");
        if (instructions == null) missing.Add("InstructionsText"); if (progress == null) missing.Add("ProgressText");
        if (title == null) missing.Add("StepTitleText"); if (assembly == null) missing.Add("ARAssemblyManager"); if (placement == null) missing.Add("ARPlacementManager");
        if (missing.Count > 0) { Debug.LogError("Assembly tutorial setup is missing: " + string.Join(", ", missing)); return; }

        var tutorial = canvas.GetComponent<ChirbitTutorial>();
        if (tutorial == null) tutorial = Undo.AddComponent<ChirbitTutorial>(canvas.gameObject);
        Undo.RecordObject(tutorial, "Connect Chirbit Assembly Tutorial");
        tutorial.tutorialID = "ARAssembly"; tutorial.autoStart = false; tutorial.sandboxOnly = false; tutorial.useCorners = true;
        tutorial.root = root; tutorial.dialogue = dialogue; tutorial.highlight = Find<RectTransform>(root, "HighlightBorder");
        tutorial.topShade = top; tutorial.bottomShade = bottom; tutorial.leftShade = left; tutorial.rightShade = right;
        tutorial.messageText = message; tutorial.stepText = step; tutorial.backButton = back; tutorial.nextButton = next; tutorial.skipButton = skip;

        var guide = canvas.GetComponent<ChirbitAssemblyTutorial>();
        if (guide == null) guide = Undo.AddComponent<ChirbitAssemblyTutorial>(canvas.gameObject);
        Undo.RecordObject(guide, "Connect assembly guide");
        guide.tutorial = tutorial; guide.assembly = assembly; guide.placement = placement;
        guide.stepTitleText = title; guide.instructionsText = instructions; guide.progressText = progress;
        guide.inventoryButton = inventory; guide.completionButton = completion;
        guide.inventoryPanel = inventoryPanel.gameObject; guide.completionPanel = completionPanel.gameObject;
        if (guide.introduction == null || guide.introduction.Length == 0)
            guide.introduction = new[] {
                new ChirbitTutorial.Step { target = title.rectTransform, message = "Welcome to PC assembly! Follow the current step title and instructions. The right component is shown for each step." },
                new ChirbitTutorial.Step { target = instructions.rectTransform, message = "Open the inventory, choose the required component, and place it on a detected surface. Move and rotate it near its target until it snaps into place." },
                new ChirbitTutorial.Step { target = progress.rectTransform, message = "Track completed components here. Assembly advances in order, so finish the current step before placing the next part." }
            };
        if (guide.disassembly == null || guide.disassembly.Length == 0)
            guide.disassembly = new[] {
                new ChirbitTutorial.Step { message = "Assembly is complete. Start Disassembly to remove installed components in reverse order, one at a time. Drag each selected part into its highlighted tray space." }
            };
        if (guide.completion == null || guide.completion.Length == 0)
            guide.completion = new[] {
                new ChirbitTutorial.Step { target = completion.transform as RectTransform, message = "The assembly activity is complete. Use this button to see your result and continue." }
            };
        tutorial.steps = guide.introduction;
        Stretch(root); var overlay = Find<RectTransform>(root, "DimOverlay"); if (overlay != null) Stretch(overlay);
        root.gameObject.SetActive(false);
        EditorUtility.SetDirty(tutorial); EditorUtility.SetDirty(guide); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeObject = guide;
        Debug.Log("Assembly tutorial connected. Save ARScene. It appears only during assembly activities. Tutorial keys are saved per AR activity ID.");
    }
    private static T Find<T>(Transform root, string name) where T : Component
    { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t.GetComponent<T>(); return null; }
    private static void Stretch(RectTransform rect)
    { Undo.RecordObject(rect, "Stretch tutorial overlay"); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}
#endif
