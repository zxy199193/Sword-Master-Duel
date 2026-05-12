using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;       // 用于播放单次音效
    public AudioSource bgmSource;       // 用于播放背景音乐

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;

    [Header("BGM")]
    public AudioClip bgmClip;           // 背景音乐

    [Header("UI Sounds")]
    public AudioClip buttonClickClip;   // 默认按钮点击音效

    [Header("Battle Sounds")]
    public AudioClip hitNormalClip;     // 正常命中
    public AudioClip hitDefendClip;     // 攻击被防御
    public AudioClip hitMissClip;       // 攻击被闪避(Miss)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 在主菜单和战斗间切换不销毁

            // 从 AudioSource Inspector 上的初始音量读取，避免覆盖手动配置
            if (bgmSource != null) bgmVolume = bgmSource.volume;
            if (sfxSource != null) sfxVolume = sfxSource.volume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PlayBGM();
    }

    // ==========================================
    // BGM
    // ==========================================
    public void PlayBGM()
    {
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    // ==========================================
    // SFX
    // ==========================================
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }

    public void PlayButtonClickSound()
    {
        PlaySFX(buttonClickClip);
    }

    /// <summary>
    /// 播放战斗打击音效
    /// </summary>
    /// <param name="hitType">0 = Miss, 1 = 正常命中, 2 = 被防御</param>
    public void PlayHitSound(int hitType)
    {
        switch (hitType)
        {
            case 0: PlaySFX(hitMissClip); break;
            case 1: PlaySFX(hitNormalClip); break;
            case 2: PlaySFX(hitDefendClip); break;
        }
    }
}