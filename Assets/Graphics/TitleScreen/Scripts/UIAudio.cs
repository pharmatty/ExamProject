using UnityEngine;

public class UIAudio : MonoBehaviour
{
    public static UIAudio Instance;

    public AudioSource audioSource;
    public AudioClip moveSound;
    public AudioClip selectSound;

    void Awake()
    {
        Instance = this;
    }

    public void PlayMove()
    {
        if (moveSound != null)
            audioSource.PlayOneShot(moveSound);
    }

    public void PlaySelect()
    {
        if (selectSound != null)
            audioSource.PlayOneShot(selectSound);
    }
}