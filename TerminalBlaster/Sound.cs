namespace TerminalBlaster;

// Arcade-style beep feedback. Console.Beep(frequency, duration) is Windows-only and
// blocks the calling thread for the duration, so every call here runs on a background
// task: it keeps the game loop from stalling and lets non-Windows platforms fail safely
// (falling back to the fixed system bell instead of a tunable tone).
//
// On Windows, Console.Beep calls the native Win32 Beep() API directly — it never touches
// the console text stream. On macOS/Linux there's no tunable tone API, so the fallback is
// the terminal BEL character, which is written to the SAME stream the render loop writes
// to, from a different thread. Sent this often, many terminals flash their "visual bell" on
// every BEL, which reads as screen flicker. So the two high-frequency sounds (shoot, enemy
// hit — up to ~8/sec while firing) are Windows-only; the rare ones still beep everywhere,
// since one occasional ping doesn't cause repeated flashing.
static class Sound
{
    public static void PlayShoot()
    {
        if (OperatingSystem.IsWindows())
            BeepAsync(1200, 30);
    }

    public static void PlayEnemyHit()
    {
        if (OperatingSystem.IsWindows())
            BeepAsync(600, 25);
    }

    public static void PlayEnemyKilled() => BeepAsync(900, 80);

    public static void PlayPlayerHit() => BeepAsync(220, 200);

    public static void PlayWaveClear() => BeepSequenceAsync(new[] { 523, 659, 784 }, 90);

    public static void PlayGameOver() => BeepSequenceAsync(new[] { 400, 300, 200, 100 }, 150);

    private static void BeepAsync(int frequency, int durationMs)
    {
        Task.Run(() => Beep(frequency, durationMs));
    }

    private static void BeepSequenceAsync(int[] frequencies, int durationMs)
    {
        Task.Run(() =>
        {
            foreach (var frequency in frequencies)
            {
                if (!Beep(frequency, durationMs))
                    return;
            }
        });
    }

    private static bool Beep(int frequency, int durationMs)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Console.Beep(frequency, durationMs);
            else
                Console.Beep(); // fixed system bell; non-Windows has no tunable tone API here

            return true;
        }
        catch
        {
            // Some terminals/environments support neither — sound is best-effort.
            return false;
        }
    }
}
