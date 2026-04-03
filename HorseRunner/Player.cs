using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HorseRunner;

public class Player
{
    private Texture2D _runTexture;
    private Texture2D _jumpTexture;

    public Vector2 Position;
    public Vector2 Velocity;

    // Animation
    private int _frameWidth;
    private int _frameHeight;
    private int _currentFrame;
    private int _totalRunFrames;
    private float _frameTimer;
    private float _frameInterval = 0.1f; // 10 fps animation

    // Physics
    private float _groundY;
    private const float Gravity = 1200f;
    private const float JumpForce = -550f;
    public bool IsJumping { get; private set; }
    private bool _jumpKeyReleased = true;

    // Size for collision
    public int Width => _frameWidth;
    public int Height => _frameHeight;

    public Player(Texture2D runTexture, Texture2D jumpTexture, Vector2 startPos)
    {
        _runTexture = runTexture;
        _jumpTexture = jumpTexture;

        // Sprite sheet: frames arranged horizontally
        // Assume each frame is 96x64 pixels (horse with rider)
        _frameWidth = 96;
        _frameHeight = 64;
        _totalRunFrames = _runTexture.Width / _frameWidth;
        if (_totalRunFrames < 1) _totalRunFrames = 1;

        // Position the player so its feet are at groundY
        _groundY = startPos.Y - _frameHeight;
        Position = new Vector2(startPos.X, _groundY);
        Velocity = Vector2.Zero;
    }

    public void Jump()
    {
        if (!IsJumping && _jumpKeyReleased)
        {
            Velocity.Y = JumpForce;
            IsJumping = true;
            _jumpKeyReleased = false;
        }
    }

    public void Update(float dt)
    {
        // Gravity
        if (IsJumping)
        {
            Velocity.Y += Gravity * dt;
            Position.Y += Velocity.Y * dt;

            // Land on ground
            if (Position.Y >= _groundY)
            {
                Position.Y = _groundY;
                Velocity.Y = 0;
                IsJumping = false;
            }
        }

        // Track key release for single-press jumping
        var keyState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        if (!keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space) &&
            !keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up))
        {
            _jumpKeyReleased = true;
        }

        // Animate run cycle
        if (!IsJumping)
        {
            _frameTimer += dt;
            if (_frameTimer >= _frameInterval)
            {
                _frameTimer -= _frameInterval;
                _currentFrame = (_currentFrame + 1) % _totalRunFrames;
            }
        }
    }

    public Rectangle GetBounds()
    {
        // Slightly smaller hitbox than the sprite for fair gameplay
        int margin = 10;
        return new Rectangle(
            (int)Position.X + margin,
            (int)Position.Y + margin,
            _frameWidth - margin * 2,
            _frameHeight - margin * 2);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Texture2D texture;
        Rectangle sourceRect;

        if (IsJumping)
        {
            texture = _jumpTexture;
            // Jump texture: single frame or first frame
            sourceRect = new Rectangle(0, 0,
                Math.Min(_frameWidth, _jumpTexture.Width),
                Math.Min(_frameHeight, _jumpTexture.Height));
        }
        else
        {
            texture = _runTexture;
            sourceRect = new Rectangle(
                _currentFrame * _frameWidth, 0,
                _frameWidth, _frameHeight);
        }

        spriteBatch.Draw(texture,
            new Rectangle((int)Position.X, (int)Position.Y, _frameWidth, _frameHeight),
            sourceRect,
            Color.White);
    }
}
