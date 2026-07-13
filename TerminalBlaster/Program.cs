// =========================
//  Console Game: Alien Invasion
//  A simple console-based shooting game
//  developed in C#
//  author - blackarck
// date   -  nov 2025
// why in terminal ? Why not? 
// =========================

using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TerminalBlaster;

public class consoleGame
{
    // =========================
    // Core engine / configuration
    // =========================

    private static readonly Random _random = new Random();
    private static readonly StringBuilder _buffer = new StringBuilder();
    private static string _lastFrame = string.Empty;
    private static int _width;
    private static int _height;
    private static bool _isGameOver = false;

    // =========================
    // Gameplay state
    // =========================

    // Score
    private static int count = 0;

    // Player
    private static int _gunPosition = 0;
    private static int _lifeCount = 3;
    private static readonly int _gunWidth = 7; // Width of gun ASCII art
    private static List<(int x, int y)> _bullets = new List<(int x, int y)>();

    // Fixed-rate fire control: decoupled from the OS/terminal's own key-repeat rate
    private static int _tickCounter = 0;
    private static int _lastSpaceSeenTick = -1_000_000; // far enough in the past; avoids int overflow in the subtraction below
    private static int _fireCooldownTimer = 0;
    private const int _fireCooldownTicks = 8;      // min ticks between shots
    private const int _spaceHeldWindowTicks = 40;  // grace window to still count space as "held"

    // Brief invulnerability after taking a hit, instead of freezing the game with Thread.Sleep
    private static int _invulnerabilityTimer = 0;
    private const int _invulnerabilityTicks = 45;  // ~0.75s at 60fps

    // Environment / pillars
    private static int _pillarCount = 4;
    private static List<Pillar> _pillars = new List<Pillar>();

    // Enemies
    private static List<Enemy> _enemies = new List<Enemy>();
    private static List<(int x, int y)> _enemyBullets = new List<(int x, int y)>();
    private static bool _movingRight = true;
    private static int _enemyMoveCounter = 0;
    private static int _enemyShootCounter = 0;
    private static int _enemyBulletMoveCounter = 0;
    // NEW: how often enemy bullets move (ticks). Smaller = faster.
    private static int _enemyBulletMoveDelay = 3;

    // Optional: track wave just for your own fun / debugging
    private static int _waveNumber = 1;

    // Parallel to _buffer: per-cell foreground color for the ANSI-colored render pass.
    // Null means "no color override" (terminal default).
    private static ConsoleColor?[] _colorBuffer = Array.Empty<ConsoleColor?>();
    private static bool _colorEnabled = false;

    // =========================
    // Program entry point
    // =========================

    public static void Main()
    {
        Console.CursorVisible = false;
        _width = Console.WindowWidth;
        _height = Console.WindowHeight;

        // Console.SetBufferSize is Windows-only; calling it on macOS/Linux throws PlatformNotSupportedException
        if (OperatingSystem.IsWindows())
        {
            Console.SetBufferSize(_width, _height); // Prevent scrollbars
        }

        _colorBuffer = new ConsoleColor?[_width * _height];
        _colorEnabled = TryEnableAnsiColor();

        // Initial game setup
        InitializePillars();
        _gunPosition = (_width - _gunWidth) / 2; // Start centered
        ShowTitleScreen();
        InitializeEnemies();

        // Main game loop (drives game logic)
        while (!_isGameOver)
        {
            GameLogic();
            RenderLoop();
            Thread.Sleep(16); // ~60 fps
        }

    }

    // =========================
    // Color support (ANSI escape codes)
    // =========================

    // macOS/Linux terminals interpret ANSI escape codes by default. Classic Windows
    // conhost does not unless ENABLE_VIRTUAL_TERMINAL_PROCESSING is turned on for
    // stdout, so this is the Windows-only piece (guarded like the SetBufferSize fix).
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    private static bool TryEnableAnsiColor()
    {
        if (!OperatingSystem.IsWindows())
            return true;

        try
        {
            IntPtr handle = GetStdHandle(StdOutputHandle);
            if (handle == IntPtr.Zero || !GetConsoleMode(handle, out uint mode))
                return false;

            return SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
        }
        catch
        {
            return false;
        }
    }

    private static string AnsiForeground(ConsoleColor color) => color switch
    {
        ConsoleColor.Black => "\x1b[30m",
        ConsoleColor.DarkRed => "\x1b[31m",
        ConsoleColor.DarkGreen => "\x1b[32m",
        ConsoleColor.DarkYellow => "\x1b[33m",
        ConsoleColor.DarkBlue => "\x1b[34m",
        ConsoleColor.DarkMagenta => "\x1b[35m",
        ConsoleColor.DarkCyan => "\x1b[36m",
        ConsoleColor.Gray => "\x1b[37m",
        ConsoleColor.DarkGray => "\x1b[90m",
        ConsoleColor.Red => "\x1b[91m",
        ConsoleColor.Green => "\x1b[92m",
        ConsoleColor.Yellow => "\x1b[93m",
        ConsoleColor.Blue => "\x1b[94m",
        ConsoleColor.Magenta => "\x1b[95m",
        ConsoleColor.Cyan => "\x1b[96m",
        ConsoleColor.White => "\x1b[97m",
        _ => AnsiReset
    };

    private const string AnsiReset = "\x1b[0m";

    private static void PaintColor(int position, int length, ConsoleColor color)
    {
        for (int i = 0; i < length; i++)
        {
            int p = position + i;
            if (p >= 0 && p < _colorBuffer.Length)
                _colorBuffer[p] = color;
        }
    }

    // =========================
    // Core loop callbacks
    // =========================


    public static void GameLogic()
    {
        // Check player lives
        if (_lifeCount <= 0)
        {
            ShowGameOver();
            Sound.PlayGameOver();
            _isGameOver = true;
            return;
        }

        // Handle input (drain all buffered key events so nothing lags across frames)
        _tickCounter++;
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (_gunPosition > 0)
                        _gunPosition--;
                    break;

                case ConsoleKey.RightArrow:
                    if (_gunPosition < _width - _gunWidth)
                        _gunPosition++;
                    break;

                case ConsoleKey.Spacebar:
                    _lastSpaceSeenTick = _tickCounter;
                    break;
            }
        }

        // Fire at a fixed rate while space is "held" (recently seen), independent of
        // the terminal's own key-repeat rate/settings.
        bool spaceHeld = (_tickCounter - _lastSpaceSeenTick) <= _spaceHeldWindowTicks;
        if (_fireCooldownTimer > 0)
            _fireCooldownTimer--;

        if (spaceHeld && _fireCooldownTimer == 0)
        {
            _bullets.Add((_gunPosition + (_gunWidth / 2), _height - 4));
            _fireCooldownTimer = _fireCooldownTicks;
            Sound.PlayShoot();
        }

        // Update player bullets
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var bullet = _bullets[i];
            bullet.y--;
            if (bullet.y < 0 || TryHitPillar(bullet.x, bullet.y))
                _bullets.RemoveAt(i);
            else
                _bullets[i] = bullet;
        }

        // Check bullet collisions with enemies
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var bullet = _bullets[i];

            for (int j = _enemies.Count - 1; j >= 0; j--)
            {
                var enemy = _enemies[j];

                if (bullet.y >= enemy.Y && bullet.y <= enemy.Y + 4 &&  // 5 rows tall
                    bullet.x >= enemy.X && bullet.x <= enemy.X + 6)    // 7 columns wide
                {
                    _bullets.RemoveAt(i);

                    enemy.Hits++;

                    if (enemy.Hits >= 10)
                    {
                        _enemies.RemoveAt(j);
                        count += 100;
                        Sound.PlayEnemyKilled();
                    }
                    else
                    {
                        Sound.PlayEnemyHit();
                    }

                    goto nextBullet;
                }
            }
        }

    nextBullet:;

        // Check enemy bullet collisions with player
        if (_invulnerabilityTimer > 0)
        {
            _invulnerabilityTimer--;
        }
        else
        {
            for (int i = _enemyBullets.Count - 1; i >= 0; i--)
            {
                var bullet = _enemyBullets[i];

                // Player hitbox near bottom
                if (bullet.y >= _height - 4 && bullet.y <= _height - 1 &&
                    bullet.x >= _gunPosition && bullet.x <= _gunPosition + _gunWidth)
                {
                    _enemyBullets.RemoveAt(i);
                    _lifeCount--;
                    Sound.PlayPlayerHit();

                    if (_lifeCount > 0)
                    {
                        // Reset player position and grant a brief on-screen invulnerability
                        // window instead of freezing the whole game with Thread.Sleep
                        _gunPosition = (_width - _gunWidth) / 2;
                        _enemyBullets.Clear();
                        _invulnerabilityTimer = _invulnerabilityTicks;
                    }
                    break;
                }
            }
        }

        // Move enemies horizontally and handle direction change
        _enemyMoveCounter++;
        if (_enemyMoveCounter >= 30)
        {
            _enemyMoveCounter = 0;
            int moveAmount = _movingRight ? 1 : -1;  // <---- here

            bool shouldChangeDirection = false;
            foreach (var enemy in _enemies)
            {
                if ((_movingRight && enemy.X >= _width - 15) ||
                    (!_movingRight && enemy.X <= 10))
                {
                    shouldChangeDirection = true;
                    break;
                }
            }

            if (shouldChangeDirection)
            {
                _movingRight = !_movingRight;
            }
            else
            {
                // Apply the move amount
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var enemy = _enemies[i];
                    enemy.X += moveAmount;   // <---- moves each enemy left/right by 1 cell
                }
            }
        }

        // Enemy shooting
        _enemyShootCounter++;
        if (_enemyShootCounter >= 30 && _enemies.Count > 0) // shooting frequency
        {
            _enemyShootCounter = 0;

            // Pick a random enemy to shoot
            var shootingEnemy = _enemies[_random.Next(_enemies.Count)];

            // Enemy bullet starts roughly under the center of the enemy sprite
            _enemyBullets.Add((shootingEnemy.X + 3, shootingEnemy.Y + 5));
        }

        // Update enemy bullets (falling down)
        _enemyBulletMoveCounter++;

        if (_enemyBulletMoveCounter >= _enemyBulletMoveDelay) // move once every 3 logic ticks (slower bullets)
        {
            _enemyBulletMoveCounter = 0;

            for (int i = _enemyBullets.Count - 1; i >= 0; i--)
            {
                var bullet = _enemyBullets[i];
                bullet.y++;

                if (bullet.y >= _height || TryHitPillar(bullet.x, bullet.y))
                    _enemyBullets.RemoveAt(i);
                else
                    _enemyBullets[i] = bullet;
            }
        }

        // If all enemies are defeated, start next wave
        if (_enemies.Count == 0)
        {
            _waveNumber++;
            Sound.PlayWaveClear();

            // Make bullets faster by reducing delay, with a floor of 1 tick
            if (_enemyBulletMoveDelay > 1)
                _enemyBulletMoveDelay--;

            // Optional: clear bullets so new wave starts "clean"
            _enemyBullets.Clear();
            _bullets.Clear();

            // Regenerate enemies and give fresh (undamaged) pillars for the new wave
            InitializeEnemies();
            InitializePillars();
        }

        //end of game logic
    }
    // =========================
    // Title Screen
    // =========================

    private static void ShowTitleScreen()
    {
        StringBuilder titleBuffer = new StringBuilder();
        int titlePosition = _width;

        // Slide-in animation from right to center
        while (titlePosition > (_width - Assets.TitleArt[0].Length) / 2)
        {
            titleBuffer.Clear();
            titleBuffer.Append(new string(' ', _width * _height));

            // Draw title art at current position
            for (int i = 0; i < Assets.TitleArt.Length; i++)
            {
                int row = (_height - Assets.TitleArt.Length) / 2 + i;
                int position = (row * _width) + titlePosition;

                if (position >= 0 && position + Assets.TitleArt[i].Length <= titleBuffer.Length)
                {
                    titleBuffer.Remove(position, Assets.TitleArt[i].Length);
                    titleBuffer.Insert(position, Assets.TitleArt[i]);
                }
            }

            Console.SetCursorPosition(0, 0);
            Console.Write(titleBuffer.ToString());

            titlePosition -= 2;
            Thread.Sleep(50);
        }

        // "Press any key" message under title
        string message = "[ Press any key to start ]";
        int messagePosition =
            ((_height + Assets.TitleArt.Length) / 2 + 2) * _width + (_width - message.Length) / 2;

        if (messagePosition >= 0 && messagePosition + message.Length <= titleBuffer.Length)
        {
            titleBuffer.Remove(messagePosition, message.Length);
            titleBuffer.Insert(messagePosition, message);
        }

        // Final render
        Console.SetCursorPosition(0, 0);
        Console.Write(titleBuffer.ToString());

        Console.ReadKey(true);
        Console.Clear();
    }

    private static void ShowGameOver()
    {
        Console.Clear();
        Console.SetCursorPosition(_width / 2 - 5, _height / 2);
        Console.Write("GAME OVER");
        //show score
        Console.SetCursorPosition(_width / 2 - 7, _height / 2 + 1);
        Console.Write($"Final Score: {count}");
    }

    // =========================
    // Enemy / level helpers
    // =========================

    private static void InitializeEnemies()
    {
        int startX = 15;
        int startY = 3;

        _enemies.Clear();

        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int x = startX + (col * 12);
                int y = startY + (row * 7);
                int type = _random.Next(Assets.EnemyVariations.Length);

                _enemies.Add(new Enemy
                {
                    X = x,
                    Y = y,
                    Type = type,
                    Hits = 0
                });
            }
        }
    }


    private static void InitializePillars()
    {
        _pillars.Clear();

        string[] design = Assets.PillarVariations[_random.Next(Assets.PillarVariations.Length)];
        int sectionWidth = _width / _pillarCount;

        for (int i = 0; i < _pillarCount; i++)
        {
            int pillarX = (i * sectionWidth) + (sectionWidth - design[0].Length) / 2;

            var rows = new char[design.Length][];
            for (int j = 0; j < design.Length; j++)
                rows[j] = design[j].ToCharArray();

            _pillars.Add(new Pillar { X = pillarX, Rows = rows });
        }
    }

    // Erodes a single pillar cell at (x, y) if one exists there, so bullets carve away
    // cover over time (classic destructible-barrier behavior) instead of passing through it.
    private static bool TryHitPillar(int x, int y)
    {
        foreach (var pillar in _pillars)
        {
            for (int j = 0; j < pillar.Rows.Length; j++)
            {
                int row = _height - 8 - j;
                if (row != y)
                    continue;

                int col = x - pillar.X;
                if (col >= 0 && col < pillar.Rows[j].Length && pillar.Rows[j][col] != ' ')
                {
                    pillar.Rows[j][col] = ' ';
                    return true;
                }
            }
        }

        return false;
    }

    // =========================
    // Rendering
    // =========================

    public static void RenderLoop()
    {
        BuildFrame();

        // Only update the console if the frame has changed
        string currentFrame = ComposeFrame();
        if (currentFrame != _lastFrame)
        {
            Console.SetCursorPosition(0, 0);
            Console.Write(currentFrame);
            _lastFrame = currentFrame;
        }
    }

    private static void BuildFrame()
    {
        // Clear the buffer
        _buffer.Clear();
        _buffer.Append(new string(' ', _width * _height));
        Array.Clear(_colorBuffer);

        // Draw pillars above the gun (per-cell, so eroded/destroyed cells stay empty)
        foreach (var pillar in _pillars)
        {
            for (int j = 0; j < pillar.Rows.Length; j++)
            {
                int row = _height - 8 - j; // Position pillars above the gun
                int rowStart = (row * _width) + pillar.X;
                var rowChars = pillar.Rows[j];

                for (int c = 0; c < rowChars.Length; c++)
                {
                    int position = rowStart + c;
                    if (rowChars[c] != ' ' && position >= 0 && position < _buffer.Length)
                    {
                        _buffer[position] = rowChars[c];
                        _colorBuffer[position] = ConsoleColor.DarkGreen;
                    }
                }
            }
        }

        // Draw player bullets
        foreach (var bullet in _bullets)
        {
            int position = (bullet.y * _width) + bullet.x;
            if (position >= 0 && position < _buffer.Length)
            {
                _buffer[position] = '|';
                _colorBuffer[position] = ConsoleColor.Yellow;
            }
        }

        // Draw enemies
        foreach (var enemy in _enemies)
        {
            string[] enemyDesign = Assets.EnemyVariations[enemy.Type];
            ConsoleColor enemyColor = Assets.EnemyColors[enemy.Type];
            for (int i = 0; i < enemyDesign.Length; i++)
            {
                int position = ((enemy.Y + i) * _width) + enemy.X;
                if (position >= 0 && position + enemyDesign[i].Length <= _buffer.Length)
                {
                    _buffer.Remove(position, enemyDesign[i].Length);
                    _buffer.Insert(position, enemyDesign[i]);
                    PaintColor(position, enemyDesign[i].Length, enemyColor);
                }
            }
        }

        // Draw enemy bullets
        foreach (var bullet in _enemyBullets)
        {
            int position = (bullet.y * _width) + bullet.x;
            if (position >= 0 && position < _buffer.Length)
            {
                _buffer[position] = '█';
                _colorBuffer[position] = ConsoleColor.Red;
            }
        }

        // Draw the gun at the bottom (blinks while briefly invulnerable after a hit)
        bool showGun = _invulnerabilityTimer == 0 || (_invulnerabilityTimer / 5) % 2 == 0;
        if (showGun)
        {
            for (int i = 0; i < Assets.GunArt.Length; i++)
            {
                int row = _height - (4 - i);
                int position = (row * _width) + _gunPosition;
                if (position >= 0 && position + Assets.GunArt[i].Length <= _buffer.Length)
                {
                    _buffer.Remove(position, Assets.GunArt[i].Length);
                    _buffer.Insert(position, Assets.GunArt[i]);
                    PaintColor(position, Assets.GunArt[i].Length, ConsoleColor.Cyan);
                }
            }
        }

        // Draw score at the top-left
        string scoreText = $"Score: {count}    Wave: {_waveNumber}";
        _buffer.Remove(0, scoreText.Length);
        _buffer.Insert(0, scoreText);
        PaintColor(0, scoreText.Length, ConsoleColor.White);

        // Draw lives at the top-right
        for (int i = 0; i < _lifeCount; i++)
        {
            for (int j = 0; j < Assets.LifeArt.Length; j++)
            {
                int row = j;
                int position = (row * _width) + (_width - (_lifeCount - i) * 7);
                if (position + Assets.LifeArt[j].Length <= _buffer.Length)
                {
                    _buffer.Remove(position, Assets.LifeArt[j].Length);
                    _buffer.Insert(position, Assets.LifeArt[j]);
                    PaintColor(position, Assets.LifeArt[j].Length, ConsoleColor.Green);
                }
            }
        }
    }

    private static string ComposeFrame()
    {
        if (!_colorEnabled)
            return _buffer.ToString();

        var output = new StringBuilder(_buffer.Length + 64);
        ConsoleColor? currentColor = null;

        for (int i = 0; i < _buffer.Length; i++)
        {
            ConsoleColor? cellColor = _colorBuffer[i];
            if (cellColor != currentColor)
            {
                output.Append(cellColor.HasValue ? AnsiForeground(cellColor.Value) : AnsiReset);
                currentColor = cellColor;
            }

            output.Append(_buffer[i]);
        }

        output.Append(AnsiReset);
        return output.ToString();
    }
}
