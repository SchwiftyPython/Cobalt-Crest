using System;
using System.Collections.Generic;
using Data;
using Managers;
using UnityEngine;

namespace Audio
{
    /// AudioService — Singleton with optional FMOD backend.
    /// Usage: AudioService.TryPlayDeathChime(species, genome, worldPos);
    public class AudioService : MonoBehaviour
    {
        public static AudioService Instance { get; private set; }

        private interface IAudioBackend
        {
            void Init(AudioService svc);
            bool CanPlay(); // voice limit / cooldown
            void PlayChime(int semitone, float timbre, int speciesId, Vector2 worldPos, float volume, float pan);
        }

        // ---------------- UnityAudio Backend (fallback) ----------------
        private class UnityAudioBackend : IAudioBackend
        {
            private readonly List<AudioSource> _pool = new();
            private AudioClip _baseClip; // A4 440Hz pluck
            private Transform _parent;
            private int _voiceLimit = 12;
            private float _lastPlayTime;
            private float _cooldown = 0.04f; // seconds

            public void Init(AudioService svc)
            {
                _parent = svc.transform;
                var sim = ConfigService.Instance?.Sim;
                _voiceLimit = Mathf.Max(1, sim?.audioVoiceLimit ?? 12);
                _cooldown = Mathf.Max(0f, (sim?.chimeCooldownMs ?? 40) / 1000f);
                _baseClip = _baseClip ?? BuildPluckClip(44100, 0.6f, 440f); // A4
                EnsurePoolSize(_voiceLimit);
            }

            private void EnsurePoolSize(int n)
            {
                while (_pool.Count < n)
                {
                    var src = new GameObject("UnityChime").AddComponent<AudioSource>();
                    src.transform.SetParent(_parent, false);
                    src.clip = _baseClip;
                    src.spatialBlend = 0f; // pure 2D
                    src.playOnAwake = false;
                    src.reverbZoneMix = 0f;
                    _pool.Add(src);
                }
            }

            public bool CanPlay()
            {
                if (Time.unscaledTime - _lastPlayTime < _cooldown)
                {
                    return false;
                }

                var active = 0;
                for (var i = 0; i < _pool.Count; i++)
                {
                    if (_pool[i].isPlaying)
                    {
                        active++;
                    }
                }

                return active < _voiceLimit;
            }

            public void PlayChime(int semitone, float timbre, int speciesId, Vector2 worldPos, float volume, float pan)
            {
                _lastPlayTime = Time.unscaledTime;
                
                var src = GetFreeSource();
                
                if (src == null)
                {
                    return;
                }

                // Pitch ratio vs A4 = 69
                var ratio = Mathf.Pow(2f, (semitone - 69) / 12f);
                src.pitch = Mathf.Clamp(ratio, 0.25f, 4f);
                src.volume = Mathf.Clamp01(volume);

                // Pan: -1..1
                src.panStereo = Mathf.Clamp(pan, -1f, 1f);

                // Very simple timbre: add 2nd harmonic into the clip by detuning pitch slightly (psychoacoustic trick).
                // For UnityAudio backend we approximate timbre by light pitch wobble on start.
                src.Stop();
                src.time = 0f;
                src.Play();
            }

            private AudioSource GetFreeSource()
            {
                for (var i = 0; i < _pool.Count; i++)
                {
                    if (!_pool[i].isPlaying)
                    {
                        return _pool[i];
                    }
                }

                return null;
            }

            // Procedural pluck (sine + quick exponential decay)
            private static AudioClip BuildPluckClip(int rate, float seconds, float baseHz)
            {
                var samples = Mathf.CeilToInt(rate * seconds);
                var data = new float[samples];
                float attack = 0.004f, decay = Mathf.Max(0.05f, seconds - attack);
                float aSamples = attack * rate, dSamples = decay * rate;

                for (var i = 0; i < samples; i++)
                {
                    var t = i / (float)rate;
                    var env = i < aSamples ? (i / aSamples) : Mathf.Exp(-(i - aSamples) / dSamples);
                    // sine + small 2nd harmonic, soft clipped
                    var y = Mathf.Sin(2f * Mathf.PI * baseHz * t) * 0.9f +
                            Mathf.Sin(2f * Mathf.PI * baseHz * 2f * t) * 0.15f;
                    data[i] = Mathf.Clamp(y * env, -1f, 1f);
                }

                var clip = AudioClip.Create("ChimeBase_A4", samples, 1, rate, false);
                clip.SetData(data, 0);
                return clip;
            }
        }

        // ---------------- FMOD Backend (optional) ----------------
#if FMOD_PRESENT
        private class FMODAudioBackend : IAudioBackend
        {
            private int _voiceLimit = 12;
            private float _cooldown = 0.04f;
            private float _lastPlay;

            public void Init(AudioService svc)
            {
                var sim = ConfigService.Instance?.Sim;
                _voiceLimit = Mathf.Max(1, sim?.audioVoiceLimit ?? 12);
                _cooldown = Mathf.Max(0f, (sim?.chimeCooldownMs ?? 40) / 1000f);
            }

            private int _active; // naive voice counting

            public bool CanPlay()
            {
                if (Time.unscaledTime - _lastPlay < _cooldown)
                {
                    return false;
                }

                return _active < _voiceLimit;
            }

            public void PlayChime(int semitone, float timbre, int speciesId, Vector2 worldPos, float volume, float pan)
            {
                _lastPlay = Time.unscaledTime;
                _active++;

                var inst = FMODUnity.RuntimeManager.CreateInstance("event:/chime");
                inst.setParameterByName("pitchSemitone", semitone);
                inst.setParameterByName("timbre", timbre);
                inst.setParameterByName("species", speciesId);
                
                // Optional: set 3D attributes if your FMOD event is 3D; otherwise ignore.
                
                inst.start();
                inst.release();

                // crude active tracking decays after 0.6s
                Instance.StartCoroutine(Dec(_ => { _active = Mathf.Max(0, _active - 1); }, 0.6f));
            }

            private System.Collections.IEnumerator Dec(Action<int> a, float sec)
            {
                yield return new WaitForSecondsRealtime(sec);
                a?.Invoke(0);
            }
        }
#endif

        // ---------------- Service core ----------------
        private IAudioBackend _backend;
        public static bool Enabled => (ConfigService.Instance?.Sim?.enableAudio ?? true);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            ChooseBackend();
        }

        private void ChooseBackend()
        {
            var sim = ConfigService.Instance?.Sim;
            var wantFMOD = sim?.useFMOD ?? false;

#if FMOD_PRESENT
            _backend = wantFMOD ? new FMODAudioBackend() : new UnityAudioBackend();
#else
        _backend = new UnityAudioBackend();
#endif

            _backend.Init(this);
        }

        public static void TryPlayDeathChime(SpeciesId species, AgentGenome genome, Vector2 worldPos)
        {
            var sim = ConfigService.Instance?.Sim;
            if (!Enabled || Instance == null || Instance._backend == null)
            {
                return;
            }

            // Map genome -> pitch range (C0..C8), then quantize to scale.
            var minOct = sim?.minOctave ?? 3;
            var maxOct = sim?.maxOctave ?? 6;
            var scale = sim?.scale ?? ScaleQuantizer.Scale.Pentatonic;

            var p = PitchFromGenome(genome, species); // 0..1
            int minSem = minOct * 12, maxSem = maxOct * 12 + 11;
            var rawSemitone = Mathf.RoundToInt(Mathf.Lerp(minSem, maxSem, p));
            var semi = ScaleQuantizer.QuantizeAndClamp(rawSemitone, scale, minOct, maxOct);

            var timbre = TimbreFromSpecies(species); // 0..1
            var speciesId = species == SpeciesId.Producer ? 0 : (species == SpeciesId.Herbivore ? 1 : 2);

            // simple pan: map world x across world bounds into [-1,1]
            var simcfg = ConfigService.Instance?.Sim;
            var pan = 0f;
            if (simcfg != null)
            {
                float minX = simcfg.worldMin.x, maxX = simcfg.worldMax.x;
                pan = Mathf.InverseLerp(minX, maxX, worldPos.x) * 2f - 1f;
            }

            var vol = Mathf.Clamp01(sim?.audioVolume ?? 0.8f);

            if (Instance._backend.CanPlay())
            {
                Instance._backend.PlayChime(semi, timbre, speciesId, worldPos, vol, pan);
            }
        }

        // --- Mapping functions ---
        private static float PitchFromGenome(AgentGenome g, SpeciesId s)
        {
            // Stable blend from genes — hue dominates, with small pushes from speed/size/metabolism.
            var hue = Mathf.Repeat(g.hue, 1f);
            var spd = Mathf.Clamp((g.speed - 0.8f) / 0.9f, 0f, 1f); // ~0..1
            var siz = Mathf.Clamp((g.size - 0.8f) / 0.9f, 0f, 1f);
            var met = Mathf.Clamp((g.metabolism - 0.7f) / 0.8f, 0f, 1f);
            var baseP = hue;
            var adj = 0.15f * spd - 0.10f * met + 0.08f * siz;
            
            // slight species tilt
            var tilt = (s == SpeciesId.Producer ? 0.05f : s == SpeciesId.Herbivore ? 0.0f : 0.10f);
            return Mathf.Repeat(baseP + adj + tilt, 1f);
        }

        private static float TimbreFromSpecies(SpeciesId s)
        {
            // 0 = warm/soft → 1 = bright/metallic
            return s switch
            {
                SpeciesId.Producer => 0.2f,
                SpeciesId.Herbivore => 0.55f,
                _ => 0.85f
            };
        }
    }
}