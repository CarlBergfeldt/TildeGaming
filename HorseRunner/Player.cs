using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HorseRunner;

public class Player
{
    private Texture2D _runTexture;
    private Texture2D _jumpTexture;
    private Texture2D _fallTexture;

    public Vector2 Position;
    public Vector2 Velocity;

    // Animation
    private int _frameWidth = 128;
    private int _frameHeight = 96;
    private int _currentFrame;
    private int _totalRunFrames;
    private float _frameTimer;
    private float _frameInterval = 0.1f;

    // Physics
    private float _groundY;
    private const float Gravity = 1200f;
    private const float JumpForce = -600f;
    public bool IsJumping { get; private set; }
    private bool _jumpKeyReleased = true;

    // Fall-off state
    public bool IsFalling { get; private set; }
    private float _fallTimer;
    private const float FallDuration = 1.5f;
    private float _invincibleTimer;
    private const float InvincibleDuration = 2.0f;
    public bool IsInvincible => _invincibleTimer > 0;

    // Lives
    public int Lives { get; private set; } = 3;
    public bool IsDead => Lives <= 0;

    // Size for collision
    public int Width => _frameWidth;
    public int Height => _frameHeight;

    public Player(Texture2D runTexture, Texture2D jumpTexture, Texture2D fallTexture, Vector2 startPos)
    {
        _runTexture = runTexture;
        _jumpTexture = jumpTexture;
        _fallTexture = fallTexture;

        _totalRunFrames = _runTexture.Width / _frameWidth;
        if (_totalRunFrames < 1) _totalRunFrames = 1;

        _groundY = startPos.Y - _frameHeight;
        Position = new Vector2(startPos.X, _groundY);
        Velocity = Vector2.Zero;
    }

    public void Jump()
    {
        if (!IsJumping && !IsFalling && _jumpKeyReleased)
        {
            Velocity.Y = JumpForce;
            IsJumping = true;
            _jumpKeyReleased = false;
        }
    }

    public void TriggerFall()
    {
        if (IsInvincible || IsFalling) return;

        Lives--;
        IsFalling = true;
        _fallTimer = FallDuration;
        IsJumping = false;
        Velocity = Vector2.Zero;
        Position.Y = _groundY;
    }

    public void Update(float dt)
    {
        // Handle fall-off state
        if (IsFalling)
        {
            _fallTimer -= dt;
            if (_fallTimer <= 0)
            {
                IsFalling = false;
                _invincibleTimer = InvincibleDuration;
            }
            return;
        }

        // Handle invincibility timer
        if (_invincibleTimer > 0)
            _invincibleTimer -= dt;

        // Gravity
        if (IsJumping)
        {
            Velocity.Y += Gravity * dt;
            Position.Y += Velocity.Y * dt;

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
        int margin = 14;
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

        if (IsFalling)
        {
            texture = _fallTexture;
            sourceRect = new Rectangle(0, 0,
                Math.Min(_frameWidth, _fallTexture.Width),
                Math.Min(_frameHeight, _fallTexture.Height));
        }
        else if (IsJumping)
        {
            texture = _jumpTexture;
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

        // Blink when invincible
        Color tint = Color.White;
        if (IsInvincible && !IsFalling)
        {
            float blink = (float)Math.Sin(_invincibleTimer * 15);
            tint = blink > 0 ? Color.White : Color.White * 0.3f;
        }

        spriteBatch.Draw(texture,
            new Rectangle((int)Position.X, (int)Position.Y, _frameWidth, _frameHeight),
            sourceRect,
            tint);
    }
}
