using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using P = HardwareComponentProfile;

// Uses the user's scene-authored four-part build panel.
public sealed class HardwareCompatibilityPanel : MonoBehaviour
{
    private static readonly string[] DropdownNames = { "MotherboardDropdown", "CPUDropdown", "RAMDropdown", "GPUDropdown" };
    private static readonly string[] Labels = { "motherboard", "CPU", "RAM", "GPU" };
    private static readonly P.ComponentKind[] Kinds = { P.ComponentKind.Motherboard, P.ComponentKind.CPU, P.ComponentKind.RAM, P.ComponentKind.GPU };
    private readonly TMP_Dropdown[] dropdowns = new TMP_Dropdown[4];
    private readonly List<P>[] options = { new List<P>(), new List<P>(), new List<P>(), new List<P>() };
    private HardwareLibraryUI library;
    private GameObject panel;
    private Button openButton, closeButton, checkButton;
    private TMP_Text resultText;
    private bool ready;

    public void Initialize(HardwareLibraryUI owner)
    {
        Unbind();
        library = owner;
        var panelTransform = HardwareLibraryUI.FindNamed<Transform>(owner.transform, "CompatibilityPanel");
        if (panelTransform == null) return;
        panel = panelTransform.gameObject;
        var missing = new List<string>();
        for (int i = 0; i < dropdowns.Length; i++)
        {
            dropdowns[i] = HardwareLibraryUI.FindNamed<TMP_Dropdown>(panelTransform, DropdownNames[i]);
            if (dropdowns[i] == null) missing.Add(DropdownNames[i] + " (TMP Dropdown)");
        }
        closeButton = HardwareLibraryUI.FindNamed<Button>(panelTransform, "CloseButton");
        checkButton = HardwareLibraryUI.FindNamed<Button>(panelTransform, "CheckButton");
        resultText = HardwareLibraryUI.FindNamed<TMP_Text>(panelTransform, "ResultText");
        openButton = HardwareLibraryUI.FindNamed<Button>(owner.transform, "CheckCompatibilityButton");
        if (closeButton == null) missing.Add("CloseButton");
        if (checkButton == null) missing.Add("CheckButton");
        if (resultText == null) missing.Add("ResultText");
        panel.SetActive(false);
        ready = missing.Count == 0;
        if (!ready)
        { Debug.LogWarning("CompatibilityPanel is missing: " + string.Join(", ", missing), owner); return; }
        if (openButton == null || openButton.transform.IsChildOf(panelTransform))
        {
            Debug.LogWarning("Add CheckCompatibilityButton outside CompatibilityPanel to open the checker.", owner);
            openButton = null;
        }
        if (openButton != null) openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
        checkButton.onClick.AddListener(Check);
        foreach (var dropdown in dropdowns) dropdown.onValueChanged.AddListener(SelectionChanged);
    }

    public static List<P> AvailableProfiles(LessonDatabase database, HardwareProfileCatalog catalog)
    {
        // Compatibility is available to everyone; lesson ownership only controls AR inventory.
        return HardwareCompatibilityCatalog.Collect(catalog,
            Resources.Load<HardwareCompatibilityCatalog>(HardwareCompatibilityCatalog.ResourcePath));
    }

    private static bool IsBuildKind(P.ComponentKind kind)
    {
        foreach (var expected in Kinds) if (kind == expected) return true;
        return false;
    }

    public void Open()
    {
        if (!ready) return;
        var available = AvailableProfiles(library.lessonDatabase, Resources.Load<HardwareProfileCatalog>(HardwareProfileCatalog.ResourcePath));
        for (int i = 0; i < dropdowns.Length; i++)
        {
            options[i].Clear();
            foreach (var profile in available)
                if (profile.kind == Kinds[i]) options[i].Add(profile);
            var dropdown = dropdowns[i];
            dropdown.Hide();
            dropdown.ClearOptions();
            var labels = new List<string> { "Choose " + (i == 0 ? "a " : "") + Labels[i] + "..." };
            foreach (var profile in options[i]) labels.Add(Name(profile));
            dropdown.AddOptions(labels);
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
            dropdown.interactable = options[i].Count > 0;
        }
        ResetResult();
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
    }

    private void SelectionChanged(int value) { ResetResult(); }

    private void ResetResult()
    {
        var missing = new List<string>();
        bool complete = true;
        for (int i = 0; i < dropdowns.Length; i++)
        {
            if (options[i].Count == 0) missing.Add(Labels[i]);
            if (Selected(i) == null) complete = false;
        }
        checkButton.interactable = complete;
        resultText.text = missing.Count > 0
            ? "No compatibility profiles for: " + string.Join(", ", missing) + ". Add these entries to the compatibility catalog."
            : complete ? "Press Check Build to check the selected parts." : "Choose a motherboard, CPU, RAM, and GPU.";
    }

    private void Check()
    {
        // Keep mismatched options selectable for learning, but revalidate catalog membership.
        var available = AvailableProfiles(library.lessonDatabase, Resources.Load<HardwareProfileCatalog>(HardwareProfileCatalog.ResourcePath));
        for (int i = 0; i < dropdowns.Length; i++)
            if (Selected(i) == null || Selected(i).kind != Kinds[i] || !available.Contains(Selected(i)))
            { Open(); return; }
        var result = HardwareCompatibility.CompareBuild(Selected(0), Selected(1), Selected(2), Selected(3));
        resultText.text = "Build check: " + StatusName(result.status) + "\n\n" +
            PairText("CPU", result.cpu) + "\n\n" + PairText("RAM", result.ram) + "\n\n" + PairText("GPU", result.gpu) +
            "\n\nScope: CPU support, one RAM module, and GPU expansion-slot compatibility with the selected motherboard. PSU power/connectors, case fit, and overall performance are not evaluated.";
    }

    private static string PairText(string label, HardwareCompatibility.Result result)
        => label + " + motherboard: " + StatusName(result.status) + "\n" + result.explanation;
    private static string StatusName(HardwareCompatibility.Status status)
        => status == HardwareCompatibility.Status.NeedsVerification ? "Needs verification" : status.ToString();

    private P Selected(int slot)
    {
        int index = dropdowns[slot].value - 1;
        return index >= 0 && index < options[slot].Count ? options[slot][index] : null;
    }
    private static string Name(P profile) => !string.IsNullOrWhiteSpace(profile.productName) ? profile.productName : profile.name;

    public void Close()
    {
        foreach (var dropdown in dropdowns) if (dropdown != null) dropdown.Hide();
        if (panel != null) panel.SetActive(false);
    }

    private void Unbind()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (checkButton != null) checkButton.onClick.RemoveListener(Check);
        foreach (var dropdown in dropdowns) if (dropdown != null) dropdown.onValueChanged.RemoveListener(SelectionChanged);
        ready = false;
    }
    private void OnDisable() { Close(); }
    private void OnDestroy() { Unbind(); }
}
