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

    // Textures - Meadow
    private Texture2D _meadowBgTexture;
    private Texture2D _meadowGroundTexture;
    private Texture2D _logSmallTexture;
    private Texture2D _logBirchTexture;
    private Texture2D _logOakTexture;

    // Textures - Night & Surprise
    private Texture2D _nightOverlayTexture;
    private Texture2D _starsTexture;
    private Texture2D _moonTexture;
    private Texture2D _fireflyTexture;
    private Texture2D _shootingStarTexture;
    private Texture2D _unicornHornTexture;
    private Texture2D _sparklesTexture;

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

    // =======================================================================
    // GAMEPLAY TUNING: World Speed & Timing
    // - LevelDuration: seconds per level. Longer = more time to clear
    //   obstacles. Try 30-60. Each level has its own timer.
    // - _scrollSpeed: pixels/sec the world scrolls. Lower = slower pace,
    //   more reaction time. Try 150 (easy) to 350 (hard).
    // =======================================================================
    private const float LevelDuration = 45f;
    private float _scrollSpeed = 220f;
    private float _scrollOffset;
    private bool _appleCollected;
    private int _currentLevel; // 0 = forest, 1 = arena, 2 = meadow

    // Level transition
    private float _levelTransitionTimer;
    private const float LevelTransitionDuration = 4f;

    // Reward animation
    private float _appleRewardTimer;
    private const float AppleRewardDuration = 6f;

    // Night transition (meadow level)
    private float _nightAlpha;

    // Firefly animation
    private float _fireflyTimer;

    // Shooting star
    private float _shootingStarTimer;
    private float _shootingStarX;
    private float _shootingStarY;

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

        Window.Title = "Horse Runner";

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

        // Meadow
        _meadowBgTexture = Content.Load<Texture2D>("Sprites/meadow_bg");
        _meadowGroundTexture = Content.Load<Texture2D>("Sprites/meadow_ground");
        _logSmallTexture = Content.Load<Texture2D>("Sprites/log_small");
        _logBirchTexture = Content.Load<Texture2D>("Sprites/log_birch");
        _logOakTexture = Content.Load<Texture2D>("Sprites/log_oak");

        // Night & Surprise
        _nightOverlayTexture = Content.Load<Texture2D>("Sprites/night_overlay");
        _starsTexture = Content.Load<Texture2D>("Sprites/stars");
        _moonTexture = Content.Load<Texture2D>("Sprites/moon");
        _fireflyTexture = Content.Load<Texture2D>("Sprites/firefly");
        _shootingStarTexture = Content.Load<Texture2D>("Sprites/shooting_star");
        _unicornHornTexture = Content.Load<Texture2D>("Sprites/unicorn_horn");
        _sparklesTexture = Content.Load<Texture2D>("Sprites/sparkles");

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
            ["log_small"] = _logSmallTexture,
            ["log_birch"] = _logBirchTexture,
            ["log_oak"] = _logOakTexture,
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
        _nightAlpha = 0f;
        _fireflyTimer = 0f;
        _shootingStarTimer = 0f;

        LevelType type = levelIndex switch
        {
            0 => LevelType.Forest,
            1 => LevelType.Arena,
            _ => LevelType.Meadow
        };

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
                    LoadLevel(_currentLevel + 1);
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

        // Night transition for meadow level (starts at 40% through, fully dark by 80%)
        if (_currentLevel == 2)
        {
            float nightStart = LevelDuration * 0.4f;
            float nightEnd = LevelDuration * 0.8f;
            if (_gameTimer > nightStart)
            {
                _nightAlpha = Math.Min(1f, (_gameTimer - nightStart) / (nightEnd - nightStart));
            }
            _fireflyTimer += dt;
            _shootingStarTimer += dt;
        }

        // Win/level complete
        if (_appleCollected)
        {
            if (_currentLevel < 2)
            {
                // Completed forest or arena -> transition to next level
                _state = GameState.LevelComplete;
                _levelTransitionTimer = 0f;
            }
            else
            {
                // Completed meadow (final level) -> full win with unicorn surprise
                _state = GameState.Won;
                _appleRewardTimer = 0f;
            }
        }

        // Time's up - but if 75% cleared, count it as a level complete
        if (_gameTimer >= LevelDuration)
        {
            GetObstacleCounts(out int endCleared, out int endTotal);
            bool passed = endTotal > 0 && (float)endCleared / endTotal >= 0.75f;

            if (passed && _currentLevel < 2)
            {
                _state = GameState.LevelComplete;
                _levelTransitionTimer = 0f;
            }
            else if (passed && _currentLevel == 2)
            {
                _state = GameState.Won;
                _appleRewardTimer = 0f;
            }
            else
            {
                _state = GameState.GameOver;
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        Color clearColor = _currentLevel switch
        {
            1 => new Color(210, 185, 140),
            2 => Color.Lerp(new Color(150, 210, 150), new Color(20, 15, 40), _nightAlpha),
            _ => new Color(135, 200, 135)
        };
        GraphicsDevice.Clear(clearColor);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        DrawBackground(_currentLevel);
        DrawGround(_currentLevel);

        // Night sky elements drawn behind gameplay but after background
        if (_currentLevel == 2 && _nightAlpha > 0.1f)
            DrawNightSky();

        switch (_state)
        {
            case GameState.Title:
                DrawTitle();
                break;
            case GameState.Playing:
                DrawGameplay(_currentLevel);
                // Night overlay on top of gameplay
                if (_currentLevel == 2 && _nightAlpha > 0f)
                    DrawNightOverlay();
                break;
            case GameState.LevelComplete:
                DrawGameplay(_currentLevel);
                DrawLevelCompleteScreen();
                break;
            case GameState.Won:
                DrawGameplay(_currentLevel);
                DrawWinScreen();
                break;
            case GameState.GameOver:
                DrawGameplay(_currentLevel);
                DrawGameOverScreen();
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawBackground(int level)
    {
        Texture2D bgTex = level switch
        {
            1 => _arenaBgTexture,
            2 => _meadowBgTexture,
            _ => _forestBgTexture
        };
        float parallax = level == 1 ? 0.15f : 0.3f;
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

    private void DrawGround(int level)
    {
        Texture2D gTex = level switch
        {
            1 => _arenaGroundTexture,
            2 => _meadowGroundTexture,
            _ => _groundTexture
        };
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

    private void DrawGameplay(int level)
    {
        foreach (var obstacle in _obstacles)
            obstacle.Draw(_spriteBatch);

        _player.Draw(_spriteBatch);
        DrawHUD(level);
    }

    private void DrawHUD(int level)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, 40), new Color(0, 0, 0, 180));

        // Level indicator
        string levelText = level switch
        {
            0 => "Level 1: Forest",
            1 => "Level 2: Arena",
            _ => "Level 3: Meadow"
        };
        Color levelColor = level switch
        {
            1 => Color.SandyBrown,
            2 => new Color(180, 220, 140),
            _ => Color.LimeGreen
        };
        _spriteBatch.DrawString(_gameFont, levelText,
            new Vector2(10, 8), levelColor);

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

        DrawCenteredText("by Tilde & Carl", _gameFont, 120, new Color(200, 180, 140));

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
            "SPACE or UP ARROW - Jump    |    You have 3 lives"
        };

        float yPos = 320;
        foreach (string line in instructions)
        {
            if (line.Length == 0) { yPos += 10; continue; }
            Color lineColor = Color.White;
            if (line.StartsWith("Press")) lineColor = Color.LimeGreen;
            else if (line.Contains("Level 1")) lineColor = new Color(120, 200, 120);
            else if (line.Contains("Level 2")) lineColor = Color.SandyBrown;
            else if (line.Contains("Level 3")) lineColor = new Color(180, 140, 255);
            else if (line.Contains("troll")) lineColor = Color.Orange;
            DrawCenteredText(line, _gameFont, yPos, lineColor);
            yPos += 28;
        }
    }

    private void DrawLevelCompleteScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        string completeText = _currentLevel == 0 ? "Level 1 Complete!" : "Level 2 Complete!";
        DrawCenteredText(completeText, _titleFont, 100, Color.Gold);

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

        if (_currentLevel == 0)
        {
            DrawCenteredText("Next: The Riding Arena!", _titleFont, 480, Color.SandyBrown);
            DrawCenteredText("Show jumping bar obstacles await!", _gameFont, 530, new Color(200, 180, 140));
        }
        else
        {
            DrawCenteredText("Next: The Meadow!", _titleFont, 480, new Color(180, 220, 140));
            DrawCenteredText("A magical night ride awaits...", _gameFont, 530, new Color(180, 140, 255));
        }

        if (_levelTransitionTimer > LevelTransitionDuration)
            DrawCenteredText("Press SPACE to continue!", _gameFont, 580, Color.LimeGreen);
    }

    private void DrawNightSky()
    {
        // Stars (fade in with night)
        _spriteBatch.Draw(_starsTexture,
            new Rectangle(0, 0, ScreenWidth, 400),
            Color.White * _nightAlpha);

        // Moon
        float moonBob = (float)Math.Sin(_gameTimer * 0.5f) * 3;
        _spriteBatch.Draw(_moonTexture,
            new Rectangle(ScreenWidth - 120, 40 + (int)moonBob, 64, 64),
            Color.White * _nightAlpha);

        // Fireflies (several bouncing around)
        for (int i = 0; i < 8; i++)
        {
            float fx = (float)(Math.Sin(_fireflyTimer * (0.7 + i * 0.3) + i * 1.5) * 200 + 400 + i * 80);
            float fy = (float)(Math.Sin(_fireflyTimer * (0.5 + i * 0.2) + i * 2.1) * 80 + 350);
            float flicker = (float)(Math.Sin(_fireflyTimer * 6 + i * 3) * 0.3 + 0.7);
            _spriteBatch.Draw(_fireflyTexture,
                new Rectangle((int)fx, (int)fy, 16, 16),
                Color.White * (_nightAlpha * flicker));
        }

        // Shooting star (every ~5 seconds)
        float shootCycle = _shootingStarTimer % 7f;
        if (shootCycle < 1.2f)
        {
            float shootProgress = shootCycle / 1.2f;
            float sx = MathHelper.Lerp(ScreenWidth + 40, -100, shootProgress);
            float sy = MathHelper.Lerp(20, 200, shootProgress);
            float shootAlpha = shootProgress < 0.5f ? shootProgress * 2f : (1f - shootProgress) * 2f;
            _spriteBatch.Draw(_shootingStarTexture,
                new Rectangle((int)sx, (int)sy, 80, 20),
                Color.White * (_nightAlpha * shootAlpha));
        }
    }

    private void DrawNightOverlay()
    {
        _spriteBatch.Draw(_nightOverlayTexture,
            new Rectangle(0, 0, ScreenWidth, ScreenHeight),
            Color.White * (_nightAlpha * 0.5f));
    }

    private void DrawWinScreen()
    {
        // Dark night sky background for the unicorn surprise
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(10, 8, 30, 230));

        // Stars in background
        _spriteBatch.Draw(_starsTexture,
            new Rectangle(0, 0, ScreenWidth, 400), Color.White);

        // Moon
        float moonBob = (float)Math.Sin(_appleRewardTimer * 0.8f) * 3;
        _spriteBatch.Draw(_moonTexture,
            new Rectangle(ScreenWidth - 140, 30 + (int)moonBob, 80, 80), Color.White);

        float animProgress = Math.Min(_appleRewardTimer / AppleRewardDuration, 1f);

        int horseX = ScreenWidth / 2 - 96;
        int horseY = 180;

        // Horse standing
        _spriteBatch.Draw(_horseRunTexture,
            new Rectangle(horseX, horseY, 192, 140),
            new Rectangle(0, 0, 192, 140), Color.White);

        // Phase 1 (0-30%): Apple floats toward horse
        if (animProgress < 0.3f)
        {
            DrawCenteredText("All Levels Complete!", _titleFont, 30, Color.Gold);

            float phase = animProgress / 0.3f;
            float appleX = MathHelper.Lerp(horseX + 250, horseX + 150, phase);
            float appleY = MathHelper.Lerp(140, horseY + 10, phase);
            float bob = (float)Math.Sin(_appleRewardTimer * 5) * 4;
            _spriteBatch.Draw(_appleTexture,
                new Rectangle((int)appleX, (int)(appleY + bob), 48, 48), Color.White);
        }
        // Phase 2 (30-45%): Yum! text
        else if (animProgress < 0.45f)
        {
            DrawCenteredText("All Levels Complete!", _titleFont, 30, Color.Gold);
            float yumAlpha = Math.Min(1f, (animProgress - 0.3f) / 0.08f);
            _spriteBatch.DrawString(_titleFont, "Yum!",
                new Vector2(horseX + 170, horseY + 10), Color.LimeGreen * yumAlpha);
        }
        // Phase 3 (45-100%): UNICORN SURPRISE!
        else
        {
            float unicornPhase = (animProgress - 0.45f) / 0.55f;

            // Title changes
            if (unicornPhase < 0.3f)
            {
                float fadeIn = unicornPhase / 0.3f;
                DrawCenteredText("Something magical is happening...", _titleFont, 30,
                    new Color(180, 140, 255) * fadeIn);
            }
            else
            {
                float pulse = (float)(Math.Sin(_appleRewardTimer * 4) * 0.15 + 0.85);
                DrawCenteredText("UNICORN!", _titleFont, 30,
                    Color.Lerp(Color.Gold, new Color(255, 180, 255), pulse));
            }

            // Golden horn grows on horse's head
            float hornGrow = Math.Min(1f, unicornPhase * 2.5f);
            int hornX = horseX + 155;
            int hornY = horseY - (int)(40 * hornGrow) + 20;
            int hornW = (int)(24 * hornGrow);
            int hornH = (int)(40 * hornGrow);
            if (hornW > 0 && hornH > 0)
            {
                _spriteBatch.Draw(_unicornHornTexture,
                    new Rectangle(hornX, hornY, hornW, hornH), Color.White);
            }

            // Magic sparkles around the horse
            if (unicornPhase > 0.2f)
            {
                float sparkleAlpha = Math.Min(1f, (unicornPhase - 0.2f) * 2f);
                for (int i = 0; i < 6; i++)
                {
                    float angle = _appleRewardTimer * 2f + i * (MathHelper.TwoPi / 6);
                    float radius = 80 + (float)Math.Sin(_appleRewardTimer * 3 + i) * 20;
                    float sx = horseX + 96 + (float)Math.Cos(angle) * radius;
                    float sy = horseY + 70 + (float)Math.Sin(angle) * radius * 0.6f;
                    float sparkleSize = 24 + (float)Math.Sin(_appleRewardTimer * 5 + i * 2) * 8;
                    _spriteBatch.Draw(_sparklesTexture,
                        new Rectangle((int)sx, (int)sy, (int)sparkleSize, (int)sparkleSize),
                        Color.White * sparkleAlpha);
                }
            }

            // Fireflies
            if (unicornPhase > 0.3f)
            {
                float ffAlpha = Math.Min(1f, (unicornPhase - 0.3f) * 2f);
                for (int i = 0; i < 10; i++)
                {
                    float fx = (float)(Math.Sin(_appleRewardTimer * (0.6 + i * 0.25) + i * 1.8) * 300 + ScreenWidth / 2);
                    float fy = (float)(Math.Sin(_appleRewardTimer * (0.4 + i * 0.15) + i * 2.5) * 120 + 300);
                    float flicker = (float)(Math.Sin(_appleRewardTimer * 7 + i * 3.5) * 0.3 + 0.7);
                    _spriteBatch.Draw(_fireflyTexture,
                        new Rectangle((int)fx, (int)fy, 14, 14),
                        Color.White * (ffAlpha * flicker));
                }
            }

            // Gold medal descends
            if (unicornPhase > 0.5f)
            {
                float medalPhase = (unicornPhase - 0.5f) / 0.5f;
                float medalX = horseX + 60;
                float medalStartY = horseY - 120;
                float medalEndY = horseY + 10;
                float medalY = MathHelper.Lerp(medalStartY, medalEndY, Math.Min(medalPhase * 1.8f, 1f));

                // Sparkle glow behind medal
                if (medalPhase > 0.3f)
                {
                    float sparkle = (float)Math.Sin(_appleRewardTimer * 8);
                    Color glowColor = Color.Gold * (0.2f + sparkle * 0.15f);
                    _spriteBatch.Draw(_pixel,
                        new Rectangle((int)medalX - 12, (int)medalY - 12, 64, 72), glowColor);
                }

                _spriteBatch.Draw(_goldMedalTexture,
                    new Rectangle((int)medalX, (int)medalY, 48, 56), Color.White);

                if (medalPhase > 0.6f)
                {
                    float textAlpha = Math.Min(1f, (medalPhase - 0.6f) / 0.2f);
                    DrawCenteredText("Gold Medal Champion!", _titleFont, horseY + 155,
                        Color.Gold * textAlpha);
                }
            }

            // Shooting star across the sky
            float shootCycle = (_appleRewardTimer * 0.8f) % 4f;
            if (shootCycle < 1f)
            {
                float sx = MathHelper.Lerp(ScreenWidth + 40, -100, shootCycle);
                float sy = MathHelper.Lerp(30, 150, shootCycle);
                float shootAlpha = shootCycle < 0.5f ? shootCycle * 2f : (1f - shootCycle) * 2f;
                _spriteBatch.Draw(_shootingStarTexture,
                    new Rectangle((int)sx, (int)sy, 80, 20),
                    Color.White * shootAlpha);
            }
        }

        // Stats
        GetObstacleCounts(out int cleared, out int total);
        DrawCenteredText($"Final Score: {_score}", _gameFont, 460, Color.White);
        DrawCenteredText($"Obstacles Cleared: {cleared}/{total}", _gameFont, 490, Color.Gold);
        DrawCenteredText($"Lives Remaining: {_player.Lives}/3", _gameFont, 520, Color.LightCoral);

        // Restart prompt after animation
        if (_appleRewardTimer > AppleRewardDuration)
            DrawCenteredText("Press SPACE to play again!", _gameFont, 580, Color.LimeGreen);
    }

    private void DrawGameOverScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        string reason = _player.IsDead ? "No Lives Left!" : "Time's Up!";
        string title = "Game Over";
        Vector2 titleSize = _titleFont.MeasureString(title);
        float titleX = (ScreenWidth - titleSize.X) / 2;

        DrawCenteredText("Game Over", _titleFont, 100, Color.White);

        _spriteBatch.Draw(_pixel,
            new Rectangle((int)(ScreenWidth - titleSize.X) / 2 - 20, 96,
                (int)titleSize.X + 40, (int)titleSize.Y + 12), new Color(150, 20, 20, 200));
        DrawCenteredText("Game Over", _titleFont, 100, Color.White);

        DrawCenteredText(reason, _gameFont, 160, Color.Orange);

        string goLevelText = _currentLevel switch
        {
            0 => "Level 1: Forest",
            1 => "Level 2: Arena",
            _ => "Level 3: Meadow"
        };
        DrawCenteredText(goLevelText, _gameFont, 190, Color.Gray);

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
