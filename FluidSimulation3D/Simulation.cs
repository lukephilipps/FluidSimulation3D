using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FluidSimulation3D
{
    public class Simulation : Game
    {
        // needs to be the same as the GroupSizeXYX defined in the compute shader
        const int ComputeGroupSizeXYZ = 8;

        // Display info
        const int ResolutionX = 1280;
        const int ResolutionY = 720;

        // Buffer constants
        const int Read = 0;
        const int Write = 1;
        const int Phi_N_Hat = 0;
        const int Phi_N_1_Hat = 1;

        // Texture 
        const int Width = 128;
        const int Height = 128;
        const int Depth = 128;

        // Simulation speed
        const float TimeStep = 0.1f;

        // Used for Jacobi compute
        public int iterations = 10;

        // Simulation parameters
        public float vorticityStrength = 1.0f;
        public float densityAmount = 1.0f;
        public float densityDissipation = 0.999f;
        public float densityBuoyancy = 1.0f;
        public float densityWeight = 0.00125f;
        public float temperatureAmount = 10.0f;
        public float temperatureDissipation = 0.995f;
        public float velocityDissipation = 0.995f;
        public float inputRadius = 0.04f;
        public Vector4 inputPos = new Vector4(0.5f, 0.1f, 0.5f, 0.0f);

        // Compute shaders
        Effect _applyAdvection;
        Effect _applyImpulse;
        Effect _computeBorders;
        Effect _applyBuoyancy;
        Effect _computeDivergence;
        Effect _computeJacobi;
        Effect _computeProjection;
        Effect _computeVorticity;
        Effect _computeConfinement;

        Vector3 _size;

        // Buffers
        StructuredBuffer[] _density, _velocity, _pressure, _temperature, _phi;
        StructuredBuffer _temp3f, _obstacles;

        // Rendering
        GraphicsDeviceManager _graphics;
        Effect _smokeRaymarcher;
        Effect _planeShader;
        Effect _glassShader;
        VertexBuffer _cubeVertices;
        VertexBuffer _planeVertices;
        SpriteBatch _spriteBatch;
        SpriteFont _textFont;
        float _rotation;
        Matrix _world, _view, _projection;
        Vector3 _camPos;

        public Simulation()
        {
            Content.RootDirectory = "Content";

            _graphics = new GraphicsDeviceManager(this);
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            _graphics.IsFullScreen = false;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = ResolutionX;
            _graphics.PreferredBackBufferHeight = ResolutionY;
            _graphics.ApplyChanges();

            IsMouseVisible = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _world = Matrix.Identity;
            _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(50), (float)ResolutionX / (float)ResolutionY, 0.1f, 1000f);

            _applyAdvection = Content.Load<Effect>("ComputeShaders/ApplyAdvection");
            _applyImpulse = Content.Load<Effect>("ComputeShaders/ApplyImpulse");
            _applyBuoyancy = Content.Load<Effect>("ComputeShaders/ApplyBuoyancy");
            _computeBorders = Content.Load<Effect>("ComputeShaders/ComputeBorders");
            _computeDivergence = Content.Load<Effect>("ComputeShaders/ComputeDivergence");
            _computeJacobi = Content.Load<Effect>("ComputeShaders/ComputeJacobi");
            _computeProjection = Content.Load<Effect>("ComputeShaders/ComputeProjection");
            _computeConfinement = Content.Load<Effect>("ComputeShaders/ComputeConfinement");
            _computeVorticity = Content.Load<Effect>("ComputeShaders/ComputeVorticity");
            _smokeRaymarcher = Content.Load<Effect>("Rendering/Raymarcher");
            _glassShader = Content.Load<Effect>("Rendering/Glass");
            _planeShader = Content.Load<Effect>("Rendering/Simple3D");
            _planeShader.Parameters["MyTexture"].SetValue(Content.Load<Texture2D>("Rendering/FloorTiles"));
            _textFont = Content.Load<SpriteFont>("Text/TextFont");

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _size = new Vector3(Width, Height, Depth);

            int bufferSize = Width * Height * Depth;

            _density = new StructuredBuffer[2];
            _density[Read] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            _density[Write] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            _temperature = new StructuredBuffer[2];
            _temperature[Read] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            _temperature[Write] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            _phi = new StructuredBuffer[2];
            _phi[Read] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            _phi[Write] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            _velocity = new StructuredBuffer[2];
            _velocity[Read] = new StructuredBuffer(GraphicsDevice, typeof(Vector3), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            _velocity[Write] = new StructuredBuffer(GraphicsDevice, typeof(Vector3), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            _pressure = new StructuredBuffer[2];
            _pressure[Read] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            _pressure[Write] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            _obstacles = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            _temp3f = new StructuredBuffer(GraphicsDevice, typeof(Vector3), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            ComputeObstacles();

            _cubeVertices = CreateCubeVertices();
            _planeVertices = CreatePlaneVertices();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

            float dt_update = (float)gameTime.ElapsedGameTime.TotalSeconds;

            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Right))
                _rotation += dt_update;
            if (keyboardState.IsKeyDown(Keys.Left))
                _rotation -= dt_update;

            if (keyboardState.IsKeyDown(Keys.A))
                inputPos -= new Vector4(0.01f, 0f, 0f, 0f);
            if (keyboardState.IsKeyDown(Keys.D))
                inputPos += new Vector4(0.01f, 0f, 0f, 0f);
            if (keyboardState.IsKeyDown(Keys.W))
                inputPos -= new Vector4(0f, 0f, 0.01f, 0f);
            if (keyboardState.IsKeyDown(Keys.S))
                inputPos += new Vector4(0f, 0f, 0.01f, 0f);
            if (keyboardState.IsKeyDown(Keys.Q))
                inputPos -= new Vector4(0f, 0.01f, 0f, 0f);
            if (keyboardState.IsKeyDown(Keys.E))
                inputPos += new Vector4(0f, 0.01f, 0f, 0f);

            inputPos = Vector4.Clamp(inputPos, new Vector4(0.1f), new Vector4(0.9f));

            _camPos = new Vector3((float)Math.Sin(_rotation) * 2f, 1f, (float)Math.Cos(_rotation) * 2f);
            _view = Matrix.CreateLookAt(_camPos, Vector3.Zero, new Vector3(0, 1, 0));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            float dt = TimeStep;

            ApplyAdvection(dt, temperatureDissipation, _temperature);

            if (false)
            {
                ApplyAdvection(dt, 1.0f, _density[Read], _phi[Phi_N_1_Hat], 1.0f);
                ApplyAdvection(dt, 1.0f, _density[Phi_N_1_Hat], _phi[Phi_N_Hat], -1.0f);
                ApplyAdvectionMacCormack(dt, densityDissipation, _density);
            }
            else ApplyAdvection(dt, densityDissipation, _density);

            ApplyAdvectionVelocity(dt);
            ApplyBuoyancy(dt);

            ApplyImpulse(dt, densityAmount, _density);
            ApplyImpulse(dt, temperatureAmount, _temperature);

            ComputeVorticityConfinement(dt);

            ComputeDivergence();
            ComputePressure();
            ComputeProjection();

            GraphicsDevice.Clear(Color.Black);
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.BlendState = BlendState.Additive;

            DrawPlane();
            DrawFluidRaymarched();

            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            DrawGlass();
            DrawText();

            base.Draw(gameTime);
        }

        void ComputeObstacles()
        {
            _computeBorders.CurrentTechnique = _computeBorders.Techniques[0];
            _computeBorders.Parameters["_Size"].SetValue(_size);
            _computeBorders.Parameters["_Write"].SetValue(_obstacles);

            foreach (var pass in _computeBorders.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }
        }

        void ApplyAdvection(float dt, float dissipation, StructuredBuffer[] buffer, float forward = 1.0f)
        {
            _applyAdvection.CurrentTechnique = _applyAdvection.Techniques[1];
            _applyAdvection.Parameters["_Size"].SetValue(_size);
            _applyAdvection.Parameters["_DeltaTime"].SetValue(dt);
            _applyAdvection.Parameters["_Dissipate"].SetValue(dissipation);
            _applyAdvection.Parameters["_Forward"].SetValue(forward);
            _applyAdvection.Parameters["_Read1f"].SetValue(buffer[Read]);
            _applyAdvection.Parameters["_Write1f"].SetValue(buffer[Write]);
            _applyAdvection.Parameters["_Velocity"].SetValue(_velocity[Read]);
            _applyAdvection.Parameters["_Obstacles"].SetValue(_obstacles);

            foreach (var pass in _applyAdvection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }

            Swap(buffer);
        }

        void ApplyAdvection(float dt, float dissipation, StructuredBuffer read, StructuredBuffer write, float forward = 0.1f)
        {
            _applyAdvection.CurrentTechnique = _applyAdvection.Techniques[1];
            _applyAdvection.Parameters["_Size"].SetValue(_size);
            _applyAdvection.Parameters["_DeltaTime"].SetValue(dt);
            _applyAdvection.Parameters["_Dissipate"].SetValue(dissipation);
            _applyAdvection.Parameters["_Forward"].SetValue(forward);
            _applyAdvection.Parameters["_Read1f"].SetValue(read);
            _applyAdvection.Parameters["_Write1f"].SetValue(write);
            _applyAdvection.Parameters["_Velocity"].SetValue(_velocity[Read]);
            _applyAdvection.Parameters["_Obstacles"].SetValue(_obstacles);

            foreach (var pass in _applyAdvection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }
        }

        void ApplyAdvectionMacCormack(float dt, float dissipation, StructuredBuffer[] buffer)
        {
            _applyAdvection.CurrentTechnique = _applyAdvection.Techniques[2];
            _applyAdvection.Parameters["_Size"].SetValue(_size);
            _applyAdvection.Parameters["_DeltaTime"].SetValue(dt);
            _applyAdvection.Parameters["_Dissipate"].SetValue(dissipation);
            _applyAdvection.Parameters["_Forward"].SetValue(1.0f);
            _applyAdvection.Parameters["_Read1f"].SetValue(buffer[Read]);
            _applyAdvection.Parameters["_Write1f"].SetValue(buffer[Write]);
            _applyAdvection.Parameters["_Phi_n_1_hat"].SetValue(_phi[Phi_N_1_Hat]);
            _applyAdvection.Parameters["_Phi_n_hat"].SetValue(_phi[Phi_N_Hat]);
            _applyAdvection.Parameters["_Velocity"].SetValue(_velocity[Read]);
            _applyAdvection.Parameters["_Obstacles"].SetValue(_obstacles);

            foreach (var pass in _applyAdvection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }

            Swap(buffer);
        }

        void ApplyAdvectionVelocity(float dt)
        {
            _applyAdvection.CurrentTechnique = _applyAdvection.Techniques[0];
            _applyAdvection.Parameters["_Size"].SetValue(_size);
            _applyAdvection.Parameters["_DeltaTime"].SetValue(dt);
            _applyAdvection.Parameters["_Dissipate"].SetValue(velocityDissipation);
            _applyAdvection.Parameters["_Forward"].SetValue(1.0f);
            _applyAdvection.Parameters["_Read3f"].SetValue(_velocity[Read]);
            _applyAdvection.Parameters["_Write3f"].SetValue(_velocity[Write]);
            _applyAdvection.Parameters["_Velocity"].SetValue(_velocity[Read]);
            _applyAdvection.Parameters["_Obstacles"].SetValue(_obstacles);

            foreach (var pass in _applyAdvection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }

            Swap(_velocity);
        }

        void ApplyBuoyancy(float dt)
        {
            _applyBuoyancy.CurrentTechnique = _applyBuoyancy.Techniques[0];
            _applyBuoyancy.Parameters["_Size"].SetValue(_size);
            _applyBuoyancy.Parameters["_Up"].SetValue(new Vector4(0f, 1f, 0f, 0f));
            _applyBuoyancy.Parameters["_Buoyancy"].SetValue(densityBuoyancy);
            _applyBuoyancy.Parameters["_Weight"].SetValue(densityWeight);
            _applyBuoyancy.Parameters["_DeltaTime"].SetValue(dt);
            _applyBuoyancy.Parameters["_Write"].SetValue(_velocity[Write]);
            _applyBuoyancy.Parameters["_Velocity"].SetValue(_velocity[Read]);
            _applyBuoyancy.Parameters["_Density"].SetValue(_density[Read]);
            _applyBuoyancy.Parameters["_Temperature"].SetValue(_temperature[Read]);

            foreach (var pass in _applyBuoyancy.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }

            Swap(_velocity);
        }

        void ApplyImpulse(float dt, float amount, StructuredBuffer[] buffer)
        {
            _applyImpulse.CurrentTechnique = _applyImpulse.Techniques[0];
            _applyImpulse.Parameters["_Size"].SetValue(_size);
            _applyImpulse.Parameters["_Radius"].SetValue(inputRadius);
            _applyImpulse.Parameters["_Amount"].SetValue(amount);
            _applyImpulse.Parameters["_DeltaTime"].SetValue(dt);
            _applyImpulse.Parameters["_Pos"].SetValue(inputPos);
            _applyImpulse.Parameters["_Read"].SetValue(buffer[Read]);
            _applyImpulse.Parameters["_Write"].SetValue(buffer[Write]);

            foreach (var pass in _applyImpulse.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }

            Swap(buffer);
        }

        void ComputeVorticityConfinement(float dt)
        {
            _computeVorticity.CurrentTechnique = _computeVorticity.Techniques[0];
            _computeVorticity.Parameters["_Size"].SetValue(_size);
            _computeVorticity.Parameters["_Write"].SetValue(_temp3f);
            _computeVorticity.Parameters["_Velocity"].SetValue(_velocity[Read]);
            foreach (var pass in _computeVorticity.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }

            _computeConfinement.CurrentTechnique = _computeConfinement.Techniques[0];
            _computeConfinement.Parameters["_Size"].SetValue(_size);
            _computeConfinement.Parameters["_DeltaTime"].SetValue(dt);
            _computeConfinement.Parameters["_Epsilon"].SetValue(vorticityStrength);
            _computeConfinement.Parameters["_Write"].SetValue(_velocity[Write]);
            _computeConfinement.Parameters["_Read"].SetValue(_velocity[Read]);
            _computeConfinement.Parameters["_Vorticity"].SetValue(_temp3f);
            foreach (var pass in _computeConfinement.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }

            Swap(_velocity);
        }

        void ComputeDivergence()
        {
            _computeDivergence.CurrentTechnique = _computeDivergence.Techniques[0];
            _computeDivergence.Parameters["_Size"].SetValue(_size);
            _computeDivergence.Parameters["_Write"].SetValue(_temp3f);
            _computeDivergence.Parameters["_Velocity"].SetValue(_velocity[Read]);
            _computeDivergence.Parameters["_Obstacles"].SetValue(_obstacles);

            foreach (var pass in _computeDivergence.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }
        }

        void ComputePressure()
        {
            _computeJacobi.CurrentTechnique = _computeJacobi.Techniques[0];
            _computeJacobi.Parameters["_Size"].SetValue(_size);
            _computeJacobi.Parameters["_Divergence"].SetValue(_temp3f);
            _computeJacobi.Parameters["_Obstacles"].SetValue(_obstacles);

            for (int i = 0; i < iterations; i++)
            {
                foreach (var pass in _computeJacobi.CurrentTechnique.Passes)
                {
                    _computeJacobi.Parameters["_Write"].SetValue(_pressure[Write]);
                    _computeJacobi.Parameters["_Pressure"].SetValue(_pressure[Read]);
                    pass.ApplyCompute();
                    GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
                    Swap(_pressure);
                }
            }
        }

        void ComputeProjection()
        {
            _computeProjection.CurrentTechnique = _computeProjection.Techniques[0];
            _computeProjection.Parameters["_Size"].SetValue(_size);
            _computeProjection.Parameters["_Obstacles"].SetValue(_obstacles);
            _computeProjection.Parameters["_Pressure"].SetValue(_pressure[Read]);
            _computeProjection.Parameters["_Velocity"].SetValue(_velocity[Read]);
            _computeProjection.Parameters["_Write"].SetValue(_velocity[Write]);

            foreach (var pass in _computeProjection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)_size.X / ComputeGroupSizeXYZ, (int)_size.Y / ComputeGroupSizeXYZ, (int)_size.Z / ComputeGroupSizeXYZ);
            }

            Swap(_velocity);
        }

        void Swap(StructuredBuffer[] buffer)
        {
            StructuredBuffer tmp = buffer[Read];
            buffer[Read] = buffer[Write];
            buffer[Write] = tmp;
        }

        private void DrawFluidRaymarched()
        {
            _smokeRaymarcher.Parameters["World"].SetValue(_world);
            _smokeRaymarcher.Parameters["View"].SetValue(_view);
            _smokeRaymarcher.Parameters["Projection"].SetValue(_projection);
            _smokeRaymarcher.Parameters["_Density"].SetValue(_density[Read]);
            _smokeRaymarcher.Parameters["_Scale"].SetValue(new Vector3(1, 1, 1));
            _smokeRaymarcher.Parameters["_Size"].SetValue(_size);
            _smokeRaymarcher.Parameters["CamPos"].SetValue(_camPos);

            foreach (var pass in _smokeRaymarcher.CurrentTechnique.Passes)
            {
                pass.Apply();

                GraphicsDevice.SetVertexBuffer(_cubeVertices);
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
            }
        }

        private void DrawPlane()
        {
            _planeShader.Parameters["World"].SetValue(Matrix.Identity);
            _planeShader.Parameters["View"].SetValue(_view);
            _planeShader.Parameters["Projection"].SetValue(_projection);

            foreach (var pass in _planeShader.CurrentTechnique.Passes)
            {
                pass.Apply();

                GraphicsDevice.SetVertexBuffer(_planeVertices);
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 6);
            }
        }

        private void DrawGlass()
        {
            _glassShader.Parameters["World"].SetValue(Matrix.Identity);
            _glassShader.Parameters["View"].SetValue(_view);
            _glassShader.Parameters["Projection"].SetValue(_projection);

            foreach (var pass in _glassShader.CurrentTechnique.Passes)
            {
                pass.Apply();

                GraphicsDevice.SetVertexBuffer(_cubeVertices);
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
            }
        }

        private void DrawText()
        {
            string text = "WASDQE to move smoke\n";
            text += "Arrows to rotate";

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_textFont, text, new Vector2(30, 30), Color.White);
            _spriteBatch.End();
        }

        private VertexBuffer CreateCubeVertices()
        {
            var vertices = new VertexPositionTexture[36];
            Vector3 center = new Vector3(0.5f);

            // Forward face
            vertices[0] = new VertexPositionTexture(new Vector3(1, 1, 0) - center, new Vector2(1, 0));
            vertices[1] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(0, 0));
            vertices[2] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(0, 1));
            vertices[3] = new VertexPositionTexture(new Vector3(1, 0, 0) - center, new Vector2(1, 1));
            vertices[4] = new VertexPositionTexture(new Vector3(1, 1, 0) - center, new Vector2(1, 0));
            vertices[5] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(0, 1));

            // Backward face
            vertices[6] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 1));
            vertices[7] = new VertexPositionTexture(new Vector3(0, 1, 1) - center, new Vector2(0, 0));
            vertices[8] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(1, 0));
            vertices[9] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 1));
            vertices[10] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(1, 0));
            vertices[11] = new VertexPositionTexture(new Vector3(1, 0, 1) - center, new Vector2(1, 1));

            // Top face
            vertices[12] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(1, 0));
            vertices[13] = new VertexPositionTexture(new Vector3(0, 1, 1) - center, new Vector2(0, 0));
            vertices[14] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(0, 1));
            vertices[15] = new VertexPositionTexture(new Vector3(1, 1, 0) - center, new Vector2(1, 1));
            vertices[16] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(1, 0));
            vertices[17] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(0, 1));

            // Bottom face
            vertices[18] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(0, 1));
            vertices[19] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 0));
            vertices[20] = new VertexPositionTexture(new Vector3(1, 0, 1) - center, new Vector2(1, 0));
            vertices[21] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(0, 1));
            vertices[22] = new VertexPositionTexture(new Vector3(1, 0, 1) - center, new Vector2(1, 0));
            vertices[23] = new VertexPositionTexture(new Vector3(1, 0, 0) - center, new Vector2(1, 1));

            // Left Face
            vertices[24] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(1, 0));
            vertices[25] = new VertexPositionTexture(new Vector3(0, 1, 1) - center, new Vector2(0, 0));
            vertices[26] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 1));
            vertices[27] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(1, 1));
            vertices[28] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(1, 0));
            vertices[29] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 1));

            // Right Face
            vertices[30] = new VertexPositionTexture(new Vector3(1, 0, 1) - center, new Vector2(0, 1));
            vertices[31] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(0, 0));
            vertices[32] = new VertexPositionTexture(new Vector3(1, 1, 0) - center, new Vector2(1, 0));
            vertices[33] = new VertexPositionTexture(new Vector3(1, 0, 1) - center, new Vector2(0, 1));
            vertices[34] = new VertexPositionTexture(new Vector3(1, 1, 0) - center, new Vector2(1, 0));
            vertices[35] = new VertexPositionTexture(new Vector3(1, 0, 0) - center, new Vector2(1, 1));

            var vb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionTexture), vertices.Length, BufferUsage.WriteOnly);
            vb.SetData(vertices);

            return vb;
        }

        private VertexBuffer CreatePlaneVertices()
        {
            var vertices = new VertexPositionTexture[6];

            vertices[0] = new VertexPositionTexture(new Vector3(0, 1, 0) - new Vector3(2f), new Vector2(0f, 1f));
            vertices[1] = new VertexPositionTexture(new Vector3(0, 1, 4) - new Vector3(2f), new Vector2(0f, 0f));
            vertices[2] = new VertexPositionTexture(new Vector3(4, 1, 4) - new Vector3(2f), new Vector2(1f, 0f));
            vertices[3] = new VertexPositionTexture(new Vector3(0, 1, 0) - new Vector3(2f), new Vector2(0f, 1f));
            vertices[4] = new VertexPositionTexture(new Vector3(4, 1, 4) - new Vector3(2f), new Vector2(1f, 0f));
            vertices[5] = new VertexPositionTexture(new Vector3(4, 1, 0) - new Vector3(2f), new Vector2(1f, 1f));

            var vb = new VertexBuffer(GraphicsDevice, typeof(VertexPositionTexture), vertices.Length, BufferUsage.WriteOnly);
            vb.SetData(vertices);

            return vb;
        }
    }
}
