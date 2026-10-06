using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public sealed class ArdentAudioManager : MonoBehaviour
{
    private const string LibraryResourceName = "ArdentAudioLibrary";
    private static ArdentAudioManager instance;

    private ArdentAudioLibrary library;
    private AudioSource musicA;
    private AudioSource musicB;
    private AudioSource sfx;
    private AudioSource activeMusic;
    private Coroutine fadeRoutine;
    private float sceneMusicScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var root = new GameObject("ArdentAudioManager");
        root.AddComponent<ArdentAudioManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        library = Resources.Load<ArdentAudioLibrary>(LibraryResourceName);
        musicA = Source("Music A", true);
        musicB = Source("Music B", true);
        sfx = Source("SFX", false);
        activeMusic = musicA;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyVolumes();
    }

    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private AudioSource Source(string objectName, bool loop)
    {
        var child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        return source;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (library == null)
        {
            library = Resources.Load<ArdentAudioLibrary>(LibraryResourceName);
            if (library == null)
            {
                Debug.LogWarning("ArdentAudioLibrary is missing. Run ARDENT > Audio > Install Complete Audio System.");
                return;
            }
        }
        SelectSceneMusic(scene.name, out AudioClip clip, out sceneMusicScale);
        ChangeMusic(clip);
        StartCoroutine(BindSceneButtons());
    }

    private void SelectSceneMusic(string sceneName, out AudioClip clip, out float scale)
    {
        scale = 1f;
        switch (sceneName)
        {
            case "Hardware_Library": clip = library.hardwareLibraryBGM; break;
            case "QuizScene": clip = library.quizBGM; break;
            case "ARScene": clip = library.mainBGM; scale = .45f; break;
            default: clip = library.mainBGM; break;
        }
    }

    private void ChangeMusic(AudioClip clip)
    {
        if (clip == null)
        {
            musicA.Stop(); musicB.Stop();
            return;
        }
        if (activeMusic != null && activeMusic.clip == clip && activeMusic.isPlaying)
        {
            ApplyVolumes();
            return;
        }
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        AudioSource next = activeMusic == musicA ? musicB : musicA;
        next.clip = clip;
        next.volume = 0f;
        next.Play();
        fadeRoutine = StartCoroutine(CrossFade(activeMusic, next, .6f));
    }

    private IEnumerator CrossFade(AudioSource previous, AudioSource next, float duration)
    {
        float previousStart = previous != null ? previous.volume : 0f;
        float target = ArdentSettings.MusicVolume * sceneMusicScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (previous != null) previous.volume = Mathf.Lerp(previousStart, 0f, t);
            next.volume = Mathf.Lerp(0f, target, t);
            yield return null;
        }
        if (previous != null) { previous.Stop(); previous.clip = null; }
        next.volume = target;
        activeMusic = next;
        fadeRoutine = null;
    }

    private IEnumerator BindSceneButtons()
    {
        yield return null;
        BindButtons();
        yield return new WaitForSecondsRealtime(.25f);
        BindButtons();
        yield return new WaitForSecondsRealtime(.75f);
        BindButtons();
    }

    private void BindButtons()
    {
        string scene = SceneManager.GetActiveScene().name;
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null || button.gameObject.scene.name != scene || button.GetComponent<ArdentButtonSound>() != null) continue;
            var binding = button.gameObject.AddComponent<ArdentButtonSound>();
            binding.Initialize(button, ButtonSound(button, scene));
        }
    }

    private static ArdentSound ButtonSound(Button button, string scene)
    {
        string name = button.name.ToLowerInvariant();
        if (scene == "QuizScene" && (name.Contains("answer") || button.GetComponent<QuizAnswerButton>() != null))
            return ArdentSound.QuizAnswerButton;
        if (scene == "Start_Learning" || scene == "COCScene" || scene == "ActivitySelectionScene" ||
            button.GetComponentInParent<LessonCard>() != null || name.Contains("lesson"))
            return ArdentSound.LessonButton;
        return ArdentSound.Button;
    }

    private void PlayInternal(ArdentSound sound)
    {
        if (library == null || sfx == null) return;
        var clip = library.Clip(sound);
        if (clip != null && ArdentSettings.SfxVolume > 0f) sfx.PlayOneShot(clip, ArdentSettings.SfxVolume);
    }

    public static void Play(ArdentSound sound)
    {
        instance?.PlayInternal(sound);
    }

    public static void RefreshVolumes()
    {
        instance?.ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (sfx != null) sfx.volume = 1f;
        float volume = ArdentSettings.MusicVolume * sceneMusicScale;
        if (activeMusic != null) activeMusic.volume = volume;
        if (musicA != null && musicA != activeMusic && !musicA.isPlaying) musicA.volume = 0f;
        if (musicB != null && musicB != activeMusic && !musicB.isPlaying) musicB.volume = 0f;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}
