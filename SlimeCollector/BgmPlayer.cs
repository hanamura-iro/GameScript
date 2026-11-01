using UnityEngine;

public class BgmPlayer : MonoBehaviour
{
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float volume = 0.5f;

    private AudioSource source;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = bgmClip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = volume;
    }

    private void OnEnable()
    {
        SoundSettings.OnChanged += Apply;
    }

    private void OnDisable()
    {
        SoundSettings.OnChanged -= Apply;
    }

    private void Start()
    {
        if (bgmClip != null) source.Play();
        Apply();
    }

    private void Apply()
    {
        source.mute = !SoundSettings.BgmEnabled;
    }
}