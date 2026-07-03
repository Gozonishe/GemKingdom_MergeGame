using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class UIAudioController : MonoBehaviour
{
    public static UIAudioController Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource uiSoundSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Clips")]
    [SerializeField] private AudioClip defaultButtonClickClip;
    [SerializeField] private AudioClip menuMusicClip;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float uiSoundVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;

    [Header("Music")]
    [SerializeField] private bool playMenuMusicOnAwake;
    [SerializeField] private bool persistBetweenScenes;

    private bool uiSoundsMuted;
    private bool musicMuted;

    public bool IsUISoundMuted => uiSoundsMuted;
    public bool IsMusicMuted => musicMuted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        ConfigureSources();

        if (playMenuMusicOnAwake)
        {
            PlayMenuMusic();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Reset()
    {
        ConfigureSources();
    }

    private void OnValidate()
    {
        if (uiSoundSource != null)
        {
            uiSoundSource.playOnAwake = false;
            uiSoundSource.loop = false;
            uiSoundSource.volume = uiSoundVolume;
        }

        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }
    }

    public void PlayButtonClick()
    {
        PlayUISound(defaultButtonClickClip);
    }

    public void ConfigureAudioClips(AudioClip buttonClickClip, AudioClip musicClip)
    {
        if (buttonClickClip != null)
        {
            defaultButtonClickClip = buttonClickClip;
        }

        if (musicClip != null)
        {
            menuMusicClip = musicClip;
        }
    }

    public void PlayUISound(AudioClip clip, float volumeScale = 1f)
    {
        if (uiSoundsMuted || clip == null)
        {
            return;
        }

        ConfigureSources();
        uiSoundSource.PlayOneShot(clip, Mathf.Clamp01(uiSoundVolume * volumeScale));
    }

    public void PlayMenuMusic()
    {
        if (musicMuted || menuMusicClip == null)
        {
            return;
        }

        ConfigureSources();

        if (musicSource.clip != menuMusicClip)
        {
            musicSource.clip = menuMusicClip;
        }

        musicSource.volume = musicVolume;

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void StopMenuMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
    }

    public void SetUISoundMuted(bool isMuted)
    {
        uiSoundsMuted = isMuted;
        ConfigureSources();
        uiSoundSource.volume = uiSoundsMuted ? 0f : uiSoundVolume;
    }

    public void SetMusicMuted(bool isMuted)
    {
        musicMuted = isMuted;
        ConfigureSources();
        musicSource.volume = musicMuted ? 0f : musicVolume;

        if (musicSource == null)
        {
            return;
        }

        if (musicMuted)
        {
            musicSource.Pause();
        }
        else
        {
            PlayMenuMusic();
        }
    }

    public void ToggleUISoundMuted()
    {
        SetUISoundMuted(!uiSoundsMuted);
    }

    public void ToggleMusicMuted()
    {
        SetMusicMuted(!musicMuted);
    }

    public void SetUISoundVolume(float volume)
    {
        uiSoundVolume = Mathf.Clamp01(volume);

        if (uiSoundSource != null)
        {
            uiSoundSource.volume = uiSoundsMuted ? 0f : uiSoundVolume;
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (musicSource != null)
        {
            musicSource.volume = musicMuted ? 0f : musicVolume;
        }
    }

    private void ConfigureSources()
    {
        uiSoundSource = uiSoundSource != null ? uiSoundSource : gameObject.AddComponent<AudioSource>();
        musicSource = musicSource != null ? musicSource : gameObject.AddComponent<AudioSource>();

        uiSoundSource.playOnAwake = false;
        uiSoundSource.loop = false;
        uiSoundSource.volume = uiSoundsMuted ? 0f : uiSoundVolume;

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicMuted ? 0f : musicVolume;
    }
}
