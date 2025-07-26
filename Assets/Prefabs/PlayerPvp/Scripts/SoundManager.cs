using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Music")]
    public AudioClip backgroundMusic;

    [Header("Sound Effects")]
    public AudioClip attackSound;
    public AudioClip criticalHitSound;
    public AudioClip damageTakenSound;
    public AudioClip dodgeSound;
    public AudioClip buttonClickSound;
    public AudioClip tabChangeSound;
    public AudioClip buySound;

    // Optional extras
    public AudioClip levelUpSound;
    public AudioClip deathSound;

    [Header("Settings")]
    public bool sfxEnabled = true;
    public bool musicEnabled = true;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PlayMusic();
    }

    // Music
    public void PlayMusic()
    {
        if (musicEnabled && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void ToggleMusic(bool isOn)
    {
        musicEnabled = isOn;
        if (musicEnabled)
            PlayMusic();
        else
            musicSource.Stop();
    }

    public void ToggleSFX(bool isOn)
    {
        sfxEnabled = isOn;
        if (sfxEnabled)
        {
            Debug.Log("SFX enabled");
        }
        else
        {
            Debug.Log("SFX disabled");
        }
    }

    // Generic SFX
    private void PlaySFX(AudioClip clip)
    {
        if (sfxEnabled && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // Public methods for specific sounds
    public void PlayAttack() => PlaySFX(attackSound);
    public void PlayCrit() => PlaySFX(criticalHitSound);
    public void PlayDamage() => PlaySFX(damageTakenSound);
    public void PlayDodge() => PlaySFX(dodgeSound);
    public void PlayButtonClick() => PlaySFX(buttonClickSound);
    public void PlayTabChange() => PlaySFX(tabChangeSound);
    public void PlayBuy() => PlaySFX(buySound);
    public void PlayLevelUp() => PlaySFX(levelUpSound);
    public void PlayDeath() => PlaySFX(deathSound);
}
