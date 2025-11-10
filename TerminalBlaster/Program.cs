// =========================
//  Console Game: Alien Invasion
//  A simple console-based shooting game
//  developed in C#
//  author - blackarck
// date   -  nov 2025
// why in terminal ? Why not? 
// =========================

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;

class Enemy
{
    public int X;
    public int Y;
    public int Type;
    public int Hits;
}
public class consoleGame
{
    // =========================
    // Core engine / configuration
    // =========================

    // Render/update interval for the timer (~30 frames per second)
    private const double interval = 1000.0 / 30.0;

    private static System.Timers.Timer? _timer;
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
    private static bool _spacePressed = false;

    // Environment / pillars
    private static int _pillarCount = 4;
    private static string[] _pillarDesign;

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
    // =========================
    // Artwork
    // =========================

    private static readonly string[] _gunArt = new[]
    {
        "  ▄█▄  ",
        "█▀▀█▀▀█",
        "  │║│  "
    };

    private static readonly string[] _lifeArt = new[]
    {
        "  ▲  ",
        "◄███►",
        "  ▼  "
    };

    // Enemy variants
    private static readonly string[][] _enemyVariations = new[]
    {
        // Classic UFO enemy
        new[]
        {
            "  ▄▄▄  ",
            " █████ ",
            "███████",
            " █████ ",
            "  ▀▀▀  "
        },
        // Robot enemy
        new[]
        {
            " ╔═══╗ ",
            " ║◄►║ ",
            "███████",
            " ║[ ]║ ",
            " ╚███╝ "
        },
        // Alien enemy
        new[]
        {
            " ▀█▀█▀ ",
            "██▄█▄██",
            "███████",
            " ╔═══╗ ",
            " ║▀▀▀║ "
        },
        // Crystal enemy
        new[]
        {
            "  ♦♦♦  ",
            " ▄███▄ ",
            "███████",
            " ▀███▀ ",
            "  ▀▀▀  "
        },
        // Tech enemy
        new[]
        {
            " ┌───┐ ",
            " │╳╳╳│ ",
            "███████",
            " │▣▣▣│ ",
            " └───┘ "
        }
    };

    // Pillar variants
    private static readonly string[][] _pillarVariations = new[]
    {
        // Classic pillar
        new[]
        {
            "  ██  ",
            "  ██  ",
            "  ██  ",
            "  ██  ",
            "██████"
        },
        // Wide pillar
        new[]
        {
            " ████ ",
            " ████ ",
            " ████ ",
            " ████ ",
            "██████"
        },
        // Ornate pillar
        new[]
        {
            "  ▲▲  ",
            " ████ ",
            "  ██  ",
            " ████ ",
            "██████"
        },
        // Crystal pillar
        new[]
        {
            "  ♦   ",
            " ████ ",
            "  ██  ",
            " ████ ",
            "══════"
        },
        // Ancient pillar
        new[]
        {
            "  ╔╗  ",
            "  ║║  ",
            "  ║║  ",
            " ═║║═ ",
            "██████"
        }
    };

    // Title art
    private static string[] _titleArt = new[]
    {
        @" ██████╗ ██████╗ ███╗   ██╗███████╗ ██████╗ ██╗     ███████╗     ██████╗ ██╗   ██╗███╗   ██╗",
        @"██╔════╝██╔═══██╗████╗  ██║██╔════╝██╔═══██╗██║     ██╔════╝    ██╔════╝ ██║   ██║████╗  ██║",
        @"██║     ██║   ██║██╔██╗ ██║███████╗██║   ██║██║     █████╗      ██║  ███╗██║   ██║██╔██╗ ██║",
        @"██║     ██║   ██║██║╚██╗██║╚════██║██║   ██║██║     ██╔══╝      ██║   ██║██║   ██║██║╚██╗██║",
        @"╚██████╗╚██████╔╝██║ ╚████║███████║╚██████╔╝███████╗███████╗    ╚██████╔╝╚██████╔╝██║ ╚████║",
        @" ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝ ╚═════╝ ╚══════╝╚══════╝     ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝"
    };

    // =========================
    // Program entry point
    // =========================

    public static void Main()
    {
        Console.CursorVisible = false;
        _width = Console.WindowWidth;
        _height = Console.WindowHeight;
        Console.SetBufferSize(_width, _height); // Prevent scrollbars

        // Initial game setup
        _pillarDesign = GetRandomPillarDesign();
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
    // Core loop callbacks
    // =========================


    public static void GameLogic()
    {
        // Check player lives
        if (_lifeCount <= 0)
        {
            ShowGameOver();
            _timer?.Stop();
            _isGameOver = true;
            return;
        }

        // Handle input
        if (Console.KeyAvailable)
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
                    if (!_spacePressed)
                    {
                        _bullets.Add((_gunPosition + (_gunWidth / 2), _height - 4));
                        _spacePressed = true;
                    }
                    break;
            }
        }
        else
        {
            _spacePressed = false;
        }

        // Update player bullets
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var bullet = _bullets[i];
            bullet.y--;
            if (bullet.y < 0)
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
                    }

                    goto nextBullet;
                }
            }
        }

    nextBullet:;

        // Check enemy bullet collisions with player
        for (int i = _enemyBullets.Count - 1; i >= 0; i--)
        {
            var bullet = _enemyBullets[i];

            // Player hitbox near bottom
            if (bullet.y >= _height - 4 && bullet.y <= _height - 1 &&
                bullet.x >= _gunPosition && bullet.x <= _gunPosition + _gunWidth)
            {
                _enemyBullets.RemoveAt(i);
                _lifeCount--;

                if (_lifeCount > 0)
                {
                    // Reset player position and clear bullets for a brief grace period
                    _gunPosition = (_width - _gunWidth) / 2;
                    _enemyBullets.Clear();
                    Thread.Sleep(1000); // Brief pause after hit
                }
                break;
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

                if (bullet.y >= _height)
                    _enemyBullets.RemoveAt(i);
                else
                    _enemyBullets[i] = bullet;
            }
        }

        // If all enemies are defeated, start next wave
        if (_enemies.Count == 0)
        {
            _waveNumber++;

            // Make bullets faster by reducing delay, with a floor of 1 tick
            if (_enemyBulletMoveDelay > 1)
                _enemyBulletMoveDelay--;

            // Optional: clear bullets so new wave starts "clean"
            _enemyBullets.Clear();
            _bullets.Clear();

            // Regenerate enemies
            InitializeEnemies();
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
        while (titlePosition > (_width - _titleArt[0].Length) / 2)
        {
            titleBuffer.Clear();
            titleBuffer.Append(new string(' ', _width * _height));

            // Draw title art at current position
            for (int i = 0; i < _titleArt.Length; i++)
            {
                int row = (_height - _titleArt.Length) / 2 + i;
                int position = (row * _width) + titlePosition;

                if (position >= 0 && position + _titleArt[i].Length <= titleBuffer.Length)
                {
                    titleBuffer.Remove(position, _titleArt[i].Length);
                    titleBuffer.Insert(position, _titleArt[i]);
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
            ((_height + _titleArt.Length) / 2 + 2) * _width + (_width - message.Length) / 2;

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
                int type = _random.Next(_enemyVariations.Length);

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


    private static string[] GetRandomPillarDesign()
    {
        return _pillarVariations[_random.Next(_pillarVariations.Length)];
    }

    // =========================
    // Rendering
    // =========================

    public static void RenderLoop()
    {
        BuildFrame();

        // Only update the console if the frame has changed
        string currentFrame = _buffer.ToString();
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

        // Draw pillars above the gun
        for (int i = 0; i < _pillarCount; i++)
        {
            int sectionWidth = _width / _pillarCount;
            int pillarX = (i * sectionWidth) + (sectionWidth - _pillarDesign[0].Length) / 2;

            for (int j = 0; j < _pillarDesign.Length; j++)
            {
                int row = _height - 8 - j; // Position pillars above the gun
                int position = (row * _width) + pillarX;

                if (position >= 0 && position + _pillarDesign[j].Length <= _buffer.Length)
                {
                    _buffer.Remove(position, _pillarDesign[j].Length);
                    _buffer.Insert(position, _pillarDesign[j]);
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
            }
        }

        // Draw enemies
        foreach (var enemy in _enemies)
        {
            string[] enemyDesign = _enemyVariations[enemy.Type];
            for (int i = 0; i < enemyDesign.Length; i++)
            {
                int position = ((enemy.Y + i) * _width) + enemy.X;
                if (position >= 0 && position + enemyDesign[i].Length <= _buffer.Length)
                {
                    _buffer.Remove(position, enemyDesign[i].Length);
                    _buffer.Insert(position, enemyDesign[i]);
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
            }
        }

        // Draw the gun at the bottom
        for (int i = 0; i < _gunArt.Length; i++)
        {
            int row = _height - (4 - i);
            int position = (row * _width) + _gunPosition;
            if (position >= 0 && position + _gunArt[i].Length <= _buffer.Length)
            {
                _buffer.Remove(position, _gunArt[i].Length);
                _buffer.Insert(position, _gunArt[i]);
            }
        }

        // Draw score at the top-left
        string scoreText = $"Score: {count}    Wave: {_waveNumber}";
        _buffer.Remove(0, scoreText.Length);
        _buffer.Insert(0, scoreText);

        // Draw lives at the top-right
        for (int i = 0; i < _lifeCount; i++)
        {
            for (int j = 0; j < _lifeArt.Length; j++)
            {
                int row = j;
                int position = (row * _width) + (_width - (_lifeCount - i) * 7);
                if (position + _lifeArt[j].Length <= _buffer.Length)
                {
                    _buffer.Remove(position, _lifeArt[j].Length);
                    _buffer.Insert(position, _lifeArt[j]);
                }
            }
        }
    }
}
