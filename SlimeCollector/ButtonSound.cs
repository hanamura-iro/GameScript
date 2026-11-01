using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    public AudioClip clip;
    public float pitch = 1f;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Play);
    }

    private void Play()
    {
        if (GameFeel.instance != null)
        {
            GameFeel.instance.PlaySound(clip, pitch);
        }
    }
}