namespace TerminalBlaster;

// All ASCII art and the colors paired with it. Pure data, no game logic.
static class Assets
{
    public static readonly string[] GunArt = new[]
    {
        "  ▄█▄  ",
        "█▀▀█▀▀█",
        "  │║│  "
    };

    public static readonly string[] LifeArt = new[]
    {
        "  ▲  ",
        "◄███►",
        "  ▼  "
    };

    // Enemy variants, each paired by index with a color in EnemyColors
    public static readonly string[][] EnemyVariations = new[]
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

    public static readonly ConsoleColor[] EnemyColors = new[]
    {
        ConsoleColor.Green,   // Classic UFO
        ConsoleColor.Red,     // Robot
        ConsoleColor.Magenta, // Alien
        ConsoleColor.Cyan,    // Crystal
        ConsoleColor.Yellow,  // Tech
    };

    // Pillar variants
    public static readonly string[][] PillarVariations = new[]
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
    public static readonly string[] TitleArt = new[]
    {
        @" ██████╗ ██████╗ ███╗   ██╗███████╗ ██████╗ ██╗     ███████╗     ██████╗ ██╗   ██╗███╗   ██╗",
        @"██╔════╝██╔═══██╗████╗  ██║██╔════╝██╔═══██╗██║     ██╔════╝    ██╔════╝ ██║   ██║████╗  ██║",
        @"██║     ██║   ██║██╔██╗ ██║███████╗██║   ██║██║     █████╗      ██║  ███╗██║   ██║██╔██╗ ██║",
        @"██║     ██║   ██║██║╚██╗██║╚════██║██║   ██║██║     ██╔══╝      ██║   ██║██║   ██║██║╚██╗██║",
        @"╚██████╗╚██████╔╝██║ ╚████║███████║╚██████╔╝███████╗███████╗    ╚██████╔╝╚██████╔╝██║ ╚████║",
        @" ╚═════╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝ ╚═════╝ ╚══════╝╚══════╝     ╚═════╝  ╚═════╝ ╚═╝  ╚═══╝"
    };
}
