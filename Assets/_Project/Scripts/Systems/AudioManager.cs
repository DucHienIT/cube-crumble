using UnityEngine;

namespace CubeBurst.Systems
{
    /// All audio is synthesized at startup — no sound assets needed.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const int Rate = 44100;

        AudioSource _sfx;
        AudioSource _music;
        AudioClip _click, _crumble, _land, _complete, _win, _lose;
        bool _soundOn = true;

        public static AudioManager Create()
        {
            var go = new GameObject("AudioManager");
            return go.AddComponent<AudioManager>();
        }

        public bool SoundOn
        {
            get => _soundOn;
            set
            {
                _soundOn = value;
                SaveSystem.SoundOn = value;
                _sfx.mute = !value;
                _music.mute = !value;
            }
        }

        void Awake()
        {
            Instance = this;
            _sfx = gameObject.AddComponent<AudioSource>();
            _music = gameObject.AddComponent<AudioSource>();
            BuildClips();
            _music.clip = BuildMusicLoop();
            _music.loop = true;
            _music.volume = GameConfig.Active.musicVolume;
            _music.Play();
            SoundOn = SaveSystem.SoundOn;
        }

        void BuildClips()
        {
            _click = Tone("click", 880f, 0.06f, 0.35f);
            _crumble = NoiseBurst("crumble", 0.16f, 0.5f);
            _land = Tone("land", 540f, 0.09f, 0.4f);
            _complete = Arp("complete", new[] { 660f, 830f, 990f }, 0.09f, 0.4f);
            _win = Arp("win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.12f, 0.45f);
            _lose = Slide("lose", 420f, 160f, 0.55f, 0.45f);
        }

        public void PlayClick() => Play(_click, 1f);
        public void PlayCrumble() => Play(_crumble, 1f + Random.Range(-0.08f, 0.08f));
        public void PlayLand(int combo) => Play(_land, 1f + Mathf.Min(combo, 12) * 0.05f);
        public void PlayComplete() => Play(_complete, 1f);
        public void PlayWin() => Play(_win, 1f);
        public void PlayLose() => Play(_lose, 1f);

        void Play(AudioClip clip, float pitch)
        {
            if (clip == null) return;
            _sfx.pitch = pitch;
            _sfx.PlayOneShot(clip);
        }

        // ---- synthesis helpers ----

        static AudioClip MakeClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Tone(string name, float freq, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-5f * t / dur) * Mathf.Min(1f, i / 180f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * vol;
            }
            return MakeClip(name, data);
        }

        static AudioClip Arp(string name, float[] freqs, float noteDur, float vol)
        {
            float total = noteDur * freqs.Length + 0.35f;
            int n = (int)(Rate * total);
            var data = new float[n];
            for (int f = 0; f < freqs.Length; f++)
            {
                int start = (int)(Rate * noteDur * f);
                for (int i = start; i < n; i++)
                {
                    float t = (i - start) / (float)Rate;
                    float env = Mathf.Exp(-6f * t) * Mathf.Min(1f, (i - start) / 150f);
                    data[i] += Mathf.Sin(2f * Mathf.PI * freqs[f] * t) * env * vol;
                }
            }
            return MakeClip(name, data);
        }

        static AudioClip NoiseBurst(string name, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            float prev = 0f;
            var rnd = new System.Random(12345);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-14f * t);
                float raw = (float)(rnd.NextDouble() * 2.0 - 1.0);
                prev = Mathf.Lerp(prev, raw, 0.35f); // cheap lowpass
                data[i] = prev * env * vol;
            }
            return MakeClip(name, data);
        }

        static AudioClip Slide(string name, float f0, float f1, float dur, float vol)
        {
            int n = (int)(Rate * dur);
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float freq = Mathf.Lerp(f0, f1, t / dur);
                phase += 2f * Mathf.PI * freq / Rate;
                float env = Mathf.Min(1f, i / 200f) * Mathf.Min(1f, (n - i) / 800f);
                data[i] = Mathf.Sin(phase) * env * vol;
            }
            return MakeClip(name, data);
        }

        /// 4-second ambient pad. Frequencies are exact multiples of 0.25 Hz so
        /// every sine completes whole cycles in the loop — no click at wrap.
        static AudioClip BuildMusicLoop()
        {
            const float T = 4f;
            int n = (int)(Rate * T);
            var data = new float[n];
            float[] freqs = { 261.5f, 329.5f, 392f, 523.25f };
            float[] vols = { 0.09f, 0.07f, 0.06f, 0.04f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float tremolo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                float v = 0f;
                for (int f = 0; f < freqs.Length; f++)
                    v += Mathf.Sin(2f * Mathf.PI * freqs[f] * t) * vols[f];
                data[i] = v * tremolo;
            }
            return MakeClip("music", data);
        }
    }
}
