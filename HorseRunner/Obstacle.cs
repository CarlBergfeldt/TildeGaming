using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HorseRunner;

public enum ObstacleType
{
    Log, Rock, Bush, Troll, Apple,
    BarSingle, BarOxer, BarTriple,
    LogSmall, LogBirch, LogOak
}

public class Obstacle
{
    private Texture2D _texture;
    public Vector2 Position;
    public int Width;
    public int Height;
    public bool IsCleared;
    public bool IsPassed;
    public bool IsApple;
    public bool IsTroll;
    public bool IsActive = true;
    public bool PlayerWasAirborne;
    public ObstacleType Type;

    // =======================================================================
    // GAMEPLAY TUNING: Troll Movement
    // - _trollMoveSpeed: extra pixels/sec the troll walks toward the player
    //   on top of the normal scroll speed. Higher = harder to react.
    //   Try 0 (stationary) to 120 (very aggressive). Current: 50.
    // =======================================================================
    private int _trollFrameWidth = 80;
    private int _trollFrameHeight = 72;
    private int _trollFrame;
    private float _trollFrameTimer;
    private int _trollTotalFrames = 4;
    private float _trollMoveSpeed = 30f;

    public Obstacle(Texture2D texture, float startX, float groundY, ObstacleType type)
    {
        _texture = texture;
        Type = type;
        IsApple = type == ObstacleType.Apple;
        IsTroll = type == ObstacleType.Troll;

        // =======================================================================
        // GAMEPLAY TUNING: Obstacle Sizes
        // - Width/Height control the visual size of each obstacle.
        //   Smaller = easier to jump over. The hitbox (in GetBounds) is even
        //   smaller than these values due to margins.
        //
        // Forest obstacles:  Log 80x52 max, Rock 60x48 max, Bush 72x52 max
        // Bar obstacles:     100 wide x 70 tall (show jumping fences)
        // Troll:             80x72 (animated sprite sheet frame size)
        // Apple:             48x48 (reward, no collision danger)
        // =======================================================================
        switch (type)
        {
            case ObstacleType.Apple:
                Width = 48;
                Height = 48;
                // Place apple so it overlaps with the player's hitbox at ground level.
                // Player hitbox top is at (groundY - 140 + 20), so place apple
                // just above ground, overlapping with the running player.
                Position = new Vector2(startX, groundY - Height - 20);
                IsActive = false;
                break;
            case ObstacleType.Troll:
                Width = _trollFrameWidth;
                Height = _trollFrameHeight;
                Position = new Vector2(startX, groundY - Height);
                break;
            case ObstacleType.BarSingle:
            case ObstacleType.BarOxer:
            case ObstacleType.BarTriple:
                Width = 100;
                Height = 70;  // slightly shorter than the sprite for easier clearance
                Position = new Vector2(startX, groundY - Height);
                break;
            case ObstacleType.LogSmall:
                Width = 70;
                Height = 35;
                Position = new Vector2(startX, groundY - Height);
                break;
            case ObstacleType.LogBirch:
                Width = 90;
                Height = 45;
                Position = new Vector2(startX, groundY - Height);
                break;
            case ObstacleType.LogOak:
                Width = 110;
                Height = 55;
                Position = new Vector2(startX, groundY - Height);
                break;
            default: // Log, Rock, Bush
                Width = texture.Width;
                Height = texture.Height;
                if (Width > 80) Width = 80;
                if (Height > 52) Height = 52;
                Position = new Vector2(startX, groundY - Height);
                break;
        }
    }

    public void Update(float scrollSpeed, float dt)
    {
        // Normal scroll
        Position.X -= scrollSpeed * dt;

        // Troll walks toward the player (extra speed)
        if (IsTroll && IsActive && !IsCleared && !IsPassed)
        {
            Position.X -= _trollMoveSpeed * dt;

            // Animate troll walk
            _trollFrameTimer += dt;
            if (_trollFrameTimer >= 0.15f)
            {
                _trollFrameTimer -= 0.15f;
                _trollFrame = (_trollFrame + 1) % _trollTotalFrames;
            }
        }
    }

    // =======================================================================
    // GAMEPLAY TUNING: Obstacle Hitbox
    // - The margin shrinks the collision rectangle relative to the visual size.
    //   Larger margin = smaller hitbox = easier to clear without touching.
    //
    //   Forest obstacles: margin 4 on each side
    //   Bar obstacles:    margin 10 on each side (generous for fairness)
    //   Troll:            margin 10 on each side
    //
    //   To make the game easier, increase these margins.
    //   To make it harder, decrease them (minimum 0).
    // =======================================================================
    public Rectangle GetBounds()
    {
        if (IsTroll)
        {
            int margin = 10;
            return new Rectangle(
                (int)Position.X + margin, (int)Position.Y + margin,
                Width - margin * 2, Height - margin * 2);
        }
        if (Type == ObstacleType.BarSingle || Type == ObstacleType.BarOxer || Type == ObstacleType.BarTriple)
        {
            int margin = 10;
            return new Rectangle(
                (int)Position.X + margin, (int)Position.Y + margin,
                Width - margin * 2, Height - margin * 2);
        }
        // Meadow logs
        if (Type == ObstacleType.LogSmall || Type == ObstacleType.LogBirch || Type == ObstacleType.LogOak)
        {
            int margin = 4;
            return new Rectangle(
                (int)Position.X + margin, (int)Position.Y + margin,
                Width - margin * 2, Height - margin * 2);
        }
        // Forest obstacles (log, rock, bush)
        int forestMargin = 4;
        return new Rectangle(
            (int)Position.X + forestMargin, (int)Position.Y + forestMargin,
            Width - forestMargin * 2, Height - forestMargin * 2);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive) return;
        if (Position.X + Width < -100 || Position.X > 1400) return;

        Color tint = Color.White;
        if (IsCleared)
            tint = Color.White * 0.3f;

        if (IsTroll && !IsCleared)
        {
            // Draw animated troll from sprite sheet
            Rectangle sourceRect = new Rectangle(
                _trollFrame * _trollFrameWidth, 0,
                _trollFrameWidth, _trollFrameHeight);

            spriteBatch.Draw(_texture,
                new Rectangle((int)Position.X, (int)Position.Y, Width, Height),
                sourceRect, tint,
                0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
        }
        else
        {
            spriteBatch.Draw(_texture,
                new Rectangle((int)Position.X, (int)Position.Y, Width, Height),
                tint);
        }
    }
}
