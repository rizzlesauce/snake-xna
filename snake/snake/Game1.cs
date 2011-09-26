using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace snake
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D snakeTexture;
        Vector2 snakePosition;
        Vector2 goalPosition;
        Vector2 blockSize;
        float snakeSpeed = 100; // pixel per second
        enum Direction {Up, Down, Left, Right};
        Direction snakeDirection = Direction.Right;
        float snakeLength;
        float growAmount;
        List<Vector2> snakePositions;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = true;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            blockSize = new Vector2(6, 6);
            snakeLength = 50;
            growAmount = 30;
            snakePositions = new List<Vector2>();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            snakeTexture = new Texture2D(GraphicsDevice, (int)blockSize.X, (int)blockSize.Y, false, SurfaceFormat.Color);
            Int32[] pixels = new Int32[snakeTexture.Width * snakeTexture.Height];
            for (int i = 0; i < snakeTexture.Width * snakeTexture.Height; ++i) {
                pixels[i] = 0xFFFFFF;
            }
            snakeTexture.SetData<Int32>(pixels, 0, snakeTexture.Width * snakeTexture.Height);

            snakePosition = new Vector2(200, 200);
            snakePositions.Add(new Vector2(snakePosition.X - snakeLength, snakePosition.Y));

            goalPosition = new Vector2();
            RepositionGoal();
        }

        /// <summary>
        /// Reposition the goal to a random location.
        /// </summary>
        protected void RepositionGoal()
        {
            // TODO: make sure it gets repositioned somewhere the snake is not at
            Random rand = new Random(System.DateTime.Now.Millisecond);

            goalPosition.X = rand.Next(0, GraphicsDevice.DisplayMode.Width - (int)blockSize.X);
            goalPosition.Y = rand.Next(0, GraphicsDevice.DisplayMode.Height - (int)blockSize.Y);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            Direction oldDirection = snakeDirection;

            // Get keyboard input
            KeyboardState state = Keyboard.GetState();
            if (state.IsKeyDown(Keys.Left))
            {
                if (snakeDirection != Direction.Right)
                {
                    snakeDirection = Direction.Left;
                }
            }
            if (state.IsKeyDown(Keys.Right))
            {
                if (snakeDirection != Direction.Left)
                {
                    snakeDirection = Direction.Right;
                }
            }
            if (state.IsKeyDown(Keys.Up))
            {
                if (snakeDirection != Direction.Down)
                {
                    snakeDirection = Direction.Up;
                }
            }
            if (state.IsKeyDown(Keys.Down))
            {
                if (snakeDirection != Direction.Up)
                {
                    snakeDirection = Direction.Down;
                }
            }
            if (state.IsKeyDown(Keys.G))
            {
                GrowSnake();
            }
            if (state.IsKeyDown(Keys.OemPlus))
            {
                snakeSpeed += 5;
            }
            if (state.IsKeyDown(Keys.OemMinus))
            {
                snakeSpeed -= 5;
                if (snakeSpeed < 0)
                {
                    snakeSpeed = 0;
                }
            }
            if (state.IsKeyDown(Keys.Q) || state.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // Move the snake

            // Remember snake moves
            if (snakeDirection != oldDirection)
            {
                // add point to list of joints
                snakePositions.Add(new Vector2(snakePosition.X, snakePosition.Y));
            }

            float displacement = (float)gameTime.ElapsedGameTime.TotalSeconds * snakeSpeed;
            switch (snakeDirection)
            {
                case Direction.Up:
                    snakePosition.Y -= displacement;
                    break;
                case Direction.Down:
                    snakePosition.Y += displacement;
                    break;
                case Direction.Left:
                    snakePosition.X -= displacement;
                    break;
                case Direction.Right:
                    snakePosition.X += displacement;
                    break;
            }

            // Slide the snake tail
            // Keep the length of the snake the same
            // Go backwards from current point through each point, adding up the displacement.
            // Stop when you reach the right length, removing history of non relevant points.
            float length = 0;
            Vector2 lastPosition = snakePosition;
            int i = snakePositions.Count - 1;
            Vector2 vector = new Vector2();
            for (; i >= 0 && length <= snakeLength; --i)
            {
                Vector2 position = snakePositions[i];
                vector = lastPosition - position;
                length += vector.Length();

                lastPosition = position;
            }
            
            if (length > snakeLength)
            {
                // modify the point
                float changeAmount = length - snakeLength;
                vector.Normalize();
                vector = vector * changeAmount;
                Vector2 newPosition = snakePositions[i + 1] + vector;
                snakePositions[i + 1] = newPosition;
            }

            if (i >= 0)
            {
                snakePositions.RemoveRange(0, i + 1);
            }

            // Check if snake got the food; grow the snake
            // set the new snake length
            /*
            vector = snakePosition - snakePositions[snakePositions.Count - 1];
            Vector2 vector2 = goalPosition - snakePositions[snakePositions.Count - 1];
            // get unit vector
            vector.Normalize();
            vector = vector * (vector2.X + (snakeSize.X / 2.0f));
            if (vector.Y > vector2.Y - (snakeSize.Y / 2.0f) && vector.Y < vector2.Y + (snakeSize.Y / 2.0f))
            {
                // intersection
                RepositionGoal();
                GrowSnake();
            }
            */
            if (snakePosition.X <= goalPosition.X + blockSize.X && snakePosition.X + blockSize.X >= goalPosition.X &&
                    snakePosition.Y <= goalPosition.Y + blockSize.Y && snakePosition.Y + blockSize.Y >= goalPosition.Y)
            {
                RepositionGoal();
                GrowSnake();
            }
            
            // Check if snake intersected with itself
            //vector = snakePositions[snakePositions.Count - 1] - snakePositions[snakePositions.Count - 2];

            base.Update(gameTime);
        }

        /*
        protected bool LinesIntersect(Rectangle line1, Rectangle line2)
        {
            
           
            return false;
        }
        */

        /// <summary>
        /// This will tell the snake to start growing.
        /// </summary>
        protected void GrowSnake() {
            snakeLength += growAmount;
        }

        /// <summary>
        /// Draw a line using sprites.
        /// </summary>
        /// <param name="batch">The sprite batch</param>
        /// <param name="blank">The texture used to draw the line</param>
        /// <param name="width">The width of the line</param>
        /// <param name="color">The color of the line</param>
        /// <param name="point1">The start point of the line</param>
        /// <param name="point2">The end point of the line</param>
        protected void DrawLine(SpriteBatch batch, Texture2D blank,
              float width, Color color, Vector2 point1, Vector2 point2)
        {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2) / blank.Height;
           
            batch.Draw(blank, point1, null, color,
                       angle, Vector2.Zero, new Vector2(length, width),
                       SpriteEffects.None, 0);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw the snake

            // Draw a line through all the snake vectors
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            // Draw the goal
            spriteBatch.Draw(snakeTexture, goalPosition, Color.White);

            // Draw the snake
            Vector2 point1 = snakePosition;
            Vector2 point2;
            for (int i = snakePositions.Count - 1; i >= 0; --i)
            {
                point2 = snakePositions[i];
                DrawLine(spriteBatch, snakeTexture, 1, Color.White, point1, point2);
                point1 = snakePositions[i];
            }
            spriteBatch.End();
            
            base.Draw(gameTime);
        }
    }
}
