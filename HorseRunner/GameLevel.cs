using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HorseRunner;

public enum LevelType { Forest, Arena }

public class GameLevel
{
    public List<Obstacle> Obstacles { get; private set; }
    public LevelType Type { get; private set; }

    public GameLevel(
        LevelType levelType,
        Dictionary<string, Texture2D> textures,
        float scrollSpeed,
        float levelDuration,
        float groundY)
    {
        Obstacles = new List<Obstacle>();
        Type = levelType;

        float totalDistance = scrollSpeed * levelDuration;

        if (levelType == LevelType.Forest)
            BuildForestLevel(textures, totalDistance, groundY);
        else
            BuildArenaLevel(textures, totalDistance, groundY);
    }

    private void BuildForestLevel(Dictionary<string, Texture2D> textures, float totalDistance, float groundY)
    {
        int regularCount = 9;
        float startOffset = 800f;
        float spacing = (totalDistance - startOffset) / (regularCount + 2);

        Texture2D[] obstacleTextures = {
            textures["log"], textures["rock"], textures["bush"]
        };
        ObstacleType[] obstacleTypes = {
            ObstacleType.Log, ObstacleType.Rock, ObstacleType.Bush
        };

        float screenW = HorseRunnerGame.ScreenWidth;

        for (int i = 0; i < regularCount; i++)
        {
            float x = screenW + startOffset + i * spacing;
            int idx = i % obstacleTextures.Length;
            var obstacle = new Obstacle(obstacleTextures[idx], x, groundY, obstacleTypes[idx]);
            Obstacles.Add(obstacle);
        }

        // Troll as the last obstacle
        float trollX = screenW + startOffset + regularCount * spacing;
        var troll = new Obstacle(textures["troll"], trollX, groundY, ObstacleType.Troll);
        Obstacles.Add(troll);

        // Apple after the troll
        float appleX = trollX + spacing;
        var apple = new Obstacle(textures["apple"], appleX, groundY, ObstacleType.Apple);
        Obstacles.Add(apple);
    }

    private void BuildArenaLevel(Dictionary<string, Texture2D> textures, float totalDistance, float groundY)
    {
        int obstacleCount = 8;
        float startOffset = 600f;
        float spacing = (totalDistance - startOffset) / (obstacleCount + 1);

        Texture2D[] barTextures = {
            textures["bar_single"], textures["bar_oxer"], textures["bar_triple"]
        };
        ObstacleType[] barTypes = {
            ObstacleType.BarSingle, ObstacleType.BarOxer, ObstacleType.BarTriple
        };

        float screenW = HorseRunnerGame.ScreenWidth;

        for (int i = 0; i < obstacleCount; i++)
        {
            float x = screenW + startOffset + i * spacing;
            int idx = i % barTextures.Length;
            var obstacle = new Obstacle(barTextures[idx], x, groundY, barTypes[idx]);
            Obstacles.Add(obstacle);
        }

        // Apple at the end of arena level
        float appleX = screenW + startOffset + obstacleCount * spacing + spacing * 0.5f;
        var apple = new Obstacle(textures["apple"], appleX, groundY, ObstacleType.Apple);
        Obstacles.Add(apple);
    }
}
