using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HorseRunner;

/// <summary>
/// Generates the level layout: obstacles spaced across the ~1 minute run.
/// In a future version, this can be loaded from a Tiled .tmx map file.
/// </summary>
public class GameLevel
{
    public List<Obstacle> Obstacles { get; private set; }

    public GameLevel(
        Texture2D logTexture,
        Texture2D rockTexture,
        Texture2D bushTexture,
        Texture2D appleTexture,
        float scrollSpeed,
        float gameDuration)
    {
        Obstacles = new List<Obstacle>();

        // Total distance the ground scrolls during the game
        float totalDistance = scrollSpeed * gameDuration; // 200 * 60 = 12000 pixels

        // Place 10 obstacles evenly, starting after some initial run-up
        int obstacleCount = 10;
        float startOffset = 600f;  // first obstacle starts off-screen to the right
        float spacing = (totalDistance - startOffset) / (obstacleCount + 1);

        Texture2D[] obstacleTextures = { logTexture, rockTexture, bushTexture };
        string[] obstacleNames = { "log", "rock", "bush" };

        float groundY = HorseRunnerGame.GroundY;

        for (int i = 0; i < obstacleCount; i++)
        {
            float x = HorseRunnerGame.ScreenWidth + startOffset + i * spacing;
            Texture2D tex = obstacleTextures[i % obstacleTextures.Length];
            var obstacle = new Obstacle(tex, x, groundY);
            Obstacles.Add(obstacle);
        }

        // Add the apple at the very end
        float appleX = HorseRunnerGame.ScreenWidth + startOffset + obstacleCount * spacing + spacing * 0.5f;
        var apple = new Obstacle(appleTexture, appleX, groundY, isApple: true);
        Obstacles.Add(apple);
    }
}
