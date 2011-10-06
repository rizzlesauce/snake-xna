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

namespace snake {
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SnakeGame : Microsoft.Xna.Framework.Game {
        GraphicsDeviceManager graphics;

        // 2D graphics
        SpriteBatch spriteBatch;
        Texture2D blockTexture;
        Vector2 blockSize;
        bool show2DSnake;
        const bool defaultShow2DSnake = false;
        SpriteFont overlayFont;
        Vector2 fontPosition;

        // Game
        bool showOverlay;
        const bool defaultShowOverlay = true;
        const bool defaultPaused = false;
        bool paused = defaultPaused;

        // Input
        KeyboardState oldKeyboardState;

        // Snake data
        Vector2 snakePosition;
        /// <summary>
        /// In pixels per second.
        /// </summary>
        float snakeSpeed;
        const float defaultSnakeSpeed = 100.0f;
        enum SnakeDirection { Up, Down, Left, Right };
        SnakeDirection snakeDirection;
        const float initialSnakeLength = 50.0f;
        float snakeLength;
        float snakeGrowLength;
        const float defaultSnakeGrowLength = 30.0f;
        List<Vector2> snakePositions;
        List<bool> disconnectedToPreviousPoint;
        VertexPositionNormalTexture[] snakeVertices;
        VertexBuffer snakeVertexBuffer;
        IndexBuffer snakeIndexBuffer;
        int[] snakeIndices;
        bool ignoreSnakeCollisions;
        const bool defaultIgnoreSnakeCollisions = false;

        // 3D world and camera
        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        BasicEffect basicEffect;
        RasterizerState rasterizerState;
        float fieldOfViewAngle;
        const float defaultFieldOfViewAngle = 45.0f;
        float nearPlaneDistance;
        float farPlaneDistance;
        Vector3 cameraPosition;
        Vector3 cameraTarget;
        Vector3 cameraUpVector;
        enum CameraType { Angled, FromAbove };
        const CameraType defaultCameraType = CameraType.FromAbove;
        CameraType cameraType;
        float cameraPitch;
        float cameraDistance;

        // Playing arena data
        VertexPositionNormalTexture[] arenaVertices;
        VertexBuffer arenaVertexBuffer;
        IndexBuffer arenaIndexBuffer;
        int[] arenaIndices = { 0, 1, 1, 2, 2, 3, 3, 0 };
        enum ArenaBoundaryType { WrapAround, Collision, NoBoundary };
        const ArenaBoundaryType defaultArenaBoundaryType = ArenaBoundaryType.WrapAround;
        ArenaBoundaryType arenaBoundaryType = defaultArenaBoundaryType;
        
        // Goal data
        Vector2 goalPosition;
        Vector2 goalUpperLeft;
        Vector2 goalUpperRight;
        Vector2 goalLowerLeft;
        Vector2 goalLowerRight;
        LineSegment2 goalLeftSide;
        LineSegment2 goalTopSide;
        LineSegment2 goalRightSide;
        LineSegment2 goalBottomSide;
        VertexPositionNormalTexture[] goalVertices;
        VertexBuffer goalVertexBuffer;
        IndexBuffer goalIndexBuffer;
        int[] goalIndices = { 0, 1, 1, 2, 2, 3, 3, 0 };

        public SnakeGame() {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = true;
            blockSize = new Vector2(6, 6);
            snakePositions = new List<Vector2>();
            disconnectedToPreviousPoint = new List<bool>();
            goalPosition = new Vector2();
            fieldOfViewAngle = defaultFieldOfViewAngle;

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            // Initialize view
            basicEffect = new BasicEffect(graphics.GraphicsDevice);

            rasterizerState = new RasterizerState();
            rasterizerState.FillMode = FillMode.Solid;
            rasterizerState.CullMode = CullMode.None;

            nearPlaneDistance = 1.0f;
            farPlaneDistance = MeterToWorldUnit(50.0f);

            SetCameraType(defaultCameraType);

            worldMatrix = Matrix.Identity;

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                    MathHelper.ToRadians(fieldOfViewAngle),
                    (float)GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height, nearPlaneDistance,
                    farPlaneDistance);

            basicEffect.World = worldMatrix;
            basicEffect.Projection = projectionMatrix;

            // lighting
            basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            basicEffect.SpecularPower = 5.0f;
            basicEffect.Alpha = 1.0f;

            basicEffect.LightingEnabled = true;
            if (basicEffect.LightingEnabled) {
                basicEffect.DirectionalLight0.Enabled = true;
                if (basicEffect.DirectionalLight0.Enabled) {
                    // x direction
                    basicEffect.DirectionalLight0.DiffuseColor = new Vector3(1, 0, 0); // range is 0 to 1
                    basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-1, 0, 0));
                    basicEffect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight1.Enabled = true;
                if (basicEffect.DirectionalLight1.Enabled) {
                    // y direction
                    basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0, 0.75f, 0);
                    basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    basicEffect.DirectionalLight1.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight2.Enabled = true;
                if (basicEffect.DirectionalLight2.Enabled) {
                    // z direction
                    basicEffect.DirectionalLight2.DiffuseColor = new Vector3(0, 0, 0.5f);
                    basicEffect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(0, 0, -1));
                    basicEffect.DirectionalLight2.SpecularColor = Vector3.One;
                }
            }

            base.Initialize();
        }

        private void SetCameraType(CameraType type) {
            cameraType = type;
            if (cameraType == CameraType.FromAbove) {
                // Create a camera position centered above the floor looking down
                cameraDistance = (graphics.GraphicsDevice.Viewport.Height / 2.0f) / 
                        (float)Math.Tan(MathHelper.ToRadians(fieldOfViewAngle / 2.0f));
                cameraPitch = 90.0f;
            } else {
                // Create a camera positioned above and in front of the floor, looking at the center of the floor
                cameraDistance = (graphics.GraphicsDevice.Viewport.Height / 2.0f) / 
                        (float)Math.Tan(MathHelper.ToRadians(fieldOfViewAngle / 2.0f)) * 2.0f;
                cameraPitch = 45.0f;
            }
            UpdateViewMatrix();
        }

        private void UpdateViewMatrix() {
            float arenaCenterX = graphics.GraphicsDevice.Viewport.Width / 2.0f;
            float arenaCenterZ = graphics.GraphicsDevice.Viewport.Height / 2.0f;

            Vector3 cameraDown = new Vector3(0, 0, cameraDistance);
            Quaternion cameraRotation = Quaternion.CreateFromYawPitchRoll(0, MathHelper.ToRadians(-cameraPitch), 0);
            cameraPosition = Vector3.Transform(cameraDown, cameraRotation);
            cameraPosition = new Vector3(arenaCenterX, cameraPosition.Y, arenaCenterZ + cameraPosition.Z);

            cameraTarget = new Vector3(arenaCenterX, 0, arenaCenterZ);
            
            // Create the camera up vector
            Vector3 lookVector = cameraPosition - cameraTarget;
            lookVector.Normalize();
            // Get the vector rotated 90 degrees around the x axis
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(0, MathHelper.ToRadians(-90.0f), 0);
            cameraUpVector = Vector3.Normalize(Vector3.Transform(lookVector, rotation));

            viewMatrix = Matrix.CreateLookAt(cameraPosition, cameraTarget, cameraUpVector);
            basicEffect.View = viewMatrix;
        }

        /// <summary>
        /// Convert unit in meters to 3D world units.
        /// </summary>
        /// <param name="meters"></param>
        /// <returns></returns>
        private float MeterToWorldUnit(float meters) {
            return meters * 1000.0f;
        }

        /// <summary>
        /// Convert 3D world units to meters.
        /// </summary>
        /// <param name="units"></param>
        /// <returns></returns>
        private float WorldUnitToMeter(float worldUnits) {
            return worldUnits / 1000.0f;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Set up the arena
            arenaVertices = new VertexPositionNormalTexture[4];
            arenaVertices[0] = new VertexPositionNormalTexture(new Vector3(0, 0, graphics.GraphicsDevice.Viewport.Height),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
            arenaVertices[1] = new VertexPositionNormalTexture(new Vector3(0, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
            arenaVertices[2] = new VertexPositionNormalTexture(new Vector3(graphics.GraphicsDevice.Viewport.Width, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
            arenaVertices[3] = new VertexPositionNormalTexture(new Vector3(graphics.GraphicsDevice.Viewport.Width, 0,
                    graphics.GraphicsDevice.Viewport.Height),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
            arenaVertexBuffer = new VertexBuffer(graphics.GraphicsDevice, typeof(VertexPositionNormalTexture),
                    arenaVertices.Length, BufferUsage.None);
            arenaVertexBuffer.SetData<VertexPositionNormalTexture>(arenaVertices);
            arenaIndexBuffer = new IndexBuffer(graphics.GraphicsDevice, typeof(int), arenaIndices.Length, BufferUsage.None);
            arenaIndexBuffer.SetData<int>(arenaIndices);

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the basic white texture for snake and goal
            blockTexture = new Texture2D(GraphicsDevice, (int)blockSize.X, (int)blockSize.Y, false, SurfaceFormat.Color);
            Int32[] pixels = new Int32[blockTexture.Width * blockTexture.Height];
            for (int i = 0; i < blockTexture.Width * blockTexture.Height; ++i) {
                pixels[i] = 0xFFFFFF;
            }
            blockTexture.SetData<Int32>(pixels, 0, blockTexture.Width * blockTexture.Height);

            overlayFont = Content.Load<SpriteFont>("OverlayFont");
            fontPosition = new Vector2(5, 5);
            
            snakeSpeed = defaultSnakeSpeed;
            snakeGrowLength = defaultSnakeGrowLength;
            fieldOfViewAngle = defaultFieldOfViewAngle;
            show2DSnake = defaultShow2DSnake;
            showOverlay = defaultShowOverlay;
            ignoreSnakeCollisions = defaultIgnoreSnakeCollisions;
            arenaBoundaryType = defaultArenaBoundaryType;
            SetCameraType(defaultCameraType);
            
            InitializeSnake();
        }

        /// <summary>
        /// Reposition the goal to a random location.
        /// </summary>
        protected void RepositionGoal() {
            // TODO: make sure it gets repositioned somewhere the snake is not at
            Random rand = new Random(System.DateTime.Now.Millisecond);

            goalPosition.X = rand.Next(0, GraphicsDevice.Viewport.Width - (int)blockSize.X);
            goalPosition.Y = rand.Next(0, GraphicsDevice.Viewport.Height - (int)blockSize.Y);

            // Check if snake reached a goal
            goalUpperLeft = new Vector2(goalPosition.X - blockSize.X / 2.0f, goalPosition.Y - blockSize.Y / 2.0f);
            goalUpperRight = new Vector2(goalPosition.X + blockSize.X / 2.0f, goalPosition.Y - blockSize.Y / 2.0f);
            goalLowerLeft = new Vector2(goalPosition.X - blockSize.X / 2.0f, goalPosition.Y + blockSize.Y / 2.0f);
            goalLowerRight = new Vector2(goalPosition.X + blockSize.X / 2.0f, goalPosition.Y + blockSize.Y / 2.0f);
            goalLeftSide = new LineSegment2(goalUpperLeft, goalLowerLeft);
            goalTopSide = new LineSegment2(goalUpperLeft, goalUpperRight);
            goalRightSide = new LineSegment2(goalUpperRight, goalLowerRight);
            goalBottomSide = new LineSegment2(goalLowerLeft, goalLowerRight);

            // Update 3D data
            goalVertices = new VertexPositionNormalTexture[4];
            goalVertices[0] = new VertexPositionNormalTexture(Snake2DTo3DVector(goalUpperLeft),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
            goalVertices[1] = new VertexPositionNormalTexture(Snake2DTo3DVector(goalUpperRight),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
            goalVertices[2] = new VertexPositionNormalTexture(Snake2DTo3DVector(goalLowerRight),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
            goalVertices[3] = new VertexPositionNormalTexture(Snake2DTo3DVector(goalLowerLeft),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
            goalVertexBuffer = new VertexBuffer(graphics.GraphicsDevice, typeof(VertexPositionNormalTexture),
                    goalVertices.Length, BufferUsage.None);
            goalVertexBuffer.SetData<VertexPositionNormalTexture>(goalVertices);
            goalIndexBuffer = new IndexBuffer(graphics.GraphicsDevice, typeof(int), goalIndices.Length, BufferUsage.None);
            goalIndexBuffer.SetData<int>(goalIndices);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Make a small snake and set a goal position.
        /// </summary>
        private void InitializeSnake() {
            snakePosition = new Vector2(200, 200);
            snakeDirection = SnakeDirection.Right;
            snakePositions.Clear();
            disconnectedToPreviousPoint.Clear();
            snakePositions.Add(new Vector2(snakePosition.X - snakeLength, snakePosition.Y));
            disconnectedToPreviousPoint.Add(false);
            snakeLength = initialSnakeLength;
            paused = false;
            Update3DSnakeData();
            RepositionGoal();
        }

        /// <summary>
        /// Determine whether a key was just pressed.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool IsKeyReleased(Keys key, KeyboardState state) {
            return oldKeyboardState.IsKeyDown(key) && state.IsKeyUp(key);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) {
                this.Exit();
            }

            SnakeDirection oldDirection = snakeDirection;
            Vector2 oldSnakePosition = snakePosition;

            // Get keyboard input
            KeyboardState state = Keyboard.GetState();
            if (IsKeyReleased(Keys.Space, state)) {
                TogglePaused();
            }
            if (IsKeyReleased(Keys.R, state)) {
                InitializeSnake();
            }
            if (!paused) {
                if (state.IsKeyDown(Keys.Left)) {
                    if (snakeDirection != SnakeDirection.Right) {
                        snakeDirection = SnakeDirection.Left;
                    }
                }
                if (state.IsKeyDown(Keys.Right)) {
                    if (snakeDirection != SnakeDirection.Left) {
                        snakeDirection = SnakeDirection.Right;
                    }
                }
                if (state.IsKeyDown(Keys.Up)) {
                    if (snakeDirection != SnakeDirection.Down) {
                        snakeDirection = SnakeDirection.Up;
                    }
                }
                if (state.IsKeyDown(Keys.Down)) {
                    if (snakeDirection != SnakeDirection.Up) {
                        snakeDirection = SnakeDirection.Down;
                    }
                }
            }
            if (IsKeyReleased(Keys.G, state)) {
                GrowSnake();
            }
            if (state.IsKeyDown(Keys.OemPlus)) {
                snakeSpeed += 5;
            }
            if (state.IsKeyDown(Keys.OemMinus)) {
                snakeSpeed -= 5;
                if (snakeSpeed < 0) {
                    snakeSpeed = 0;
                }
            }
            if (state.IsKeyDown(Keys.OemCloseBrackets)) {
                snakeLength += 1.0f;
            }
            if (state.IsKeyDown(Keys.OemOpenBrackets)) {
                snakeLength -= 1.0f;
                if (snakeLength < initialSnakeLength) {
                    snakeLength = initialSnakeLength;
                }
            }
            if (state.IsKeyDown(Keys.W)) {
                cameraPitch += 1.0f;
                if (cameraPitch > 360.0f) {
                    cameraPitch -= 360.0f;
                }
                UpdateViewMatrix();
            }
            if (state.IsKeyDown(Keys.S)) {
                cameraPitch -= 1.0f;
                if (cameraPitch < 0) {
                    cameraPitch += 360.0f;
                }
                UpdateViewMatrix();
            }
            if (state.IsKeyDown(Keys.D)) {
                cameraDistance += 1.0f;
                UpdateViewMatrix();
            }
            if (state.IsKeyDown(Keys.A)) {
                cameraDistance -= 1.0f;
                if (cameraDistance < 0) {
                    cameraDistance = 0;
                }
                UpdateViewMatrix();
            }
            if (IsKeyReleased(Keys.V, state)) {
                if (cameraType == CameraType.FromAbove) {
                    SetCameraType(CameraType.Angled);
                } else {
                    SetCameraType(CameraType.FromAbove);
                }
            }
            if (IsKeyReleased(Keys.T, state)) {
                Toggle2DSnake();
            }
            if (IsKeyReleased(Keys.I, state)) {
                ToggleOverlay();
            }
            if (IsKeyReleased(Keys.C, state)) {
                ToggleIgnoreCollisions();
            }
            if (IsKeyReleased(Keys.B, state)) {
                if (arenaBoundaryType == ArenaBoundaryType.WrapAround) {
                    arenaBoundaryType = ArenaBoundaryType.Collision;
                } else if (arenaBoundaryType == ArenaBoundaryType.Collision) {
                    arenaBoundaryType = ArenaBoundaryType.NoBoundary;
                } else {
                    arenaBoundaryType = ArenaBoundaryType.WrapAround;
                }
            }
            if (state.IsKeyDown(Keys.Q) || state.IsKeyDown(Keys.Escape)) {
                Exit();
            }
            oldKeyboardState = state;

            if (!paused) {
                // Check if snake switched direction
                if (snakeDirection != oldDirection) {
                    // Add position to list of joints
                    snakePositions.Add(new Vector2(oldSnakePosition.X, oldSnakePosition.Y));
                    disconnectedToPreviousPoint.Add(false);
                }

                // Move the snake
                float displacement = (float)gameTime.ElapsedGameTime.TotalSeconds * snakeSpeed;
                switch (snakeDirection) {
                    case SnakeDirection.Up:
                        snakePosition.Y -= displacement;
                        break;
                    case SnakeDirection.Down:
                        snakePosition.Y += displacement;
                        break;
                    case SnakeDirection.Left:
                        snakePosition.X -= displacement;
                        break;
                    case SnakeDirection.Right:
                        snakePosition.X += displacement;
                        break;
                }

                bool hit = false;

                // Check if snake went out of bounds
                if (arenaBoundaryType == ArenaBoundaryType.Collision) {
                    if (snakePosition.X < 0 ||
                            snakePosition.X > graphics.GraphicsDevice.Viewport.Width - 1 ||
                            snakePosition.Y < 0 ||
                            snakePosition.Y > graphics.GraphicsDevice.Viewport.Height - 1) {
                        hit = true;
                    }
                } else if (arenaBoundaryType == ArenaBoundaryType.WrapAround) {
                    if (snakePosition.X < 0) {
                        oldSnakePosition = new Vector2(graphics.GraphicsDevice.Viewport.Width, snakePosition.Y);
                        snakePositions.Add(new Vector2(0, snakePosition.Y));
                        disconnectedToPreviousPoint.Add(false);
                        snakePositions.Add(oldSnakePosition);
                        disconnectedToPreviousPoint.Add(true);
                        snakePosition = new Vector2(graphics.GraphicsDevice.Viewport.Width + snakePosition.X, snakePosition.Y);
                    } else if (snakePosition.X > graphics.GraphicsDevice.Viewport.Width) {
                        oldSnakePosition = new Vector2(0, snakePosition.Y);
                        snakePositions.Add(new Vector2(graphics.GraphicsDevice.Viewport.Width, snakePosition.Y));
                        disconnectedToPreviousPoint.Add(false);
                        snakePositions.Add(oldSnakePosition);
                        disconnectedToPreviousPoint.Add(true);
                        snakePosition = new Vector2(snakePosition.X - graphics.GraphicsDevice.Viewport.Width, snakePosition.Y);
                    } else if (snakePosition.Y < 0) {
                        oldSnakePosition = new Vector2(snakePosition.X, graphics.GraphicsDevice.Viewport.Height);
                        snakePositions.Add(new Vector2(snakePosition.X, 0));
                        disconnectedToPreviousPoint.Add(false);
                        snakePositions.Add(oldSnakePosition);
                        disconnectedToPreviousPoint.Add(true);
                        snakePosition = new Vector2(snakePosition.X, graphics.GraphicsDevice.Viewport.Height + snakePosition.Y);
                    } else if (snakePosition.Y > graphics.GraphicsDevice.Viewport.Height) {
                        oldSnakePosition = new Vector2(snakePosition.X, 0);
                        snakePositions.Add(new Vector2(snakePosition.X, graphics.GraphicsDevice.Viewport.Height));
                        disconnectedToPreviousPoint.Add(false);
                        snakePositions.Add(oldSnakePosition);
                        disconnectedToPreviousPoint.Add(true);
                        snakePosition = new Vector2(snakePosition.X, snakePosition.Y - graphics.GraphicsDevice.Viewport.Height);
                    }
                }

                // Trim the snake tail, making sure the length of the snake is correct.
                // Go backwards from current point through each point, adding up the displacement.
                // Stop when you reach the right length, removing history of non relevant points.
                float length = 0;
                Vector2 lastPosition = snakePosition;
                Vector2 vector = new Vector2();
                int i;
                
                for (i = snakePositions.Count - 1; i >= 0 && length <= snakeLength; --i) {
                    Vector2 position = snakePositions[i];

                    if (i == snakePositions.Count - 1 || !disconnectedToPreviousPoint[i + 1]) {
                        vector = lastPosition - position;
                        length += vector.Length();
                    }

                    lastPosition = position;
                }
                if (length > snakeLength) {
                    // Modify the tail end position
                    float changeAmount = length - snakeLength;
                    vector.Normalize();
                    vector = vector * changeAmount;
                    Vector2 newPosition = snakePositions[i + 1] + vector;
                    snakePositions[i + 1] = newPosition;
                }
                if (i >= 0) {
                    snakePositions.RemoveRange(0, i + 1);
                    disconnectedToPreviousPoint.RemoveRange(0, i + 1);
                    // Make sure the first element (the tail end point) is set to false
                    disconnectedToPreviousPoint[0] = false;
                }

                // Check if snake intersected with itself
                LineSegment2 recentMovement = new LineSegment2(oldSnakePosition, snakePosition);
                if (!hit && !ignoreSnakeCollisions) {
                    if (snakePositions.Count > 1) {
                        lastPosition = snakePositions[snakePositions.Count - 2];
                        i = snakePositions.Count - 3;
                        for (; i >= 0 && !hit; --i) {
                            Vector2 position = snakePositions[i];
                            LineSegment2 snakeSegment = new LineSegment2(position, lastPosition);

                            if (!disconnectedToPreviousPoint[i + 1]) {
                                hit = LineSegment2.SegmentsIntersect(recentMovement, snakeSegment);
                            }

                            lastPosition = position;
                        }
                    }
                }
                if (hit) {
                    InitializeSnake();
                } else {
                    // Check if snake reached a goal
                    if (LineSegment2.SegmentsIntersect(recentMovement, goalLeftSide) ||
                            LineSegment2.SegmentsIntersect(recentMovement, goalRightSide) ||
                            LineSegment2.SegmentsIntersect(recentMovement, goalBottomSide) ||
                            LineSegment2.SegmentsIntersect(recentMovement, goalLeftSide)) {
                        // intersection
                        GrowSnake();
                        RepositionGoal();
                    }

                    Update3DSnakeData();
                }
            }
            
            base.Update(gameTime);
        }

        private void Toggle2DSnake() {
            show2DSnake = !show2DSnake;
        }

        private void ToggleOverlay() {
            showOverlay = !showOverlay;
        }

        private void ToggleIgnoreCollisions() {
            ignoreSnakeCollisions = !ignoreSnakeCollisions;
        }

        private void TogglePaused() {
            paused = !paused;
        }

        private int CountDisconnectedPoints() {
            int count = 0;
            for (int i = 0; i < disconnectedToPreviousPoint.Count; ++i) {
                if (disconnectedToPreviousPoint[i]) {
                    ++count;
                }
            }
            return count;
        }

        private void Update3DSnakeData() {
            // Update the snake's 3D data
            snakeVertices = new VertexPositionNormalTexture[snakePositions.Count + 1];
            snakeIndices = new int[(snakePositions.Count - CountDisconnectedPoints()) * 2];

            snakeVertices[0] = SnakePositionTo3DVertex(snakePosition);

            int index = 0;

            for (int j = 0; j < snakePositions.Count; ++j) {
                // Snake vertices go in order from head to tail
                snakeVertices[j + 1] = SnakePositionTo3DVertex(snakePositions[snakePositions.Count - 1 - j]);
                // Snake indices go (0,1),(1,2),(2,3)...
                if (j == 0 || !disconnectedToPreviousPoint[snakePositions.Count - j]) {
                    if (index + 1 >= snakeIndices.Length) {
                        System.Console.WriteLine("number of verts:" + snakeVertices.Length +
                                "\nnumber of indices:" + snakeIndices.Length +
                                "\nnumber of disconnects:" + CountDisconnectedPoints());
                        Exit();
                    }
                    snakeIndices[index++] = j;
                    snakeIndices[index++] = j + 1;
                }
            }
            snakeVertexBuffer = new VertexBuffer(graphics.GraphicsDevice, typeof(VertexPositionNormalTexture),
                    snakeVertices.Length, BufferUsage.None);
            snakeVertexBuffer.SetData<VertexPositionNormalTexture>(snakeVertices);
            snakeIndexBuffer = new IndexBuffer(graphics.GraphicsDevice, typeof(int), snakeIndices.Length, BufferUsage.None);
            snakeIndexBuffer.SetData<int>(snakeIndices);
        }

        /// <summary>
        /// Determine whether a value is small enough to be considered zero.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsPracticallyZero(float value) {
            return (value >= -0.0001f && value <= 0.0001f);
        }
 
        private VertexPositionNormalTexture SnakePositionTo3DVertex(Vector2 position) {
            return new VertexPositionNormalTexture(Snake2DTo3DVector(position),
                    new Vector3(0, 1, 0),
                    new Vector2(0, 0));
        }

        private Vector3 Snake2DTo3DVector(Vector2 position) {
            return new Vector3(position.X, 0, position.Y);
        }

        /// <summary>
        /// This will tell the snake to start growing.
        /// </summary>
        protected void GrowSnake() {
            snakeLength += snakeGrowLength;
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
                float width, Color color, Vector2 point1, Vector2 point2) {
            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = (Vector2.Distance(point1, point2) / blank.Height);// +blank.Height;
       
            batch.Draw(blank, point1, null, color,
                       angle, Vector2.Zero, new Vector2(length, width),
                       SpriteEffects.None, 0);
        }

        private string CreateToggleStatement(string description, string key, bool value) {
            return description + "(\"" + key + "\"):" + (value ? "Yes" : "No");
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw sprites
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            if (showOverlay) {
                // Draw The Overlay
                string output = "";
                output += CreateToggleStatement("Show Info", "I", showOverlay) + "\n";
                output += CreateToggleStatement("Show 2D Snake", "T", show2DSnake) + "\n";
                output += CreateToggleStatement("Ignore Snake Collisions", "C", ignoreSnakeCollisions) + "\n";
                output += CreateToggleStatement("Paused", "<SPACE>", paused) + "\n";
                output += "Arena Boundary Type(\"B\"):" + arenaBoundaryType + "\n";
                output += "Snake Speed(\"+\"/\"-\"):" + snakeSpeed + "px/sec\n";
                output += "Grow Snake (\"G\")\n";
                output += "View Type(\"V\"):" + cameraType + "\n";
                output += "Camera Pitch(\"W\"/\"S\"):" + cameraPitch + "deg\n";
                output += "Camera Distance(\"A\"/\"D\"):" + (int)cameraDistance + "\n";
                output += "Snake Length(\"[\"/\"]\"):" + snakeLength + "\n";
                output += "Snake Position(" + (int)snakePosition.X + "," + (int)snakePosition.Y + ")" + "\n";
                output += "Snake Direction:" + snakeDirection + "\n";
                output += "Reset Snake(\"R\")";

                // Find the center of the string
                Vector2 textSize = overlayFont.MeasureString(output);
                //Vector2 fontOrigin = textSize / 2;
                //Vector2 fontPosition = new Vector2(graphics.GraphicsDevice.Viewport.Width - 10 - textSize.X,
                //10);
                // Draw the string
                spriteBatch.DrawString(overlayFont, output, fontPosition, Color.Purple,
                        0, Vector2.Zero, 1.0f, SpriteEffects.None, 0.5f);
            }

            if (show2DSnake) {
                // Draw the 2D goal
                spriteBatch.Draw(blockTexture, new Vector2(goalPosition.X - (blockSize.X / 2),
                        goalPosition.Y - (blockSize.Y / 2)), Color.White);

                // Draw the 2D snake
                Vector2 point1 = snakePosition;
                Vector2 point2;
                for (int i = snakePositions.Count - 1; i >= 0; --i) {
                    if (i == snakePositions.Count - 1 || !disconnectedToPreviousPoint[i + 1]) {
                        point2 = snakePositions[i];
                        DrawLine(spriteBatch, blockTexture, 1, Color.White, point1, point2);
                    }
                    point1 = snakePositions[i];
                }
            }
            spriteBatch.End();

            // Draw 3D world
            graphics.GraphicsDevice.RasterizerState = rasterizerState;
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes) {
                pass.Apply();

                // Draw snake
                graphics.GraphicsDevice.SetVertexBuffer(snakeVertexBuffer);
                graphics.GraphicsDevice.Indices = snakeIndexBuffer;
                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.LineList,
                    0, 0,
                    snakeVertices.Length,
                    0,
                    snakeIndices.Length / 2
                );

                // Draw arena
                graphics.GraphicsDevice.SetVertexBuffer(arenaVertexBuffer);
                graphics.GraphicsDevice.Indices = arenaIndexBuffer;
                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.LineList,
                    0, 0,
                    arenaVertices.Length,
                    0,
                    arenaIndices.Length / 2
                );

                // Draw goal
                graphics.GraphicsDevice.SetVertexBuffer(goalVertexBuffer);
                graphics.GraphicsDevice.Indices = goalIndexBuffer;
                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.LineList,
                    0, 0,
                    goalVertices.Length,
                    0,
                    goalIndices.Length / 2
                );
            }
            
            base.Draw(gameTime);
        }
    }
}
