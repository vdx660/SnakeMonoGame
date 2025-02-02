using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

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

        private string[] _menuItems = { "Human Player", "AI Player", "AI Player Advanced" };

        private int _selectedIndex = 0;

        private bool _isAI = false;

        private Vector2 _previousDirection;

        private KeyboardState _previousState;

        private int _score = 0;

        // Add these variables at the top of your Game1 class
        private int _highScore = 0;
        private string _highScoreFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MySnakeGame", "highscore.txt");

        private GameMode _gameMode;

        // Base accessible space weight
        private double accessibleSpaceWeight = 0.1;

        // Variables to track length intervals
        private int previousLengthInterval = 0;

        public enum GameState
        {
            Menu,
            Playing,
            GameOver
        }
        public enum GameMode
        {
            Human,
            AI,
            AIAdvanced
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
            LoadHighScore();
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
                    if (_gameMode == GameMode.Human)
                    {
                        HandleInput();
                    }
                    else if (_gameMode == GameMode.AI)
                    {
                        UpdateAI(gameTime); // Basic AI
                    }
                    else if (_gameMode == GameMode.AIAdvanced)
                    {
                        UpdateAIAdvanced(gameTime); // Advanced AI
                    }

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
                if (_score > _highScore)
                {
                    _highScore = _score;
                    SaveHighScore();
                }

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

            // Display the High Score below the title
            string highScoreText = $"High Score: {_highScore}";
            Vector2 highScoreSize = _menuFont.MeasureString(highScoreText);
            _spriteBatch.DrawString(_menuFont, highScoreText, new Vector2(
                (GraphicsDevice.Viewport.Width - highScoreSize.X) / 2,
                150), Color.Yellow);

            for (int i = 0; i < _menuItems.Length; i++)
            {
                Color color = i == _selectedIndex ? Color.Yellow : Color.White;
                Vector2 size = _menuFont.MeasureString(_menuItems[i]);
                _spriteBatch.DrawString(_menuFont, _menuItems[i], new Vector2(
                    (GraphicsDevice.Viewport.Width - size.X) / 2,
                    250 + i * 40), color);
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

            // Display current score
            string scoreText = $"Score: {_score}";
            _spriteBatch.DrawString(_menuFont, scoreText, new Vector2(10, 10), Color.White);

            // Display high score
            string highScoreText = $"High Score: {_highScore}";
            _spriteBatch.DrawString(_menuFont, highScoreText, new Vector2(10, 40), Color.Yellow);

            // Display the game mode
            string modeText = "";
            if (_gameMode == GameMode.Human)
                modeText = "Mode: Human Player";
            else if (_gameMode == GameMode.AI)
                modeText = "Mode: AI Player";
            else if (_gameMode == GameMode.AIAdvanced)
                modeText = "Mode: AI Player Advanced";

            Vector2 modeSize = _menuFont.MeasureString(modeText);
            _spriteBatch.DrawString(_menuFont, modeText, new Vector2(
                GraphicsDevice.Viewport.Width - modeSize.X - 10,
                10), Color.White);

        }

        private void DrawGameOver()
        {
            string gameOverText = "Game Over!";
            Vector2 gameOverSize = _menuFont.MeasureString(gameOverText);
            _spriteBatch.DrawString(_menuFont, gameOverText, new Vector2(
                (GraphicsDevice.Viewport.Width - gameOverSize.X) / 2,
                (GraphicsDevice.Viewport.Height - gameOverSize.Y) / 2 - 60), Color.Red);

            // Display the final score
            string scoreText = $"Your Score: {_score}";
            Vector2 scoreSize = _menuFont.MeasureString(scoreText);
            _spriteBatch.DrawString(_menuFont, scoreText, new Vector2(
                (GraphicsDevice.Viewport.Width - scoreSize.X) / 2,
                (GraphicsDevice.Viewport.Height - scoreSize.Y) / 2 - 20), Color.White);

            // Display the high score
            string highScoreText = $"High Score: {_highScore}";
            Vector2 highScoreSize = _menuFont.MeasureString(highScoreText);
            _spriteBatch.DrawString(_menuFont, highScoreText, new Vector2(
                (GraphicsDevice.Viewport.Width - highScoreSize.X) / 2,
                (GraphicsDevice.Viewport.Height - highScoreSize.Y) / 2 + 20), Color.Yellow);

            string instructions = "Press Enter to return to Menu";
            Vector2 instructionsSize = _menuFont.MeasureString(instructions);
            _spriteBatch.DrawString(_menuFont, instructions, new Vector2(
                (GraphicsDevice.Viewport.Width - instructionsSize.X) / 2,
                (GraphicsDevice.Viewport.Height - instructionsSize.Y) / 2 + 60), Color.White);
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
                // Set the game mode based on the selected index
                switch (_selectedIndex)
                {
                    case 0:
                        _gameMode = GameMode.Human;
                        break;
                    case 1:
                        _gameMode = GameMode.AI;
                        break;
                    case 2:
                        _gameMode = GameMode.AIAdvanced;
                        break;
                }

                ResetGame();
                _currentGameState = GameState.Playing;
            }

            // Update previous keyboard state
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

                accessibleSpaceWeight = 0.1 + (_snake.Count / 450.0);
            }
            else
            {
                // Remove the tail segment if food not eaten
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
            // Existing reset logic...
            _snake.Clear();
            _direction = new Vector2(1, 0);
            _timer = 0f;
            _interval = 100f;
            _gameOver = false;
            _score = 0;
            previousLengthInterval = 0;
            accessibleSpaceWeight = 0.1; // Reset to base weight

            // Initialize the snake
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

        private void UpdateAIAdvanced(GameTime gameTime)
        {
            Vector2 head = _snake[0];

            // Get all possible directions
            List<Vector2> allDirections = new List<Vector2>
    {
        new Vector2(0, -1), // Up
        new Vector2(0, 1),  // Down
        new Vector2(-1, 0), // Left
        new Vector2(1, 0)   // Right
    };

            // List to store possible moves with their scores
            List<(Vector2 direction, double score)> moves = new List<(Vector2, double)>();

            foreach (var dir in allDirections)
            {
                double score = EvaluateMove(dir);
                if (score > double.NegativeInfinity)
                {
                    moves.Add((dir, score));
                }
            }

            if (moves.Count > 0)
            {
                // Select the move with the highest score
                var bestMove = moves.OrderByDescending(m => m.score).First();
                _direction = bestMove.direction;
            }
            else
            {
                // No valid moves; proceed in current direction or handle game over scenario
            }
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

        private bool IsFutureCollision(Vector2 direction)
        {
            Vector2 newHeadPosition = _snake[0] + direction * _size;

            // Check wall collisions
            if (newHeadPosition.X < 0 || newHeadPosition.X >= _graphics.PreferredBackBufferWidth ||
                newHeadPosition.Y < 0 || newHeadPosition.Y >= _graphics.PreferredBackBufferHeight)
            {
                return true;
            }

            // Check self-collision (excluding tail if not growing)
            for (int i = 0; i < _snake.Count - 1; i++)
            {
                if (_snake[i] == newHeadPosition)
                    return true;
            }

            return false;
        }

        private int FloodFill(Vector2 position, int depth, HashSet<Vector2> visited)
        {
            // Base cases
            if (depth <= 0 || visited.Contains(position))
                return 0;

            // Check for wall collisions
            if (position.X < 0 || position.X >= _graphics.PreferredBackBufferWidth ||
                position.Y < 0 || position.Y >= _graphics.PreferredBackBufferHeight)
            {
                return 0;
            }

            // Check for self-collision
            if (_snake.Contains(position))
                return 0;

            visited.Add(position);

            int count = 1; // Count this tile

            // Explore neighboring tiles
            Vector2[] directions = new Vector2[]
            {
                new Vector2(0, -1), // Up
                new Vector2(0, 1),  // Down
                new Vector2(-1, 0), // Left
                new Vector2(1, 0)   // Right
            };

            foreach (var dir in directions)
            {
                Vector2 newPosition = position + dir * _size;
                count += FloodFill(newPosition, depth - 1, visited);
            }

            return count;
        }

        private double EvaluateMove(Vector2 direction)
        {
            Vector2 newHeadPosition = _snake[0] + direction * _size;

            if (IsFutureCollision(direction))
                return double.NegativeInfinity; // Invalid move

            // Calculate distance to food
            double distanceToFood = Vector2.Distance(newHeadPosition, _foodPosition);

            // Perform flood fill to assess accessible space
            HashSet<Vector2> visited = new HashSet<Vector2>();
            int accessibleSpace = FloodFill(newHeadPosition, 50, visited);

            // Combine factors into a score
            double score = -distanceToFood + accessibleSpaceWeight * accessibleSpace;

            return score;
        }

        private void LoadHighScore()
        {
            try
            {
                if (File.Exists(_highScoreFilePath))
                {
                    string scoreText = File.ReadAllText(_highScoreFilePath);
                    if (int.TryParse(scoreText, out int loadedScore))
                    {
                        _highScore = loadedScore;
                    }
                }
                else
                {
                    _highScore = 0;
                }
            }
            catch (Exception ex)
            {
                // Handle errors (e.g., log the error)
                _highScore = 0;
            }
        }

        private void SaveHighScore()
        {
            try
            {
                string directory = Path.GetDirectoryName(_highScoreFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(_highScoreFilePath, _highScore.ToString());
            }
            catch (Exception ex)
            {
                // Handle errors
            }
        }

    }
}
