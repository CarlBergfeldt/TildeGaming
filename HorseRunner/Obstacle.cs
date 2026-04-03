using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HorseRunner;

public class Obstacle
{
    private Texture2D _texture;
    public Vector2 Position;
    public int Width;
    public int Height;
    public bool IsCleared;  // successfully jumped over
    public bool IsPassed;   // scrolled past (hit or missed)
    public bool IsApple;    // the final apple reward
    public bool IsActive = true; // visible and collidable

    public Obstacle(Texture2D texture, float startX, float groundY, bool isApple = false)
    {
        _texture = texture;
        IsApple = isApple;

        if (isApple)
        {
            Width = 32;
            Height = 32;
            // Apple floats above ground
            Position = new Vector2(startX, groundY - 64 - Height);
            IsActive = false; // only appears when all obstacles cleared
        }
        else
        {
            Width = texture.Width;
            Height = texture.Height;
            // Clamp to reasonable sizes
            if (Width > 64) Width = 64;
            if (Height > 48) Height = 48;
            // Place on the ground
            Position = new Vector2(startX, groundY - Height);
        }
    }

    public void Update(float scrollSpeed, float dt)
    {
        // Obstacles scroll to the left
        Position.X -= scrollSpeed * dt;
    }

    public Rectangle GetBounds()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive) return;
        if (Position.X + Width < -50 || Position.X > 850) return; // off screen

        Color tint = Color.White;
        if (IsCleared)
            tint = Color.White * 0.3f; // fade out cleared obstacles

        spriteBatch.Draw(_texture,
            new Rectangle((int)Position.X, (int)Position.Y, Width, Height),
            tint);
    }
}
