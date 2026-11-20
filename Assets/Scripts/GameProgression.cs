// Lynxos Bowling — progression: coins, achievements, missions, daily
// rewards, unlockable balls and gear. Everything is earned by playing:
// there is NO real money and NO in-app purchase code anywhere in this
// game. All state lives in PlayerPrefs under the LB12_ prefix, which
// does not collide with the existing LynxosBowlingBest key.
using UnityEngine;
using System;
using System.Collections.Generic;

public static class GameProgression
{
    private const string P = "LB12_";

    // --- Coins ---

    public static int Coins
    {
        get { return PlayerPrefs.GetInt(P + "Coins", 0); }
    }

    public static int TotalEarned
    {
        get { return PlayerPrefs.GetInt(P + "TotalEarned", 0); }
    }

    public static int GamesPlayed
    {
        get { return PlayerPrefs.GetInt(P + "GamesPlayed", 0); }
    }

    // Adds coins. A grant that pushes lifetime earnings to 100 unlocks
    // the Coin Collector achievement (queued in pendingAchievements so
    // several unlocks in one game can all be announced).
    private static Queue<string> pendingAchievements = new Queue<string>();

    public static string TakePendingAchievement()
    {
        return pendingAchievements.Count > 0 ? pendingAchievements.Dequeue() : null;
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(P + "Coins", Coins + amount);
        PlayerPrefs.SetInt(P + "TotalEarned", TotalEarned + amount);
        PlayerPrefs.Save();
        if (TotalEarned >= 100 && !IsAchievementUnlocked("rich_100"))
        {
            UnlockAchievement("rich_100");
        }
    }

    public static bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (Coins < amount) return false;
        PlayerPrefs.SetInt(P + "Coins", Coins - amount);
        PlayerPrefs.Save();
        return true;
    }

    // --- Settings ---

    public static bool TimerOn
    {
        get { return PlayerPrefs.GetInt(P + "TimerOn", 0) == 1; }
        set
        {
            PlayerPrefs.SetInt(P + "TimerOn", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // --- Achievements ---

    public struct Achievement
    {
        public string Id;
        public string Name;
        public string Desc;
        public Achievement(string id, string name, string desc)
        {
            Id = id;
            Name = name;
            Desc = desc;
        }
    }

    public static readonly Achievement[] Achievements = new Achievement[]
    {
        new Achievement("first_strike", "First Strike",  "Get your first strike"),
        new Achievement("first_spare",  "First Spare",   "Get your first spare"),
        new Achievement("turkey",       "Turkey",        "Three strikes in a row"),
        new Achievement("century",      "Century",       "Score 100 in a game"),
        new Achievement("quick_win",    "Quick Winner",  "Score 40 in a Quick Match"),
        new Achievement("clean_game",   "Clean Game",    "Finish a full game with no gutter balls"),
        new Achievement("fifty_frame",  "Pin Crusher",   "Knock down 50 pins in one game"),
        new Achievement("ten_games",    "Regular",       "Play 10 games"),
        new Achievement("first_ball",   "New Gear",      "Unlock your first ball"),
        new Achievement("rich_100",     "Coin Collector","Earn 100 coins in total"),
    };

    public static bool IsAchievementUnlocked(string id)
    {
        return PlayerPrefs.GetInt(P + "Ach_" + id, 0) == 1;
    }

    // Unlocks an achievement. Returns its display name when it is newly
    // unlocked, null when it was already unlocked. The name is ALSO left
    // in pendingAchievements so a single drain point can announce it.
    public static string UnlockAchievement(string id)
    {
        if (IsAchievementUnlocked(id)) return null;
        PlayerPrefs.SetInt(P + "Ach_" + id, 1);
        PlayerPrefs.Save();
        string name = AchievementName(id);
        pendingAchievements.Enqueue(name);
        return name;
    }

    public static string AchievementName(string id)
    {
        foreach (Achievement a in Achievements)
        {
            if (a.Id == id) return a.Name;
        }
        return id;
    }

    // --- Owned / equipped gear ---

    public static bool OwnsBall(int index)
    {
        if (index < 0 || index >= BowlingConfig.Balls.Length) return false;
        if (BowlingConfig.Balls[index].Cost <= 0) return true; // starters
        return PlayerPrefs.GetInt(P + "OwnBall_" + index, 0) == 1;
    }

    public static int EquippedBall
    {
        get { return PlayerPrefs.GetInt(P + "EquippedBall", 0); }
        set
        {
            PlayerPrefs.SetInt(P + "EquippedBall", value);
            PlayerPrefs.Save();
        }
    }

    // Returns true when the ball is bought (or was already owned).
    // Buying also equips it.
    public static bool BuyBall(int index)
    {
        if (index < 0 || index >= BowlingConfig.Balls.Length) return false;
        if (OwnsBall(index))
        {
            EquippedBall = index;
            return true;
        }
        int cost = BowlingConfig.Balls[index].Cost;
        if (!SpendCoins(cost)) return false;
        PlayerPrefs.SetInt(P + "OwnBall_" + index, 1);
        EquippedBall = index;
        PlayerPrefs.Save();
        // First unlockable ball owned = the New Gear achievement.
        bool anyUnlockable = false;
        for (int i = 0; i < BowlingConfig.Balls.Length; i++)
        {
            if (BowlingConfig.Balls[i].Cost > 0 && OwnsBall(i)) { anyUnlockable = true; break; }
        }
        if (anyUnlockable) UnlockAchievement("first_ball");
        return true;
    }

    public static bool OwnsSkin(int index)
    {
        if (index < 0 || index >= BowlingConfig.Skins.Length) return false;
        return PlayerPrefs.GetInt(P + "OwnSkin_" + index, 0) == 1;
    }

    // -1 = no skin (the ball's own colour).
    public static int EquippedSkin
    {
        get { return PlayerPrefs.GetInt(P + "EquippedSkin", -1); }
        set
        {
            PlayerPrefs.SetInt(P + "EquippedSkin", value);
            PlayerPrefs.Save();
        }
    }

    public static bool BuySkin(int index)
    {
        if (index < 0 || index >= BowlingConfig.Skins.Length) return false;
        if (OwnsSkin(index))
        {
            EquippedSkin = index;
            return true;
        }
        if (!SpendCoins(BowlingConfig.Skins[index].Cost)) return false;
        PlayerPrefs.SetInt(P + "OwnSkin_" + index, 1);
        EquippedSkin = index;
        PlayerPrefs.Save();
        return true;
    }

    public static bool OwnsTheme(int index)
    {
        if (index < 0 || index >= BowlingConfig.Themes.Length) return false;
        return PlayerPrefs.GetInt(P + "OwnTheme_" + index, 0) == 1;
    }

    // -1 = classic gold accent.
    public static int EquippedTheme
    {
        get { return PlayerPrefs.GetInt(P + "EquippedTheme", -1); }
        set
        {
            PlayerPrefs.SetInt(P + "EquippedTheme", value);
            PlayerPrefs.Save();
        }
    }

    public static bool BuyTheme(int index)
    {
        if (index < 0 || index >= BowlingConfig.Themes.Length) return false;
        if (OwnsTheme(index))
        {
            EquippedTheme = index;
            return true;
        }
        if (!SpendCoins(BowlingConfig.Themes[index].Cost)) return false;
        PlayerPrefs.SetInt(P + "OwnTheme_" + index, 1);
        EquippedTheme = index;
        PlayerPrefs.Save();
        return true;
    }

    public static Color AccentColor()
    {
        int t = EquippedTheme;
        if (t >= 0 && t < BowlingConfig.Themes.Length) return BowlingConfig.Themes[t].Accent;
        return BowlingConfig.Gold;
    }

    // --- Daily reward ---

    public const int DailyRewardCoins = 25;

    private static string TodayString()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    // Grants today's reward once per day. Returns the coins granted, 0
    // when today was already claimed.
    public static int ClaimDaily()
    {
        string today = TodayString();
        if (PlayerPrefs.GetString(P + "LastDaily", "") == today) return 0;
        PlayerPrefs.SetString(P + "LastDaily", today);
        PlayerPrefs.Save();
        AddCoins(DailyRewardCoins);
        return DailyRewardCoins;
    }

    // --- Missions ---

    public enum MissionKind { Pins, Spare, Strike, Games, QuickWin }

    public struct MissionDef
    {
        public string Desc;
        public int Target;
        public int Reward;
        public MissionKind Kind;
        public MissionDef(string desc, int target, int reward, MissionKind kind)
        {
            Desc = desc;
            Target = target;
            Reward = reward;
            Kind = kind;
        }
    }

    private static readonly MissionDef[] MissionPool = new MissionDef[]
    {
        new MissionDef("Knock down 20 pins", 20, 20, MissionKind.Pins),
        new MissionDef("Get a spare",          1, 25, MissionKind.Spare),
        new MissionDef("Get a strike",         1, 25, MissionKind.Strike),
        new MissionDef("Knock down 50 pins", 50, 40, MissionKind.Pins),
        new MissionDef("Finish 2 games",       2, 30, MissionKind.Games),
        new MissionDef("Win a Quick Match",    1, 30, MissionKind.QuickWin),
    };

    // Three missions a day, rotating by day of year (steps of 2 through
    // the 6-item pool, so the three are always distinct).
    public static MissionDef[] MissionsToday()
    {
        int start = DateTime.Now.DayOfYear % MissionPool.Length;
        return new MissionDef[]
        {
            MissionPool[start],
            MissionPool[(start + 2) % MissionPool.Length],
            MissionPool[(start + 4) % MissionPool.Length],
        };
    }

    // Per-day progress counters (reset automatically on a new day).
    private static void EnsureMissionDay()
    {
        string today = TodayString();
        if (PlayerPrefs.GetString(P + "MDate", "") != today)
        {
            PlayerPrefs.SetString(P + "MDate", today);
            PlayerPrefs.SetInt(P + "MP_Pins", 0);
            PlayerPrefs.SetInt(P + "MP_Spare", 0);
            PlayerPrefs.SetInt(P + "MP_Strike", 0);
            PlayerPrefs.SetInt(P + "MP_Games", 0);
            PlayerPrefs.SetInt(P + "MP_QuickWin", 0);
            PlayerPrefs.SetInt(P + "MD_0", 0);
            PlayerPrefs.SetInt(P + "MD_1", 0);
            PlayerPrefs.SetInt(P + "MD_2", 0);
            PlayerPrefs.Save();
        }
    }

    private static int ProgressFor(MissionKind kind)
    {
        EnsureMissionDay();
        switch (kind)
        {
            case MissionKind.Pins: return PlayerPrefs.GetInt(P + "MP_Pins", 0);
            case MissionKind.Spare: return PlayerPrefs.GetInt(P + "MP_Spare", 0);
            case MissionKind.Strike: return PlayerPrefs.GetInt(P + "MP_Strike", 0);
            case MissionKind.Games: return PlayerPrefs.GetInt(P + "MP_Games", 0);
            default: return PlayerPrefs.GetInt(P + "MP_QuickWin", 0);
        }
    }

    private static void BumpProgress(MissionKind kind, int amount)
    {
        EnsureMissionDay();
        string key;
        switch (kind)
        {
            case MissionKind.Pins: key = P + "MP_Pins"; break;
            case MissionKind.Spare: key = P + "MP_Spare"; break;
            case MissionKind.Strike: key = P + "MP_Strike"; break;
            case MissionKind.Games: key = P + "MP_Games"; break;
            default: key = P + "MP_QuickWin"; break;
        }
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + amount);
        PlayerPrefs.Save();
    }

    public static int MissionProgress(int slot)
    {
        MissionDef[] defs = MissionsToday();
        if (slot < 0 || slot >= defs.Length) return 0;
        return Mathf.Min(ProgressFor(defs[slot].Kind), defs[slot].Target);
    }

    public static bool MissionDone(int slot)
    {
        EnsureMissionDay();
        return PlayerPrefs.GetInt(P + "MD_" + slot, 0) == 1;
    }

    // Checks every slot of one kind after progress moved. Returns one
    // completion message per mission finished ("Mission complete: ...").
    private static List<string> CheckMissions(MissionKind kind)
    {
        List<string> done = new List<string>();
        MissionDef[] defs = MissionsToday();
        for (int slot = 0; slot < defs.Length; slot++)
        {
            if (defs[slot].Kind != kind) continue;
            if (MissionDone(slot)) continue;
            if (ProgressFor(kind) < defs[slot].Target) continue;
            PlayerPrefs.SetInt(P + "MD_" + slot, 1);
            PlayerPrefs.Save();
            AddCoins(defs[slot].Reward);
            done.Add("Mission complete: " + defs[slot].Desc + ". Plus " + defs[slot].Reward + " coins.");
        }
        return done;
    }

    public static List<string> NotePins(int fallen)
    {
        if (fallen <= 0) return new List<string>();
        BumpProgress(MissionKind.Pins, fallen);
        return CheckMissions(MissionKind.Pins);
    }

    public static List<string> NoteSpare()
    {
        BumpProgress(MissionKind.Spare, 1);
        return CheckMissions(MissionKind.Spare);
    }

    public static List<string> NoteStrike()
    {
        BumpProgress(MissionKind.Strike, 1);
        return CheckMissions(MissionKind.Strike);
    }

    public static List<string> NoteQuickWin()
    {
        BumpProgress(MissionKind.QuickWin, 1);
        return CheckMissions(MissionKind.QuickWin);
    }

    public static List<string> NoteGamePlayed()
    {
        PlayerPrefs.SetInt(P + "GamesPlayed", GamesPlayed + 1);
        PlayerPrefs.Save();
        BumpProgress(MissionKind.Games, 1);
        return CheckMissions(MissionKind.Games);
    }
}
