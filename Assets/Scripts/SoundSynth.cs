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
    private AudioClip scatterClip;
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
        // 0.9, so the effects vanished next to the voice.
        // v1.3.1: the v1.2.0 fix overshot the roll — brown noise at 0.9
        // peak through TV speakers is a loud wind rush, and the crash was
        // the same noise family, so it vanished under the roll. The roll
        // is now a quiet dark rumble and the crash is a real wooden
        // clatter (staggered knocks + thump), with a scatter clatter
        // after it.
        crashClip = MakePinCrash("PinCrash", 1.2f);
        scatterClip = MakePinScatter("PinScatter", 0.7f);
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

    // Second, smaller clatter as the pins scatter and hit the floor.
    // Played a fraction of a second after the main crash.
    public void PlayScatter()
    {
        if (oneShotSource != null && scatterClip != null) oneShotSource.PlayOneShot(scatterClip, 0.7f);
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
            // v1.3.1: quiet on purpose. The v1.2.0 roll at 0.7 volume
            // was a wind rush on TV speakers; the roll is a low rumble
            // under the narration, not the loudest thing in the room.
            rollSource.volume = 0.25f;
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

    // v1.3.1: a real wooden pin clatter, NOT filtered noise. The old
    // crash was white noise through a low-pass — the same sound family
    // as the roll loop — so it read as more wind, not pins. A clatter
    // is several short wooden knocks landing a few milliseconds apart,
    // each a sine thump at a knock frequency with a fast decay, plus a
    // low body thump at impact and a soft rattle tail. The staggered
    // knocks are what the ear recognises as "pins".
    private AudioClip MakePinCrash(string name, float seconds)
    {
        float[] data = NewBuffer(seconds);
        // Knock times, frequencies and strengths: first hit hardest,
        // the rest scattered as the pins fly into each other.
        float[] knockTimes = { 0.00f, 0.02f, 0.05f, 0.09f, 0.14f, 0.21f, 0.30f };
        float[] knockFreqs = { 280f, 420f, 350f, 520f, 300f, 610f, 380f };
        float[] knockAmps = { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f };
        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float s = 0f;
            // Low body thump at the moment of impact.
            s += Mathf.Sin(2f * Mathf.PI * 85f * t) * Mathf.Exp(-t * 18f) * 0.9f;
            // Staggered wooden knocks.
            for (int k = 0; k < knockTimes.Length; k++)
            {
                float dt = t - knockTimes[k];
                if (dt < 0f) continue;
                float decay = Mathf.Exp(-dt * 55f);
                float knock = Mathf.Sin(2f * Mathf.PI * knockFreqs[k] * dt) * decay;
                float noise = Random.Range(-1f, 1f) * decay * 0.3f;
                s += (knock + noise) * knockAmps[k];
            }
            // Soft rattle tail so the crash does not end dead.
            float noiseTail = Random.Range(-1f, 1f);
            low += 0.35f * (noiseTail - low);
            s += low * Mathf.Exp(-t * 2.5f) * 0.15f;
            data[i] = s;
        }
        Normalize(data, 0.95f);
        return MakeClip(name, seconds, data);
    }

    // Quieter, shorter clatter for the pins scattering after the first
    // impact — fewer knocks, lower level, no big thump.
    private AudioClip MakePinScatter(string name, float seconds)
    {
        float[] data = NewBuffer(seconds);
        float[] knockTimes = { 0.00f, 0.06f, 0.13f, 0.22f };
        float[] knockFreqs = { 320f, 460f, 300f, 540f };
        float[] knockAmps = { 0.6f, 0.5f, 0.4f, 0.3f };
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / SampleRate;
            float s = 0f;
            for (int k = 0; k < knockTimes.Length; k++)
            {
                float dt = t - knockTimes[k];
                if (dt < 0f) continue;
                float decay = Mathf.Exp(-dt * 60f);
                float knock = Mathf.Sin(2f * Mathf.PI * knockFreqs[k] * dt) * decay;
                float noise = Random.Range(-1f, 1f) * decay * 0.3f;
                s += (knock + noise) * knockAmps[k];
            }
            data[i] = s;
        }
        Normalize(data, 0.7f);
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
    // v1.3.1: much darker and quieter than v1.2.0. The old loop moved
    // too fast (0.05f smoothing) and normalised to 0.9 peak, which is
    // a wind rush on TV speakers. Slower movement keeps only the low
    // end, and the lower peak plus the lower StartRoll volume make it
    // a rumble under the narration instead of the loudest sound.
    private AudioClip MakeRollLoop(string name, float seconds)
    {
        float[] data = NewBuffer(seconds);
        float brown = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            brown += (Random.Range(-1f, 1f) - brown) * 0.02f;
            float fade = Mathf.Min(1f, i / (SampleRate * 0.02f));
            fade *= Mathf.Min(1f, (data.Length - i) / (SampleRate * 0.02f));
            data[i] = brown * 8f * fade;
        }
        Normalize(data, 0.5f);
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
