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
    private Texture2D _obstacleLogTexture;
    private Texture2D _obstacleRockTexture;
    private Texture2D _obstacleBushTexture;
    private Texture2D _appleTexture;
    private Texture2D _forestBgTexture;
    private Texture2D _groundTexture;
    private Texture2D _pixel; // for drawing simple shapes/UI

    // Font (we'll draw text using a pixel texture since we avoid extra deps)
    private SpriteFont _font;

    // Game state
    private enum GameState { Title, Playing, Won, GameOver }
    private GameState _state;
    private int _score;
    private float _gameTimer;
    private const float GameDuration = 60f; // 1 minute
    private float _scrollSpeed = 200f;
    private float _scrollOffset;
    private bool _appleCollected;

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

        // Load textures - these are sprite sheets / individual sprites
        _horseRunTexture = Content.Load<Texture2D>("Sprites/horse_rider_run");
        _horseJumpTexture = Content.Load<Texture2D>("Sprites/horse_rider_jump");
        _obstacleLogTexture = Content.Load<Texture2D>("Sprites/obstacle_log");
        _obstacleRockTexture = Content.Load<Texture2D>("Sprites/obstacle_rock");
        _obstacleBushTexture = Content.Load<Texture2D>("Sprites/obstacle_bush");
        _appleTexture = Content.Load<Texture2D>("Sprites/apple");
        _forestBgTexture = Content.Load<Texture2D>("Sprites/forest_bg");
        _groundTexture = Content.Load<Texture2D>("Sprites/ground");

        // Create a 1x1 pixel texture for UI drawing
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

        // Create player (horse + rider)
        _player = new Player(
            _horseRunTexture,
            _horseJumpTexture,
            new Vector2(120, GroundY));

        // Create the level with obstacles
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
                if (keyState.IsKeyDown(Keys.Space) || keyState.IsKeyDown(Keys.Enter))
                    _state = GameState.Playing;
                break;

            case GameState.Playing:
                UpdatePlaying(keyState, dt);
                break;

            case GameState.Won:
            case GameState.GameOver:
                if (keyState.IsKeyDown(Keys.R))
                    StartNewGame();
                if (keyState.IsKeyDown(Keys.Space) || keyState.IsKeyDown(Keys.Enter))
                {
                    StartNewGame();
                    _state = GameState.Playing;
                }
                break;
        }

        base.Update(gameTime);
    }

    private void UpdatePlaying(KeyboardState keyState, float dt)
    {
        _gameTimer += dt;
        _scrollOffset += _scrollSpeed * dt;

        // Player input: Space or Up to jump
        if (keyState.IsKeyDown(Keys.Space) || keyState.IsKeyDown(Keys.Up))
            _player.Jump();

        _player.Update(dt);

        // Update obstacles
        bool allCleared = true;
        foreach (var obstacle in _obstacles)
        {
            obstacle.Update(_scrollSpeed, dt);

            if (!obstacle.IsCleared && !obstacle.IsApple)
                allCleared = false;

            // Check collision
            if (!obstacle.IsPassed && !obstacle.IsCleared)
            {
                if (_player.GetBounds().Intersects(obstacle.GetBounds()))
                {
                    if (_player.IsJumping && _player.Velocity.Y <= 0)
                    {
                        // Successfully clearing the obstacle (jumping over it)
                        obstacle.IsCleared = true;
                        if (obstacle.IsApple)
                        {
                            _appleCollected = true;
                            _score += 50;
                        }
                        else
                        {
                            _score += 10;
                        }
                    }
                    else if (!_player.IsJumping)
                    {
                        // Hit the obstacle on the ground
                        obstacle.IsPassed = true; // mark as passed but not cleared
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

        // Check if the apple should appear (all non-apple obstacles cleared)
        if (allCleared)
        {
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.IsApple)
                    obstacle.IsActive = true;
            }
        }

        // Win condition: apple collected
        if (_appleCollected)
            _state = GameState.Won;

        // Time's up
        if (_gameTimer >= GameDuration)
            _state = GameState.GameOver;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(135, 200, 135)); // light green

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // Draw parallax forest background
        DrawBackground();

        // Draw ground
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
        // Tile the forest background with parallax (slower scroll)
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
        {
            obstacle.Draw(_spriteBatch);
        }

        // Draw player
        _player.Draw(_spriteBatch);

        // Draw HUD
        DrawHUD();
    }

    private void DrawHUD()
    {
        // Score bar background
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, 32), new Color(0, 0, 0, 150));

        // We can't easily draw text without a SpriteFont loaded.
        // The score and timer are shown via simple bar indicators.

        // Score indicator: green bar
        int scoreBarWidth = Math.Min(_score * 3, 300);
        _spriteBatch.Draw(_pixel, new Rectangle(10, 8, scoreBarWidth, 16), Color.LimeGreen);

        // Score text area marker
        _spriteBatch.Draw(_pixel, new Rectangle(8, 6, 4, 20), Color.White);

        // Timer bar: shows remaining time
        float timeRatio = Math.Max(0, 1f - _gameTimer / GameDuration);
        int timerBarWidth = (int)(200 * timeRatio);
        Color timerColor = timeRatio > 0.25f ? Color.CornflowerBlue : Color.Red;
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth - 210, 8, timerBarWidth, 16), timerColor);
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth - 212, 6, 4, 20), Color.White);

        // Obstacle progress: small dots for each obstacle
        int dotX = ScreenWidth / 2 - (_obstacles.Count * 12) / 2;
        foreach (var obs in _obstacles)
        {
            if (obs.IsApple) continue;
            Color dotColor = obs.IsCleared ? Color.Gold : (obs.IsPassed ? Color.DarkRed : Color.Gray);
            _spriteBatch.Draw(_pixel, new Rectangle(dotX, 10, 8, 12), dotColor);
            dotX += 12;
        }
    }

    private void DrawTitle()
    {
        // Dark overlay
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 180));

        // Title "banner"
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 200, 100, 400, 60), new Color(80, 50, 20));
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 196, 104, 392, 52), new Color(139, 90, 43));

        // Horse icon in center
        _spriteBatch.Draw(_horseRunTexture,
            new Rectangle(ScreenWidth / 2 - 48, 200, 96, 64),
            Color.White);

        // "Press SPACE" indicator
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 80, 320, 160, 30), new Color(60, 120, 60));
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 76, 324, 152, 22), new Color(80, 160, 80));
    }

    private void DrawWinScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 150));

        // Victory banner
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 180, 150, 360, 50), Color.Gold);
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 176, 154, 352, 42), new Color(255, 215, 0));

        // Apple icon
        _spriteBatch.Draw(_appleTexture,
            new Rectangle(ScreenWidth / 2 - 24, 220, 48, 48),
            Color.White);

        // Score display bar
        int scoreWidth = Math.Min(_score * 2, 300);
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 150, 290, 300, 20), new Color(50, 50, 50));
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 150, 290, scoreWidth, 20), Color.LimeGreen);

        // Restart prompt
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 80, 340, 160, 24), new Color(60, 120, 60));
    }

    private void DrawGameOverScreen()
    {
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 150));

        // Game over banner
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 180, 150, 360, 50), Color.DarkRed);
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 176, 154, 352, 42), new Color(180, 30, 30));

        // Score display
        int scoreWidth = Math.Min(_score * 2, 300);
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 150, 240, 300, 20), new Color(50, 50, 50));
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 150, 240, scoreWidth, 20), Color.Orange);

        // Restart prompt
        _spriteBatch.Draw(_pixel, new Rectangle(ScreenWidth / 2 - 80, 300, 160, 24), new Color(60, 120, 60));
    }

    protected override void UnloadContent()
    {
        _pixel?.Dispose();
        base.UnloadContent();
    }
}
