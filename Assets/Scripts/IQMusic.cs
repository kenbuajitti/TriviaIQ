using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// One player survives scene changes; each TriviaIQ canvas owns its controls.
public sealed class IQMusic : MonoBehaviour
{
    const string Preference = "TriviaIQ.SoundEnabled";
    static IQMusic instance;
    AudioSource source;
    bool started;
    public bool SoundEnabled { get; private set; }

    public static IQMusic GetPlayer()
    {
        if (instance != null) return instance;
        var go = new GameObject("TriviaIQ Music");
        instance = go.AddComponent<IQMusic>();
        DontDestroyOnLoad(go);
        return instance;
    }
    void Awake()
    {
        SoundEnabled = PlayerPrefs.GetInt(Preference, 1) != 0;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0;
        source.volume = .12f;
        source.clip = Resources.Load<AudioClip>("IQAudio/Puzzling");
        if (source.clip == null) Debug.LogError("Missing IQAudio/Puzzling music asset.");
    }
    void Update()
    {
        bool gesture = false;
#if ENABLE_INPUT_SYSTEM
        gesture = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        gesture = Input.GetMouseButtonDown(0) || Input.anyKeyDown || Input.touchCount > 0;
#endif
        if (gesture) Begin();
    }
    public void Begin()
    {
        if (started || !SoundEnabled || source.clip == null) return;
        started = true;
        source.Play();
    }
    public void ToggleSound()
    {
        SoundEnabled = !SoundEnabled;
        source.mute = !SoundEnabled;
        PlayerPrefs.SetInt(Preference, SoundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        if (SoundEnabled) Begin();
    }
}

