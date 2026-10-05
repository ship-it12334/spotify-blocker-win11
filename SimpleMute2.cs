using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

class Win
{
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
}

class SimpleMute2
{
    const uint WM_APPCOMMAND = 0x0319;
    const int APPCOMMAND_VOLUME_MUTE = 0x80000;

    static HashSet<string> Blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static string LastTitle = null;
    static bool Muted = false;

    static readonly string[] AdHints = new[]
    {
        "без реклами", "реклама", "слушайте", "listening without ads",
        "advertisement", "ad break", "spotify ad", "sponsor", "preroll",
        "advert", "intro", "interstitial", "spotify free", "watch ad", "video ad",
        "free on spotify", "spotify - advertisement", "advertisement on spotify",
        "al je muziek", "zonder reclame"
    };

    static void Main()
    {
        Blocked = BlockConfig.Load("blocklist.txt");
        Console.WriteLine("SimpleMute2");
        while (true)
        {
            Tick();
            Thread.Sleep(50);
        }
    }

    static string Norm(string t) => t?.Trim().ToLowerInvariant() ?? "";

    static bool ShouldMute(string t)
    {
        var s = Norm(t);
        if (Blocked.Contains(s)) return true;
        foreach (var h in AdHints) if (s.Contains(h)) return true;
        if (!s.Contains(" - ") && s.Length > 0) return true;
        return false;
    }

    static void MuteSpotify()
    {
        Win.EnumWindows((h, p) => {
            Win.GetWindowThreadProcessId(h, out var pid);
            try
            {
                var pr = Process.GetProcessById((int)pid);
                if (pr.ProcessName.ToLowerInvariant() == "spotify")
                {
                    // send mute to this window
                    Win.SendMessage(h, WM_APPCOMMAND, h, (IntPtr)APPCOMMAND_VOLUME_MUTE);
                }
                pr.Dispose();
            } catch {}
            return true;
        }, IntPtr.Zero);
    }

    static void Tick()
    {
        Blocked = BlockConfig.Load("blocklist.txt");
        var title = CurrentTitle();
        if (title != LastTitle)
        {
            LastTitle = title;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] '" + title + "'");
        }
        bool should = ShouldMute(title);
        if (should && !Muted)
        {
            MuteSpotify();
            Muted = true;
        }
        else if (!should && Muted)
        {
            MuteSpotify();
            Muted = false;
        }
    }

    static string CurrentTitle()
    {
        string best = null;
        Win.EnumWindows((h, l) => {
            Win.GetWindowThreadProcessId(h, out var pid);
            try
            {
                var pr = Process.GetProcessById((int)pid);
                if (pr.ProcessName.ToLowerInvariant() != "spotify") { pr.Dispose(); return true; }
                pr.Dispose();
            } catch { return true; }
            if (!Win.IsWindowVisible(h)) return true;
            var sb = new StringBuilder(512);
            Win.GetWindowText(h, sb, sb.Capacity);
            var t = sb.ToString();
            if (string.IsNullOrEmpty(t)) return true;
            var tl = t.ToLowerInvariant();
            if (tl.Contains("gdi+") || tl.Contains("ime")) return true;
            if (best == null || t.Length > best.Length) best = t;
            return true;
        }, IntPtr.Zero);
        return best;
    }
}
