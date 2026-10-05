using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChirbitAssemblyTutorial : MonoBehaviour
{
    public ChirbitTutorial tutorial;
    public ARPlacementManager placement;
    public ARAssemblyManager assembly;
    public TMP_Text stepTitleText, instructionsText, progressText;
    public Button inventoryButton, completionButton;
    public GameObject inventoryPanel, completionPanel;
    public ChirbitTutorial.Step[] introduction, disassembly, completion;
    private string activeStage;

    private ARActivityData Activity => placement != null ? placement.CurrentActivity : null;
    private bool IsAssembly => !ARSandboxSession.IsActive && Activity != null &&
        Activity.activityType == ARActivityType.Assembly && Activity.assemblyActivity != null;
    private string Prefix => "ARAssembly." + (Activity != null && !string.IsNullOrWhiteSpace(Activity.activityID)
        ? Activity.activityID : "Default");

    private void Awake()
    {
        if (tutorial != null) { tutorial.autoStart = false; tutorial.sandboxOnly = false; }
    }
    private void OnEnable() { if (tutorial != null) tutorial.Completed += OnCompleted; }
    private void OnDisable() { if (tutorial != null) tutorial.Completed -= OnCompleted; }
    private static string Key(string stage) => "ARDENT.Chirbit.ARAssembly." + stage + ".v1";
    private bool Seen(string stage) => PlayerPrefs.GetInt(Key(Prefix + "." + stage), 0) != 0;
    private void OnCompleted(bool skipped)
    {
        if (!IsAssembly || string.IsNullOrEmpty(activeStage)) return;
        PlayerPrefs.SetInt(Key(Prefix + "." + activeStage), 1);
        PlayerPrefs.Save();
        activeStage = null;
    }

    private void Update()
    {
        if (!IsAssembly || tutorial == null || !tutorial.IsReady || tutorial.IsShowing ||
            assembly == null || ARCheckpointSession.BlocksInput || UIManager.HasOpenPanel ||
            SandboxInventoryPanel.IsOpen || (inventoryPanel != null && inventoryPanel.activeInHierarchy) ||
            (completionPanel != null && completionPanel.activeInHierarchy)) return;
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return;
        if (Touchscreen.current != null)
            foreach (var touch in Touchscreen.current.touches) if (touch.press.isPressed) return;

        switch (assembly.Phase)
        {
            case ARAssemblyManager.ActivityPhase.Assembly:
                if (!Seen("Introduction")) Show("Introduction", introduction);
                break;
            case ARAssemblyManager.ActivityPhase.ReadyForDisassembly:
                if (!Seen("Disassembly"))
                {
                    var button = FindDisassemblyButton();
                    if (button != null) Show("Disassembly", disassembly, button.transform as RectTransform);
                    else if (instructionsText != null) Show("Disassembly", disassembly, instructionsText.rectTransform);
                }
                break;
            case ARAssemblyManager.ActivityPhase.Complete:
                if (!Seen("Completion") && completionButton != null && completionButton.gameObject.activeInHierarchy)
                    Show("Completion", completion, completionButton.transform as RectTransform);
                break;
        }
    }

    private Button FindDisassemblyButton()
    {
        foreach (var button in assembly.GetComponentsInChildren<Button>(true))
            if (button != null && button.gameObject.activeInHierarchy && button.name == "StartDisassemblyButton") return button;
        foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (button != null && button.gameObject.scene == gameObject.scene &&
                button.gameObject.activeInHierarchy && button.name == "StartDisassemblyButton") return button;
        return null;
    }

    private void Show(string stage, ChirbitTutorial.Step[] steps, RectTransform runtimeTarget = null)
    {
        if (steps == null || steps.Length == 0) return;
        activeStage = stage;
        tutorial.tutorialID = Prefix + "." + stage;
        tutorial.steps = steps;
        if (runtimeTarget != null && steps.Length > 0 && steps[0].target == null)
            steps[0].target = runtimeTarget;
        tutorial.Begin();
        if (!tutorial.IsShowing) activeStage = null;
    }
}
