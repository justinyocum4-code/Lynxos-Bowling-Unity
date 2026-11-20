// Lynxos Bowling — fully procedural sound effects.
// No audio asset files: every clip is synthesised in code at startup.
// One AudioSource plays one-shots; a second loops the ball-roll rumble.
using UnityEngine;

public class SoundSynth : MonoBehaviour
{
    private const int SampleRate = 22050;

    private AudioSource oneShotSource;
    private AudioSource rollSource;
    private AudioSource musicSource;
    private AudioClip crashClip;
    private AudioClip gutterClip;
    private AudioClip cheerClip;
    private AudioClip clickClip;
    private AudioClip spareClip;
    private AudioClip rollClip;
    private AudioClip musicClip;

    private void Awake()
    {
        oneShotSource = gameObject.AddComponent<AudioSource>();
        rollSource = gameObject.AddComponent<AudioSource>();
        rollSource.loop = true;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        BuildClips();
        StartMusic();
    }

    private void BuildClips()
    {
        // v1.2.0: every clip is normalised to ~0.9 peak. The v1.1 clips
        // were near-silent BY CONSTRUCTION (roll ~0.03 out, music ~0.02,
        // crash a 0.45 s burst with a fast decay) while narration plays at
        // 0.9, so the effects vanished next to the voice. The crash is
        // also longer now (1.2 s, slower decay) so it reads as pins.
        crashClip = MakeNoiseBurst("PinCrash", 1.2f, 0.9f);
        gutterClip = MakeDescendingTone("Gutter", 0.7f, 420f, 140f);
        cheerClip = MakeCheer("Cheer", 1.4f);
        clickClip = MakeTone("Click", 0.07f, 880f, 0.8f);
        spareClip = MakeTwoTone("Spare", 0.12f, 660f, 990f);
        rollClip = MakeRollLoop("Roll", 1.2f);
        musicClip = MakeMusicLoop("Music", 8f);
    }

    // Quiet background music, looping from the moment the game starts.
    public void StartMusic()
    {
        if (musicSource != null && musicClip != null && !musicSource.isPlaying)
        {
            musicSource.clip = musicClip;
            musicSource.volume = 0.35f;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Stop();
    }

    public void PlayCrash(float volume)
    {
        if (oneShotSource != null && crashClip != null) oneShotSource.PlayOneShot(crashClip, Mathf.Clamp01(volume));
    }

    public void PlayGutter()
    {
        if (oneShotSource != null && gutterClip != null) oneShotSource.PlayOneShot(gutterClip, 0.9f);
    }

    public void PlayCheer()
    {
        if (oneShotSource != null && cheerClip != null) oneShotSource.PlayOneShot(cheerClip, 1f);
    }

    public void PlayClick()
    {
        if (oneShotSource != null && clickClip != null) oneShotSource.PlayOneShot(clickClip, 0.8f);
    }

    public void PlaySpare()
    {
        if (oneShotSource != null && spareClip != null) oneShotSource.PlayOneShot(spareClip, 0.9f);
    }

    public void StartRoll()
    {
        if (rollSource != null && rollClip != null && !rollSource.isPlaying)
        {
            rollSource.clip = rollClip;
            rollSource.volume = 0.7f;
            rollSource.Play();
        }
    }

    public void SetRollVolume(float volume)
    {
        if (rollSource != null) rollSource.volume = Mathf.Clamp01(volume);
    }

    public void StopRoll()
    {
        if (rollSource != null && rollSource.isPlaying) rollSource.Stop();
    }

    // --- Clip builders (all mono, SampleRate Hz) ---

    private AudioClip MakeClip(string name, float seconds, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private float[] NewBuffer(float seconds)
    {
        return new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate))];
    }

    // Scales a buffer so its loudest sample hits the target peak. Every
    // clip builder runs through this, so no effect can ever be
    // accidentally near-silent again (the v1.1.5 bug).
    private static void Normalize(float[] data, float peak)
    {
        float max = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float a = Mathf.Abs(data[i]);
            if (a > max) max = a;
        }
        if (max <= 0.0001f) return;
        float scale = peak / max;
        for (int i = 0; i < data.Length; i++) data[i] *= scale;
    }

    // White noise with an exponential decay, lightly low-passed: pin crash.
    private AudioClip MakeNoiseBurst(string name, float seconds, float volume)
    {
        float[] data = NewBuffer(seconds);
        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length;
            float noise = Random.Range(-1f, 1f);
            low += 0.35f * (noise - low); // one-pole low-pass
            float env = Mathf.Exp(-2.5f * t); // slow decay: pins keep rattling
            data[i] = low * env * volume;
        }
        Normalize(data, 0.9f);
        return MakeClip(name, seconds, data);
    }

    // Sine sliding from freqStart down to freqEnd, decaying: gutter moan.
    private AudioClip MakeDescendingTone(string name, float seconds, float freqStart, float freqEnd)
    {
        float[] data = NewBuffer(seconds);
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length;
            float freq = Mathf.Lerp(freqStart, freqEnd, t);
            phase += 2f * Mathf.PI * freq / SampleRate;
            float env = (1f - t) * (1f - t);
            data[i] = Mathf.Sin(phase) * env * 0.6f;
        }
        Normalize(data, 0.9f);
        return MakeClip(name, seconds, data);
    }

    // Crowd cheer: noise swelling up then fading, with a slow wobble.
    private AudioClip MakeCheer(string name, float seconds)
    {
        float[] data = NewBuffer(seconds);
        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length;
            float noise = Random.Range(-1f, 1f);
            low += 0.15f * (noise - low);
            float rise = Mathf.Clamp01(t / 0.25f);
            float fall = 1f - Mathf.Clamp01((t - 0.25f) / 0.75f);
            float wobble = 0.75f + 0.25f * Mathf.Sin(t * 40f);
            data[i] = low * rise * fall * wobble * 0.9f;
        }
        Normalize(data, 0.9f);
        return MakeClip(name, seconds, data);
    }

    // Short sine blip: UI click.
    private AudioClip MakeTone(string name, float seconds, float freq, float volume)
    {
        float[] data = NewBuffer(seconds);
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length;
            float env = Mathf.Min(1f, t * 20f) * (1f - t);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate) * env * volume;
        }
        Normalize(data, 0.9f);
        return MakeClip(name, seconds, data);
    }

    // Two rising notes: spare chime.
    private AudioClip MakeTwoTone(string name, float noteSeconds, float freq1, float freq2)
    {
        float total = noteSeconds * 2f;
        float[] data = NewBuffer(total);
        int half = data.Length / 2;
        for (int i = 0; i < data.Length; i++)
        {
            float freq = i < half ? freq1 : freq2;
            int local = i < half ? i : i - half;
            float t = (float)local / half;
            float env = Mathf.Min(1f, t * 20f) * (1f - t);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * local / SampleRate) * env * 0.6f;
        }
        Normalize(data, 0.9f);
        return MakeClip(name, total, data);
    }

    // Low brown-ish rumble, looped while the ball rolls.
    // Short fades at both ends keep the loop seamless.
    private AudioClip MakeRollLoop(string name, float seconds)
    {
        float[] data = NewBuffer(seconds);
        float brown = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            brown += (Random.Range(-1f, 1f) - brown) * 0.05f;
            float fade = Mathf.Min(1f, i / (SampleRate * 0.02f));
            fade *= Mathf.Min(1f, (data.Length - i) / (SampleRate * 0.02f));
            data[i] = brown * 8f * fade;
        }
        Normalize(data, 0.9f);
        return MakeClip(name, seconds, data);
    }

    // Background music: a mellow 8-second loop, Am – F – C – G, 2 s per
    // chord. Soft sine pad (root + third + fifth) with a low bass pulse
    // on every half-second beat. Quiet by design (0.16 volume) so it
    // sits under the narration and the pin sounds. Chord boundaries fall
    // exactly on whole seconds so the loop point lands on a chord change;
    // a short fade at both ends masks the join.
    private AudioClip MakeMusicLoop(string name, float seconds)
    {
        float[] data = NewBuffer(seconds);
        // Per chord: pad triad + bass root, in Hz.
        float[][] chords = new float[][]
        {
            new float[] { 220f, 261.63f, 329.63f, 55f },   // Am
            new float[] { 174.61f, 220f, 261.63f, 43.65f }, // F
            new float[] { 261.63f, 329.63f, 392f, 65.41f }, // C
            new float[] { 196f, 246.94f, 293.66f, 49f },    // G
        };
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            int chord = Mathf.Min(3, (int)(t / 2f));
            float chordT = t - chord * 2f;
            float[] notes = chords[chord];
            float padEnv = Mathf.Min(1f, chordT * 4f) * (1f - chordT * 0.15f);
            float pad = 0f;
            for (int n = 0; n < 3; n++)
            {
                pad += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * 0.09f;
            }
            float beat = t % 0.5f;
            float bassEnv = Mathf.Exp(-4f * beat);
            float bass = Mathf.Sin(2f * Mathf.PI * notes[3] * t) * bassEnv * 0.30f;
            float edge = Mathf.Min(1f, i / (SampleRate * 0.03f));
            edge *= Mathf.Min(1f, (data.Length - i) / (SampleRate * 0.03f));
            data[i] = (pad * padEnv + bass) * edge;
        }
        // Music sits under the voice, so it normalises lower than the
        // effects (0.55 peak) — but it must still be audible, unlike the
        // ~0.02 it settled at in v1.1.
        Normalize(data, 0.55f);
        return MakeClip(name, seconds, data);
    }
}
