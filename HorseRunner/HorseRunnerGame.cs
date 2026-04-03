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
    private Texture2D _appleTexture;
    private Texture2D _forestBgTexture;
    private Texture2D _groundTexture;
    private Texture2D _heartTexture;
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
    private const float AppleRewardDuration = 3f;

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
        _appleTexture = Content.Load<Texture2D>("Sprites/apple");
        _forestBgTexture = Content.Load<Texture2D>("Sprites/forest_bg");
        _groundTexture = Content.Load<Texture2D>("Sprites/ground");
        _heartTexture = Content.Load<Texture2D>("Sprites/heart");

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

    private void UpdatePlaying(KeyboardState keyState, float dt)
    {
        _gameTimer += dt;

        // Only scroll when player is not in fall state
        if (!_player.IsFalling)
            _scrollOffset += _scrollSpeed * dt;

        // Player input
        if (keyState.IsKeyDown(Keys.Space) || keyState.IsKeyDown(Keys.Up))
            _player.Jump();

        _player.Update(dt);

        // Check if player is dead (no lives)
        if (_player.IsDead)
        {
            _state = GameState.GameOver;
            return;
        }

        // Update obstacles
        bool allCleared = true;
        foreach (var obstacle in _obstacles)
        {
            if (!_player.IsFalling)
                obstacle.Update(_scrollSpeed, dt);

            if (!obstacle.IsCleared && !obstacle.IsApple)
                allCleared = false;

            // Check collision
            if (!obstacle.IsPassed && !obstacle.IsCleared && obstacle.IsActive)
            {
                if (_player.GetBounds().Intersects(obstacle.GetBounds()))
                {
                    if (obstacle.IsApple)
                    {
                        // Collect the apple
                        obstacle.IsCleared = true;
                        _appleCollected = true;
                        _score += 50;
                    }
                    else if (_player.IsJumping)
                    {
                        // Successfully clearing the obstacle while jumping
                        obstacle.IsCleared = true;
                        _score += 10;
                    }
                    else if (!_player.IsInvincible && !_player.IsFalling)
                    {
                        // Hit obstacle on the ground - rider falls off!
                        _player.TriggerFall();
                        obstacle.IsPassed = true;
                    }
                }
            }

            // Mark as passed if scrolled past player
            if (!obstacle.IsPassed && !obstacle.IsCleared &&
                obstacle.Position.X + obstacle.Width < _player.Position.X)
            {
                obstacle.IsPassed = true;
            }
        }

        // Activate apple when all obstacles cleared
        if (allCleared)
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
        // Draw obstacles
        foreach (var obstacle in _obstacles)
            obstacle.Draw(_spriteBatch);

        // Draw player
        _player.Draw(_spriteBatch);

        // Draw HUD
        DrawHUD();
    }

    private void DrawHUD()
    {
        // HUD background bar
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, 36), new Color(0, 0, 0, 180));

        // Lives (hearts)
        for (int i = 0; i < _player.Lives; i++)
        {
            _spriteBatch.Draw(_heartTexture,
                new Rectangle(10 + i * 28, 8, 24, 22),
                Color.White);
        }
        // Grey hearts for lost lives
        for (int i = _player.Lives; i < 3; i++)
        {
            _spriteBatch.Draw(_heartTexture,
                new Rectangle(10 + i * 28, 8, 24, 22),
                Color.DarkGray * 0.5f);
        }

        // Score text
        string scoreText = $"Score: {_score}";
        _spriteBatch.DrawString(_gameFont, scoreText,
            new Vector2(100, 6), Color.White);

        // Timer
        float timeLeft = Math.Max(0, GameDuration - _gameTimer);
        string timerText = $"Time: {timeLeft:F0}s";
        Color timerColor = timeLeft > 15 ? Color.White : Color.Red;
        Vector2 timerSize = _gameFont.MeasureString(timerText);
        _spriteBatch.DrawString(_gameFont, timerText,
            new Vector2(ScreenWidth - timerSize.X - 10, 6), timerColor);

        // Obstacle progress dots
        int dotX = ScreenWidth / 2 - (_obstacles.Count * 14) / 2;
        foreach (var obs in _obstacles)
        {
            if (obs.IsApple) continue;
            Color dotColor = obs.IsCleared ? Color.Gold : (obs.IsPassed ? Color.DarkRed : Color.Gray);
            _spriteBatch.Draw(_pixel, new Rectangle(dotX, 12, 10, 14), dotColor);
            dotX += 14;
        }
    }

    private void DrawTitle()
    {
        // Dark overlay
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        // Title text
        string title = "Horse Runner";
        Vector2 titleSize = _titleFont.MeasureString(title);
        float titleX = (ScreenWidth - titleSize.X) / 2;

        // Title banner background
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
            new Rectangle(ScreenWidth / 2 - 64, 180, 128, 96),
            new Rectangle(0, 0, 128, 96),
            Color.White);

        // Instructions
        string[] instructions = {
            "Jump over obstacles in the forest!",
            "Clear all obstacles to win the apple!",
            "",
            "SPACE or UP - Jump",
            "You have 3 lives",
            "",
            "Press SPACE to start!"
        };

        float yPos = 290;
        foreach (string line in instructions)
        {
            if (line.Length == 0) { yPos += 10; continue; }
            Vector2 lineSize = _gameFont.MeasureString(line);
            Color lineColor = line.StartsWith("Press") ? Color.LimeGreen : Color.White;
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
            new Rectangle((int)titleX - 20, 60, (int)titleSize.X + 40, (int)titleSize.Y + 20),
            Color.Gold);
        _spriteBatch.DrawString(_titleFont, title,
            new Vector2(titleX, 68), new Color(80, 50, 20));

        // Apple reward animation: horse eating the apple
        float animProgress = Math.Min(_appleRewardTimer / AppleRewardDuration, 1f);

        // Draw the horse standing still
        _spriteBatch.Draw(_horseRunTexture,
            new Rectangle(ScreenWidth / 2 - 100, 170, 128, 96),
            new Rectangle(0, 0, 128, 96),
            Color.White);

        // Animate apple moving toward horse's mouth
        float appleStartX = ScreenWidth / 2 + 80;
        float appleEndX = ScreenWidth / 2 + 10;
        float appleStartY = 140;
        float appleEndY = 185;
        float appleX = MathHelper.Lerp(appleStartX, appleEndX, animProgress);
        float appleY = MathHelper.Lerp(appleStartY, appleEndY, animProgress);

        if (animProgress < 0.9f)
        {
            // Apple still visible, floating toward horse
            float bob = (float)Math.Sin(_appleRewardTimer * 4) * 5;
            _spriteBatch.Draw(_appleTexture,
                new Rectangle((int)appleX, (int)(appleY + bob), 48, 48),
                Color.White);
        }
        else
        {
            // Apple eaten - show "Yum!" text
            string yum = "Yum!";
            Vector2 yumSize = _titleFont.MeasureString(yum);
            _spriteBatch.DrawString(_titleFont, yum,
                new Vector2(ScreenWidth / 2 + 20, 170), Color.LimeGreen);
        }

        // Score
        string scoreText = $"Final Score: {_score}";
        Vector2 scoreSize = _gameFont.MeasureString(scoreText);
        _spriteBatch.DrawString(_gameFont, scoreText,
            new Vector2((ScreenWidth - scoreSize.X) / 2, 290), Color.White);

        // Obstacles cleared count
        int cleared = 0;
        int total = 0;
        foreach (var obs in _obstacles)
        {
            if (obs.IsApple) continue;
            total++;
            if (obs.IsCleared) cleared++;
        }
        string clearedText = $"Obstacles Cleared: {cleared}/{total}";
        Vector2 clearedSize = _gameFont.MeasureString(clearedText);
        _spriteBatch.DrawString(_gameFont, clearedText,
            new Vector2((ScreenWidth - clearedSize.X) / 2, 320), Color.Gold);

        // Lives remaining
        string livesText = $"Lives Remaining: {_player.Lives}/3";
        Vector2 livesSize = _gameFont.MeasureString(livesText);
        _spriteBatch.DrawString(_gameFont, livesText,
            new Vector2((ScreenWidth - livesSize.X) / 2, 350), Color.LightCoral);

        // Restart prompt
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

        // Game over title
        string reason = _player.IsDead ? "No Lives Left!" : "Time's Up!";
        string title = "Game Over";
        Vector2 titleSize = _titleFont.MeasureString(title);
        float titleX = (ScreenWidth - titleSize.X) / 2;

        _spriteBatch.Draw(_pixel,
            new Rectangle((int)titleX - 20, 100, (int)titleSize.X + 40, (int)titleSize.Y + 20),
            Color.DarkRed);
        _spriteBatch.DrawString(_titleFont, title,
            new Vector2(titleX, 108), Color.White);

        // Reason
        Vector2 reasonSize = _gameFont.MeasureString(reason);
        _spriteBatch.DrawString(_gameFont, reason,
            new Vector2((ScreenWidth - reasonSize.X) / 2, 160), Color.Orange);

        // Fallen horse
        _spriteBatch.Draw(_horseFallTexture,
            new Rectangle(ScreenWidth / 2 - 64, 190, 128, 96),
            Color.White);

        // Score
        string scoreText = $"Final Score: {_score}";
        Vector2 scoreSize = _gameFont.MeasureString(scoreText);
        _spriteBatch.DrawString(_gameFont, scoreText,
            new Vector2((ScreenWidth - scoreSize.X) / 2, 300), Color.White);

        // Obstacles info
        int cleared = 0;
        int total = 0;
        foreach (var obs in _obstacles)
        {
            if (obs.IsApple) continue;
            total++;
            if (obs.IsCleared) cleared++;
        }
        string clearedText = $"Obstacles Cleared: {cleared}/{total}";
        Vector2 clearedSize = _gameFont.MeasureString(clearedText);
        _spriteBatch.DrawString(_gameFont, clearedText,
            new Vector2((ScreenWidth - clearedSize.X) / 2, 330), Color.Gold);

        // Restart
        string restartText = "Press SPACE to try again!";
        Vector2 restartSize = _gameFont.MeasureString(restartText);
        _spriteBatch.DrawString(_gameFont, restartText,
            new Vector2((ScreenWidth - restartSize.X) / 2, 390), Color.LimeGreen);
    }

    protected override void UnloadContent()
    {
        _pixel?.Dispose();
        base.UnloadContent();
    }
}
