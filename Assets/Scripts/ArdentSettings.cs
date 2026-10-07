using UnityEngine;

public static class ArdentSettings
{
    private const string Prefix = "ARDENT.Settings.";
    public static float Rotation => Read("Rotation", 1f, .25f, 2f);
    public static float Zoom => Read("Zoom", 1f, .25f, 2f);
    public static float CardGap => Read("CardGap", .05f, .01f, .30f);
    public static float MusicVolume => Read("MusicVolume", .40f, 0f, 1f);
    public static float SfxVolume => Read("SfxVolume", .80f, 0f, 1f);
    public static bool ShowCards => PlayerPrefs.GetInt(Prefix + "ShowCards", 1) != 0;
    private static float Read(string key, float fallback, float min, float max)
    {
        float value = PlayerPrefs.GetFloat(Prefix + key, fallback);
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value,min,max);
    }
    private static void Set(string key,float value,float min,float max)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return;
        PlayerPrefs.SetFloat(Prefix + key, Mathf.Clamp(value,min,max)); PlayerPrefs.Save();
    }
    public static void SetRotation(float value) => Set("Rotation",value,.25f,2f);
    public static void SetZoom(float value) => Set("Zoom",value,.25f,2f);
    public static void SetCardGap(float value) => Set("CardGap",value,.01f,.30f);
    public static void SetMusicVolume(float value) { Set("MusicVolume", value, 0f, 1f); ArdentAudioManager.RefreshVolumes(); }
    public static void SetSfxVolume(float value) { Set("SfxVolume", value, 0f, 1f); ArdentAudioManager.RefreshVolumes(); }
    public static void SetShowCards(bool value) { PlayerPrefs.SetInt(Prefix+"ShowCards",value?1:0); PlayerPrefs.Save(); }
    public static void RestoreDefaults()
    {
        foreach (string key in new[] { "Rotation", "Zoom", "CardGap", "ShowCards", "MusicVolume", "SfxVolume" }) PlayerPrefs.DeleteKey(Prefix+key);
        PlayerPrefs.Save();
        ArdentAudioManager.RefreshVolumes();
    }
}
