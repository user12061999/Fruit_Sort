using System;
using System.Collections;
using UnityEngine;

namespace FruitSort
{
    public enum GameEffectType
    {
        None = 0,
        DotHit = 1,
        DotDestroyed = 2,
        DotLand = 3,
        BucketReceive = 4,
        BucketComplete = 5,
        BucketRelease = 6,
        SpawnerOpen = 7,
        ConveyorGrind = 8,
        LevelWin = 9,
        LevelLose = 10,
        BoosterUsed = 11,
        ScreenClick = 12
    }

    [Serializable]
    public struct EffectDefinition
    {
        public GameEffectType effectType;
        [Tooltip("Prefab particle, ví dụ một prefab từ Epic Toon FX.")]
        public GameObject particlePrefab;
        public AudioClip sound;
        [Min(0f), Tooltip("Số giây chờ sau particle trước khi phát sound.")]
        public float soundDelay;
        [Range(0f, 1f)] public float volume;
        [Min(0f), Tooltip("0 = tự tính thời lượng particle. Dùng giá trị dương cho effect loop.")]
        public float cleanupDelay;
        [Tooltip("Gắn particle vào target để effect đi theo target.")]
        public bool followTarget;

        public EffectDefinition Normalized()
        {
            EffectDefinition normalized = this;
            normalized.soundDelay = Mathf.Max(0f, soundDelay);
            normalized.volume = Mathf.Clamp01(volume);
            normalized.cleanupDelay = Mathf.Max(0f, cleanupDelay);
            return normalized;
        }
    }

    /// <summary>
    /// Phát particle và sound gameplay theo một ID chung. Cấu hình ở Inspector để đổi asset
    /// (bao gồm Epic Toon FX) mà không phải sửa các script gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] GameAudioLibrary audioLibrary;
        [SerializeField] AudioSource effectAudioSource;

        [Header("Effect Library")]
        [SerializeField] EffectLibrary effectLibrary;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (audioLibrary != null) audioLibrary.Activate();
            if (GetComponent<ScreenClickFeedback>() == null)
                gameObject.AddComponent<ScreenClickFeedback>();
            EnsureAudioSource();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (audioLibrary != null)
                audioLibrary.PlayMusic(GameMusicTrack.Menu);
        }

        /// <summary>Thay toàn bộ thư viện effect. Hữu ích khi load level hoặc trong test.</summary>
        public void SetEffectLibrary(EffectLibrary library)
        {
            effectLibrary = library;
        }

        public bool TryGetDefinition(GameEffectType effectType, out EffectDefinition definition)
        {
            if (effectLibrary == null)
            {
                definition = default;
                return false;
            }
            return effectLibrary.TryGetDefinition(effectType, out definition);
        }

        public bool Play(GameEffectType effectType, Vector3 position)
        {
            return Play(effectType, position, null);
        }

        public bool Play(GameEffectType effectType, Transform target)
        {
            if (target == null) return false;
            return Play(effectType, target.position, target);
        }

        public bool Play(GameEffectType effectType, Vector3 position, Transform target)
        {
            if (!TryGetDefinition(effectType, out EffectDefinition definition)) return false;

            bool hasParticle = definition.particlePrefab != null;
            bool hasConfiguredSound = definition.sound != null;
            bool hasLibrarySound = audioLibrary != null &&
                audioLibrary.GetEffectSound(effectType) != null &&
                !audioLibrary.GetEffectSound(effectType).IsEmpty;
            bool hasSound = hasConfiguredSound || hasLibrarySound;
            if (!hasParticle && !hasSound) return false;

            if (hasParticle)
            {
                Transform parent = definition.followTarget ? target : null;
                GameObject instance = Instantiate(definition.particlePrefab, position, Quaternion.identity, parent);
                Destroy(instance, GetCleanupDelay(instance, definition.cleanupDelay));
            }

            if (hasSound)
            {
                if (hasConfiguredSound)
                {
                    EnsureAudioSource();
                    if (definition.soundDelay <= 0f)
                        effectAudioSource.PlayOneShot(definition.sound, definition.volume);
                    else
                        StartCoroutine(PlaySoundAfterDelay(definition.sound, definition.volume, definition.soundDelay));
                }
                else if (hasLibrarySound) audioLibrary.PlayEffect(effectType);

            }

            return true;
        }

        void EnsureAudioSource()
        {
            if (effectAudioSource != null) return;
            effectAudioSource = GetComponent<AudioSource>();
            if (effectAudioSource == null) effectAudioSource = gameObject.AddComponent<AudioSource>();
            effectAudioSource.playOnAwake = false;
            effectAudioSource.spatialBlend = 0f;
        }

        IEnumerator PlaySoundAfterDelay(AudioClip clip, float volume, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (this != null && effectAudioSource != null && clip != null)
                effectAudioSource.PlayOneShot(clip, volume);
        }

        static float GetCleanupDelay(GameObject instance, float configuredDelay)
        {
            if (configuredDelay > 0f) return configuredDelay;

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            float longestLifetime = 0.1f;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                if (main.loop) return 5f;
                longestLifetime = Mathf.Max(longestLifetime,
                    main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
            }
            return longestLifetime;
        }
    }
}
