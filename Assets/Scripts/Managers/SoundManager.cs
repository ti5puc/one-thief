using UnityEngine;

public enum SoundType
{
    JUMP1,
    JUMP2,
    JUMP3,
    TRAPPENDULUM,
    TRAPPENDULUMR,
    TRAPFALL,
    TRAPIMPACT,
    DEATH,
    COIN
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundList;
    private static SoundManager instance;
    private AudioSource audioSource;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1f)
    {
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }

    public static AudioClip GetClip(SoundType sound)
    {
        return instance.soundList[(int)sound];
    }
}
