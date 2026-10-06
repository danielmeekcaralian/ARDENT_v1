using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable]
public class ARCheckpointData
{
    public int version = 1;
    public int lessonId;
    public string signature;
    public string[] inspected = new string[0];
    public int assembled;
    public int removed;
    public int phase;
    public NetworkCheckpointData network;
    public RJ45CheckpointData rj45;
    public NetworkCableCheckpointData networkCable;
}

public static class ARCheckpointStore
{
    private static string PathFor(int lesson) => Path.Combine(Application.persistentDataPath,
        "ar-checkpoint-" + lesson + ".json");

    public static string Signature(ARActivityData activity)
    {
        var text = new StringBuilder("v1|").Append(activity.activityID).Append('|').Append((int)activity.activityType);
        if (activity.availableObjects != null)
            foreach (var item in activity.availableObjects)
                text.Append('|').Append(item?.prefab != null ? item.prefab.name : "null");
        if (activity.assemblyActivity != null)
        {
            text.Append('|').Append(activity.assemblyActivity.includeDisassembly);
            if (activity.assemblyActivity.steps != null)
                foreach (var step in activity.assemblyActivity.steps)
                    text.Append('|').Append(step?.component?.prefab != null ? step.component.prefab.name : "null")
                        .Append(':').Append(step?.anchorID).Append(':').Append(step?.targetID);
        }
        if (activity.activityType == ARActivityType.NetworkDesign)
        {
            text.Append("|network-v1");
            if (activity.availableObjects != null)
                foreach (var item in activity.availableObjects)
                {
                    text.Append('|').Append(item?.LibraryItemId);
                    if (item?.prefab == null) continue;
                    var node = item.prefab.GetComponent<NetworkNode>();
                    text.Append(':').Append(node != null ? (int)node.NodeType : -1);
                    var scale = item.prefab.transform.localScale;
                    text.Append(':').Append(scale.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                        .Append(':').Append(scale.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                        .Append(':').Append(scale.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                }
        }
        if(activity.activityType == ARActivityType.RJ45Termination)
        {
            text.Append("|rj45-checkpoint-v1");
            if(activity.availableObjects!=null)foreach(var item in activity.availableObjects)text.Append('|').Append(item?.LibraryItemId);
        }
        if (activity.activityType == ARActivityType.NetworkCables)
        {
            text.Append("|network-cables-v1");
            if (activity.availableObjects != null)
                foreach (var item in activity.availableObjects) text.Append('|').Append(item?.LibraryItemId);
        }
        using (var hash = SHA256.Create())
            return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
    }

    public static bool IsValid(ARCheckpointData data, int lesson, string signature, bool assembly, int steps, bool disassembly, int networkPrefabCount = -1, bool rj45 = false, bool networkCable = false)
    {
        if (data == null || data.version != 1 || data.lessonId != lesson || data.signature != signature ||
            data.inspected == null || data.inspected.Length > 10000) return false;
        // JsonUtility materializes empty serializable child objects when loading JSON,
        // so unused network/RJ45 payloads cannot reliably be checked for null.
        // Validate only the payload that belongs to the current activity type.
        if (networkCable) return !assembly && data.assembled == 0 && data.removed == 0 && data.phase == 0 &&
            data.inspected.Length == 0 && NetworkCableCheckpointRules.IsValid(data.networkCable);
        if (rj45) return !assembly && data.assembled == 0 && data.removed == 0 && data.phase == 0 &&
            data.inspected.Length == 0 && RJ45CheckpointRules.IsValid(data.rj45);
        if (networkPrefabCount >= 0)
            return !assembly && data.assembled == 0 && data.removed == 0 && data.phase == 0 &&
                data.inspected.Length == 0 && NetworkCheckpointRules.IsValid(data.network, networkPrefabCount);
        if (!assembly) return data.assembled == 0 && data.removed == 0 && data.phase == 0 && data.inspected.Length > 0;
        if (steps <= 0 || data.assembled < 1 || data.assembled > steps || data.removed < 0 || data.removed >= steps) return false;
        if (data.phase == 1) return data.assembled < steps && data.removed == 0;
        if (data.phase == 2) return disassembly && data.assembled == steps && data.removed == 0;
        return data.phase == 3 && disassembly && data.assembled == steps;
    }

    public static ARCheckpointData Load(int lesson, string signature, bool assembly, int steps, bool disassembly, int networkPrefabCount = -1, bool rj45 = false, bool networkCable = false)
    {
        foreach (var path in new[] { PathFor(lesson), PathFor(lesson) + ".bak" })
        {
            try
            {
                if (!File.Exists(path)) continue;
                if (new FileInfo(path).Length > 4 * 1024 * 1024) continue;
                var data = JsonUtility.FromJson<ARCheckpointData>(File.ReadAllText(path));
                if (IsValid(data, lesson, signature, assembly, steps, disassembly, networkPrefabCount, rj45, networkCable)) return data;
            }
            catch (Exception ex) { Debug.LogWarning("Could not read AR checkpoint: " + ex.Message); }
        }
        return null;
    }

    public static bool Save(ARCheckpointData data)
    {
        string path = PathFor(data.lessonId), temp = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(temp, JsonUtility.ToJson(data));
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            // Keep the previous valid snapshot if replacement is interrupted.
            File.Copy(temp, path, true);
            File.Delete(temp);
            return true;
        }
        catch (Exception ex) { Debug.LogWarning("AR checkpoint could not be saved: " + ex.Message); return false; }
    }

    public static void Clear(int lesson)
    {
        try
        {
            foreach (var suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(PathFor(lesson) + suffix)) File.Delete(PathFor(lesson) + suffix);
        }
        catch (Exception ex) { Debug.LogWarning("AR checkpoint could not be cleared: " + ex.Message); }
    }
}
