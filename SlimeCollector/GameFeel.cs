using System.Collections;
using UnityEngine;

public class GameFeel : MonoBehaviour
{
    public static GameFeel instance;

    [Header("히트스톱 (초)")]
    public float mergeHitStop = 0.05f;
    public float newDiscoveryHitStop = 0.1f;

    [Header("카메라 흔들림")]
    public float mergeShake = 0.08f;
    public float newDiscoveryShake = 0.18f;
    public float shakeDuration = 0.25f;

    [Header("진동 (길이 ms / 세기 1~255)")]
    public int grabVibrateMs = 12;
    public int grabAmplitude = 60;
    public int dropVibrateMs = 8;
    public int dropAmplitude = 35;
    public int mergeVibrateMs = 35;
    public int mergeAmplitude = 180;
    public int newDiscoveryVibrateMs = 80;
    public int newDiscoveryAmplitude = 255;

    [Header("사운드 (선택, 비워두면 소리 없음)")]
    public AudioClip grabClip;
    public AudioClip dropClip;
    public AudioClip mergeClip;
    public AudioClip newDiscoveryClip;
    [Range(0f, 1f)] public float volume = 1f;
    public float pitchPerTier = 0.03f;   // 단계가 오를수록 합치는 소리가 조금씩 높아짐
    public float maxPitch = 1.6f;

    private AudioSource audioSource;
    private Coroutine hitStopRoutine;
    private float savedTimeScale = 1f;

    private void Awake()
    {
        instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnDisable()
    {
        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
            hitStopRoutine = null;
            Time.timeScale = savedTimeScale;
        }
    }

    public void OnGrab()
    {
        Haptics.Vibrate(grabVibrateMs, grabAmplitude);
        PlaySound(grabClip, Random.Range(1.05f, 1.15f));
    }

    public void OnDrop()
    {
        Haptics.Vibrate(dropVibrateMs, dropAmplitude);
        PlaySound(dropClip, Random.Range(0.95f, 1.05f));
    }

    public void OnMerge(int tier, bool isNew)
    {
        HitStop(isNew ? newDiscoveryHitStop : mergeHitStop);

        if (CameraController.instance != null)
            CameraController.instance.Shake(isNew ? newDiscoveryShake : mergeShake, shakeDuration);

        if (isNew)
            Haptics.Vibrate(newDiscoveryVibrateMs, newDiscoveryAmplitude);
        else
            Haptics.Vibrate(mergeVibrateMs, mergeAmplitude);

        if (isNew && newDiscoveryClip != null)
        {
            PlaySound(newDiscoveryClip, 1f);
        }
        else
        {
            PlaySound(mergeClip, Mathf.Min(1f + tier * pitchPerTier, maxPitch));
        }
    }

    public void PlaySound(AudioClip clip, float pitch = 1f)
    {
        if (clip == null || !SoundSettings.SfxEnabled) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
    }

    private void HitStop(float duration)
    {
        if (duration <= 0f) return;

        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
        }
        else
        {
            savedTimeScale = Time.timeScale;
        }

        hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = savedTimeScale;
        hitStopRoutine = null;
    }
}