// Lynxos Bowling — standard 10-pin scoring.
// Pure logic, no Unity dependencies: frames 1-9 with strike/spare bonuses,
// 10th frame with up to 3 balls. All frame math is 1-based (frames 1..10).
using System.Collections.Generic;

public class BowlingScore
{
    private List<int> rolls = new List<int>();

    // Frames in the match: 10 for a full game, 3 for a Quick Match. The
    // last frame always plays by 10th-frame rules (up to 3 balls).
    public int TotalFrames = 10;

    public IReadOnlyList<int> Rolls { get { return rolls; } }
    public int RollCount { get { return rolls.Count; } }

    public void AddRoll(int pins)
    {
        rolls.Add(pins);
    }

    public void Reset()
    {
        rolls.Clear();
    }

    // Start roll index of a frame, or -1 if the frame has not started yet.
    public int FrameStartIndex(int frame)
    {
        int idx = 0;
        for (int f = 1; f < frame; f++)
        {
            if (idx >= rolls.Count) return -1;
            if (f < TotalFrames)
            {
                if (rolls[idx] == 10) idx += 1;
                else idx += 2;
            }
        }
        return idx;
    }

    public int CurrentFrame()
    {
        int idx = 0;
        for (int f = 1; f < TotalFrames; f++)
        {
            if (idx >= rolls.Count) return f;
            if (rolls[idx] == 10)
            {
                idx += 1;
            }
            else
            {
                if (idx + 1 >= rolls.Count) return f;
                idx += 2;
            }
        }
        return TotalFrames;
    }

    // Balls already thrown in the current frame (0, 1 or 2).
    public int BallsInCurrentFrame()
    {
        int start = FrameStartIndex(CurrentFrame());
        if (start < 0) return 0;
        return rolls.Count - start;
    }

    // Pins knocked down by a ball inside a frame (0-based ball index), -1 if not thrown.
    public int RollInFrame(int frame, int ballIndex)
    {
        int start = FrameStartIndex(frame);
        if (start < 0) return -1;
        int idx = start + ballIndex;
        return idx < rolls.Count ? rolls[idx] : -1;
    }

    public bool IsComplete()
    {
        if (CurrentFrame() < TotalFrames) return false;
        int start = FrameStartIndex(TotalFrames);
        if (start < 0) return false;
        int n = rolls.Count - start;
        if (n < 2) return false;
        if (n >= 3) return true;
        int b1 = rolls[start];
        int b2 = rolls[start + 1];
        if (b1 == 10) return false;       // strike: one more ball
        if (b1 + b2 == 10) return false;  // spare: one more ball
        return true;                      // open frame: done after 2 balls
    }

    // Cumulative score through a frame, or -1 if the frame is not scored yet
    // (strike/spare bonus balls still missing).
    public int FrameCumulativeScore(int frame)
    {
        int total = 0;
        int idx = 0;
        for (int f = 1; f <= frame; f++)
        {
            if (f < TotalFrames)
            {
                if (idx >= rolls.Count) return -1;
                if (rolls[idx] == 10)
                {
                    if (idx + 2 >= rolls.Count) return -1;
                    total += 10 + rolls[idx + 1] + rolls[idx + 2];
                    idx += 1;
                }
                else
                {
                    if (idx + 1 >= rolls.Count) return -1;
                    int framePins = rolls[idx] + rolls[idx + 1];
                    if (framePins == 10)
                    {
                        if (idx + 2 >= rolls.Count) return -1;
                        total += 10 + rolls[idx + 2];
                    }
                    else
                    {
                        total += framePins;
                    }
                    idx += 2;
                }
            }
            else
            {
                // 10th frame: sum of its own balls.
                int remaining = rolls.Count - idx;
                if (remaining == 0) return -1;
                int b1 = rolls[idx];
                int need = 2;
                if (b1 == 10) need = 3;
                else if (remaining >= 2 && b1 + rolls[idx + 1] == 10) need = 3;
                if (remaining < need) return -1;
                for (int i = 0; i < need; i++) total += rolls[idx + i];
            }
        }
        return total;
    }

    // Total score so far, counting strike/spare bonuses only where the bonus
    // balls have actually been thrown. Before the game ends this is a live
    // running total, not a final score.
    public int Total()
    {
        int total = 0;
        int idx = 0;
        for (int f = 1; f <= TotalFrames; f++)
        {
            if (f < TotalFrames)
            {
                if (idx >= rolls.Count) break;
                if (rolls[idx] == 10)
                {
                    total += 10;
                    if (idx + 1 < rolls.Count) total += rolls[idx + 1];
                    if (idx + 2 < rolls.Count) total += rolls[idx + 2];
                    idx += 1;
                }
                else if (idx + 1 < rolls.Count)
                {
                    int framePins = rolls[idx] + rolls[idx + 1];
                    total += framePins;
                    if (framePins == 10 && idx + 2 < rolls.Count) total += rolls[idx + 2];
                    idx += 2;
                }
                else
                {
                    total += rolls[idx];
                    idx += 1;
                }
            }
            else
            {
                while (idx < rolls.Count)
                {
                    total += rolls[idx];
                    idx++;
                }
            }
        }
        return total;
    }

    // Display marks for a frame, e.g. "X", "7 /", "9 -", "X X X" (10th).
    public string FrameRolls(int frame)
    {
        int start = FrameStartIndex(frame);
        if (start < 0 || start >= rolls.Count) return "";
        int n = rolls.Count - start;
        if (frame < TotalFrames)
        {
            int b1 = rolls[start];
            if (b1 == 10) return "X";
            string s = Mark(b1);
            if (n >= 2)
            {
                int b2 = rolls[start + 1];
                s += (b1 + b2 == 10) ? " /" : " " + Mark(b2);
            }
            return s;
        }
        // 10th frame: up to 3 marks.
        string t = "";
        for (int i = 0; i < n && i < 3; i++)
        {
            int pins = rolls[start + i];
            string m;
            if (i == 0)
            {
                m = (pins == 10) ? "X" : Mark(pins);
            }
            else if (i == 1)
            {
                int b1 = rolls[start];
                if (b1 == 10) m = (pins == 10) ? "X" : Mark(pins);
                else m = (b1 + pins == 10) ? "/" : Mark(pins);
            }
            else
            {
                int b1 = rolls[start];
                int b2 = rolls[start + 1];
                if (pins == 10) m = "X";
                // Ball 3 is a spare mark only when it clears what ball 2
                // left on the same rack (ball 1 was a strike, ball 2 was
                // not). After a spare (balls 1+2) ball 3 is a fresh rack,
                // so its pins are just a number.
                else if (b1 == 10 && b2 < 10 && b2 + pins == 10) m = "/";
                else m = Mark(pins);
            }
            if (i > 0) t += " ";
            t += m;
        }
        return t;
    }

    private string Mark(int pins)
    {
        return pins == 0 ? "-" : pins.ToString();
    }
}
