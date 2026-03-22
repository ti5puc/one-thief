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
    private AudioSource audioSource;

    public static SoundManager Instance { get; private set; }
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static void PlaySound(SoundType sound, float volume = 1f)
    {
        Instance.audioSource.PlayOneShot(Instance.soundList[(int)sound], volume);
    }

    public static AudioClip GetClip(SoundType sound)
    {
        return Instance.soundList[(int)sound];
    }
}
