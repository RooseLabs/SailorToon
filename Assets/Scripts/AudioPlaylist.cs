using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioPlaylist : MonoBehaviour
{
    public static AudioPlaylist Instance;

    [Header("Playlist")]
    [Tooltip("Audio clips played in order. Loops back to the first clip after the last one.")]
    public List<AudioClip> playlist = new();

    [Tooltip("Play the playlist automatically on Start.")]
    public bool playOnStart = true;

    [Header("Mix")]
    [Range(0f, 1f)]
    [Tooltip("Target volume for whichever clip is currently active.")]
    public float volume = 1f;

    [Min(0f)]
    [Tooltip("Crossfade duration (seconds) used both between playlist tracks and when entering/leaving override.")]
    public float crossfadeDuration = 1.5f;

    [Header("Source Template (optional)")]
    [Tooltip("If assigned, the two internal AudioSources copy this source's settings (spatial blend, output mixer, etc.). Leave null for plain 2D playback.")]
    public AudioSource sourceTemplate;

    private AudioSource m_a;
    private AudioSource m_b;
    private AudioSource m_active;
    private AudioSource m_idle;

    private int m_playlistIndex = -1;
    private bool m_isOverriding;
    private AudioClip m_overrideClip;
    private Coroutine m_transitionRoutine;

    private void Awake()
    {
        m_a = CreateSource("AudioPlaylist_A");
        m_b = CreateSource("AudioPlaylist_B");
        m_active = m_a;
        m_idle = m_b;
    }

    private void Start()
    {
        if (playOnStart && playlist.Count > 0)
            PlayPlaylist();
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void Update()
    {
        // Auto-advance to the next playlist clip a little before the current one ends,
        // so the crossfade overlaps the tail of the outgoing track.
        if (m_isOverriding || m_transitionRoutine != null) return;
        if (!m_active.isPlaying || !m_active.clip) return;

        float remaining = m_active.clip.length - m_active.time;
        if (remaining <= Mathf.Max(0.05f, crossfadeDuration))
            AdvancePlaylist();
    }

    // --- Public API -----------------------------------------------------------------------------

    /// <summary>Start (or restart) the playlist from the first clip.</summary>
    public void PlayPlaylist()
    {
        if (playlist.Count == 0) return;
        m_isOverriding = false;
        m_overrideClip = null;
        m_playlistIndex = -1;
        AdvancePlaylist();
    }

    /// <summary>
    /// Crossfade into <paramref name="clip"/> and loop it until <see cref="ReturnToPlaylist"/> is called.
    /// Calling again with a different clip crossfades to that new clip.
    /// </summary>
    public void PlayOverride(AudioClip clip, float? crossfadeDurationOverride = null)
    {
        if (clip == null) return;
        if (m_isOverriding && m_overrideClip == clip) return;

        m_isOverriding = true;
        m_overrideClip = clip;
        StartTransitionTo(clip, loop: true, crossfadeDurationOverride);
    }

    /// <summary>Crossfade back to the playlist, picking up at the next clip.</summary>
    public void ReturnToPlaylist()
    {
        if (!m_isOverriding) return;
        m_isOverriding = false;
        m_overrideClip = null;
        AdvancePlaylist();
    }

    /// <summary>Stop everything with a quick fade-out.</summary>
    public void StopAll()
    {
        if (m_transitionRoutine != null) StopCoroutine(m_transitionRoutine);
        m_transitionRoutine = StartCoroutine(FadeOutAndStop());
    }

    // --- Internal -------------------------------------------------------------------------------

    private void AdvancePlaylist()
    {
        if (playlist.Count == 0) return;

        m_playlistIndex = (m_playlistIndex + 1) % playlist.Count;
        AudioClip next = playlist[m_playlistIndex];

        // Skip null entries gracefully.
        int safety = playlist.Count;
        while (!next && safety-- > 0)
        {
            m_playlistIndex = (m_playlistIndex + 1) % playlist.Count;
            next = playlist[m_playlistIndex];
        }
        if (!next) return;

        StartTransitionTo(next, loop: false);
    }

    private void StartTransitionTo(AudioClip clip, bool loop, float? crossfadeDurationOverride = null)
    {
        if (m_transitionRoutine != null) StopCoroutine(m_transitionRoutine);
        m_transitionRoutine = StartCoroutine(CrossfadeTo(clip, loop, crossfadeDurationOverride));
    }

    private IEnumerator CrossfadeTo(AudioClip clip, bool loop, float? crossfadeDurationOverride = null)
    {
        // Swap roles: the idle source becomes the new active source.
        AudioSource incoming = m_idle;
        AudioSource outgoing = m_active;

        incoming.clip = clip;
        incoming.loop = loop;
        incoming.volume = 0f;
        incoming.time = 0f;
        incoming.Play();

        float startOutVol = outgoing.isPlaying ? outgoing.volume : 0f;
        float duration = Mathf.Max(0.0001f, crossfadeDurationOverride ?? crossfadeDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            // Equal-power-ish curve for a smoother perceived blend.
            float kIn = Mathf.Sin(k * Mathf.PI * 0.5f);
            float kOut = Mathf.Cos(k * Mathf.PI * 0.5f);
            incoming.volume = volume * kIn;
            outgoing.volume = startOutVol * kOut;
            yield return null;
        }

        incoming.volume = volume;
        outgoing.Stop();
        outgoing.clip = null;
        outgoing.volume = 0f;

        m_active = incoming;
        m_idle = outgoing;
        m_transitionRoutine = null;
    }

    private IEnumerator FadeOutAndStop()
    {
        float duration = Mathf.Max(0.0001f, crossfadeDuration);
        float startA = m_a.volume;
        float startB = m_b.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            m_a.volume = startA * k;
            m_b.volume = startB * k;
            yield return null;
        }
        m_a.Stop(); m_b.Stop();
        m_a.clip = null; m_b.clip = null;
        m_transitionRoutine = null;
    }

    private AudioSource CreateSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();

        if (sourceTemplate != null)
        {
            src.outputAudioMixerGroup = sourceTemplate.outputAudioMixerGroup;
            src.spatialBlend = sourceTemplate.spatialBlend;
            src.rolloffMode = sourceTemplate.rolloffMode;
            src.minDistance = sourceTemplate.minDistance;
            src.maxDistance = sourceTemplate.maxDistance;
            src.dopplerLevel = sourceTemplate.dopplerLevel;
            src.spread = sourceTemplate.spread;
            src.priority = sourceTemplate.priority;
            src.pitch = sourceTemplate.pitch;
            src.panStereo = sourceTemplate.panStereo;
            src.reverbZoneMix = sourceTemplate.reverbZoneMix;
            src.bypassEffects = sourceTemplate.bypassEffects;
            src.bypassListenerEffects = sourceTemplate.bypassListenerEffects;
            src.bypassReverbZones = sourceTemplate.bypassReverbZones;
        }

        src.playOnAwake = false;
        src.loop = false;
        src.volume = 0f;
        return src;
    }
}
