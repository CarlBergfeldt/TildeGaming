using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace HorseRunner;

public class HorseRunnerGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // Game objects
    private Player _player;
    private List<Obstacle> _obstacles;
    private GameLevel _level;

    // Textures
    private Texture2D _horseRunTexture;
    private Texture2D _horseJumpTexture;
    private Texture2D _horseFallTexture;
    private Texture2D _obstacleLogTexture;
    private Texture2D _obstacleRockTexture;
    private Texture2D _obstacleBushTexture;
    private Texture2D _trollTexture;
    private Texture2D _appleTexture;
    private Texture2D _forestBgTexture;
    private Texture2D _groundTexture;
    private Texture2D _heartTexture;
    private Texture2D _goldMedalTexture;
    private Texture2D _pixel;

    // Fonts
    private SpriteFont _gameFont;
    private SpriteFont _titleFont;

    // Game state
    private enum GameState { Title, Playing, Won, GameOver }
    private GameState _state;
    private int _score;
    private float _gameTimer;
    private const float GameDuration = 60f;
    private float _scrollSpeed = 200f;
    private float _scrollOffset;
    private bool _appleCollected;

    // Apple reward animation
    private float _appleRewardTimer;
    private const float AppleRewardDuration = 6f;

    // Input debounce
    private KeyboardState _prevKeyState;

    // Screen
    public const int ScreenWidth = 800;
    public const int ScreenHeight = 480;
    public const int GroundY = 380;

    public HorseRunnerGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
        _graphics.ApplyChanges();

        Window.Title = "Horse Runner - Forest Adventure";

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _horseRunTexture = Content.Load<Texture2D>("Sprites/horse_rider_run");
        _horseJumpTexture = Content.Load<Texture2D>("Sprites/horse_rider_jump");
        _horseFallTexture = Content.Load<Texture2D>("Sprites/horse_rider_fall");
        _obstacleLogTexture = Content.Load<Texture2D>("Sprites/obstacle_log");
        _obstacleRockTexture = Content.Load<Texture2D>("Sprites/obstacle_rock");
        _obstacleBushTexture = Content.Load<Texture2D>("Sprites/obstacle_bush");
        _trollTexture = Content.Load<Texture2D>("Sprites/troll");
        _appleTexture = Content.Load<Texture2D>("Sprites/apple");
        _forestBgTexture = Content.Load<Texture2D>("Sprites/forest_bg");
        _groundTexture = Content.Load<Texture2D>("Sprites/ground");
        _heartTexture = Content.Load<Texture2D>("Sprites/heart");
        _goldMedalTexture = Content.Load<Texture2D>("Sprites/gold_medal");

        _gameFont = Content.Load<SpriteFont>("GameFont");
        _titleFont = Content.Load<SpriteFont>("TitleFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        StartNewGame();
    }

    private void StartNewGame()
    {
        _state = GameState.Title;
        _score = 0;
        _gameTimer = 0f;
        _scrollOffset = 0f;
        _appleCollected = false;
        _appleRewardTimer = 0f;

        _player = new Player(
            _horseRunTexture,
            _horseJumpTexture,
            _horseFallTexture,
            new Vector2(100, GroundY));

        _level = new GameLevel(
            _obstacleLogTexture,
            _obstacleRockTexture,
            _obstacleBushTexture,
            _trollTexture,
            _appleTexture,
            _scrollSpeed,
            GameDuration);

        _obstacles = _level.Obstacles;
    }

    protected override void Update(GameTime gameTime)
    {
        var keyState = Keyboard.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (keyState.IsKeyDown(Keys.Escape))
            Exit();

        switch (_state)
        {
            case GameState.Title:
                if (IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter))
                    _state = GameState.Playing;
                break;

            case GameState.Playing:
                UpdatePlaying(keyState, dt);
                break;

            case GameState.Won:
                _appleRewardTimer += dt;
                if (_appleRewardTimer > AppleRewardDuration &&
                    (IsKeyPressed(keyState, Keys.R) || IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter)))
                {
                    StartNewGame();
                    _state = GameState.Playing;
                }
                break;

            case GameState.GameOver:
                if (IsKeyPressed(keyState, Keys.R) || IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter))
                {
                    StartNewGame();
                    _state = GameState.Playing;
                }
                break;
        }

        _prevKeyState = keyState;
        base.Update(gameTime);
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _prevKeyState.IsKeyUp(key);
    }

    private void GetObstacleCounts(out int cleared, out int total)
    {
        cleared = 0;
        total = 0;
        foreach (var obs in _obstacles)
        {
            if (obs.IsApple) continue;
            total++;
            if (obs.IsCleared) cleared++;
        }
    }

    private void UpdatePlaying(KeyboardState keyState, float dt)
    {
        _gameTimer += dt;

        if (!_player.IsFalling)
            _scrollOffset += _scrollSpeed * dt;

        // Player input
        if (keyState.IsKeyDown(Keys.Space) || keyState.IsKeyDown(Keys.Up))
            _player.Jump();

        _player.Update(dt);

        if (_player.IsDead)
        {
            _state = GameState.GameOver;
            return;
        }

        // Update obstacles and check collisions
        foreach (var obstacle in _obstacles)
        {
            if (!_player.IsFalling)
                obstacle.Update(_scrollSpeed, dt);

            if (!obstacle.IsPassed && !obstacle.IsCleared && obstacle.IsActive)
            {
                Rectangle playerBounds = _player.GetBounds();
                Rectangle obstacleBounds = obstacle.GetBounds();

                // Track if player is airborne while horizontally overlapping the obstacle
                bool horizontalOverlap = playerBounds.Right > obstacleBounds.Left &&
                                         playerBounds.Left < obstacleBounds.Right;
                if (horizontalOverlap && _player.IsJumping)
                    obstacle.PlayerWasAirborne = true;

                if (playerBounds.Intersects(obstacleBounds))
                {
                    if (obstacle.IsApple)
                    {
                        obstacle.IsCleared = true;
                        _appleCollected = true;
                        _score += 50;
                    }
                    else if (!_player.IsInvincible && !_player.IsFalling)
                    {
                        // Physical collision - rider falls off!
                        _player.TriggerFall();
                        obstacle.IsPassed = true;
                    }
                }
            }

            // Check if obstacle has scrolled past the player
            if (!obstacle.IsPassed && !obstacle.IsCleared && !obstacle.IsApple &&
                obstacle.Position.X + obstacle.Width < _player.Position.X)
            {
                if (obstacle.PlayerWasAirborne)
                {
                    // Player jumped over it successfully!
                    obstacle.IsCleared = true;
                    _score += obstacle.IsTroll ? 20 : 10;
                }
                else
                {
                    // Obstacle passed without jumping over it (shouldn't normally
                    // happen without collision, but just in case)
                    obstacle.IsPassed = true;
                }
            }
        }

        // Check if >= 75% obstacles cleared -> activate apple
        GetObstacleCounts(out int cleared, out int total);
        if (total > 0 && (float)cleared / total >= 0.75f)
        {
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.IsApple)
                    obstacle.IsActive = true;
            }
        }

        // Win condition
        if (_appleCollected)
        {
            _state = GameState.Won;
            _appleRewardTimer = 0f;
        }

        // Time's up
        if (_gameTimer >= GameDuration)
            _state = GameState.GameOver;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(135, 200, 135));

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        DrawBackground();
        DrawGround();

        switch (_state)
        {
            case GameState.Title:
                DrawTitle();
                break;
            case GameState.Playing:
                DrawGameplay();
                break;
            case GameState.Won:
                DrawGameplay();
                DrawWinScreen();
                break;
            case GameState.GameOver:
                DrawGameplay();
                DrawGameOverScreen();
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawBackground()
    {
        float bgScroll = _scrollOffset * 0.3f;
        int bgWidth = _forestBgTexture.Width;
        int startX = -(int)(bgScroll % bgWidth);

        for (int x = startX; x < ScreenWidth; x += bgWidth)
        {
            _spriteBatch.Draw(_forestBgTexture,
                new Rectangle(x, 0, bgWidth, GroundY),
                Color.White);
        }
    }

    private void DrawGround()
    {
        float groundScroll = _scrollOffset;
        int gWidth = _groundTexture.Width;
        int startX = -(int)(groundScroll % gWidth);

        for (int x = startX; x < ScreenWidth; x += gWidth)
        {
            _spriteBatch.Draw(_groundTexture,
                new Rectangle(x, GroundY, gWidth, ScreenHeight - GroundY),
                Color.White);
        }
    }

    private void DrawGameplay()
    {
        foreach (var obstacle in _obstacles)
            obstacle.Draw(_spriteBatch);

        _player.Draw(_spriteBatch);
        DrawHUD();
    }

    private void DrawHUD()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, 36), new Color(0, 0, 0, 180));

        // Lives (hearts)
        for (int i = 0; i < 3; i++)
        {
            Color heartColor = i < _player.Lives ? Color.White : Color.DarkGray * 0.5f;
            _spriteBatch.Draw(_heartTexture,
                new Rectangle(10 + i * 28, 8, 24, 22), heartColor);
        }

        // Score
        _spriteBatch.DrawString(_gameFont, $"Score: {_score}",
            new Vector2(100, 6), Color.White);

        // Timer
        float timeLeft = Math.Max(0, GameDuration - _gameTimer);
        string timerText = $"Time: {timeLeft:F0}s";
        Color timerColor = timeLeft > 15 ? Color.White : Color.Red;
        Vector2 timerSize = _gameFont.MeasureString(timerText);
        _spriteBatch.DrawString(_gameFont, timerText,
            new Vector2(ScreenWidth - timerSize.X - 10, 6), timerColor);

        // Obstacle progress dots
        GetObstacleCounts(out int cleared, out int total);
        int dotX = ScreenWidth / 2 - (total * 14) / 2;
        foreach (var obs in _obstacles)
        {
            if (obs.IsApple) continue;
            Color dotColor = obs.IsCleared ? Color.Gold : (obs.IsPassed ? Color.DarkRed : Color.Gray);
            // Troll dot is slightly bigger
            int dotSize = obs.IsTroll ? 14 : 10;
            int dotY = obs.IsTroll ? 10 : 12;
            _spriteBatch.Draw(_pixel, new Rectangle(dotX, dotY, dotSize, dotSize), dotColor);
            dotX += 14;
        }

        // Show 75% threshold line
        string pctText = $"{cleared}/{total}";
        Vector2 pctSize = _gameFont.MeasureString(pctText);
        float pctX = ScreenWidth / 2 + (total * 14) / 2 + 6;
        _spriteBatch.DrawString(_gameFont, pctText,
            new Vector2(pctX, 6), cleared >= (int)(total * 0.75f) ? Color.LimeGreen : Color.Gray);
    }

    private void DrawTitle()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        // Title
        string title = "Horse Runner";
        Vector2 titleSize = _titleFont.MeasureString(title);
        float titleX = (ScreenWidth - titleSize.X) / 2;

        _spriteBatch.Draw(_pixel,
            new Rectangle((int)titleX - 20, 70, (int)titleSize.X + 40, (int)titleSize.Y + 20),
            new Color(80, 50, 20));
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)titleX - 16, 74, (int)titleSize.X + 32, (int)titleSize.Y + 12),
            new Color(139, 90, 43));
        _spriteBatch.DrawString(_titleFont, title,
            new Vector2(titleX, 78), Color.Gold);

        // Subtitle
        string subtitle = "Forest Adventure";
        Vector2 subSize = _gameFont.MeasureString(subtitle);
        _spriteBatch.DrawString(_gameFont, subtitle,
            new Vector2((ScreenWidth - subSize.X) / 2, 130), new Color(200, 180, 140));

        // Horse preview
        _spriteBatch.Draw(_horseRunTexture,
            new Rectangle(ScreenWidth / 2 - 64, 170, 128, 96),
            new Rectangle(0, 0, 128, 96),
            Color.White);

        // Instructions
        string[] instructions = {
            "Jump over obstacles in the forest!",
            "Clear 75% of obstacles to earn the apple!",
            "Watch out for the troll at the end!",
            "",
            "SPACE or UP - Jump",
            "You have 3 lives",
            "",
            "Press SPACE to start!"
        };

        float yPos = 280;
        foreach (string line in instructions)
        {
            if (line.Length == 0) { yPos += 8; continue; }
            Vector2 lineSize = _gameFont.MeasureString(line);
            Color lineColor = line.StartsWith("Press") ? Color.LimeGreen :
                              line.Contains("troll") ? Color.Orange : Color.White;
            _spriteBatch.DrawString(_gameFont, line,
                new Vector2((ScreenWidth - lineSize.X) / 2, yPos), lineColor);
            yPos += 24;
        }
    }

    private void DrawWinScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        // Victory title
        string title = "You Win!";
        Vector2 titleSize = _titleFont.MeasureString(title);
        float titleX = (ScreenWidth - titleSize.X) / 2;

        _spriteBatch.Draw(_pixel,
            new Rectangle((int)titleX - 20, 40, (int)titleSize.X + 40, (int)titleSize.Y + 20),
            Color.Gold);
        _spriteBatch.DrawString(_titleFont, title,
            new Vector2(titleX, 48), new Color(80, 50, 20));

        float animProgress = Math.Min(_appleRewardTimer / AppleRewardDuration, 1f);

        // --- Apple reward scene ---
        int horseX = ScreenWidth / 2 - 100;
        int horseY = 140;

        // Draw horse standing
        _spriteBatch.Draw(_horseRunTexture,
            new Rectangle(horseX, horseY, 128, 96),
            new Rectangle(0, 0, 128, 96),
            Color.White);

        // Phase 1 (0-50%): Apple floats toward horse's mouth
        // Phase 2 (50-70%): Horse eats apple, "Yum!" appears
        // Phase 3 (70-100%): Gold medal descends onto rider

        if (animProgress < 0.5f)
        {
            // Apple floating toward horse
            float phase = animProgress / 0.5f;
            float appleX = MathHelper.Lerp(horseX + 180, horseX + 100, phase);
            float appleY = MathHelper.Lerp(110, horseY + 15, phase);
            float bob = (float)Math.Sin(_appleRewardTimer * 5) * 4;
            _spriteBatch.Draw(_appleTexture,
                new Rectangle((int)appleX, (int)(appleY + bob), 48, 48),
                Color.White);
        }
        else if (animProgress < 0.7f)
        {
            // Apple eaten - Yum!
            string yum = "Yum!";
            Vector2 yumSize = _titleFont.MeasureString(yum);
            float yumAlpha = Math.Min(1f, (animProgress - 0.5f) / 0.1f);
            _spriteBatch.DrawString(_titleFont, yum,
                new Vector2(horseX + 120, horseY), Color.LimeGreen * yumAlpha);
        }
        else
        {
            // Gold medal descends onto rider
            float medalPhase = (animProgress - 0.7f) / 0.3f;
            float medalTargetX = horseX + 40;
            float medalTargetY = horseY + 20;
            float medalStartY = horseY - 80;
            float medalY = MathHelper.Lerp(medalStartY, medalTargetY, Math.Min(medalPhase * 1.5f, 1f));
            float medalX = medalTargetX;

            // Sparkle effect
            if (medalPhase > 0.5f)
            {
                float sparkle = (float)Math.Sin(_appleRewardTimer * 8);
                Color sparkleColor = Color.Gold * (0.3f + sparkle * 0.3f);
                _spriteBatch.Draw(_pixel,
                    new Rectangle((int)medalX - 8, (int)medalY - 8, 56, 64),
                    sparkleColor);
            }

            _spriteBatch.Draw(_goldMedalTexture,
                new Rectangle((int)medalX, (int)medalY, 40, 48),
                Color.White);

            // "Gold Medal!" text
            if (medalPhase > 0.6f)
            {
                string medalText = "Gold Medal!";
                Vector2 medalSize = _gameFont.MeasureString(medalText);
                float textAlpha = Math.Min(1f, (medalPhase - 0.6f) / 0.2f);
                _spriteBatch.DrawString(_gameFont, medalText,
                    new Vector2((ScreenWidth - medalSize.X) / 2, horseY + 100),
                    Color.Gold * textAlpha);
            }
        }

        // Score and stats (always visible)
        GetObstacleCounts(out int cleared, out int total);

        string scoreText = $"Final Score: {_score}";
        Vector2 scoreSize = _gameFont.MeasureString(scoreText);
        _spriteBatch.DrawString(_gameFont, scoreText,
            new Vector2((ScreenWidth - scoreSize.X) / 2, 270), Color.White);

        string clearedText = $"Obstacles Cleared: {cleared}/{total}";
        Vector2 clearedSize = _gameFont.MeasureString(clearedText);
        _spriteBatch.DrawString(_gameFont, clearedText,
            new Vector2((ScreenWidth - clearedSize.X) / 2, 300), Color.Gold);

        string livesText = $"Lives Remaining: {_player.Lives}/3";
        Vector2 livesSize = _gameFont.MeasureString(livesText);
        _spriteBatch.DrawString(_gameFont, livesText,
            new Vector2((ScreenWidth - livesSize.X) / 2, 330), Color.LightCoral);

        // Restart prompt after animation
        if (_appleRewardTimer > AppleRewardDuration)
        {
            string restartText = "Press SPACE to play again!";
            Vector2 restartSize = _gameFont.MeasureString(restartText);
            _spriteBatch.DrawString(_gameFont, restartText,
                new Vector2((ScreenWidth - restartSize.X) / 2, 400), Color.LimeGreen);
        }
    }

    private void DrawGameOverScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        string reason = _player.IsDead ? "No Lives Left!" : "Time's Up!";
        string title = "Game Over";
        Vector2 titleSize = _titleFont.MeasureString(title);
        float titleX = (ScreenWidth - titleSize.X) / 2;

        _spriteBatch.Draw(_pixel,
            new Rectangle((int)titleX - 20, 100, (int)titleSize.X + 40, (int)titleSize.Y + 20),
            Color.DarkRed);
        _spriteBatch.DrawString(_titleFont, title,
            new Vector2(titleX, 108), Color.White);

        Vector2 reasonSize = _gameFont.MeasureString(reason);
        _spriteBatch.DrawString(_gameFont, reason,
            new Vector2((ScreenWidth - reasonSize.X) / 2, 160), Color.Orange);

        _spriteBatch.Draw(_horseFallTexture,
            new Rectangle(ScreenWidth / 2 - 64, 190, 128, 96),
            Color.White);

        GetObstacleCounts(out int cleared, out int total);

        string scoreText = $"Final Score: {_score}";
        Vector2 scoreSize = _gameFont.MeasureString(scoreText);
        _spriteBatch.DrawString(_gameFont, scoreText,
            new Vector2((ScreenWidth - scoreSize.X) / 2, 300), Color.White);

        string clearedText = $"Obstacles Cleared: {cleared}/{total}";
        Vector2 clearedSize = _gameFont.MeasureString(clearedText);
        _spriteBatch.DrawString(_gameFont, clearedText,
            new Vector2((ScreenWidth - clearedSize.X) / 2, 330), Color.Gold);

        // Show how close they were to 75%
        int needed = (int)Math.Ceiling(total * 0.75f);
        if (cleared < needed)
        {
            string neededText = $"Needed {needed} cleared for the apple!";
            Vector2 neededSize = _gameFont.MeasureString(neededText);
            _spriteBatch.DrawString(_gameFont, neededText,
                new Vector2((ScreenWidth - neededSize.X) / 2, 360), Color.Orange);
        }

        string restartText = "Press SPACE to try again!";
        Vector2 restartSize = _gameFont.MeasureString(restartText);
        _spriteBatch.DrawString(_gameFont, restartText,
            new Vector2((ScreenWidth - restartSize.X) / 2, 400), Color.LimeGreen);
    }

    protected override void UnloadContent()
    {
        _pixel?.Dispose();
        base.UnloadContent();
    }
}
