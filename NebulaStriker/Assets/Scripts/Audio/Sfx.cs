using System.Collections.Generic;
using UnityEngine;

namespace NebulaStriker
{
    public enum SfxId { Laser, EnemyShot, Explosion, BigExplosion, Hit, PowerUp, Bomb, Shield, Ui, Roll }

    /// <summary>
    /// Procedural audio: every sound effect and the ambient synth music loop are synthesised
    /// at start-up with AudioClip.Create (no audio files needed). Plays through a small voice pool.
    /// </summary>
    public static class Sfx
    {
        const int SR = 44100;
        const float TwoPi = Mathf.PI * 2f;

        public static float MasterVolume = 0.8f;
        public static float MusicVolume = 0.3f;

        static AudioSource[] pool;
        static int next;
        static AudioSource music;
        static readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        static System.Random rng = new System.Random(1234);

        public static void Init(GameObject host)
        {
            pool = new AudioSource[14];
            for (int i = 0; i < pool.Length; i++)
            {
                var s = host.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool[i] = s;
            }
            clips.Clear();
            clips[SfxId.Laser] = Laser();
            clips[SfxId.EnemyShot] = EnemyShot();
            clips[SfxId.Explosion] = Explosion(0.9f, 3.5f, 55f, "explosion");
            clips[SfxId.BigExplosion] = Explosion(2.2f, 1.5f, 38f, "big_explosion");
            clips[SfxId.Hit] = Hit();
            clips[SfxId.PowerUp] = PowerUp();
            clips[SfxId.Bomb] = Bomb();
            clips[SfxId.Shield] = Shield();
            clips[SfxId.Ui] = Ui();
            clips[SfxId.Roll] = Roll();

            music = host.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.volume = MusicVolume;
            music.clip = Music();
            music.Play();
        }

        public static void Play(SfxId id, float volume = 1f, float pitch = 1f)
        {
            if (pool == null) return;
            if (!clips.TryGetValue(id, out var clip) || clip == null) return;
            var s = pool[next];
            next = (next + 1) % pool.Length;
            if (s == null) return;
            s.Stop();
            s.clip = clip;
            s.volume = volume * MasterVolume;
            s.pitch = pitch;
            s.Play();
        }

        // ------------------------------------------------------------------ synthesis helpers
        static AudioClip Make(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, SR, false);
            c.SetData(data, 0);
            return c;
        }

        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
        static float SinD(double ph) => (float)System.Math.Sin(ph);

        static AudioClip Laser()
        {
            float dur = 0.16f; int n = (int)(SR * dur); var d = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float f = Mathf.Lerp(1800f, 280f, Mathf.Pow(k, 0.6f));
                ph += TwoPi * f / SR;
                float s = SinD(ph);
                float sq = Mathf.Sign(s);
                float env = Mathf.Exp(-k * 5f) * (1f - k);
                d[i] = (sq * 0.3f + s * 0.7f) * env * 0.55f;
            }
            return Make("laser", d);
        }

        static AudioClip EnemyShot()
        {
            float dur = 0.22f; int n = (int)(SR * dur); var d = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float f = Mathf.Lerp(700f, 160f, k);
                ph += TwoPi * f / SR;
                float saw = (float)(ph / TwoPi % 1.0) * 2f - 1f;
                d[i] = (saw * 0.5f + SinD(ph) * 0.5f) * (1f - k) * (1f - k) * 0.5f;
            }
            return Make("enemy_shot", d);
        }

        static AudioClip Explosion(float dur, float decay, float thumpHz, string name)
        {
            int n = (int)(SR * dur); var d = new float[n]; float lp = 0f; double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, k = i / (float)n;
                float a = Mathf.Lerp(0.45f, 0.015f, Mathf.Sqrt(k));
                lp += a * (Noise() - lp);
                ph += TwoPi * Mathf.Lerp(thumpHz * 1.6f, thumpHz * 0.6f, k) / SR;
                float thump = SinD(ph) * Mathf.Exp(-t * 7f);
                float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 200f);
                float s = lp * 2.2f * env + thump * 0.8f;
                d[i] = s / (1f + Mathf.Abs(s)); // soft clip
            }
            return Make(name, d);
        }

        static AudioClip Hit()
        {
            int n = (int)(SR * 0.07f); var d = new float[n]; float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n; float x = Noise();
                lp += 0.2f * (x - lp);
                d[i] = (x - lp) * (1f - k) * 0.6f;
            }
            return Make("hit", d);
        }

        static AudioClip PowerUp()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            float noteDur = 0.08f; int per = (int)(SR * noteDur); int n = per * notes.Length + (int)(SR * 0.15f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                int idx = Mathf.Min(i / per, notes.Length - 1);
                float local = (i - idx * per) / (float)SR;
                float f = notes[idx];
                float t = i / (float)SR;
                float env = Mathf.Exp(-local * 9f) * (idx == notes.Length - 1 ? 1f : 0.85f);
                d[i] = (Mathf.Sin(TwoPi * f * t) * 0.7f + Mathf.Sin(TwoPi * f * 2f * t) * 0.2f) * env * 0.5f;
            }
            return Make("powerup", d);
        }

        static AudioClip Bomb()
        {
            float dur = 1.7f; int n = (int)(SR * dur); var d = new float[n]; double ph = 0; float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, k = i / (float)n;
                ph += TwoPi * Mathf.Lerp(240f, 32f, Mathf.Pow(k, 0.4f)) / SR;
                lp += 0.05f * (Noise() - lp);
                float env = Mathf.Exp(-t * 1.8f) * Mathf.Min(1f, t * 80f);
                float s = (SinD(ph) * 0.9f + lp * 1.5f) * env;
                d[i] = s / (1f + Mathf.Abs(s)) * 0.9f;
            }
            return Make("bomb", d);
        }

        static AudioClip Shield()
        {
            int n = (int)(SR * 0.28f); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, k = i / (float)n;
                float trem = 0.6f + 0.4f * Mathf.Sin(TwoPi * 32f * t);
                d[i] = (Mathf.Sin(TwoPi * 420f * t) + 0.4f * Mathf.Sin(TwoPi * 630f * t)) * trem * (1f - k) * 0.35f;
            }
            return Make("shield", d);
        }

        static AudioClip Ui()
        {
            int n = (int)(SR * 0.14f); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, k = i / (float)n;
                float f = k < 0.5f ? 880f : 1320f;
                d[i] = Mathf.Sin(TwoPi * f * t) * Mathf.Exp(-(k % 0.5f) * 8f) * 0.35f;
            }
            return Make("ui", d);
        }

        static AudioClip Roll()
        {
            int n = (int)(SR * 0.38f); var d = new float[n]; float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                lp += Mathf.Lerp(0.02f, 0.25f, Mathf.Sin(k * Mathf.PI)) * (Noise() - lp);
                d[i] = lp * Mathf.Sin(k * Mathf.PI) * 1.4f;
            }
            return Make("roll", d);
        }

        /// <summary>16-second ambient loop: Am – F – C – G pads, pulsing bass and a soft arpeggio.</summary>
        static AudioClip Music()
        {
            float chordLen = 4f;
            float[] roots = { 110f, 87.31f, 130.81f, 98f };
            bool[] minor = { true, false, false, false };
            int n = (int)(SR * chordLen * roots.Length);
            var d = new float[n];
            float peak = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                int ci = Mathf.Min((int)(t / chordLen), roots.Length - 1);
                float lt = t - ci * chordLen;
                float root = roots[ci];
                float third = root * (minor[ci] ? 1.1892f : 1.2599f);
                float fifth = root * 1.4983f;

                float env = Mathf.Clamp01(lt / 0.9f) * Mathf.Clamp01((chordLen - lt) / 0.9f);
                float lfo = 0.75f + 0.25f * Mathf.Sin(TwoPi * 0.25f * t);
                float pad = 0f;
                pad += Mathf.Sin(TwoPi * root * 2f * t) + Mathf.Sin(TwoPi * root * 2.006f * t);
                pad += Mathf.Sin(TwoPi * third * 2f * t) + Mathf.Sin(TwoPi * third * 1.996f * t);
                pad += Mathf.Sin(TwoPi * fifth * 2f * t) + Mathf.Sin(TwoPi * fifth * 2.004f * t);
                pad *= 0.11f * env * lfo;

                float beat = lt % 0.5f;
                float bass = Mathf.Sin(TwoPi * root * 0.5f * t) * Mathf.Exp(-beat * 5f) * 0.35f * env;

                float step = lt % 0.25f;
                int arpIdx = (int)(lt / 0.25f) % 4;
                float arpF = (arpIdx == 0 ? root : arpIdx == 1 ? third : arpIdx == 2 ? fifth : third) * 4f;
                float arp = Mathf.Sin(TwoPi * arpF * t) * Mathf.Exp(-step * 14f) * 0.08f;

                float s = pad + bass + arp;
                d[i] = s;
                peak = Mathf.Max(peak, Mathf.Abs(s));
            }
            float norm = 0.7f / peak;
            for (int i = 0; i < n; i++) d[i] *= norm;
            return Make("music_loop", d);
        }
    }
}
