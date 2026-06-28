using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // ---------- Singleton ----------
    public static SoundManager Instance { get; private set; }

    // ---------- Audio Sources ----------
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;      // for background music
    [SerializeField] private AudioSource sfxSource;        // primary SFX source (optional)
    [SerializeField] private int sfxPoolSize = 10;

    // ---------- Volume Control ----------
    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    // ---------- Private Fields ----------
    private List<AudioSource> sfxPool;
    private int currentPoolIndex = 0;
    private Coroutine crossfadeCoroutine;

    // ---------- Unity Lifecycle ----------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxPool = new List<AudioSource>();
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            newSource.volume = sfxVolume * masterVolume;
            sfxPool.Add(newSource);
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    private void Update()
    {
        musicSource.volume = musicVolume * masterVolume;
        foreach (AudioSource src in sfxPool)
            src.volume = sfxVolume * masterVolume;
        sfxSource.volume = sfxVolume * masterVolume;
    }

    // ---------- Public API ----------

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null) return;
        AudioSource source = GetAvailablePooledSource();
        if (source != null)
        {
            source.clip = clip;
            source.volume = sfxVolume * masterVolume * volume;
            source.pitch = pitch;
            source.loop = loop;
            source.Play();
        }
        else
        {
            sfxSource.clip = clip;
            sfxSource.volume = sfxVolume * masterVolume * volume;
            sfxSource.pitch = pitch;
            sfxSource.loop = loop;
            sfxSource.Play();
        }
    }

    public void PlaySFXOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
    }

    // stop a specific SFX clip if it's playing in the pool
    public void StopSFX(AudioClip clip)
    {
        if (clip == null) return;
        foreach (AudioSource src in sfxPool)
        {
            if (src.clip == clip && src.isPlaying)
            {
                src.Stop();
                break;
            }
        }
        if (sfxSource.clip == clip && sfxSource.isPlaying)
            sfxSource.Stop();
    }

    /// <summary>
    /// Plays background music (instantly, no crossfade).
    /// </summary>
    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (clip == null) return;
        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);
        musicSource.clip = clip;
        musicSource.volume = musicVolume * masterVolume * volume;
        musicSource.loop = loop;
        musicSource.Play();
    }

    /// <summary>
    /// Crossfades from current music to a new clip over a specified duration.
    /// </summary>
    public void CrossfadeMusic(AudioClip newClip, float targetVolume = 1f, float duration = 2f)
    {
        if (newClip == null) return;
        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(newClip, targetVolume, duration));
    }

    private IEnumerator CrossfadeCoroutine(AudioClip newClip, float targetVolume, float duration)
    {
        // If nothing is playing, just start the new clip at full volume
        if (!musicSource.isPlaying || musicSource.clip == null)
        {
            PlayMusic(newClip, targetVolume);
            crossfadeCoroutine = null;
            yield break;
        }

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        // Fade out current
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        musicSource.volume = 0f;
        musicSource.Stop();

        // Start new clip at 0 volume and fade in
        PlayMusic(newClip, 0f);
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume * musicVolume * masterVolume, elapsed / duration);
            yield return null;
        }
        musicSource.volume = targetVolume * musicVolume * masterVolume;

        crossfadeCoroutine = null;
    }

    public void StopMusic() => musicSource.Stop();
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();
    public void StopAllSFX()
    {
        foreach (AudioSource src in sfxPool) src.Stop();
        sfxSource.Stop();
    }

    public void SetMasterVolume(float vol) => masterVolume = Mathf.Clamp01(vol);
    public void SetMusicVolume(float vol) => musicVolume = Mathf.Clamp01(vol);
    public void SetSFXVolume(float vol) => sfxVolume = Mathf.Clamp01(vol);

    // ---------- Private Helpers ----------
    private AudioSource GetAvailablePooledSource()
    {
        for (int i = 0; i < sfxPool.Count; i++)
        {
            int index = (currentPoolIndex + i) % sfxPool.Count;
            if (!sfxPool[index].isPlaying)
            {
                currentPoolIndex = (index + 1) % sfxPool.Count;
                return sfxPool[index];
            }
        }
        return null;
    }
}