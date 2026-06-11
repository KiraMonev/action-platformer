using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    SwordAttack,
    Run,
    Jump,
    Land,
    FireballAttack
}

[System.Serializable]
public struct SoundEffect
{
    public SoundType type;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
    [Range(0.1f, 3f)] public float pitch;
    public bool useRandomPitch;
    [Range(0f, 0.5f)] public float pitchVariation;
    
    [Header("Bassy Boost Settings")]
    public bool applyLowPass;
    [Range(500f, 10000f)] public float lowPassCutoff;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AudioManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("AudioManager");
                    _instance = obj.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }
    private static AudioManager _instance;

    [Header("Sound Configurations")]
    [SerializeField] private List<SoundEffect> soundEffects = new List<SoundEffect>();

    [Header("Performance Settings")]
    [SerializeField] private int poolSize = 10;

    private List<AudioSource> _sfxSourcesPool;
    private Dictionary<SoundType, SoundEffect> _soundsDictionary;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
            InitializeDictionary();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        _sfxSourcesPool = new List<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject sourceObj = new GameObject($"SFX_Source_{i}");
            sourceObj.transform.SetParent(transform);
            
            AudioSource source = sourceObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            
            _sfxSourcesPool.Add(source);
        }
    }

    private void InitializeDictionary()
    {
        _soundsDictionary = new Dictionary<SoundType, SoundEffect>();
        foreach (var sfx in soundEffects)
        {
            if (!_soundsDictionary.ContainsKey(sfx.type))
            {
                _soundsDictionary.Add(sfx.type, sfx);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Duplicate sound type registration: {sfx.type}");
            }
        }
    }

    /// <summary>
    /// Plays a sound effect by type with pre-configured settings and optional dynamic pitch variation.
    /// </summary>
    public void Play(SoundType type)
    {
        if (_soundsDictionary == null || !_soundsDictionary.TryGetValue(type, out SoundEffect sfx))
        {
            Debug.LogWarning($"[AudioManager] Sound type '{type}' not found or configured!");
            return;
        }

        if (sfx.clip == null)
        {
            Debug.LogWarning($"[AudioManager] AudioClip is null for sound type '{type}'!");
            return;
        }

        AudioSource freeSource = GetFreeAudioSource();
        if (freeSource == null)
        {
            Debug.LogWarning("[AudioManager] SFX Pool exhausted! Sound skipped.");
            return;
        }

        freeSource.clip = sfx.clip;
        freeSource.volume = sfx.volume;

        float targetPitch = sfx.pitch;
        if (sfx.useRandomPitch)
        {
            targetPitch += Random.Range(-sfx.pitchVariation, sfx.pitchVariation);
        }
        freeSource.pitch = Mathf.Clamp(targetPitch, 0.1f, 3f);

        // Apply Bassy Low Pass Filter dynamically
        AudioLowPassFilter filter = freeSource.GetComponent<AudioLowPassFilter>();
        if (sfx.applyLowPass)
        {
            if (filter == null)
            {
                filter = freeSource.gameObject.AddComponent<AudioLowPassFilter>();
            }
            filter.enabled = true;
            filter.cutoffFrequency = sfx.lowPassCutoff;
        }
        else
        {
            if (filter != null)
            {
                filter.enabled = false;
            }
        }

        freeSource.Play();
    }

    private AudioSource GetFreeAudioSource()
    {
        for (int i = 0; i < _sfxSourcesPool.Count; i++)
        {
            if (!_sfxSourcesPool[i].isPlaying)
            {
                return _sfxSourcesPool[i];
            }
        }

        // Optional: dynamic growth if pool is full
        GameObject sourceObj = new GameObject($"SFX_Source_{_sfxSourcesPool.Count}");
        sourceObj.transform.SetParent(transform);
        AudioSource source = sourceObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        _sfxSourcesPool.Add(source);
        
        return source;
    }
}
