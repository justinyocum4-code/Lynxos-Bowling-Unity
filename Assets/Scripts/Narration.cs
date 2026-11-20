// Lynxos Bowling — voice narration via Android TextToSpeech.
// Everything is guarded: if TTS is unavailable, not on Android, or any call
// throws, narration is silently skipped and the game keeps running. The HUD
// text always mirrors the spoken line, so nothing is voice-only.
using UnityEngine;

public static class Narration
{
    public static bool Enabled = true;

    // Baked voice clips (VoiceBank), when the manager has created one.
    public static VoiceBank Bank;

    // Why the voice is silent, when we know: shown on screen by the
    // manager instead of failing silently. Empty while TTS is fine or
    // while its async init is still pending.
    public static string LastError { get; private set; }

    private static AndroidJavaObject tts;
    private static bool ttsReady = false;
    private static bool ttsFailed = false;
    private static string pendingText;

    private class TTSInitListener : AndroidJavaProxy
    {
        public TTSInitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }

        // Called by Android when the TTS engine finishes initialising.
        // status 0 = TextToSpeech.SUCCESS.
        public void onInit(int status)
        {
            if (status == 0)
            {
                ttsReady = true;
                if (!string.IsNullOrEmpty(pendingText))
                {
                    string text = pendingText;
                    pendingText = null;
                    Speak(text);
                }
            }
            else
            {
                ttsFailed = true;
                LastError = "Voice engine failed to start (code " + status + ").";
            }
        }
    }

    // Fixed line with a baked clip: play the clip when it exists (this is
    // how menu buttons speak on a TV with no TTS voice), otherwise fall
    // back to live TTS for the full fallback text.
    public static void SpeakKey(string key, string fallbackText)
    {
        if (!Enabled) return;
        if (Bank != null && Bank.HasClip(key))
        {
            Bank.Play(key);
            return;
        }
        Speak(fallbackText);
    }

    // Same as SpeakKey, but interrupts the queue: for fast-changing
    // feedback (power meter bands) where queued clips would lag behind.
    public static void SpeakKeyNow(string key, string fallbackText)
    {
        if (!Enabled) return;
        if (Bank != null && Bank.HasClip(key))
        {
            Bank.PlayNow(key);
            return;
        }
        Speak(fallbackText);
    }

    // v1.2.0: UI labels (menus, buttons, toggles). When TalkBack is on,
    // TalkBack already reads those labels aloud — the game saying them too
    // is the "two voices talking over each other" bug from v1.1.5. So UI
    // lines are skipped entirely under TalkBack; gameplay lines (aim,
    // power, results, score) still use SpeakKey/Speak because TalkBack
    // cannot know them.
    public static void SpeakUIKey(string key, string fallbackText)
    {
        if (TalkBackOn) return;
        SpeakKey(key, fallbackText);
    }

    // True when Android's touch exploration (TalkBack) is on. Checked
    // once and cached. Same AndroidJavaObject pattern as the TV check in
    // BowlingUI.IsTelevision. False on any failure, so a broken check
    // never silences the game's own voice.
    private static bool talkBackChecked;
    private static bool talkBackOn;

    public static bool TalkBackOn
    {
        get
        {
            if (!talkBackChecked)
            {
                talkBackChecked = true;
                talkBackOn = DetectTalkBack();
            }
            return talkBackOn;
        }
    }

    private static bool DetectTalkBack()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject am = activity.Call<AndroidJavaObject>("getSystemService", "accessibility"))
            {
                return am.Call<bool>("isTouchExplorationEnabled");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Narration] TalkBack check failed: " + e.Message);
            return false;
        }
#else
        return false;
#endif
    }

    public static void Speak(string text)
    {
        if (!Enabled) return;
        if (string.IsNullOrEmpty(text)) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            EnsureTTS();
            if (tts == null) return;
            if (!ttsReady)
            {
                // Engine still starting: remember the latest line and speak it
                // as soon as onInit fires.
                pendingText = text;
                return;
            }
            // speak(String, int queueMode, Bundle params, String utteranceId);
            // queueMode 0 = QUEUE_FLUSH.
            tts.Call<int>("speak", text, 0, null, "lynxos-bowling");
        }
        catch (System.Exception e)
        {
            ttsFailed = true;
            LastError = "Voice speak failed: " + e.Message;
            Debug.LogWarning("[Narration] TTS speak failed: " + e.Message);
        }
#else
        Debug.Log("[Narration] " + text);
#endif
    }

    public static void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (tts != null) tts.Call<int>("stop");
        }
        catch (System.Exception)
        {
            // Never let a TTS call break the game.
        }
#endif
    }

    private static void EnsureTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (tts != null || ttsFailed) return;
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new TTSInitListener());
            AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale");
            AndroidJavaObject localeUS = localeClass.GetStatic<AndroidJavaObject>("US");
            int langResult = tts.Call<int>("setLanguage", localeUS);
            if (langResult < 0)
            {
                // US voice data is often missing on TVs: try plain English,
                // then whatever the device default is.
                AndroidJavaObject localeEN = localeClass.GetStatic<AndroidJavaObject>("ENGLISH");
                langResult = tts.Call<int>("setLanguage", localeEN);
            }
            if (langResult < 0)
            {
                AndroidJavaObject localeDefault = localeClass.CallStatic<AndroidJavaObject>("getDefault");
                langResult = tts.Call<int>("setLanguage", localeDefault);
            }
            if (langResult < 0)
            {
                // -1 = not supported, -2 = voice data missing. Speak calls
                // will silently do nothing, so record it for the screen.
                LastError = "No English voice on this TV (code " + langResult + ").";
            }
            tts.Call<int>("setSpeechRate", 0.95f);
        }
        catch (System.Exception e)
        {
            ttsFailed = true;
            LastError = "Voice init failed: " + e.Message;
            Debug.LogWarning("[Narration] TTS init failed: " + e.Message);
        }
#endif
    }
}
