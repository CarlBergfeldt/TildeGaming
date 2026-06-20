using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HorseRunner;

public class Player
{
    // Horse coat layers (selected coat) ...
    private Texture2D _runTexture;
    private Texture2D _jumpTexture;
    private Texture2D _fallTexture;

    // Rider jacket layers (greyscale, tinted with the chosen rider colour) ...
    private Texture2D _jacketRunTexture;
    private Texture2D _jacketJumpTexture;
    private Texture2D _jacketFallTexture;

    // The chosen rider jacket colour (Yellow / Blue / White / Black).
    private Color _riderColor;

    public Vector2 Position;
    public Vector2 Velocity;

    // Animation - sprite sheet frame size
    private int _frameWidth = 192;
    private int _frameHeight = 140;
    private int _currentFrame;
    private int _totalRunFrames;
    private float _frameTimer;
    private float _frameInterval = 0.1f;

    // =======================================================================
    // GAMEPLAY TUNING: Jump Physics
    // - Gravity: how fast the horse falls back down. Lower = floatier jumps,
    //   higher = snappier jumps. Try 1000-2000.
    // - JumpForce: initial upward velocity. More negative = higher jump.
    //   Try -700 (low) to -1100 (very high).
    // - These two together determine jump height and air time.
    //   Current settings give a high, forgiving jump arc.
    // =======================================================================
    private float _groundY;
    private const float Gravity = 1400f;
    private const float JumpForce = -920f;
    public bool IsJumping { get; private set; }
    private bool _jumpKeyReleased = true;

    // =======================================================================
    // GAMEPLAY TUNING: Fall-off & Recovery
    // - FallDuration: how long the fall animation plays (seconds).
    //   Longer = more punishment. Try 0.8-2.0.
    // - InvincibleDuration: how long the player blinks/is invincible after
    //   recovering. Longer = more forgiving. Try 1.0-3.0.
    // =======================================================================
    public bool IsFalling { get; private set; }
    private float _fallTimer;
    private const float FallDuration = 1.2f;
    private float _invincibleTimer;
    private const float InvincibleDuration = 2.5f;
    public bool IsInvincible => _invincibleTimer > 0;

    // =======================================================================
    // GAMEPLAY TUNING: Lives
    // - Starting lives. More = easier game. Try 1-5.
    // =======================================================================
    public int Lives { get; set; } = 3;
    public bool IsDead => Lives <= 0;

    // Size for collision
    public int Width => _frameWidth;
    public int Height => _frameHeight;

    public Player(
        Texture2D runTexture, Texture2D jumpTexture, Texture2D fallTexture,
        Texture2D jacketRunTexture, Texture2D jacketJumpTexture, Texture2D jacketFallTexture,
        Color riderColor,
        Vector2 startPos)
    {
        _runTexture = runTexture;
        _jumpTexture = jumpTexture;
        _fallTexture = fallTexture;

        _jacketRunTexture = jacketRunTexture;
        _jacketJumpTexture = jacketJumpTexture;
        _jacketFallTexture = jacketFallTexture;
        _riderColor = riderColor;

        _totalRunFrames = _runTexture.Width / _frameWidth;
        if (_totalRunFrames < 1) _totalRunFrames = 1;

        _groundY = startPos.Y - _frameHeight;
        Position = new Vector2(startPos.X, _groundY);
        Velocity = Vector2.Zero;
    }

    public void ResetForLevel(float groundY)
    {
        _groundY = groundY - _frameHeight;
        Position.Y = _groundY;
        Velocity = Vector2.Zero;
        IsJumping = false;
        IsFalling = false;
        _invincibleTimer = 0;
        _fallTimer = 0;
        _currentFrame = 0;
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

    // =======================================================================
    // GAMEPLAY TUNING: Player Hitbox
    // - marginX/marginY shrink the collision box relative to the sprite.
    //   Larger margins = smaller hitbox = easier to avoid obstacles.
    //   The player sprite is 192x140, so margin 40/20 gives a 112x100 hitbox.
    //   Try marginX 20-50, marginY 10-30.
    // =======================================================================
    public Rectangle GetBounds()
    {
        int marginX = 40;
        int marginY = 20;
        return new Rectangle(
            (int)Position.X + marginX,
            (int)Position.Y + marginY,
            _frameWidth - marginX * 2,
            _frameHeight - marginY * 2);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Texture2D coat;
        Texture2D jacket;
        Rectangle sourceRect;

        if (IsFalling)
        {
            coat = _fallTexture;
            jacket = _jacketFallTexture;
            sourceRect = new Rectangle(0, 0,
                Math.Min(_frameWidth, _fallTexture.Width),
                Math.Min(_frameHeight, _fallTexture.Height));
        }
        else if (IsJumping)
        {
            coat = _jumpTexture;
            jacket = _jacketJumpTexture;
            sourceRect = new Rectangle(0, 0,
                Math.Min(_frameWidth, _jumpTexture.Width),
                Math.Min(_frameHeight, _jumpTexture.Height));
        }
        else
        {
            coat = _runTexture;
            jacket = _jacketRunTexture;
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

        var dest = new Rectangle((int)Position.X, (int)Position.Y, _frameWidth, _frameHeight);

        // Layer 1: the horse coat (+ rider body) ...
        spriteBatch.Draw(coat, dest, sourceRect, tint);
        // Layer 2: the rider's jacket, tinted with the chosen rider colour ...
        spriteBatch.Draw(jacket, dest, sourceRect, MultiplyColor(_riderColor, tint));
    }

    // Multiply two colours channel-by-channel (so the blink fade also applies
    // to the tinted jacket).
    public static Color MultiplyColor(Color a, Color b)
    {
        return new Color(
            a.R * b.R / 255,
            a.G * b.G / 255,
            a.B * b.B / 255,
            a.A * b.A / 255);
    }
}
