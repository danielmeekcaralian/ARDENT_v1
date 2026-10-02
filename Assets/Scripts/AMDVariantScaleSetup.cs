#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AMDVariantScaleSetup
{
    private static readonly string[] Paths = {
        "Assets/Models/cpu_AMD_Ryzen_5_5600.prefab",
        "Assets/Models/mb_MSI_B550M.prefab",
        "Assets/Models/ram_Kingston_DDR4.prefab"
    };
    // Unity units are meters. CPU size is an approximate modeling baseline.
    private static readonly float[] Lengths = { .040f, .244f, .13335f };

    [MenuItem("ARDENT/Hardware Library/Resize AMD Variants to Life Size")]
    public static void Resize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("Exit Play Mode before resizing prefabs."); return; }
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        { Debug.LogWarning("Save and close Prefab Mode, then run Resize AMD Variants to Life Size again."); return; }
        var scales = new Vector3[Paths.Length];
        // Preflight all three before changing any asset. The current models each
        // have a root mesh, so temporary children and assembly targets are excluded.
        for (int i = 0; i < Paths.Length; i++)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Paths[i]);
            if (asset == null) { Debug.LogError("Missing prefab: " + Paths[i]); return; }
            var mesh = asset.GetComponent<MeshFilter>();
            if (mesh == null || mesh.sharedMesh == null)
            { Debug.LogError("Expected a root mesh on " + Paths[i] + ". No changes made."); return; }
            var current = asset.transform.localScale;
            var size = Vector3.Scale(mesh.sharedMesh.bounds.size, current);
            float longest = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            if (longest <= 0 || float.IsNaN(longest) || float.IsInfinity(longest) ||
                current.x <= 0 || current.y <= 0 || current.z <= 0)
            { Debug.LogError("Invalid mesh dimensions or root scale on " + Paths[i]); return; }
            scales[i] = current * (Lengths[i] / longest);
        }

        string backup = Path.GetFullPath("Library/ARDENTScaleBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backup);
        foreach (string path in Paths) File.Copy(path, Path.Combine(backup, Path.GetFileName(path)));
        var report = new StringBuilder("AMD variant scale update (mesh dimensions in mm):\n");
        try
        {
            for (int i = 0; i < Paths.Length; i++)
            {
                var root = PrefabUtility.LoadPrefabContents(Paths[i]);
                try
                {
                    root.transform.localScale = scales[i];
                    PrefabUtility.SaveAsPrefabAsset(root, Paths[i], out bool success);
                    if (!success) throw new InvalidOperationException("Could not save " + Paths[i]);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(Paths[i]);
                var size = Vector3.Scale(saved.GetComponent<MeshFilter>().sharedMesh.bounds.size, saved.transform.localScale);
                float longest = Mathf.Max(size.x, size.y, size.z);
                if (Mathf.Abs(longest - Lengths[i]) > .00001f)
                    throw new InvalidOperationException("Saved size verification failed: " + Paths[i]);
                report.AppendLine($"{saved.name}: {size.x * 1000f:0.##} x {size.y * 1000f:0.##} x {size.z * 1000f:0.##}; scale {saved.transform.localScale}");
            }
        }
        catch (Exception exception)
        {
            foreach (string path in Paths)
            {
                File.Copy(Path.Combine(backup, Path.GetFileName(path)), path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            Debug.LogError("Scale update rolled back: " + exception.Message);
            return;
        }
        report.AppendLine("Uniform scaling preserves modeled proportions; secondary dimensions are not reshaped. CPU uses an approximate 40 mm footprint.");
        report.AppendLine("Original prefab backups: " + backup);
        File.WriteAllText(Path.Combine(backup, "result.txt"), report.ToString());
        Debug.Log(report.ToString());
    }
}
#endif
