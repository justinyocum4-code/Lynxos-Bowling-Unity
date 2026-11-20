// Lynxos Bowling — baked-in spoken voice clips.
// Android TTS is silent on Justin's Google TV (never spoke across v1.1,
// v1.1.1 or v1.1.2 — the TV likely has no English TTS voice data), so the
// fixed spoken lines are pre-recorded Piper clips shipped in
// Assets/Resources/Voices/<key>.ogg and played through Unity audio (which
// works once the camera has an AudioListener). Dynamic lines (aim in
// boards, scores, pins standing) still go through Android TTS, and the HUD
// always mirrors every spoken line in big text.
using UnityEngine;
using System.Collections.Generic;

public class VoiceBank : MonoBehaviour
{
    private AudioSource source;
    private Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
    }

    public bool HasClip(string key)
    {
        return Load(key) != null;
    }

    public void Play(string key)
    {
        AudioClip clip = Load(key);
        if (clip != null && source != null) source.PlayOneShot(clip, 0.9f);
    }

    private AudioClip Load(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        AudioClip clip;
        if (cache.TryGetValue(key, out clip)) return clip;
        clip = Resources.Load<AudioClip>("Voices/" + key);
        cache[key] = clip;
        if (clip == null) Debug.LogWarning("[VoiceBank] Missing voice clip: " + key);
        return clip;
    }
}
