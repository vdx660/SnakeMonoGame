using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;

namespace Snake
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;


        // Snake properties
        private List<Vector2> _snake = new List<Vector2>();
        private Vector2 _direction = new Vector2(1, 0); // Start moving to the right
        private float _timer = 0f;
        private float _interval = 100f; // Snake moves every 100 milliseconds
        private int _size = 25; // Size of the snake segment
        private bool _gameOver = false;

        // Food properties
        private Vector2 _foodPosition;
        private Texture2D _foodTexture;

        // Textures
        private Texture2D _snakeTexture;

        // Random generator
        private Random _random = new Random();

        private GameState _currentGameState = GameState.Menu;

        private SpriteFont _menuFont;
        private string[] _menuItems = { "Human Player", "AI Player" };
        private int _selectedIndex = 0;

        private bool _isAI = false;

        private Vector2 _previousDirection;

        private KeyboardState _previousState;

        private int _score = 0;

        public enum GameState
        {
            Menu,
            Playing,
            GameOver
        }


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set the preferred window size
            _graphics.PreferredBackBufferWidth = 1024; // Desired width
            _graphics.PreferredBackBufferHeight = 768; // Desired height
            _graphics.ApplyChanges();

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += Window_ClientSizeChanged; ;
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _previousState = Keyboard.GetState();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Initialize snake texture
            _snakeTexture = new Texture2D(GraphicsDevice, 1, 1);
            _snakeTexture.SetData(new Color[] { Color.Green });

            // Initialize food texture
            _foodTexture = new Texture2D(GraphicsDevice, 1, 1);
            _foodTexture.SetData(new Color[] { Color.Red });

            // Load the menu font
            _menuFont = Content.Load<SpriteFont>("MenuFont");
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState state = Keyboard.GetState();

            if (_currentGameState == GameState.Menu)
            {
                UpdateMenu();
            }
            else if (_currentGameState == GameState.Playing)
            {
                if (_gameOver)
                {
                    _currentGameState = GameState.GameOver;
                }
                else
                {
                    if (_isAI)
                        UpdateAI(gameTime);
                    else
                        HandleInput();

                    _timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                    if (_timer >= _interval)
                    {
                        _timer = 0f;
                        MoveSnake();
                    }
                }
            }
            else if (_currentGameState == GameState.GameOver)
            {
                // Wait for the player to press Enter to return to the menu
                if (state.IsKeyDown(Keys.Enter) && !_previousState.IsKeyDown(Keys.Enter))
                {
                    _currentGameState = GameState.Menu;
                }
            }

            // Update the previous keyboard state
            _previousState = state;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            if (_currentGameState == GameState.Menu)
            {
                DrawMenu();
            }
            else if (_currentGameState == GameState.Playing)
            {
                DrawGame();
            }
            else if (_currentGameState == GameState.GameOver)
            {
                DrawGameOver();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }


        private void DrawMenu()
        {
            string title = "Snake Game";
            Vector2 titleSize = _menuFont.MeasureString(title);
            _spriteBatch.DrawString(_menuFont, title, new Vector2(
                (GraphicsDevice.Viewport.Width - titleSize.X) / 2,
                100), Color.White);

            for (int i = 0; i < _menuItems.Length; i++)
            {
                Color color = i == _selectedIndex ? Color.Yellow : Color.White;
                Vector2 size = _menuFont.MeasureString(_menuItems[i]);
                _spriteBatch.DrawString(_menuFont, _menuItems[i], new Vector2(
                    (GraphicsDevice.Viewport.Width - size.X) / 2,
                    200 + i * 40), color);
            }
        }

        private void DrawGame()
        {
            // Draw snake
            foreach (Vector2 segment in _snake)
            {
                _spriteBatch.Draw(_snakeTexture, new Rectangle(segment.ToPoint(), new Point(_size, _size)), Color.Green);
            }

            // Draw food
            _spriteBatch.Draw(_foodTexture, new Rectangle(_foodPosition.ToPoint(), new Point(_size, _size)), Color.Red);

            // Draw score (optional)
            _spriteBatch.DrawString(_menuFont, $"Score: {_score}", new Vector2(10, 10), Color.White);
        }


        private void DrawGameOver()
        {
            string gameOverText = "Game Over!";
            Vector2 size = _menuFont.MeasureString(gameOverText);
            _spriteBatch.DrawString(_menuFont, gameOverText, new Vector2(
                (GraphicsDevice.Viewport.Width - size.X) / 2,
                (GraphicsDevice.Viewport.Height - size.Y) / 2 - 20), Color.Red);

            string instructions = "Press Enter to return to Menu";
            size = _menuFont.MeasureString(instructions);
            _spriteBatch.DrawString(_menuFont, instructions, new Vector2(
                (GraphicsDevice.Viewport.Width - size.X) / 2,
                (GraphicsDevice.Viewport.Height - size.Y) / 2 + 20), Color.White);
        }

        private void UpdateMenu()
        {
            KeyboardState state = Keyboard.GetState();

            if (state.IsKeyDown(Keys.Up) && !_previousState.IsKeyDown(Keys.Up))
            {
                _selectedIndex = (_selectedIndex - 1 + _menuItems.Length) % _menuItems.Length;
            }
            else if (state.IsKeyDown(Keys.Down) && !_previousState.IsKeyDown(Keys.Down))
            {
                _selectedIndex = (_selectedIndex + 1) % _menuItems.Length;
            }
            else if (state.IsKeyDown(Keys.Enter) && !_previousState.IsKeyDown(Keys.Enter))
            {
                _isAI = _selectedIndex == 1; // Index 1 is AI Player
                ResetGame();
                _currentGameState = GameState.Playing;
            }

            // Update the previous keyboard state
            _previousState = state;
        }

        private void HandleInput()
        {
            KeyboardState state = Keyboard.GetState();

            if (state.IsKeyDown(Keys.Up) && _direction != new Vector2(0, 1))
                _direction = new Vector2(0, -1);
            else if (state.IsKeyDown(Keys.Down) && _direction != new Vector2(0, -1))
                _direction = new Vector2(0, 1);
            else if (state.IsKeyDown(Keys.Left) && _direction != new Vector2(1, 0))
                _direction = new Vector2(-1, 0);
            else if (state.IsKeyDown(Keys.Right) && _direction != new Vector2(-1, 0))
                _direction = new Vector2(1, 0);
        }

        private void MoveSnake()
        {
            Vector2 newHeadPosition = _snake[0] + _direction * _size;

            // Check for collisions
            if (IsCollision(newHeadPosition))
            {
                _gameOver = true;
                return;
            }

            // Move the snake
            _snake.Insert(0, newHeadPosition);

            // Check if food is eaten
            if (newHeadPosition == _foodPosition)
            {
                SpawnFood();
                _score += 10;
            }
            else
            {
                // Remove the tail segment
                _snake.RemoveAt(_snake.Count - 1);
            }
        }

        private void SpawnFood()
        {
            int maxX = (_graphics.PreferredBackBufferWidth / _size);
            int maxY = (_graphics.PreferredBackBufferHeight / _size);

            _foodPosition = new Vector2(
                _random.Next(0, maxX) * _size,
                _random.Next(0, maxY) * _size
            );

            // Ensure food doesn't spawn on the snake
            while (_snake.Contains(_foodPosition))
            {
                _foodPosition = new Vector2(
                    _random.Next(0, maxX) * _size,
                    _random.Next(0, maxY) * _size
                );
            }
        }


        private void ResetGame()
        {
            _snake.Clear();
            _direction = new Vector2(1, 0);
            _timer = 0f;
            _interval = 100f;
            _gameOver = false;
            _score = 0;

            // Add the initial snake segment
            _snake.Add(new Vector2(
                (_graphics.PreferredBackBufferWidth / 2) / _size * _size,
                (_graphics.PreferredBackBufferHeight / 2) / _size * _size));

            SpawnFood();
        }


        /// <summary>
        /// Update AI movement
        /// </summary>
        /// <param name="gameTime"></param>
        private void UpdateAI(GameTime gameTime)
        {
            Vector2 head = _snake[0];

            // List of possible directions: Up, Down, Left, Right
            List<Vector2> possibleDirections = new List<Vector2>
            {
                new Vector2(0, -1), // Up
                new Vector2(0, 1),  // Down
                new Vector2(-1, 0), // Left
                new Vector2(1, 0)   // Right
            };

            // Prioritize directions towards the food
            possibleDirections = GetPrioritizedDirections(head, _foodPosition);

            // Find a safe direction
            foreach (var dir in possibleDirections)
            {
                Vector2 newHeadPosition = head + dir * _size;

                if (!IsCollision(newHeadPosition))
                {
                    // Safe to move in this direction
                    _direction = dir;
                    return;
                }
            }

            // If no safe directions, keep current direction (may result in collision)
            // Alternatively, could implement logic to move back or stay in place
        }

        private List<Vector2> GetPrioritizedDirections(Vector2 head, Vector2 target)
        {
            List<Vector2> directions = new List<Vector2>();

            // Determine horizontal and vertical directions towards the target
            if (target.X < head.X)
                directions.Add(new Vector2(-1, 0)); // Left
            else if (target.X > head.X)
                directions.Add(new Vector2(1, 0));  // Right

            if (target.Y < head.Y)
                directions.Add(new Vector2(0, -1)); // Up
            else if (target.Y > head.Y)
                directions.Add(new Vector2(0, 1));  // Down

            // Add the remaining directions
            List<Vector2> allDirections = new List<Vector2>
            {
                new Vector2(0, -1), // Up
                new Vector2(0, 1),  // Down
                new Vector2(-1, 0), // Left
                new Vector2(1, 0)   // Right
            };

            // Add directions not already in the list
            foreach (var dir in allDirections)
            {
                if (!directions.Contains(dir))
                    directions.Add(dir);
            }

            return directions;
        }


        private bool IsCollision(Vector2 position)
        {
            // Check wall collisions
            if (position.X < 0 || position.X >= _graphics.PreferredBackBufferWidth ||
                position.Y < 0 || position.Y >= _graphics.PreferredBackBufferHeight)
            {
                return true;
            }

            // Check self-collision (exclude the tail if it will move)
            for (int i = 0; i < _snake.Count - 1; i++)
            {
                if (_snake[i] == position)
                    return true;
            }

            return false;
        }
    }
}
