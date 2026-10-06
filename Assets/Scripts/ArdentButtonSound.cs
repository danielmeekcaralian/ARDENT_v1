using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ArdentButtonSound : MonoBehaviour
{
    private Button button;
    private ArdentSound sound;
    private bool initialized;

    public void Initialize(Button target, ArdentSound value)
    {
        if (initialized || target == null) return;
        initialized = true;
        button = target;
        sound = value;
        button.onClick.AddListener(Play);
    }

    private void Play()
    {
        ArdentAudioManager.Play(sound);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(Play);
    }
}
