using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    private const int SampleRate = 22050;

    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float engineVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.85f;
    [SerializeField] private AudioClip musicTrack;
    [SerializeField] private PlayerLaneController player;

    public static GameAudioController Instance { get; private set; }

    private AudioSource musicSource;
    private AudioSource engineSource;
    private AudioSource effectsSource;
    private GameState currentState;
    private bool hasState;
    private bool hasLoggedMissingPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one GameAudioController can be active at a time.", this);
            enabled = false;
            return;
        }

        Instance = this;
        CreateAudioSources();
        if (!CreateAudioClips())
        {
            enabled = false;
        }
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("A GameManager is required to synchronize game audio.", this);
            enabled = false;
            return;
        }

        if (FindFirstObjectByType<AudioListener>() == null)
        {
            Debug.LogError("No AudioListener is active in the scene; game audio cannot be heard.", this);
            enabled = false;
            return;
        }

        if (AudioListener.volume <= 0f || AudioListener.pause)
        {
            Debug.LogWarning("Unity's global audio volume is muted or paused.", this);
        }

        GameManager.Instance.StateChanged += HandleStateChanged;
        GameManager.Instance.DamageTaken += PlayCollision;
        SetGameState(GameManager.Instance.State);
        Debug.Log("Procedural music and game sound effects are active.", this);
    }

    private void Update()
    {
        if (engineSource == null || !engineSource.isPlaying || player == null)
        {
            if (engineSource != null && engineSource.isPlaying && player == null && !hasLoggedMissingPlayer)
            {
                Debug.LogError("Assign the player to synchronize the engine sound with vehicle speed.", this);
                hasLoggedMissingPlayer = true;
            }

            return;
        }

        engineSource.pitch = Mathf.Lerp(0.82f, 1.65f, Mathf.InverseLerp(12f, 24f, player.ForwardSpeed));
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= HandleStateChanged;
            GameManager.Instance.DamageTaken -= PlayCollision;
        }

        if (Instance == this)
        {
            Instance = null;
        }

        if (engineSource != null)
        {
            Destroy(engineSource.clip);
        }
    }

    public void PlayCoinPickup()
    {
        PlayEffect(CreateTone(0.22f, 880f, 1320f, 0.3f, 0.12f, 2f, true));
    }

    public void PlayFuelPickup()
    {
        PlayEffect(CreateTone(0.32f, 440f, 880f, 0.28f, 0.14f, 2f, true));
    }

    public void PlayUiClick()
    {
        PlayEffect(CreateTone(0.055f, 720f, 520f, 0.18f, 0.035f, 0f, false));
    }

    public void PlayFirework()
    {
        PlayEffect(CreateTone(0.65f, 220f, 900f, 0.24f, 0.1f, 1f, true));
    }

    private void CreateAudioSources()
    {
        musicSource = CreateSource("Music", true, musicVolume);
        engineSource = CreateSource("Engine", true, engineVolume);
        effectsSource = CreateSource("Effects", false, effectsVolume);
    }

    private AudioSource CreateSource(string sourceName, bool loop, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume * masterVolume;
        source.dopplerLevel = 0f;
        return source;
    }

    private bool CreateAudioClips()
    {
        if (musicTrack == null)
        {
            Debug.LogError("Assign the licensed music track to Music Track.", this);
            return false;
        }

        musicSource.clip = musicTrack;
        engineSource.clip = CreateEngineLoop();
        return true;
    }

    private void HandleStateChanged(GameState state)
    {
        SetGameState(state);
    }

    private void SetGameState(GameState state)
    {
        if (hasState && currentState == state)
        {
            return;
        }

        currentState = state;
        hasState = true;

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }

        if (state == GameState.Playing)
        {
            if (!engineSource.isPlaying)
            {
                engineSource.Play();
            }
        }
        else
        {
            engineSource.Stop();
            if (state == GameState.GameOver)
            {
                PlayEffect(CreateTone(0.8f, 440f, 110f, 0.3f, 0.16f, 0f, false));
            }
        }
    }

    private void PlayCollision()
    {
        PlayEffect(CreateNoiseBurst(0.28f, 0.32f, 0.3f));
    }

    private void PlayEffect(AudioClip clip)
    {
        if (effectsSource == null || clip == null)
        {
            return;
        }

        effectsSource.PlayOneShot(clip);
        Destroy(clip, clip.length + 0.1f);
    }

    private static AudioClip CreateEngineLoop()
    {
        const float duration = 2f;
        AudioClip clip = CreateClip("Engine Loop", duration);
        float[] samples = new float[Mathf.CeilToInt(SampleRate * duration)];

        for (int i = 0; i < samples.Length; i++)
        {
            float time = (float)i / SampleRate;
            float fundamental = Mathf.Sin(2f * Mathf.PI * 55f * time);
            float harmonic = Mathf.Sin(2f * Mathf.PI * 110f * time);
            float rattle = Mathf.Sin(2f * Mathf.PI * 220f * time);
            float modulation = 0.82f + 0.18f * Mathf.Sin(2f * Mathf.PI * 2f * time);
            samples[i] = (fundamental * 0.58f + harmonic * 0.28f + rattle * 0.14f) * modulation * 0.32f;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateTone(
        float duration,
        float startFrequency,
        float endFrequency,
        float amplitude,
        float attack,
        float decay,
        bool addHarmonic)
    {
        AudioClip clip = CreateClip("Generated Sound Effect", duration);
        float[] samples = new float[Mathf.CeilToInt(SampleRate * duration)];
        float phase = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float time = (float)i / SampleRate;
            float progress = time / duration;
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            phase += 2f * Mathf.PI * frequency / SampleRate;
            float envelope = Mathf.Clamp01(time / Mathf.Max(0.001f, attack));
            if (decay > 0f)
            {
                envelope *= Mathf.Exp(-time * decay);
            }

            float sample = Mathf.Sin(phase);
            if (addHarmonic)
            {
                sample = sample * 0.75f + Mathf.Sin(phase * 2f) * 0.25f;
            }

            samples[i] = sample * envelope * amplitude;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateNoiseBurst(float duration, float amplitude, float decay)
    {
        AudioClip clip = CreateClip("Collision Sound", duration);
        float[] samples = new float[Mathf.CeilToInt(SampleRate * duration)];
        float previousNoise = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float time = (float)i / SampleRate;
            float noise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(noise, previousNoise, 0.42f);
            float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(95f, 48f, time / duration) * time);
            samples[i] = (previousNoise * 0.68f + thud * 0.32f) * Mathf.Exp(-time / decay) * amplitude;
        }

        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateClip(string clipName, float duration)
    {
        return AudioClip.Create(clipName, Mathf.CeilToInt(SampleRate * duration), 1, SampleRate, false);
    }
}
