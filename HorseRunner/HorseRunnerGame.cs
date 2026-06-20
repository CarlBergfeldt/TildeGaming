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

    // Horse coat layers (one set per selectable coat) + tintable rider jacket
    private Texture2D[] _coatRun = new Texture2D[3];
    private Texture2D[] _coatJump = new Texture2D[3];
    private Texture2D[] _coatFall = new Texture2D[3];
    private Texture2D _jacketRunTexture;
    private Texture2D _jacketJumpTexture;
    private Texture2D _jacketFallTexture;
    private Texture2D _riderStandTexture;
    private Texture2D _riderStandJacketTexture;

    // Textures - Forest
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
    private Texture2D _shadowTexture;
    private Texture2D _pixel;

    // Fonts
    private SpriteFont _gameFont;
    private SpriteFont _titleFont;

    // Game state
    private enum GameState { Title, HorseSelect, Playing, LevelComplete, Won, GameOver }
    private GameState _state;
    private int _score;
    private float _gameTimer;

    // =======================================================================
    // Horse & rider selection
    // - 3 horse coats to pick from, and 4 rider jacket colours.
    // =======================================================================
    private static readonly string[] CoatKeys = { "chestnut", "black", "snow" };
    private static readonly string[] CoatNames = { "Bramble", "Shadow", "Snowflake" };
    private static readonly string[] CoatDescriptions =
        { "Chestnut", "Black", "White" };
    private static readonly Color[] RiderColors =
    {
        new Color(240, 210, 40),   // Yellow
        new Color(55, 90, 200),    // Blue
        new Color(244, 244, 248),  // White
        new Color(50, 50, 62),     // Black
    };
    private static readonly string[] RiderColorNames =
        { "Yellow", "Blue", "White", "Black" };
    private static readonly string[] LevelNames = { "Forest", "Arena", "Meadow" };
    private static readonly Color[] LevelColors =
    {
        new Color(90, 170, 90), new Color(210, 185, 140), new Color(110, 120, 190),
    };
    private int _selectedHorse;
    private int _selectedRider = 1; // default Blue (matches the classic rider)
    private int _selectedStartLevel;
    private int _selectFocus; // which row is focused: 0=horse, 1=rider, 2=level
    private float _menuTimer; // drives the running-horse preview animation

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

        // Horse coat layers (one per selectable coat) ...
        for (int i = 0; i < CoatKeys.Length; i++)
        {
            _coatRun[i] = Content.Load<Texture2D>($"Sprites/horse_{CoatKeys[i]}_run");
            _coatJump[i] = Content.Load<Texture2D>($"Sprites/horse_{CoatKeys[i]}_jump");
            _coatFall[i] = Content.Load<Texture2D>($"Sprites/horse_{CoatKeys[i]}_fall");
        }
        // Tintable rider jacket layers (shared across coats) ...
        _jacketRunTexture = Content.Load<Texture2D>("Sprites/rider_jacket_run");
        _jacketJumpTexture = Content.Load<Texture2D>("Sprites/rider_jacket_jump");
        _jacketFallTexture = Content.Load<Texture2D>("Sprites/rider_jacket_fall");
        // Standing rider for the podium finish ...
        _riderStandTexture = Content.Load<Texture2D>("Sprites/rider_stand");
        _riderStandJacketTexture = Content.Load<Texture2D>("Sprites/rider_stand_jacket");

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
        _shadowTexture = Content.Load<Texture2D>("Sprites/shadow");

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
        // The player is built once the horse & rider have been chosen (StartRun).
        _player = null;
    }

    // Build the player from the currently-selected horse coat and rider colour,
    // then start level 1.
    private void StartRun()
    {
        _player = new Player(
            _coatRun[_selectedHorse], _coatJump[_selectedHorse], _coatFall[_selectedHorse],
            _jacketRunTexture, _jacketJumpTexture, _jacketFallTexture,
            RiderColors[_selectedRider],
            new Vector2(120, GroundY));

        LoadLevel(_selectedStartLevel);
        _state = GameState.Playing;
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
                    _state = GameState.HorseSelect;
                break;

            case GameState.HorseSelect:
                UpdateHorseSelect(keyState, dt);
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
                    StartNewGame(); // back to the title; keeps the chosen horse/rider
                }
                break;

            case GameState.GameOver:
                if (IsKeyPressed(keyState, Keys.R) || IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter))
                {
                    StartNewGame(); // back to the title; keeps the chosen horse/rider
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

    private static int Wrap(int value, int count) => ((value % count) + count) % count;

    private void UpdateHorseSelect(KeyboardState keyState, float dt)
    {
        _menuTimer += dt;

        // UP / DOWN moves between the three rows (Horse / Rider / Level).
        if (IsKeyPressed(keyState, Keys.Down) || IsKeyPressed(keyState, Keys.S))
            _selectFocus = Wrap(_selectFocus + 1, 3);
        if (IsKeyPressed(keyState, Keys.Up) || IsKeyPressed(keyState, Keys.W))
            _selectFocus = Wrap(_selectFocus - 1, 3);

        // LEFT / RIGHT changes the value of the focused row.
        int dir = 0;
        if (IsKeyPressed(keyState, Keys.Right) || IsKeyPressed(keyState, Keys.D)) dir = 1;
        if (IsKeyPressed(keyState, Keys.Left) || IsKeyPressed(keyState, Keys.A)) dir = -1;
        if (dir != 0)
        {
            if (_selectFocus == 0)
                _selectedHorse = Wrap(_selectedHorse + dir, CoatKeys.Length);
            else if (_selectFocus == 1)
                _selectedRider = Wrap(_selectedRider + dir, RiderColors.Length);
            else
                _selectedStartLevel = Wrap(_selectedStartLevel + dir, LevelNames.Length);
        }

        // Number keys 1-3 jump straight to a starting level.
        if (IsKeyPressed(keyState, Keys.D1)) _selectedStartLevel = 0;
        if (IsKeyPressed(keyState, Keys.D2)) _selectedStartLevel = 1;
        if (IsKeyPressed(keyState, Keys.D3)) _selectedStartLevel = 2;

        if (IsKeyPressed(keyState, Keys.Space) || IsKeyPressed(keyState, Keys.Enter))
            StartRun();
    }

    // Draw a still horse+rider (coat layer + tinted jacket) at the given
    // destination, using the standing/jump pose. Used by menus & finish screens.
    private void DrawHorseRiderStill(int coatIndex, int riderIndex, Rectangle dest, Color baseTint)
    {
        var src = new Rectangle(0, 0, 192, 140);
        _spriteBatch.Draw(_coatJump[coatIndex], dest, src, baseTint);
        _spriteBatch.Draw(_jacketJumpTexture, dest, src,
            Player.MultiplyColor(RiderColors[riderIndex], baseTint));
    }

    // Draw an animated running horse+rider at the given destination.
    private void DrawHorseRiderRunning(int coatIndex, int riderIndex, Rectangle dest, float animTime, Color baseTint)
    {
        int frames = _coatRun[coatIndex].Width / 192;
        if (frames < 1) frames = 1;
        int frame = ((int)(animTime / 0.1f)) % frames;
        var src = new Rectangle(frame * 192, 0, 192, 140);
        _spriteBatch.Draw(_coatRun[coatIndex], dest, src, baseTint);
        _spriteBatch.Draw(_jacketRunTexture, dest, src,
            Player.MultiplyColor(RiderColors[riderIndex], baseTint));
    }

    // Draw the standing (arms-up) rider at the given destination, jacket tinted.
    private void DrawStandingRider(int riderIndex, Rectangle dest, Color baseTint)
    {
        _spriteBatch.Draw(_riderStandTexture, dest, baseTint);
        _spriteBatch.Draw(_riderStandJacketTexture, dest,
            Player.MultiplyColor(RiderColors[riderIndex], baseTint));
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
            case GameState.HorseSelect:
                DrawHorseSelect();
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

        DrawPlayerShadow();
        _player.Draw(_spriteBatch);
        DrawHUD(level);
    }

    // Soft contact shadow under the horse; it shrinks and fades as the horse
    // jumps higher off the ground.
    private void DrawPlayerShadow()
    {
        float k = MathHelper.Clamp(1f - _player.AirHeight / 280f, 0.4f, 1f);
        int sw = (int)(150 * k);
        int sh = (int)(28 * k);
        int sx = (int)(_player.Position.X + _player.Width / 2f) - sw / 2;
        int sy = GroundY - sh / 2 + 4;
        _spriteBatch.Draw(_shadowTexture, new Rectangle(sx, sy, sw, sh), Color.White * (0.55f * k));
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

        // Horse preview (shows the currently-selected horse & rider) ...
        DrawHorseRiderStill(_selectedHorse, _selectedRider,
            new Rectangle(ScreenWidth / 2 - 96, 160, 192, 140), Color.White);

        // Instructions
        string[] instructions = {
            "Level 1: Ride through the forest - jump logs, rocks, and bushes!",
            "Level 2: Enter the arena - clear show jumping bar obstacles!",
            "Clear 75% of obstacles to earn the apple!",
            "Watch out for the troll at the end of the forest!",
            "",
            "SPACE or ENTER - Choose your horse & rider, then ride!"
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

    private void DrawHorseSelect()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 200));

        DrawCenteredText("Choose Your Ride", _titleFont, 18, Color.Gold);

        // Big animated preview of the chosen combination.
        DrawHorseRiderRunning(_selectedHorse, _selectedRider,
            new Rectangle(ScreenWidth / 2 - 130, 62, 260, 190), _menuTimer, Color.White);

        // ---- Horse row ----
        DrawRowLabel("Horse", 260, _selectFocus == 0);
        int n = CoatKeys.Length;
        int cellW = 150, cellH = 80, gap = 24;
        int startX = (ScreenWidth - (n * cellW + (n - 1) * gap)) / 2;
        int rowY = 282;
        for (int i = 0; i < n; i++)
        {
            int cx = startX + i * (cellW + gap);
            bool sel = i == _selectedHorse;
            DrawSelectionBox(new Rectangle(cx, rowY, cellW, cellH), sel);
            DrawHorseRiderStill(i, _selectedRider,
                new Rectangle(cx + cellW / 2 - 58, rowY - 12, 116, 85), Color.White);
            DrawCenteredTextIn(CoatNames[i] + " - " + CoatDescriptions[i], _gameFont,
                cx, cellW, rowY + cellH - 24, sel ? Color.Gold : Color.White);
        }

        // ---- Rider colour row ----
        DrawRowLabel("Rider Colour", 380, _selectFocus == 1);
        int m = RiderColors.Length;
        int sw = 110, sgap = 18, sh = 56;
        int sStartX = (ScreenWidth - (m * sw + (m - 1) * sgap)) / 2;
        int sRowY = 402;
        for (int i = 0; i < m; i++)
        {
            int cx = sStartX + i * (sw + sgap);
            bool sel = i == _selectedRider;
            DrawSelectionBox(new Rectangle(cx, sRowY, sw, sh), sel);
            _spriteBatch.Draw(_pixel, new Rectangle(cx + 12, sRowY + 8, sw - 24, 22), RiderColors[i]);
            DrawCenteredTextIn(RiderColorNames[i], _gameFont, cx, sw, sRowY + sh - 22,
                sel ? Color.Gold : Color.White);
        }

        // ---- Starting level row ----
        DrawRowLabel("Starting Level", 474, _selectFocus == 2);
        int L = LevelNames.Length;
        int lw = 150, lgap = 24, lh = 64;
        int lStartX = (ScreenWidth - (L * lw + (L - 1) * lgap)) / 2;
        int lRowY = 496;
        for (int i = 0; i < L; i++)
        {
            int cx = lStartX + i * (lw + lgap);
            bool sel = i == _selectedStartLevel;
            DrawSelectionBox(new Rectangle(cx, lRowY, lw, lh), sel);
            _spriteBatch.Draw(_pixel, new Rectangle(cx + 10, lRowY + 10, lw - 20, 16), LevelColors[i]);
            DrawCenteredTextIn($"{i + 1}. {LevelNames[i]}", _gameFont, cx, lw, lRowY + lh - 26,
                sel ? Color.Gold : Color.White);
        }

        DrawCenteredText("UP / DOWN: pick a row     LEFT / RIGHT: change", _gameFont, 588, new Color(200, 220, 255));
        DrawCenteredText("Press SPACE or ENTER to ride!", _gameFont, 624, Color.LimeGreen);
    }

    private void DrawRowLabel(string text, float y, bool focused)
    {
        string label = focused ? "> " + text + " <" : text;
        DrawCenteredText(label, _gameFont, y, focused ? Color.Gold : new Color(150, 160, 185));
    }

    private void DrawSelectionBox(Rectangle r, bool selected)
    {
        Color fill = selected ? new Color(60, 50, 20, 220) : new Color(30, 30, 40, 180);
        _spriteBatch.Draw(_pixel, r, fill);
        Color border = selected ? Color.Gold : new Color(90, 90, 110);
        int t = selected ? 3 : 1;
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, t), border);
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), border);
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, t, r.Height), border);
        _spriteBatch.Draw(_pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), border);
    }

    private void DrawCenteredTextIn(string text, SpriteFont font, int x, int width, float y, Color color)
    {
        Vector2 size = font.MeasureString(text);
        _spriteBatch.DrawString(font, text, new Vector2(x + (width - size.X) / 2, y), color);
    }

    private void DrawLevelCompleteScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        string completeText = _currentLevel == 0 ? "Level 1 Complete!" : "Level 2 Complete!";
        DrawCenteredText(completeText, _titleFont, 100, Color.Gold);

        GetObstacleCounts(out int cleared, out int total);

        // Horse with apple
        DrawHorseRiderStill(_selectedHorse, _selectedRider,
            new Rectangle(ScreenWidth / 2 - 96, 190, 192, 140), Color.White);

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

    // =======================================================================
    // VICTORY: the rider gallops in, leaps off the horse and lands on top of
    // the #1 spot of the 1-2-3 prize podium.
    // =======================================================================
    private void DrawWinScreen()
    {
        // --- Festive ceremony backdrop (bright sky + arena floor) ---
        for (int y = 0; y < ScreenHeight; y++)
        {
            float t = y / (float)ScreenHeight;
            Color sky = Color.Lerp(new Color(120, 180, 235), new Color(225, 238, 245), t);
            _spriteBatch.Draw(_pixel, new Rectangle(0, y, ScreenWidth, 1), sky);
        }
        int groundLineY = 600;
        _spriteBatch.Draw(_pixel, new Rectangle(0, groundLineY, ScreenWidth, ScreenHeight - groundLineY),
            new Color(196, 170, 120));

        float animProgress = Math.Min(_appleRewardTimer / AppleRewardDuration, 1f);

        // --- Podium geometry (1st in the middle & tallest) ---
        int cx = ScreenWidth / 2;
        int blockW = 150;
        int podBottom = 660;
        var first = new Rectangle(cx - blockW / 2, 460, blockW, podBottom - 460);
        var second = new Rectangle(cx - blockW / 2 - blockW, 510, blockW, podBottom - 510);
        var third = new Rectangle(cx + blockW / 2, 535, blockW, podBottom - 535);
        DrawPodiumBlock(second, new Color(205, 207, 214), new Color(150, 152, 160), "2");
        DrawPodiumBlock(third, new Color(208, 150, 92), new Color(150, 104, 60), "3");
        DrawPodiumBlock(first, new Color(235, 205, 95), new Color(176, 142, 50), "1");

        // Where the champion rider comes to rest on top of block #1.
        int riderW = 86, riderH = 140;
        int riderRestX = cx - riderW / 2;
        int riderRestY = first.Y - riderH + 6;

        // Where the horse gallops in to (just left of the podium).
        int horseStandX = cx - 360;
        int horseY = 466;

        const float pA = 0.34f;   // gallop in
        const float pB = 0.55f;   // leap off onto the podium

        if (animProgress < pA)
        {
            // Phase A: horse + rider gallop in from the left.
            DrawCenteredText("Champion's Lap!", _titleFont, 40, Color.Gold);
            float t = animProgress / pA;
            float ease = 1f - (1f - t) * (1f - t);
            int hx = (int)MathHelper.Lerp(-220, horseStandX, ease);
            DrawHorseRiderRunning(_selectedHorse, _selectedRider,
                new Rectangle(hx, horseY, 192, 140), _appleRewardTimer, Color.White);
        }
        else if (animProgress < pB)
        {
            // Phase B: rider leaps off; horse trots back off to the left.
            DrawCenteredText("And the leap onto the podium!", _titleFont, 40, Color.Gold);
            float t = (animProgress - pA) / (pB - pA);

            int hx = (int)MathHelper.Lerp(horseStandX, -260, t);
            DrawHorseRiderRunning(_selectedHorse, _selectedRider,
                new Rectangle(hx, horseY, 192, 140), _appleRewardTimer, Color.White);

            // Rider arcs from the saddle up onto block #1.
            float startX = horseStandX + 78;
            float startY = horseY + 8;
            float x = MathHelper.Lerp(startX, riderRestX, t);
            float y = MathHelper.Lerp(startY, riderRestY, t)
                      - 150f * (float)Math.Sin(Math.PI * t);
            DrawStandingRider(_selectedRider, new Rectangle((int)x, (int)y, riderW, riderH), Color.White);
        }
        else
        {
            // Phase C: champion stands on top, medal & confetti.
            float t = (animProgress - pB) / (1f - pB);
            float pulse = (float)(Math.Sin(_appleRewardTimer * 4) * 0.12 + 0.88);
            DrawCenteredText("CHAMPION!", _titleFont, 40,
                Color.Lerp(Color.Gold, new Color(255, 240, 160), pulse));

            int bob = (int)(Math.Sin(_appleRewardTimer * 3) * 3);
            DrawStandingRider(_selectedRider,
                new Rectangle(riderRestX, riderRestY + bob, riderW, riderH), Color.White);

            // Gold medal descends onto the champion.
            if (t > 0.15f)
            {
                float mt = Math.Min(1f, (t - 0.15f) * 2.2f);
                int medalW = 32, medalH = 38;
                int medalX = cx - medalW / 2;
                int medalY = (int)MathHelper.Lerp(riderRestY - 120, riderRestY + 60, mt);
                if (mt > 0.4f)
                {
                    float g = (float)Math.Sin(_appleRewardTimer * 8);
                    _spriteBatch.Draw(_pixel, new Rectangle(medalX - 8, medalY - 8, medalW + 16, medalH + 16),
                        Color.Gold * (0.18f + g * 0.12f));
                }
                _spriteBatch.Draw(_goldMedalTexture, new Rectangle(medalX, medalY, medalW, medalH), Color.White);
            }

            // Sparkles around the champion.
            float sa = Math.Min(1f, t * 2f);
            for (int i = 0; i < 6; i++)
            {
                float angle = _appleRewardTimer * 2f + i * (MathHelper.TwoPi / 6);
                float radius = 70 + (float)Math.Sin(_appleRewardTimer * 3 + i) * 18;
                float sx = cx + (float)Math.Cos(angle) * radius;
                float sy = (riderRestY + 60) + (float)Math.Sin(angle) * radius * 0.5f;
                float sz = 22 + (float)Math.Sin(_appleRewardTimer * 5 + i * 2) * 8;
                _spriteBatch.Draw(_sparklesTexture, new Rectangle((int)sx, (int)sy, (int)sz, (int)sz),
                    Color.White * sa);
            }
        }

        // Confetti rains down once the leap begins.
        if (animProgress >= pA)
            DrawConfetti(Math.Min(1f, (animProgress - pA) * 3f));

        // Stats (top-left so they don't cover the podium).
        GetObstacleCounts(out int cleared, out int total);
        _spriteBatch.Draw(_pixel, new Rectangle(20, 96, 300, 96), new Color(0, 0, 0, 140));
        _spriteBatch.DrawString(_gameFont, $"Final Score: {_score}", new Vector2(34, 104), Color.White);
        _spriteBatch.DrawString(_gameFont, $"Obstacles Cleared: {cleared}/{total}", new Vector2(34, 134), Color.Gold);
        _spriteBatch.DrawString(_gameFont, $"Lives Remaining: {_player.Lives}/3", new Vector2(34, 164), Color.LightCoral);

        // Restart prompt after the animation.
        if (_appleRewardTimer > AppleRewardDuration)
            DrawCenteredText("Press SPACE to play again!", _gameFont, 690, Color.LimeGreen);
    }

    private void DrawPodiumBlock(Rectangle r, Color face, Color shade, string label)
    {
        _spriteBatch.Draw(_pixel, r, face);
        // top highlight strip + base shadow for a little depth
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, 6), Color.Lerp(face, Color.White, 0.4f));
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - 10, r.Width, 10), shade);
        _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, 4, r.Height), shade);
        _spriteBatch.Draw(_pixel, new Rectangle(r.Right - 4, r.Y, 4, r.Height), shade);
        Vector2 size = _titleFont.MeasureString(label);
        _spriteBatch.DrawString(_titleFont, label,
            new Vector2(r.X + (r.Width - size.X) / 2, r.Y + 24), new Color(70, 55, 20));
    }

    private static readonly Color[] ConfettiColors =
    {
        new Color(240, 80, 80), new Color(80, 160, 240), new Color(90, 210, 110),
        new Color(245, 210, 70), new Color(220, 110, 220), new Color(255, 150, 60),
        Color.White,
    };

    private void DrawConfetti(float alpha)
    {
        for (int i = 0; i < 60; i++)
        {
            float speed = 70 + (i % 6) * 26;
            float x = (i * 97) % ScreenWidth + (float)Math.Sin(_appleRewardTimer * 2 + i) * 16;
            float y = ((_appleRewardTimer * speed + i * 53) % (ScreenHeight + 40)) - 20;
            int size = 6 + (i % 3) * 2;
            Color c = ConfettiColors[i % ConfettiColors.Length] * alpha;
            _spriteBatch.Draw(_pixel, new Rectangle((int)x, (int)y, size, size), c);
        }
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

        var goFallDest = new Rectangle(ScreenWidth / 2 - 96, 220, 192, 140);
        var goFallSrc = new Rectangle(0, 0, 192, 140);
        _spriteBatch.Draw(_coatFall[_selectedHorse], goFallDest, goFallSrc, Color.White);
        _spriteBatch.Draw(_jacketFallTexture, goFallDest, goFallSrc,
            Player.MultiplyColor(RiderColors[_selectedRider], Color.White));

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
