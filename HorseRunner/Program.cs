using System;

namespace HorseRunner;

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new HorseRunnerGame();
        game.Run();
    }
}
