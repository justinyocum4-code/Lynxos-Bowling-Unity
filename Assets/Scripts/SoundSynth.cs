// Lynxos Bowling — fully procedural sound effects.
// No audio asset files: every clip is synthesised in code at startup.
// One AudioSource plays one-shots; a second loops the ball-roll rumble.
using UnityEngine;

public class SoundSynth : MonoBehaviour
{
    private const int SampleRate = 22050;

    private AudioSource oneShotSource;
    private AudioSource rollSource;
    private AudioClip crashClip;
    private AudioClip gutterClip;
    private AudioClip cheerClip;
    private AudioClip clickClip;
    private AudioClip spareClip;
    private AudioClip rollClip;

    private void Awake()
    {
        oneShotSource = gameObject.AddComponent<AudioSource>();
        rollSource = gameObject.AddComponent<AudioSource>();
        rollSource.loop = true;
        BuildClips();
    }

    private void BuildClips()
    {
        crashClip = MakeNoiseBurst("PinCrash", 0.45f, 0.9f);
        gutterClip = MakeDescendingTone("Gutter", 0.7f, 420f, 140f);
        cheerClip = MakeCheer("Cheer", 1.4f);
        clickClip = MakeTone("Click", 0.07f, 880f, 0.5f);
        spareClip = MakeTwoTone("Spare", 0.12f, 660f, 990f);
        rollClip = MakeRollLoop("Roll", 1.2f);
    }

    public void PlayCrash(float volume)
    {
        if (crashClip != null) oneShotSource.PlayOneShot(crashClip, Mathf.Clamp01(volume));
    }

    public void PlayGutter()
    {
        if (gutterClip != null) oneShotSource.PlayOneShot(gutterClip, 0.7f);
    }

    public void PlayCheer()
    {
        if (cheerClip != null) oneShotSource.PlayOneShot(cheerClip, 0.8f);
    }

    public void PlayClick()
    {
        if (clickClip != null) oneShotSource.PlayOneShot(clickClip, 0.6f);
    }

    public void PlaySpare()
    {
        if (spareClip != null) oneShotSource.PlayOneShot(spareClip, 0.7f);
    }

    public void StartRoll()
    {
        if (rollClip != null && !rollSource.isPlaying)
        {
            rollSource.clip = rollClip;
            rollSource.volume = 0.25f;
            rollSource.Play();
        }
    }

    public void SetRollVolume(float volume)
    {
        rollSource.volume = Mathf.Clamp01(volume);
    }

    public void StopRoll()
    {
        if (rollSource.isPlaying) rollSource.Stop();
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

    // White noise with an exponential decay, lightly low-passed: pin crash.
    private AudioClip MakeNoiseBurst(string name, float seconds, float volume)
    {
        float[] data = NewBuffer(seconds);
        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length;
            float noise = Random.Range(-1f, 1f);
            low += 0.25f * (noise - low); // one-pole low-pass
            float env = Mathf.Exp(-6f * t);
            data[i] = low * env * volume;
        }
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
            brown += (Random.Range(-1f, 1f) - brown) * 0.02f;
            float fade = Mathf.Min(1f, i / (SampleRate * 0.02f));
            fade *= Mathf.Min(1f, (data.Length - i) / (SampleRate * 0.02f));
            data[i] = brown * 1.6f * fade;
        }
        return MakeClip(name, seconds, data);
    }
}
