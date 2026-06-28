using System.Collections;
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
    COIN,
    CLICK
}

public enum MusicTrack
{
    Menu = 0,
    Level = 1
}

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundList;
    [SerializeField] private AudioSource audioSource;

    [Space(10)]
    [SerializeField] private AudioClip[] musicList;
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private float fadeDuration = 1f;

    public static SoundManager Instance { get; private set; }

    private AudioSource activeMusic;
    private AudioSource inactiveMusic;
    private Coroutine fadeCoroutine;
    private float musicMaxVolume;
    private MusicTrack currentTrack;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        activeMusic = musicSourceA;
        inactiveMusic = musicSourceB;
        musicMaxVolume = musicSourceA.volume;

        PlayerSave.OnLevelLoaded += OnLevelLoaded;
    }

    private void OnDestroy()
    {
        PlayerSave.OnLevelLoaded -= OnLevelLoaded;
    }

    private void Start()
    {
        PlayMusic(MusicTrack.Menu);
    }

    private void OnLevelLoaded()
    {
        CrossfadeTo(MusicTrack.Level);
    }

    public static void CrossfadeToMenu() => Instance.CrossfadeTo(MusicTrack.Menu);

    public static void PlaySound(SoundType sound, float volume = 1f)
    {
        Instance.audioSource.PlayOneShot(Instance.soundList[(int)sound], volume);
    }

    public static AudioClip GetClip(SoundType sound)
    {
        return Instance.soundList[(int)sound];
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && GameManager.CurrentGameState != GameState.Exploring)
            PlaySound(SoundType.CLICK);
    }

    private void PlayMusic(MusicTrack track)
    {
        AudioClip clip = musicList[(int)track];
        activeMusic.clip = clip;
        activeMusic.volume = musicMaxVolume;
        activeMusic.loop = true;
        activeMusic.Play();
    }

    private void CrossfadeTo(MusicTrack track)
    {
        if (currentTrack == track && fadeCoroutine == null && activeMusic.isPlaying)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        currentTrack = track;
        fadeCoroutine = StartCoroutine(FadeRoutine(musicList[(int)track]));
    }

    private IEnumerator FadeRoutine(AudioClip nextClip)
    {
        inactiveMusic.clip = nextClip;
        inactiveMusic.volume = 0f;
        inactiveMusic.loop = true;
        inactiveMusic.Play();

        float startVolume = activeMusic.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            activeMusic.volume = Mathf.Lerp(startVolume, 0f, t);
            inactiveMusic.volume = Mathf.Lerp(0f, musicMaxVolume, t);
            yield return null;
        }

        activeMusic.Stop();
        activeMusic.volume = 0f;

        (activeMusic, inactiveMusic) = (inactiveMusic, activeMusic);

        fadeCoroutine = null;
    }
}
