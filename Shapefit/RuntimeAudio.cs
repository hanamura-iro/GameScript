using System.Collections.Generic;
using UnityEngine;

public static class RuntimeAudio
{
    private static AudioSource sharedSource;
    private static readonly Dictionary<int, AudioClip> toneCache = new Dictionary<int, AudioClip>();

    private static AudioSource Shared
    {
        get
        {
            if (sharedSource == null)
            {
                var go = new GameObject("ShapeFitAudio");
                Object.DontDestroyOnLoad(go);
                sharedSource = go.AddComponent<AudioSource>();
                sharedSource.playOnAwake = false;
            }
            return sharedSource;
        }
    }

    public static void PlayTone(float frequency, float duration = 0.12f, float volume = 0.5f)
    {
        int key = Mathf.RoundToInt(frequency * 10f) * 10000 + Mathf.RoundToInt(duration * 1000f);
        if (!toneCache.TryGetValue(key, out var clip))
        {
            clip = GenerateTone(frequency, duration);
            toneCache[key] = clip;
        }
        Shared.PlayOneShot(clip, volume);
    }

    public static void PlaySequence(MonoBehaviour runner, float[] frequencies, float noteDuration, float volume = 0.5f)
    {
        runner.StartCoroutine(SequenceRoutine(frequencies, noteDuration, volume));
    }

    private static System.Collections.IEnumerator SequenceRoutine(float[] frequencies, float noteDuration, float volume)
    {
        foreach (var f in frequencies)
        {
            PlayTone(f, noteDuration * 1.4f, volume);
            yield return new WaitForSeconds(noteDuration);
        }
    }

    private static AudioClip GenerateTone(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleLength = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
        var clip = AudioClip.Create($"tone_{frequency}_{duration}", sampleLength, 1, sampleRate, false);
        var data = new float[sampleLength];
        for (int i = 0; i < sampleLength; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (float)i / sampleLength; 
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.9f;
        }
        clip.SetData(data, 0);
        return clip;
    }
}