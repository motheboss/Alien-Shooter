using System.Collections.Generic;
using UnityEngine;

public enum Sfx { Shoot, EnemyHit, Explosion, PlayerHit, PowerUp, WaveStart, GameOver, UiClick }

/// <summary>
/// Plays all sound effects and background music. Every sound is synthesised in code,
/// so no audio files are required. Drag your own AudioClips into the optional slots to override.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volumes")]
    [Range(0f, 1f)] public float sfxVolume = 0.6f;
    [Range(0f, 1f)] public float musicVolume = 0.2f;
    public bool playMusic = true;

    [Header("Optional overrides (leave empty to use generated sounds)")]
    public AudioClip shootClip;
    public AudioClip enemyHitClip;
    public AudioClip explosionClip;
    public AudioClip playerHitClip;
    public AudioClip powerUpClip;
    public AudioClip waveStartClip;
    public AudioClip gameOverClip;
    public AudioClip uiClickClip;
    public AudioClip musicClip;

    const int SampleRate = 44100;

    enum Waveform { Sine, Square, Saw }

    AudioSource sfxSource;
    AudioSource musicSource;
    readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;

        BuildClips();

        if (playMusic)
        {
            musicSource.clip = musicClip != null ? musicClip : GenerateMusic();
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (musicSource != null) musicSource.volume = playMusic ? musicVolume : 0f;
    }

    /// <summary>Plays a sound effect. Safe to call even if no AudioManager exists (one is created).</summary>
    public static void Play(Sfx sfx, float volumeScale = 1f, float pitchVariation = 0f)
    {
        if (Instance == null)
        {
            AudioManager found = GameUtil.Find<AudioManager>();
            if (found == null) new GameObject("AudioManager").AddComponent<AudioManager>();
            if (Instance == null) return;
        }
        Instance.PlayInternal(sfx, volumeScale, pitchVariation);
    }

    void PlayInternal(Sfx sfx, float volumeScale, float pitchVariation)
    {
        AudioClip clip;
        if (!clips.TryGetValue(sfx, out clip) || clip == null) return;

        sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    // ------------------------------------------------------------------ clip building

    void BuildClips()
    {
        clips[Sfx.Shoot] = Pick(shootClip, MakeClip("Shoot", Tone(0.12f, 900f, 300f, Waveform.Square, 0.25f, 1f)));
        clips[Sfx.EnemyHit] = Pick(enemyHitClip, MakeClip("EnemyHit", Tone(0.07f, 220f, 120f, Waveform.Saw, 0.35f, 1.5f)));

        float[] boom = Mix(Noise(0.5f, 1.6f, 0.85f, 2f), Tone(0.45f, 120f, 40f, Waveform.Sine, 0.8f, 2f));
        clips[Sfx.Explosion] = Pick(explosionClip, MakeClip("Explosion", boom));

        float[] hit = Mix(Noise(0.35f, 1.2f, 0.8f, 1.5f), Tone(0.35f, 200f, 60f, Waveform.Saw, 0.5f, 1f));
        clips[Sfx.PlayerHit] = Pick(playerHitClip, MakeClip("PlayerHit", hit));

        float[] power = Concat(
            Tone(0.07f, Note(3), Note(3), Waveform.Square, 0.25f, 0.5f),
            Tone(0.07f, Note(7), Note(7), Waveform.Square, 0.25f, 0.5f),
            Tone(0.12f, Note(12), Note(12), Waveform.Square, 0.25f, 1f));
        clips[Sfx.PowerUp] = Pick(powerUpClip, MakeClip("PowerUp", power));

        float[] wave = Concat(
            Tone(0.12f, Note(-5), Note(-5), Waveform.Saw, 0.3f, 0.8f),
            Tone(0.22f, Note(2), Note(2), Waveform.Saw, 0.3f, 1f));
        clips[Sfx.WaveStart] = Pick(waveStartClip, MakeClip("WaveStart", wave));

        float[] over = Concat(
            Tone(0.25f, Note(0), Note(0), Waveform.Saw, 0.35f, 0.7f),
            Tone(0.25f, Note(-3), Note(-3), Waveform.Saw, 0.35f, 0.7f),
            Tone(0.25f, Note(-7), Note(-7), Waveform.Saw, 0.35f, 0.7f),
            Tone(0.7f, Note(-12), Note(-24), Waveform.Saw, 0.4f, 1f));
        clips[Sfx.GameOver] = Pick(gameOverClip, MakeClip("GameOver", over));

        clips[Sfx.UiClick] = Pick(uiClickClip, MakeClip("UiClick", Tone(0.05f, 900f, 700f, Waveform.Square, 0.2f, 1f)));
    }

    static AudioClip Pick(AudioClip custom, AudioClip generated)
    {
        return custom != null ? custom : generated;
    }

    static AudioClip GenerateMusic()
    {
        // 32 steps of an A-minor arpeggio bass line, 0.2 seconds each.
        int[] steps =
        {
            0, 7, 12, 7,  0, 7, 12, 7,  -4, 3, 8, 3,  -4, 3, 8, 3,
            -2, 5, 10, 5, -2, 5, 10, 5, -5, 2, 7, 2,  -5, 2, 7, 2
        };

        float[][] parts = new float[steps.Length][];
        for (int i = 0; i < steps.Length; i++)
        {
            float f = Note(-24 + steps[i]);
            parts[i] = Tone(0.2f, f, f, Waveform.Square, 0.12f, 1.2f);
        }
        return MakeClip("Music", Concat(parts));
    }

    static AudioClip MakeClip(string name, float[] data)
    {
        for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ------------------------------------------------------------------ tiny synth

    static float Note(int semitonesFromA4)
    {
        return 440f * Mathf.Pow(2f, semitonesFromA4 / 12f);
    }

    static float Osc(Waveform wave, float phase)
    {
        float p = phase - Mathf.Floor(phase);
        switch (wave)
        {
            case Waveform.Square: return p < 0.5f ? 1f : -1f;
            case Waveform.Saw: return 2f * p - 1f;
            default: return Mathf.Sin(p * 2f * Mathf.PI);
        }
    }

    static float[] Tone(float duration, float freqStart, float freqEnd, Waveform wave, float volume, float decayPower)
    {
        int n = Mathf.Max(1, (int)(duration * SampleRate));
        float[] data = new float[n];
        float phase = 0f;
        int attack = Mathf.Max(1, (int)(0.004f * SampleRate));

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float freq = Mathf.Lerp(freqStart, freqEnd, t);
            phase += freq / SampleRate;
            float env = Mathf.Pow(1f - t, decayPower) * Mathf.Min(1f, i / (float)attack);
            data[i] = Osc(wave, phase) * env * volume;
        }
        return data;
    }

    static float[] Noise(float duration, float volume, float smoothing, float decayPower)
    {
        int n = Mathf.Max(1, (int)(duration * SampleRate));
        float[] data = new float[n];
        System.Random rng = new System.Random(1234);
        float last = 0f;

        for (int i = 0; i < n; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            last = Mathf.Lerp(white, last, smoothing);
            float t = i / (float)n;
            data[i] = last * Mathf.Pow(1f - t, decayPower) * volume;
        }
        return data;
    }

    static float[] Mix(float[] a, float[] b)
    {
        float[] result = new float[Mathf.Max(a.Length, b.Length)];
        for (int i = 0; i < result.Length; i++)
        {
            float va = i < a.Length ? a[i] : 0f;
            float vb = i < b.Length ? b[i] : 0f;
            result[i] = va + vb;
        }
        return result;
    }

    static float[] Concat(params float[][] parts)
    {
        int total = 0;
        for (int i = 0; i < parts.Length; i++) total += parts[i].Length;

        float[] result = new float[total];
        int offset = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            System.Array.Copy(parts[i], 0, result, offset, parts[i].Length);
            offset += parts[i].Length;
        }
        return result;
    }
}
