// Lynxos Bowling — baked-in spoken voice clips.
// Android TTS is silent on Justin's Google TV (never spoke across v1.1,
// v1.1.1 or v1.1.2 — the TV likely has no English TTS voice data), so the
// fixed spoken lines are pre-recorded Piper clips shipped in
// Assets/Resources/Voices/<key>.ogg and played through Unity audio (which
// works once the camera has an AudioListener). Dynamic lines (aim in
// boards, scores, pins standing) still go through Android TTS, and the HUD
// always mirrors every spoken line in big text.
//
// v1.2.0: clips are QUEUED, never overlapped. Before this, PlayOneShot
// let the menu clip, the ball-away clip and the result clip all talk over
// each other. Play() now appends to a queue played one clip at a time;
// PlayNow() interrupts for fast-changing feedback (the power meter).
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VoiceBank : MonoBehaviour
{
    private AudioSource source;
    private Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private Queue<string> queue = new Queue<string>();
    private Coroutine queueRoutine;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.volume = 0.9f;
    }

    public bool HasClip(string key)
    {
        return Load(key) != null;
    }

    // Seconds of queued audio still to play (including the clip that is
    // playing now). Lets callers delay a TTS line until the clips finish.
    public float QueuedSeconds()
    {
        float total = 0f;
        if (source != null && source.isPlaying && source.clip != null)
        {
            total += Mathf.Max(0f, source.clip.length - source.time);
        }
        foreach (string key in queue)
        {
            AudioClip clip = Load(key);
            if (clip != null) total += clip.length;
        }
        return total;
    }

    public float ClipLength(string key)
    {
        AudioClip clip = Load(key);
        return clip != null ? clip.length : 0f;
    }

    // Queued play: clips never overlap each other.
    public void Play(string key)
    {
        AudioClip clip = Load(key);
        if (clip == null || source == null) return;
        queue.Enqueue(key);
        if (queueRoutine == null) queueRoutine = StartCoroutine(PlayQueue());
    }

    // Interrupt: stop everything and play this clip right now. For
    // fast-changing feedback (power meter bands) where a queue would lag.
    public void PlayNow(string key)
    {
        AudioClip clip = Load(key);
        if (clip == null || source == null) return;
        if (queueRoutine != null)
        {
            StopCoroutine(queueRoutine);
            queueRoutine = null;
        }
        queue.Clear();
        source.Stop();
        source.clip = clip;
        source.Play();
    }

    public void StopAll()
    {
        if (queueRoutine != null)
        {
            StopCoroutine(queueRoutine);
            queueRoutine = null;
        }
        queue.Clear();
        if (source != null) source.Stop();
    }

    private IEnumerator PlayQueue()
    {
        while (queue.Count > 0)
        {
            string key = queue.Dequeue();
            AudioClip clip = Load(key);
            if (clip == null) continue;
            source.clip = clip;
            source.Play();
            yield return new WaitForSeconds(clip.length + 0.05f);
        }
        queueRoutine = null;
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
