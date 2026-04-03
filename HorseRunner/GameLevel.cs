using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HorseRunner;

public class GameLevel
{
    public List<Obstacle> Obstacles { get; private set; }

    public GameLevel(
        Texture2D logTexture,
        Texture2D rockTexture,
        Texture2D bushTexture,
        Texture2D trollTexture,
        Texture2D appleTexture,
        float scrollSpeed,
        float gameDuration)
    {
        Obstacles = new List<Obstacle>();

        float totalDistance = scrollSpeed * gameDuration;

        // 9 regular obstacles + 1 troll at the end = 10 total
        int regularCount = 9;
        float startOffset = 600f;
        float spacing = (totalDistance - startOffset) / (regularCount + 2); // +2 for troll + apple

        Texture2D[] obstacleTextures = { logTexture, rockTexture, bushTexture };
        float groundY = HorseRunnerGame.GroundY;

        for (int i = 0; i < regularCount; i++)
        {
            float x = HorseRunnerGame.ScreenWidth + startOffset + i * spacing;
            Texture2D tex = obstacleTextures[i % obstacleTextures.Length];
            var obstacle = new Obstacle(tex, x, groundY);
            Obstacles.Add(obstacle);
        }

        // Troll as the last obstacle
        float trollX = HorseRunnerGame.ScreenWidth + startOffset + regularCount * spacing;
        var troll = new Obstacle(trollTexture, trollX, groundY, isTroll: true);
        Obstacles.Add(troll);

        // Apple after the troll
        float appleX = trollX + spacing;
        var apple = new Obstacle(appleTexture, appleX, groundY, isApple: true);
        Obstacles.Add(apple);
    }
}
