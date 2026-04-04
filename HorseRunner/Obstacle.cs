using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HorseRunner;

public enum ObstacleType
{
    Log, Rock, Bush, Troll, Apple,
    BarSingle, BarOxer, BarTriple
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

    // Troll animation
    private int _trollFrameWidth = 80;
    private int _trollFrameHeight = 72;
    private int _trollFrame;
    private float _trollFrameTimer;
    private int _trollTotalFrames = 4;
    private float _trollMoveSpeed = 60f;

    public Obstacle(Texture2D texture, float startX, float groundY, ObstacleType type)
    {
        _texture = texture;
        Type = type;
        IsApple = type == ObstacleType.Apple;
        IsTroll = type == ObstacleType.Troll;

        switch (type)
        {
            case ObstacleType.Apple:
                Width = 48;
                Height = 48;
                Position = new Vector2(startX, groundY - 140 - Height);
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
                Height = 80;
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
        Position.X -= scrollSpeed * dt;

        if (IsTroll && IsActive && !IsCleared && !IsPassed)
        {
            Position.X -= _trollMoveSpeed * dt;

            _trollFrameTimer += dt;
            if (_trollFrameTimer >= 0.15f)
            {
                _trollFrameTimer -= 0.15f;
                _trollFrame = (_trollFrame + 1) % _trollTotalFrames;
            }
        }
    }

    public Rectangle GetBounds()
    {
        if (IsTroll)
        {
            int margin = 8;
            return new Rectangle(
                (int)Position.X + margin, (int)Position.Y + margin,
                Width - margin * 2, Height - margin * 2);
        }
        if (Type == ObstacleType.BarSingle || Type == ObstacleType.BarOxer || Type == ObstacleType.BarTriple)
        {
            // Hitbox is the pole area, not the full standard height
            int margin = 6;
            return new Rectangle(
                (int)Position.X + margin, (int)Position.Y + margin,
                Width - margin * 2, Height - margin * 2);
        }
        return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
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
