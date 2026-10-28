using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("---------- Audio Source ----------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("---------- Audio Clip ----------")]
    public AudioClip background;
    public AudioClip death;
    public AudioClip enemy;
    public AudioClip enemy2;
    public AudioClip Boss;
    public AudioClip Hpitem;
    public AudioClip walk;
    public AudioClip run;


    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.ChangeBackgroundMusic(this.background);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayBackground(background);
    }

    public void PlayBackground(AudioClip clip)
    {
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void ChangeBackgroundMusic(AudioClip newClip)
    {
        if (musicSource.clip != newClip)
        {
            PlayBackground(newClip);
        }


    }
}