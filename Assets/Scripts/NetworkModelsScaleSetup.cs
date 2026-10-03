#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NetworkModelsScaleSetup
{
    private static readonly string[] Names = {
        "pc_unit", "switch_DLink_DGS108", "router_DLink_AC1200", "patch_panel",
        "fishtapes", "crimping_tool", "wirestripper", "cable_stripper_fiberoptics"
    };
    // Reference lengths in meters. Generic tools are approximate modeling baselines.
    private static readonly float[] Targets = { .6858f, .162f, .270f, .4826f, .25f, .20f, .165f, .165f };
    private static readonly int[] Axes = { 0, 0, 0, 0, 0, 2, 0, 2 };
    private const float ScreenDiagonalToSetupWidth = .7808708332367584f;
    private const string PcMeshHash = "d0e4b5abb4206a414f50cffc3e542014ba71193a847b21005d10554b6f88664c";
    private static string AssetPath(int i) => "Assets/Models/" + Names[i] + ".prefab";

    [MenuItem("ARDENT/Models/Resize New Network Models to Life Size")]
    public static void Resize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
        { Debug.LogWarning("Exit Play Mode and save/close Prefab Mode before resizing."); return; }

        string pcPath = "Assets/Resources/3D/pc_unit.fbx";
        using (var sha = SHA256.Create())
        {
            if (!File.Exists(pcPath) || BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(pcPath))).Replace("-", "").ToLowerInvariant() != PcMeshHash)
            { Debug.LogError("The PC mesh changed. Re-measure its visible screen before applying the 27-inch calibration. No changes made."); return; }
        }

        var scales = new Vector3[Names.Length];
        try
        {
            // Preflight everything before writing. Exclude colliders, connection points and lines.
            for (int i = 0; i < Names.Length; i++)
            {
                var root = PrefabUtility.LoadPrefabContents(AssetPath(i));
                try
                {
                    Vector3 scale = root.transform.localScale;
                    if (!Positive(scale.x) || !Positive(scale.y) || !Positive(scale.z))
                        throw new InvalidOperationException("Invalid root scale: " + Names[i]);
                    if (i == 0 && (Mathf.Abs(scale.x - scale.y) > .00001f || Mathf.Abs(scale.x - scale.z) > .00001f))
                        throw new InvalidOperationException("PC root must have uniform scale for screen calibration.");
                    float current = Measure(i, MeshBounds(root), scale);
                    if (!Positive(current)) throw new InvalidOperationException("Invalid dimensions: " + Names[i]);
                    scales[i] = scale * (Targets[i] / current);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        catch (Exception e) { Debug.LogError("No changes made: " + e.Message); return; }

        string backup = Path.GetFullPath("Library/ARDENTScaleBackups/Network-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backup);
        for (int i = 0; i < Names.Length; i++) File.Copy(AssetPath(i), Path.Combine(backup, Names[i] + ".prefab"));
        var report = new StringBuilder("Network model scale update — full mesh dimensions X/Y/Z in mm:\n");
        try
        {
            for (int i = 0; i < Names.Length; i++)
            {
                var root = PrefabUtility.LoadPrefabContents(AssetPath(i));
                try
                {
                    root.transform.localScale = scales[i];
                    if (Names[i] == "cable_stripper_fiberoptics")
                    {
                        var collider = root.GetComponent<BoxCollider>();
                        if (collider != null)
                        {
                            Bounds bounds = MeshBounds(root);
                            collider.center = bounds.center;
                            collider.size = bounds.size;
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, AssetPath(i), out bool success);
                    if (!success) throw new InvalidOperationException("Save failed: " + Names[i]);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }

                var saved = PrefabUtility.LoadPrefabContents(AssetPath(i));
                try
                {
                    Bounds bounds = MeshBounds(saved);
                    if (Mathf.Abs(Measure(i, bounds, saved.transform.localScale) - Targets[i]) > .00001f)
                        throw new InvalidOperationException("Saved measurement failed: " + Names[i]);
                    Vector3 size = Vector3.Scale(bounds.size, saved.transform.localScale) * 1000f;
                    report.AppendLine($"{Names[i]}: {size.x:0.##} x {size.y:0.##} x {size.z:0.##}; root scale {saved.transform.localScale.ToString("F6")}");
                }
                finally { PrefabUtility.UnloadPrefabContents(saved); }
            }
        }
        catch (Exception e)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                File.Copy(Path.Combine(backup, Names[i] + ".prefab"), AssetPath(i), true);
                AssetDatabase.ImportAsset(AssetPath(i), ImportAssetOptions.ForceUpdate);
            }
            Debug.LogError("Scale update rolled back: " + e.Message);
            return;
        }
        report.AppendLine("PC visible screen diagonal: 685.8 mm (27 inches). Other desktop pieces retain their modeled proportions.");
        report.AppendLine("Reference widths: switch 162 mm; router 270 mm; patch panel 482.6 mm. Router retains two antennas.");
        report.AppendLine("Approximate tools: fish-tape reel width 250 mm; crimper length 200 mm; wire stripper length 165 mm; fiber stripper length 165 mm.");
        report.AppendLine("Uniform scaling preserves proportions; secondary dimensions are not reshaped to manufacturer specifications. Fiber-stripper collider fitted to the full mesh.");
        report.AppendLine("Backup: " + backup);
        File.WriteAllText(Path.Combine(backup, "result.txt"), report.ToString());
        Debug.Log(report.ToString());
    }

    private static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    private static float Measure(int i, Bounds bounds, Vector3 scale)
    {
        Vector3 size = Vector3.Scale(bounds.size, scale);
        return i == 0 ? size.x * ScreenDiagonalToSetupWidth : size[Axes[i]];
    }

    // Include every mesh (including the stripper's spring) in root-local space.
    private static Bounds MeshBounds(GameObject root)
    {
        bool found = false;
        Bounds total = default;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            Bounds bounds = filter.sharedMesh.bounds;
            Matrix4x4 matrix = Matrix4x4.identity;
            for (Transform child = filter.transform; child != root.transform; child = child.parent)
                matrix = Matrix4x4.TRS(child.localPosition, child.localRotation, child.localScale) * matrix;
            for (int mask = 0; mask < 8; mask++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((mask & 1) == 0 ? -1 : 1, (mask & 2) == 0 ? -1 : 1, (mask & 4) == 0 ? -1 : 1));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                if (!found) { total = new Bounds(point, Vector3.zero); found = true; }
                else total.Encapsulate(point);
            }
        }
        if (!found) throw new InvalidOperationException("No mesh found: " + root.name);
        return total;
    }
}
#endif
