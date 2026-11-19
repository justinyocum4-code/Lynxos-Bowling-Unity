// Lynxos Bowling — voice narration via Android TextToSpeech.
// Everything is guarded: if TTS is unavailable, not on Android, or any call
// throws, narration is silently skipped and the game keeps running. The HUD
// text always mirrors the spoken line, so nothing is voice-only.
using UnityEngine;

public static class Narration
{
    public static bool Enabled = true;

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
            }
        }
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
            tts.Call<int>("setLanguage", localeUS);
            tts.Call<int>("setSpeechRate", 0.95f);
        }
        catch (System.Exception e)
        {
            ttsFailed = true;
            Debug.LogWarning("[Narration] TTS init failed: " + e.Message);
        }
#endif
    }
}
