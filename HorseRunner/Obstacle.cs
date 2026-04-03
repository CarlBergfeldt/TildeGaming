using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    public bool IsActive = true;

    public Obstacle(Texture2D texture, float startX, float groundY, bool isApple = false)
    {
        _texture = texture;
        IsApple = isApple;

        if (isApple)
        {
            Width = 48;
            Height = 48;
            Position = new Vector2(startX, groundY - 96 - Height);
            IsActive = false;
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
        Position.X -= scrollSpeed * dt;
    }

    public Rectangle GetBounds()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive) return;
        if (Position.X + Width < -50 || Position.X > 850) return;

        Color tint = Color.White;
        if (IsCleared)
            tint = Color.White * 0.3f;

        spriteBatch.Draw(_texture,
            new Rectangle((int)Position.X, (int)Position.Y, Width, Height),
            tint);
    }
}
