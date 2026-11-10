using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.World;
using UnityEngine;

namespace LightWeaver.Audio
{
    /// <summary>
    /// Harici ses dosyasına ihtiyaç duymadan çalışma anında Do Minör Pentatonik (C Minor Pentatonic)
    /// kristal cam/arp ASMR tınıları ve atmosferik uzay pad müziği sentezleyen ses motoru.
    /// </summary>
    public sealed class HarmonicAudioManager : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private FlashBurstController flashBurst;

        [Header("Audio Volumes")]
        [Range(0f, 1f)] [SerializeField] private float sfxMasterVolume = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float musicMasterVolume = 0.22f;

        private AudioSource _sfxSource;
        private AudioSource _ambientSource;

        private AudioClip[] _pentatonicCrystalClips;
        private AudioClip _perfectWeaveClip;
        private AudioClip _energyPickupClip;
        private AudioClip _jumpPadClip;
        private AudioClip _flashBurstClip;
        private AudioClip _hazardHitClip;
        private AudioClip _ambientPadClip;

        private int _melodyNoteIndex;
        private float _lastCrystalSoundTime = -1f;

        // C Minor Pentatonic frekansları (C4, Eb4, F4, G4, Bb4, C5, Eb5, G5)
        private static readonly float[] PentatonicHz = { 261.63f, 311.13f, 349.23f, 392.00f, 466.16f, 523.25f, 622.25f, 783.99f };
        private static readonly int[] MelodicWalk = { 0, 1, 2, 3, 5, 4, 3, 6, 5, 7, 5, 3 };

        private void Awake()
        {
            ProgressManager.EnsureLoaded();
            SetupAudioSources();
            SynthesizeAllProceduralClips();
            RefreshMuteStates();
        }

        private void OnEnable()
        {
            CrystalPlatform.OnPlatformSolidified += HandleCrystalSolidified;
            EnergyOrb.OnEnergyCollected += HandleEnergyCollected;
            PrismaticJumpPad.OnJumpPadLaunched += HandleJumpPadLaunched;
            ShadowHazard.OnHazardResolved += HandleHazardResolved;
            if (batterySystem != null) batterySystem.OnPerfectWeaveTriggered += HandlePerfectWeave;
            if (flashBurst != null) flashBurst.OnBurstExecuted += HandleFlashBurst;
        }

        private void OnDisable()
        {
            CrystalPlatform.OnPlatformSolidified -= HandleCrystalSolidified;
            EnergyOrb.OnEnergyCollected -= HandleEnergyCollected;
            PrismaticJumpPad.OnJumpPadLaunched -= HandleJumpPadLaunched;
            ShadowHazard.OnHazardResolved -= HandleHazardResolved;
            if (batterySystem != null) batterySystem.OnPerfectWeaveTriggered -= HandlePerfectWeave;
            if (flashBurst != null) flashBurst.OnBurstExecuted -= HandleFlashBurst;
        }

        public void RefreshMuteStates()
        {
            if (_sfxSource != null) _sfxSource.mute = !ProgressManager.SfxEnabled;
            if (_ambientSource != null)
            {
                _ambientSource.mute = !ProgressManager.MusicEnabled;
                if (ProgressManager.MusicEnabled && !_ambientSource.isPlaying && _ambientPadClip != null)
                {
                    _ambientSource.Play();
                }
            }
        }

        private void SetupAudioSources()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.playOnAwake = false;
            _ambientSource.loop = true;
            _ambientSource.spatialBlend = 0f;
            _ambientSource.volume = musicMasterVolume;
        }

        private void SynthesizeAllProceduralClips()
        {
            _pentatonicCrystalClips = new AudioClip[PentatonicHz.Length];
            for (int i = 0; i < PentatonicHz.Length; i++)
            {
                _pentatonicCrystalClips[i] = CreateCrystalBellClip($"CrystalChime_{i}", PentatonicHz[i], 0.48f);
            }

            _perfectWeaveClip = CreateChordClip("PerfectWeaveChord", new[] { 523.25f, 622.25f, 783.99f }, 0.55f);
            _energyPickupClip = CreateSweepChimeClip("EnergyPickup", 523.25f, 1046.50f, 0.32f);
            _jumpPadClip = CreateSweepChimeClip("JumpPadLaunch", 196.00f, 659.25f, 0.36f);
            _flashBurstClip = CreateFlashBurstClip("FlashBurst", 0.45f);
            _hazardHitClip = CreateLowImpactClip("HazardHit", 125f, 55f, 0.25f);
            _ambientPadClip = CreateSpacePadLoopClip("VoidAmbientPad", 4.0f);

            if (_ambientSource != null && _ambientPadClip != null)
            {
                _ambientSource.clip = _ambientPadClip;
                if (ProgressManager.MusicEnabled) _ambientSource.Play();
            }
        }

        private void HandleCrystalSolidified(CrystalPlatform platform, float intensity)
        {
            if (!ProgressManager.SfxEnabled || _sfxSource == null || _pentatonicCrystalClips == null) return;
            if (Time.unscaledTime - _lastCrystalSoundTime < 0.08f) return;
            _lastCrystalSoundTime = Time.unscaledTime;

            int noteIdx = MelodicWalk[_melodyNoteIndex % MelodicWalk.Length];
            _melodyNoteIndex++;

            _sfxSource.pitch = Random.Range(0.98f, 1.03f);
            _sfxSource.PlayOneShot(_pentatonicCrystalClips[noteIdx], sfxMasterVolume * 0.58f);
        }

        private void HandlePerfectWeave(Vector2 pos, float bonus)
        {
            if (!ProgressManager.SfxEnabled || _sfxSource == null || _perfectWeaveClip == null) return;
            _sfxSource.pitch = 1.0f;
            _sfxSource.PlayOneShot(_perfectWeaveClip, sfxMasterVolume * 0.72f);
        }

        private void HandleEnergyCollected(EnergyOrb orb, float amount)
        {
            if (!ProgressManager.SfxEnabled || _sfxSource == null || _energyPickupClip == null) return;
            _sfxSource.pitch = Random.Range(0.96f, 1.08f);
            _sfxSource.PlayOneShot(_energyPickupClip, sfxMasterVolume * 0.85f);
        }

        private void HandleJumpPadLaunched(PrismaticJumpPad pad)
        {
            if (!ProgressManager.SfxEnabled || _sfxSource == null || _jumpPadClip == null) return;
            _sfxSource.pitch = Random.Range(0.95f, 1.05f);
            _sfxSource.PlayOneShot(_jumpPadClip, sfxMasterVolume * 0.85f);
        }

        private void HandleFlashBurst()
        {
            if (!ProgressManager.SfxEnabled || _sfxSource == null || _flashBurstClip == null) return;
            _sfxSource.pitch = 1.0f;
            _sfxSource.PlayOneShot(_flashBurstClip, sfxMasterVolume * 0.90f);
        }

        private void HandleHazardResolved(ShadowHazard hazard, bool bySkill)
        {
            if (!ProgressManager.SfxEnabled || _sfxSource == null) return;
            if (bySkill && _perfectWeaveClip != null)
            {
                _sfxSource.pitch = 1.15f;
                _sfxSource.PlayOneShot(_perfectWeaveClip, sfxMasterVolume * 0.65f);
            }
            else if (!bySkill && _hazardHitClip != null)
            {
                _sfxSource.pitch = Random.Range(0.9f, 1.05f);
                _sfxSource.PlayOneShot(_hazardHitClip, sfxMasterVolume * 0.9f);
            }
        }

        #region Procedural ASMR Audio Synthesis

        private static AudioClip CreateCrystalBellClip(string clipName, float freqHz, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float attack = Mathf.Clamp01(t / 0.008f);
                float env = attack * Mathf.Exp(-t * 8.5f);
                float wave = Mathf.Sin(2f * Mathf.PI * freqHz * t) * 0.65f
                           + Mathf.Sin(2f * Mathf.PI * (freqHz * 2.0f) * t) * 0.25f * Mathf.Exp(-t * 14f)
                           + Mathf.Sin(2f * Mathf.PI * (freqHz * 3.01f) * t) * 0.10f * Mathf.Exp(-t * 20f);
                data[i] = wave * env;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChordClip(string clipName, float[] freqs, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Clamp01(t / 0.012f) * Mathf.Exp(-t * 6.5f);
                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * t);
                }
                data[i] = (sum / freqs.Length) * env;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSweepChimeClip(string clipName, float startHz, float endHz, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float progress = (float)i / samples;
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(startHz, endHz, progress);
                phase += 2f * Mathf.PI * freq / sampleRate;
                float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 7.5f);
                data[i] = (Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 1.5f) * 0.3f) * env;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateFlashBurstClip(string clipName, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float subFreq = Mathf.Lerp(210f, 52f, t / duration);
                phase += 2f * Mathf.PI * subFreq / sampleRate;
                float chime = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * Mathf.Exp(-t * 10f) * 0.35f;
                float sub = Mathf.Sin(phase) * Mathf.Exp(-t * 7f) * 0.65f;
                data[i] = (sub + chime) * Mathf.Clamp01(t / 0.005f);
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateLowImpactClip(string clipName, float startHz, float endHz, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(startHz, endHz, t / duration);
                phase += 2f * Mathf.PI * freq / sampleRate;
                float env = Mathf.Exp(-t * 12f);
                data[i] = Mathf.Clamp(Mathf.Sin(phase) * 1.4f, -1f, 1f) * env;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSpacePadLoopClip(string clipName, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            // Kusursuz döngü için tam periyot katlı düşük derin akor (C2 = 65.5Hz, G2 = 98Hz, Eb3 = 155.5Hz)
            float f1 = Mathf.Round(65.5f * duration) / duration;
            float f2 = Mathf.Round(98.0f * duration) / duration;
            float f3 = Mathf.Round(155.5f * duration) / duration;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float lfo = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * (1f / duration) * t);
                float sample = (Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.50f
                             + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.32f
                             + Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.18f) * lfo * 0.55f;
                data[i] = sample;
            }

            AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        #endregion
    }
}
