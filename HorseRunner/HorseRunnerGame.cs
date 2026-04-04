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

    // Textures - Forest
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

    // Textures - Arena
    private Texture2D _arenaBgTexture;
    private Texture2D _arenaGroundTexture;
    private Texture2D _barSingleTexture;
    private Texture2D _barOxerTexture;
    private Texture2D _barTripleTexture;

    // UI textures
    private Texture2D _heartTexture;
    private Texture2D _goldMedalTexture;
    private Texture2D _pixel;

    // Fonts
    private SpriteFont _gameFont;
    private SpriteFont _titleFont;

    // Game state
    private enum GameState { Title, Playing, LevelComplete, Won, GameOver }
    private GameState _state;
    private int _score;
    private float _gameTimer;
    private const float LevelDuration = 45f; // each level is 45 seconds
    private float _scrollSpeed = 250f;
    private float _scrollOffset;
    private bool _appleCollected;
    private int _currentLevel; // 0 = forest, 1 = arena

    // Level transition
    private float _levelTransitionTimer;
    private const float LevelTransitionDuration = 4f;

    // Reward animation
    private float _appleRewardTimer;
    private const float AppleRewardDuration = 6f;

    // Input debounce
    private KeyboardState _prevKeyState;

    // Screen - BIGGER resolution
    public const int ScreenWidth = 1280;
    public const int ScreenHeight = 720;
    public const int GroundY = 580;

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

        Window.Title = "Horse Runner - Forest & Arena";

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Horse textures
        _horseRunTexture = Content.Load<Texture2D>("Sprites/horse_rider_run");
        _horseJumpTexture = Content.Load<Texture2D>("Sprites/horse_rider_jump");
        _horseFallTexture = Content.Load<Texture2D>("Sprites/horse_rider_fall");

        // Forest obstacles
        _obstacleLogTexture = Content.Load<Texture2D>("Sprites/obstacle_log");
        _obstacleRockTexture = Content.Load<Texture2D>("Sprites/obstacle_rock");
        _obstacleBushTexture = Content.Load<Texture2D>("Sprites/obstacle_bush");
        _trollTexture = Content.Load<Texture2D>("Sprites/troll");

        // Arena obstacles
        _barSingleTexture = Content.Load<Texture2D>("Sprites/bar_single");
        _barOxerTexture = Content.Load<Texture2D>("Sprites/bar_oxer");
        _barTripleTexture = Content.Load<Texture2D>("Sprites/bar_triple");

        // Shared
        _appleTexture = Content.Load<Texture2D>("Sprites/apple");
        _forestBgTexture = Content.Load<Texture2D>("Sprites/forest_bg");
        _groundTexture = Content.Load<Texture2D>("Sprites/ground");
        _arenaBgTexture = Content.Load<Texture2D>("Sprites/arena_bg");
        _arenaGroundTexture = Content.Load<Texture2D>("Sprites/arena_ground");

        // UI
        _heartTexture = Content.Load<Texture2D>("Sprites/heart");
        _goldMedalTexture = Content.Load<Texture2D>("Sprites/gold_medal");

        _gameFont = Content.Load<SpriteFont>("GameFont");
        _titleFont = Content.Load<SpriteFont>("TitleFont");

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        StartNewGame();
    }

    private Dictionary<string, Texture2D> GetTextures()
    {
        return new Dictionary<string, Texture2D>
        {
            ["log"] = _obstacleLogTexture,
            ["rock"] = _obstacleRockTexture,
            ["bush"] = _obstacleBushTexture,
            ["troll"] = _trollTexture,
            ["apple"] = _appleTexture,
            ["bar_single"] = _barSingleTexture,
            ["bar_oxer"] = _barOxerTexture,
            ["bar_triple"] = _barTripleTexture,
        };
    }

    private void StartNewGame()
    {
        _state = GameState.Title;
        _score = 0;
        _gameTimer = 0f;
        _scrollOffset = 0f;
        _appleCollected = false;
        _appleRewardTimer = 0f;
        _currentLevel = 0;

        _player = new Player(
            _horseRunTexture,
            _horseJumpTexture,
            _horseFallTexture,
            new Vector2(120, GroundY));

        LoadLevel(0);
    }

    private void LoadLevel(int levelIndex)
    {
        _currentLevel = levelIndex;
        _gameTimer = 0f;
        _scrollOffset = 0f;
        _appleCollected = false;

        LevelType type = levelIndex == 0 ? LevelType.Forest : LevelType.Arena;

        _level = new GameLevel(type, GetTextures(), _scrollSpeed, LevelDuration, GroundY);
        _obstacles = _level.Obstacles;

        _player.ResetForLevel(GroundY);
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

            case GameState.LevelComplete:
                _levelTransitionTimer += dt;
                if (_levelTransitionTimer > LevelTransitionDuration &&
                    (IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter)))
                {
                    LoadLevel(1);
                    _state = GameState.Playing;
                }
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

            // Check if obstacle scrolled past player
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

        // 75% cleared -> activate apple
        GetObstacleCounts(out int cleared, out int total);
        if (total > 0 && (float)cleared / total >= 0.75f)
        {
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.IsApple)
                    obstacle.IsActive = true;
            }
        }

        // Win/level complete
        if (_appleCollected)
        {
            if (_currentLevel == 0)
            {
                // Completed forest level -> transition to arena
                _state = GameState.LevelComplete;
                _levelTransitionTimer = 0f;
            }
            else
            {
                // Completed arena level -> full win with reward
                _state = GameState.Won;
                _appleRewardTimer = 0f;
            }
        }

        if (_gameTimer >= LevelDuration)
            _state = GameState.GameOver;
    }

    protected override void Draw(GameTime gameTime)
    {
        bool isArena = _currentLevel == 1;
        GraphicsDevice.Clear(isArena ? new Color(210, 185, 140) : new Color(135, 200, 135));

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        DrawBackground(isArena);
        DrawGround(isArena);

        switch (_state)
        {
            case GameState.Title:
                DrawTitle();
                break;
            case GameState.Playing:
                DrawGameplay(isArena);
                break;
            case GameState.LevelComplete:
                DrawGameplay(false);
                DrawLevelCompleteScreen();
                break;
            case GameState.Won:
                DrawGameplay(true);
                DrawWinScreen();
                break;
            case GameState.GameOver:
                DrawGameplay(isArena);
                DrawGameOverScreen();
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawBackground(bool isArena)
    {
        Texture2D bgTex = isArena ? _arenaBgTexture : _forestBgTexture;
        float parallax = isArena ? 0.15f : 0.3f;
        float bgScroll = _scrollOffset * parallax;
        int bgWidth = bgTex.Width;
        int startX = -(int)(bgScroll % bgWidth);

        for (int x = startX; x < ScreenWidth; x += bgWidth)
        {
            _spriteBatch.Draw(bgTex,
                new Rectangle(x, 0, bgWidth, GroundY),
                Color.White);
        }
    }

    private void DrawGround(bool isArena)
    {
        Texture2D gTex = isArena ? _arenaGroundTexture : _groundTexture;
        float groundScroll = _scrollOffset;
        int gWidth = gTex.Width;
        int startX = -(int)(groundScroll % gWidth);

        for (int x = startX; x < ScreenWidth; x += gWidth)
        {
            _spriteBatch.Draw(gTex,
                new Rectangle(x, GroundY, gWidth, ScreenHeight - GroundY),
                Color.White);
        }
    }

    private void DrawGameplay(bool isArena)
    {
        foreach (var obstacle in _obstacles)
            obstacle.Draw(_spriteBatch);

        _player.Draw(_spriteBatch);
        DrawHUD(isArena);
    }

    private void DrawHUD(bool isArena)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, 40), new Color(0, 0, 0, 180));

        // Level indicator
        string levelText = _currentLevel == 0 ? "Level 1: Forest" : "Level 2: Arena";
        _spriteBatch.DrawString(_gameFont, levelText,
            new Vector2(10, 8), isArena ? Color.SandyBrown : Color.LimeGreen);

        // Hearts
        for (int i = 0; i < 3; i++)
        {
            Color heartColor = i < _player.Lives ? Color.White : Color.DarkGray * 0.5f;
            _spriteBatch.Draw(_heartTexture,
                new Rectangle(220 + i * 30, 10, 26, 24), heartColor);
        }

        // Score
        _spriteBatch.DrawString(_gameFont, $"Score: {_score}",
            new Vector2(330, 8), Color.White);

        // Timer
        float timeLeft = Math.Max(0, LevelDuration - _gameTimer);
        string timerText = $"Time: {timeLeft:F0}s";
        Color timerColor = timeLeft > 10 ? Color.White : Color.Red;
        Vector2 timerSize = _gameFont.MeasureString(timerText);
        _spriteBatch.DrawString(_gameFont, timerText,
            new Vector2(ScreenWidth - timerSize.X - 10, 8), timerColor);

        // Obstacle progress dots
        GetObstacleCounts(out int cleared, out int total);
        int dotX = ScreenWidth / 2 - (total * 14) / 2;
        foreach (var obs in _obstacles)
        {
            if (obs.IsApple) continue;
            Color dotColor = obs.IsCleared ? Color.Gold : (obs.IsPassed ? Color.DarkRed : Color.Gray);
            // Troll dot is slightly bigger
            int dotSize = obs.IsTroll ? 14 : 10;
            int dotY = obs.IsTroll ? 12 : 14;
            _spriteBatch.Draw(_pixel, new Rectangle(dotX, dotY, dotSize, dotSize), dotColor);
            dotX += 14;
        }

        // Cleared counter
        string pctText = $"{cleared}/{total}";
        float pctX = ScreenWidth / 2 + (total * 14) / 2 + 8;
        _spriteBatch.DrawString(_gameFont, pctText,
            new Vector2(pctX, 8), cleared >= (int)(total * 0.75f) ? Color.LimeGreen : Color.Gray);
    }

    private void DrawCenteredText(string text, SpriteFont font, float y, Color color)
    {
        Vector2 size = font.MeasureString(text);
        _spriteBatch.DrawString(font, text,
            new Vector2((ScreenWidth - size.X) / 2, y), color);
    }

    private void DrawTitle()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        // Title banner
        string title = "Horse Runner";
        Vector2 titleSize = _titleFont.MeasureString(title);
        float titleX = (ScreenWidth - titleSize.X) / 2;

        _spriteBatch.Draw(_pixel,
            new Rectangle((int)titleX - 24, 60, (int)titleSize.X + 48, (int)titleSize.Y + 24),
            new Color(80, 50, 20));
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)titleX - 20, 64, (int)titleSize.X + 40, (int)titleSize.Y + 16),
            new Color(139, 90, 43));
        _spriteBatch.DrawString(_titleFont, title, new Vector2(titleX, 68), Color.Gold);

        DrawCenteredText("Forest & Arena Challenge", _gameFont, 120, new Color(200, 180, 140));

        // Horse preview (bigger now)
        _spriteBatch.Draw(_horseRunTexture,
            new Rectangle(ScreenWidth / 2 - 96, 160, 192, 140),
            new Rectangle(0, 0, 192, 140),
            Color.White);

        // Instructions
        string[] instructions = {
            "Level 1: Ride through the forest - jump logs, rocks, and bushes!",
            "Level 2: Enter the arena - clear show jumping bar obstacles!",
            "Clear 75% of obstacles to earn the apple!",
            "Watch out for the troll at the end of the forest!",
            "",
            "SPACE or UP ARROW - Jump    |    You have 3 lives",
            "",
            "Press SPACE to start!"
        };

        float yPos = 320;
        foreach (string line in instructions)
        {
            if (line.Length == 0) { yPos += 10; continue; }
            Color lineColor = Color.White;
            if (line.StartsWith("Press")) lineColor = Color.LimeGreen;
            else if (line.Contains("Level 1")) lineColor = new Color(120, 200, 120);
            else if (line.Contains("Level 2")) lineColor = Color.SandyBrown;
            else if (line.Contains("troll")) lineColor = Color.Orange;
            DrawCenteredText(line, _gameFont, yPos, lineColor);
            yPos += 28;
        }
    }

    private void DrawLevelCompleteScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        DrawCenteredText("Level 1 Complete!", _titleFont, 100, Color.Gold);

        GetObstacleCounts(out int cleared, out int total);

        // Horse with apple
        _spriteBatch.Draw(_horseRunTexture,
            new Rectangle(ScreenWidth / 2 - 96, 190, 192, 140),
            new Rectangle(0, 0, 192, 140), Color.White);

        float anim = Math.Min(_levelTransitionTimer / 2f, 1f);
        if (anim < 1f)
        {
            float appleX = MathHelper.Lerp(ScreenWidth / 2 + 140, ScreenWidth / 2 + 60, anim);
            float bob = (float)Math.Sin(_levelTransitionTimer * 4) * 4;
            _spriteBatch.Draw(_appleTexture,
                new Rectangle((int)appleX, 200 + (int)bob, 48, 48), Color.White);
        }
        else
        {
            DrawCenteredText("Yum!", _titleFont, 200, Color.LimeGreen);
        }

        DrawCenteredText($"Score: {_score}", _gameFont, 360, Color.White);
        DrawCenteredText($"Obstacles Cleared: {cleared}/{total}", _gameFont, 390, Color.Gold);
        DrawCenteredText($"Lives: {_player.Lives}/3", _gameFont, 420, Color.LightCoral);

        DrawCenteredText("Next: The Riding Arena!", _titleFont, 480, Color.SandyBrown);
        DrawCenteredText("Show jumping bar obstacles await!", _gameFont, 530, new Color(200, 180, 140));

        if (_levelTransitionTimer > LevelTransitionDuration)
            DrawCenteredText("Press SPACE to continue!", _gameFont, 580, Color.LimeGreen);
    }

    private void DrawWinScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        DrawCenteredText("Champion!", _titleFont, 40, Color.Gold);
        DrawCenteredText("Both levels completed!", _gameFont, 90, new Color(255, 215, 0));

        float animProgress = Math.Min(_appleRewardTimer / AppleRewardDuration, 1f);

        int horseX = ScreenWidth / 2 - 96;
        int horseY = 130;

        // Horse standing
        _spriteBatch.Draw(_horseRunTexture,
            new Rectangle(horseX, horseY, 192, 140),
            new Rectangle(0, 0, 192, 140), Color.White);

        // Phase 1 (0-40%): Apple floats toward horse
        if (animProgress < 0.4f)
        {
            float phase = animProgress / 0.4f;
            float appleX = MathHelper.Lerp(horseX + 250, horseX + 150, phase);
            float appleY = MathHelper.Lerp(100, horseY + 10, phase);
            float bob = (float)Math.Sin(_appleRewardTimer * 5) * 4;
            _spriteBatch.Draw(_appleTexture,
                new Rectangle((int)appleX, (int)(appleY + bob), 48, 48), Color.White);
        }
        // Phase 2 (40-60%): Yum!
        else if (animProgress < 0.6f)
        {
            float yumAlpha = Math.Min(1f, (animProgress - 0.4f) / 0.1f);
            _spriteBatch.DrawString(_titleFont, "Yum!",
                new Vector2(horseX + 170, horseY + 10), Color.LimeGreen * yumAlpha);
        }
        // Phase 3 (60-100%): Gold medal descends onto rider
        else
        {
            float medalPhase = (animProgress - 0.6f) / 0.4f;
            float medalX = horseX + 60;
            float medalStartY = horseY - 100;
            float medalEndY = horseY + 10;
            float medalY = MathHelper.Lerp(medalStartY, medalEndY, Math.Min(medalPhase * 1.5f, 1f));

            // Sparkle glow
            if (medalPhase > 0.4f)
            {
                float sparkle = (float)Math.Sin(_appleRewardTimer * 8);
                Color glowColor = Color.Gold * (0.2f + sparkle * 0.2f);
                _spriteBatch.Draw(_pixel,
                    new Rectangle((int)medalX - 12, (int)medalY - 12, 64, 72), glowColor);
            }

            _spriteBatch.Draw(_goldMedalTexture,
                new Rectangle((int)medalX, (int)medalY, 48, 56), Color.White);

            if (medalPhase > 0.5f)
            {
                float textAlpha = Math.Min(1f, (medalPhase - 0.5f) / 0.2f);
                DrawCenteredText("Gold Medal!", _titleFont, horseY + 150,
                    Color.Gold * textAlpha);
            }
        }

        // Stats
        GetObstacleCounts(out int cleared, out int total);
        DrawCenteredText($"Final Score: {_score}", _gameFont, 420, Color.White);
        DrawCenteredText($"Obstacles Cleared: {cleared}/{total}", _gameFont, 450, Color.Gold);
        DrawCenteredText($"Lives Remaining: {_player.Lives}/3", _gameFont, 480, Color.LightCoral);

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
            DrawCenteredText("Press SPACE to play again!", _gameFont, 550, Color.LimeGreen);
    }

    private void DrawGameOverScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        string reason = _player.IsDead ? "No Lives Left!" : "Time's Up!";
        string title = "Game Over";
        Vector2 titleSize = _titleFont.MeasureString(title);
        float titleX = (ScreenWidth - titleSize.X) / 2;

        DrawCenteredText("Game Over", _titleFont, 100, Color.White);

        // Red banner behind title
        Vector2 titleSize = _titleFont.MeasureString("Game Over");
        _spriteBatch.Draw(_pixel,
            new Rectangle((int)(ScreenWidth - titleSize.X) / 2 - 20, 96,
                (int)titleSize.X + 40, (int)titleSize.Y + 12), new Color(150, 20, 20, 200));
        DrawCenteredText("Game Over", _titleFont, 100, Color.White);

        DrawCenteredText(reason, _gameFont, 160, Color.Orange);

        string levelText = _currentLevel == 0 ? "Level 1: Forest" : "Level 2: Arena";
        DrawCenteredText(levelText, _gameFont, 190, Color.Gray);

        _spriteBatch.Draw(_horseFallTexture,
            new Rectangle(ScreenWidth / 2 - 96, 220, 192, 140), Color.White);

        GetObstacleCounts(out int cleared, out int total);
        DrawCenteredText($"Final Score: {_score}", _gameFont, 380, Color.White);
        DrawCenteredText($"Obstacles Cleared: {cleared}/{total}", _gameFont, 410, Color.Gold);

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
            DrawCenteredText($"Needed {needed} cleared for the apple!", _gameFont, 440, Color.Orange);

        DrawCenteredText("Press SPACE to try again!", _gameFont, 500, Color.LimeGreen);
    }

    protected override void UnloadContent()
    {
        _pixel?.Dispose();
        base.UnloadContent();
    }
}
