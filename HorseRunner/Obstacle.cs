using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace HorseRunner;

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
    public bool PlayerWasAirborne; // tracks if player jumped while obstacle was in range

    // Troll animation
    private int _trollFrameWidth = 80;
    private int _trollFrameHeight = 72;
    private int _trollFrame;
    private float _trollFrameTimer;
    private int _trollTotalFrames = 4;
    private float _trollMoveSpeed = 60f; // moves toward player

    public Obstacle(Texture2D texture, float startX, float groundY,
        bool isApple = false, bool isTroll = false)
    {
        _texture = texture;
        IsApple = isApple;
        IsTroll = isTroll;

        if (isApple)
        {
            Width = 48;
            Height = 48;
            Position = new Vector2(startX, groundY - 96 - Height);
            IsActive = false;
        }
        else if (isTroll)
        {
            Width = _trollFrameWidth;
            Height = _trollFrameHeight;
            Position = new Vector2(startX, groundY - Height);
        }
        else
        {
            Width = texture.Width;
            Height = texture.Height;
            if (Width > 80) Width = 80;
            if (Height > 52) Height = 52;
            Position = new Vector2(startX, groundY - Height);
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

    public Rectangle GetBounds()
    {
        if (IsTroll)
        {
            // Slightly smaller hitbox for fairness
            int margin = 8;
            return new Rectangle(
                (int)Position.X + margin,
                (int)Position.Y + margin,
                Width - margin * 2,
                Height - margin * 2);
        }
        return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive) return;
        if (Position.X + Width < -50 || Position.X > 850) return;

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
                sourceRect,
                tint,
                0f, Vector2.Zero,
                SpriteEffects.FlipHorizontally, // face left toward player
                0f);
        }
        else
        {
            spriteBatch.Draw(_texture,
                new Rectangle((int)Position.X, (int)Position.Y, Width, Height),
                tint);
        }
    }
}
