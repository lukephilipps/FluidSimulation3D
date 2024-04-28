using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FluidSimulation3D
{
    public class Simulation : Game
    {
        const int ResolutionX = 1280;
        const int ResolutionY = 720;

        const int ComputeGroupSizeXYZ = 8; // needs to be the same as the GroupSizeXYX defined in the compute shader

        const int READ = 0;
        const int WRITE = 1;
        const int PHI_N_HAT = 0;
        const int PHI_N_1_HAT = 1;

        // Texture 
        const int Width = 128;
        const int Height = 128;
        const int Depth = 128;
        const int TextureSize = 128;

        float timeStep = 0.1f;

        // Used for Jacobi compute
        public int m_iterations = 10;

        public float m_vorticityStrength = 1.0f;
        public float m_densityAmount = 1.0f;
        public float m_densityDissipation = 0.999f;
        public float m_densityBuoyancy = 1.0f;
        public float m_densityWeight = 0.0125f;
        public float m_temperatureAmount = 10.0f;
        public float m_temperatureDissipation = 0.995f;
        public float m_velocityDissipation = 0.995f;
        public float m_inputRadius = 0.04f;
        public Vector4 m_inputPos = new Vector4(0.5f, 0.1f, 0.5f, 0.0f);

        // Simulation
        Effect applyAdvection;
        Effect applyImpulse;
        Effect computeBorders;
        Effect applyBuoyancy;
        Effect computeDivergence;
        Effect computeJacobi;
        Effect computeProjection;
        Effect computeVorticity;
        Effect computeConfinement;

        Vector3 size;

        // Buffers
        StructuredBuffer[] density, velocity, pressure, temperature, phi;
        StructuredBuffer temp3f, obstacles;

        // Rendering
        Effect raytracer;
        GraphicsDeviceManager graphics;
        VertexBuffer cubeSlices;
        SpriteBatch spriteBatch;
        SpriteFont textFont;

        float rotation;

        //Skybox current_skybox;
        Matrix view, projection;
        Vector3 camPos;
        Model plane;
        Effect planeShader;

        public Simulation()
        {
            Content.RootDirectory = "Content";

            graphics = new GraphicsDeviceManager(this);
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            graphics.IsFullScreen = false;
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = ResolutionX;
            graphics.PreferredBackBufferHeight = ResolutionY;
            graphics.ApplyChanges();

            IsMouseVisible = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            //string[] skybox_textures2 = { "Skybox/skybox_negx", "Skybox/skybox_negx", "Skybox/skybox_negx", "Skybox/skybox_negx", "Skybox/skybox_negx", "Skybox/skybox_negx" };
            //current_skybox = new Skybox(skybox_textures2, Content, graphics.GraphicsDevice, "Skybox/cube", "Skybox/Skybox");

            plane = Content.Load<Model>("Skybox/Plane");
            planeShader = Content.Load<Effect>("Skybox/Simple3D");
            planeShader.Parameters["MyTexture"].SetValue(Content.Load<Texture2D>("Skybox/DarkBorders"));

            raytracer = Content.Load<Effect>("Raytracer");
            applyAdvection = Content.Load<Effect>("ComputeShaders/ApplyAdvection");
            applyImpulse = Content.Load<Effect>("ComputeShaders/ApplyImpulse");
            applyBuoyancy = Content.Load<Effect>("ComputeShaders/ApplyBuoyancy");
            computeBorders = Content.Load<Effect>("ComputeShaders/ComputeBorders");
            computeDivergence = Content.Load<Effect>("ComputeShaders/ComputeDivergence");
            computeJacobi = Content.Load<Effect>("ComputeShaders/ComputeJacobi");
            computeProjection = Content.Load<Effect>("ComputeShaders/ComputeProjection");
            computeConfinement = Content.Load<Effect>("ComputeShaders/ComputeConfinement");
            computeVorticity = Content.Load<Effect>("ComputeShaders/ComputeVorticity");
            textFont = Content.Load<SpriteFont>("TextFont");

            spriteBatch = new SpriteBatch(GraphicsDevice);

            size = new Vector3(Width, Height, Depth);

            int bufferSize = Width * Height * Depth;

            density = new StructuredBuffer[2];
            density[READ] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            density[WRITE] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            temperature = new StructuredBuffer[2];
            temperature[READ] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            temperature[WRITE] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            phi = new StructuredBuffer[2];
            phi[READ] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            phi[WRITE] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            velocity = new StructuredBuffer[2];
            velocity[READ] = new StructuredBuffer(GraphicsDevice, typeof(Vector3), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            velocity[WRITE] = new StructuredBuffer(GraphicsDevice, typeof(Vector3), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            pressure = new StructuredBuffer[2];
            pressure[READ] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);
            pressure[WRITE] = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            obstacles = new StructuredBuffer(GraphicsDevice, typeof(float), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            temp3f = new StructuredBuffer(GraphicsDevice, typeof(Vector3), bufferSize, BufferUsage.None, ShaderAccess.ReadWrite);

            ComputeObstacles();

            cubeSlices = CreateCubeSlices();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Right))
                rotation += dt;
            if (keyboardState.IsKeyDown(Keys.Left))
                rotation -= dt;
            if (keyboardState.IsKeyDown(Keys.A))
                m_inputPos -= new Vector4(0.01f, 0f, 0f, 0f);
            if (keyboardState.IsKeyDown(Keys.D))
                m_inputPos += new Vector4(0.01f, 0f, 0f, 0f);
            if (keyboardState.IsKeyDown(Keys.W))
                m_inputPos -= new Vector4(0f, 0f, 0.01f, 0f);
            if (keyboardState.IsKeyDown(Keys.S))
                m_inputPos += new Vector4(0f, 0f, 0.01f, 0f);
            if (keyboardState.IsKeyDown(Keys.Q))
                m_inputPos -= new Vector4(0f, 0.01f, 0f, 0f);
            if (keyboardState.IsKeyDown(Keys.E))
                m_inputPos += new Vector4(0f, 0.01f, 0f, 0f);

            m_inputPos = Vector4.Clamp(m_inputPos, new Vector4(0.03f), new Vector4(0.97f));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            float dt = timeStep;

            ApplyAdvection(dt, m_temperatureDissipation, temperature);

            if (false)
            {
                ApplyAdvection(dt, 1.0f, density[READ], phi[PHI_N_1_HAT], 1.0f);
                ApplyAdvection(dt, 1.0f, density[PHI_N_1_HAT], phi[PHI_N_HAT], -1.0f);
                ApplyAdvectionMacCormack(dt, m_densityDissipation, density);
            }
            else ApplyAdvection(dt, m_densityDissipation, density);


            ApplyAdvectionVelocity(dt);
            ApplyBuoyancy(dt);

            ApplyImpulse(dt, m_densityAmount, density);
            ApplyImpulse(dt, m_temperatureAmount, temperature);

            ComputeVorticityConfinement(dt);

            ComputeDivergence();
            ComputePressure();
            ComputeProjection();

            GraphicsDevice.Clear(Color.Black);

            //GraphicsDevice.BlendState = BlendState.Opaque;
            //GraphicsDevice.DepthStencilState = new DepthStencilState();

            // Draw skybox
            //RasterizerState originalRasterizerState = GraphicsDevice.RasterizerState;
            //RasterizerState rasterizerState = new RasterizerState();
            //rasterizerState.CullMode = CullMode.None;
            //GraphicsDevice.RasterizerState = rasterizerState;
            //current_skybox.Draw(view, projection, camPos);
            //GraphicsDevice.RasterizerState = originalRasterizerState;

            planeShader.Parameters["World"].SetValue(Matrix.Identity);
            planeShader.Parameters["View"].SetValue(view);
            planeShader.Parameters["Projection"].SetValue(projection);
            

            Draw3DTextureCube();
            DrawText();

            base.Draw(gameTime);
        }

        void ComputeObstacles()
        {
            computeBorders.CurrentTechnique = computeBorders.Techniques[0];
            computeBorders.Parameters["_Size"].SetValue(size);
            computeBorders.Parameters["_Write"].SetValue(obstacles);

            foreach (var pass in computeBorders.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }
        }

        void ApplyAdvection(float dt, float dissipation, StructuredBuffer[] buffer, float forward = 1.0f)
        {
            applyAdvection.CurrentTechnique = applyAdvection.Techniques[1];
            applyAdvection.Parameters["_Size"].SetValue(size);
            applyAdvection.Parameters["_DeltaTime"].SetValue(dt);
            applyAdvection.Parameters["_Dissipate"].SetValue(dissipation);
            applyAdvection.Parameters["_Forward"].SetValue(forward);
            applyAdvection.Parameters["_Read1f"].SetValue(buffer[READ]);
            applyAdvection.Parameters["_Write1f"].SetValue(buffer[WRITE]);
            applyAdvection.Parameters["_Velocity"].SetValue(velocity[READ]);
            applyAdvection.Parameters["_Obstacles"].SetValue(obstacles);

            foreach (var pass in applyAdvection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }

            Swap(buffer);
        }

        void ApplyAdvection(float dt, float dissipation, StructuredBuffer read, StructuredBuffer write, float forward = 0.1f)
        {
            applyAdvection.CurrentTechnique = applyAdvection.Techniques[1];
            applyAdvection.Parameters["_Size"].SetValue(size);
            applyAdvection.Parameters["_DeltaTime"].SetValue(dt);
            applyAdvection.Parameters["_Dissipate"].SetValue(dissipation);
            applyAdvection.Parameters["_Forward"].SetValue(forward);
            applyAdvection.Parameters["_Read1f"].SetValue(read);
            applyAdvection.Parameters["_Write1f"].SetValue(write);
            applyAdvection.Parameters["_Velocity"].SetValue(velocity[READ]);
            applyAdvection.Parameters["_Obstacles"].SetValue(obstacles);

            foreach (var pass in applyAdvection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }
        }

        void ApplyAdvectionMacCormack(float dt, float dissipation, StructuredBuffer[] buffer)
        {
            applyAdvection.CurrentTechnique = applyAdvection.Techniques[2];
            applyAdvection.Parameters["_Size"].SetValue(size);
            applyAdvection.Parameters["_DeltaTime"].SetValue(dt);
            applyAdvection.Parameters["_Dissipate"].SetValue(dissipation);
            applyAdvection.Parameters["_Forward"].SetValue(1.0f);

            applyAdvection.Parameters["_Read1f"].SetValue(buffer[READ]);
            applyAdvection.Parameters["_Write1f"].SetValue(buffer[WRITE]);
            applyAdvection.Parameters["_Phi_n_1_hat"].SetValue(phi[PHI_N_1_HAT]);
            applyAdvection.Parameters["_Phi_n_hat"].SetValue(phi[PHI_N_HAT]);
            applyAdvection.Parameters["_Velocity"].SetValue(velocity[READ]);
            applyAdvection.Parameters["_Obstacles"].SetValue(obstacles);

            foreach (var pass in applyAdvection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }

            Swap(buffer);
        }

        void ApplyAdvectionVelocity(float dt)
        {
            applyAdvection.CurrentTechnique = applyAdvection.Techniques[0];
            applyAdvection.Parameters["_Size"].SetValue(size);
            applyAdvection.Parameters["_DeltaTime"].SetValue(dt);
            applyAdvection.Parameters["_Dissipate"].SetValue(m_velocityDissipation);
            applyAdvection.Parameters["_Forward"].SetValue(1.0f);
            applyAdvection.Parameters["_Read3f"].SetValue(velocity[READ]);
            applyAdvection.Parameters["_Write3f"].SetValue(velocity[WRITE]);
            applyAdvection.Parameters["_Velocity"].SetValue(velocity[READ]);
            applyAdvection.Parameters["_Obstacles"].SetValue(obstacles);

            foreach (var pass in applyAdvection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }

            Swap(velocity);
        }

        void ApplyBuoyancy(float dt)
        {
            applyBuoyancy.CurrentTechnique = applyBuoyancy.Techniques[0];
            applyBuoyancy.Parameters["_Size"].SetValue(size);
            applyBuoyancy.Parameters["_Up"].SetValue(new Vector4(0f, 1f, 0f, 0f));
            applyBuoyancy.Parameters["_Buoyancy"].SetValue(m_densityBuoyancy);
            applyBuoyancy.Parameters["_Weight"].SetValue(m_densityWeight);
            applyBuoyancy.Parameters["_DeltaTime"].SetValue(dt);
            applyBuoyancy.Parameters["_Write"].SetValue(velocity[WRITE]);
            applyBuoyancy.Parameters["_Velocity"].SetValue(velocity[READ]);
            applyBuoyancy.Parameters["_Density"].SetValue(density[READ]);
            applyBuoyancy.Parameters["_Temperature"].SetValue(temperature[READ]);

            foreach (var pass in applyBuoyancy.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }

            Swap(velocity);
        }

        void ApplyImpulse(float dt, float amount, StructuredBuffer[] buffer)
        {
            applyImpulse.CurrentTechnique = applyImpulse.Techniques[0];
            applyImpulse.Parameters["_Size"].SetValue(size);
            applyImpulse.Parameters["_Radius"].SetValue(m_inputRadius);
            applyImpulse.Parameters["_Amount"].SetValue(amount);
            applyImpulse.Parameters["_DeltaTime"].SetValue(dt);
            applyImpulse.Parameters["_Pos"].SetValue(m_inputPos);
            applyImpulse.Parameters["_Read"].SetValue(buffer[READ]);
            applyImpulse.Parameters["_Write"].SetValue(buffer[WRITE]);

            foreach (var pass in applyImpulse.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }

            Swap(buffer);
        }

        void ComputeVorticityConfinement(float dt)
        {
            computeVorticity.CurrentTechnique = computeVorticity.Techniques[0];
            computeVorticity.Parameters["_Size"].SetValue(size);
            computeVorticity.Parameters["_Write"].SetValue(temp3f);
            computeVorticity.Parameters["_Velocity"].SetValue(velocity[READ]);
            foreach (var pass in computeVorticity.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }

            computeConfinement.CurrentTechnique = computeConfinement.Techniques[0];
            computeConfinement.Parameters["_Size"].SetValue(size);
            computeConfinement.Parameters["_DeltaTime"].SetValue(dt);
            computeConfinement.Parameters["_Epsilon"].SetValue(m_vorticityStrength);

            computeConfinement.Parameters["_Write"].SetValue(velocity[WRITE]);
            computeConfinement.Parameters["_Read"].SetValue(velocity[READ]);
            computeConfinement.Parameters["_Vorticity"].SetValue(temp3f);
            foreach (var pass in computeConfinement.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }

            Swap(velocity);
        }

        void ComputeDivergence()
        {
            computeDivergence.CurrentTechnique = computeDivergence.Techniques[0];
            computeDivergence.Parameters["_Size"].SetValue(size);
            computeDivergence.Parameters["_Write"].SetValue(temp3f);
            computeDivergence.Parameters["_Velocity"].SetValue(velocity[READ]);
            computeDivergence.Parameters["_Obstacles"].SetValue(obstacles);

            foreach (var pass in computeDivergence.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }
        }

        void ComputePressure()
        {
            computeJacobi.CurrentTechnique = computeJacobi.Techniques[0];
            computeJacobi.Parameters["_Size"].SetValue(size);
            computeJacobi.Parameters["_Divergence"].SetValue(temp3f);
            computeJacobi.Parameters["_Obstacles"].SetValue(obstacles);

            for (int i = 0; i < m_iterations; i++)
            {
                foreach (var pass in computeJacobi.CurrentTechnique.Passes)
                {
                    computeJacobi.Parameters["_Write"].SetValue(pressure[WRITE]);
                    computeJacobi.Parameters["_Pressure"].SetValue(pressure[READ]);
                    pass.ApplyCompute();
                    GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
                    Swap(pressure);
                }
            }
        }

        void ComputeProjection()
        {
            computeProjection.CurrentTechnique = computeProjection.Techniques[0];
            computeProjection.Parameters["_Size"].SetValue(size);
            computeProjection.Parameters["_Obstacles"].SetValue(obstacles);

            computeProjection.Parameters["_Pressure"].SetValue(pressure[READ]);
            computeProjection.Parameters["_Velocity"].SetValue(velocity[READ]);
            computeProjection.Parameters["_Write"].SetValue(velocity[WRITE]);

            foreach (var pass in computeProjection.CurrentTechnique.Passes)
            {
                pass.ApplyCompute();
                GraphicsDevice.DispatchCompute((int)size.X / ComputeGroupSizeXYZ, (int)size.Y / ComputeGroupSizeXYZ, (int)size.Z / ComputeGroupSizeXYZ);
            }

            Swap(velocity);
        }

        void Swap(StructuredBuffer[] buffer)
        {
            StructuredBuffer tmp = buffer[READ];
            buffer[READ] = buffer[WRITE];
            buffer[WRITE] = tmp;
        }

        private void Draw3DTextureCube()
        {
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.BlendState = BlendState.Additive;

            camPos = new Vector3((float)Math.Sin(rotation) * 2f, 1f, (float)Math.Cos(rotation) * 2f);

            Matrix world = Matrix.Identity;
            view = Matrix.CreateLookAt(camPos, Vector3.Zero, new Vector3(0, 1, 0));
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(40), (float)ResolutionX / (float)ResolutionY, 0.1f, 1000f);

            raytracer.Parameters["World"].SetValue(world);
            raytracer.Parameters["View"].SetValue(view);
            raytracer.Parameters["Projection"].SetValue(projection);
            raytracer.Parameters["_Density"].SetValue(density[READ]);
            raytracer.Parameters["_Scale"].SetValue(new Vector3(1, 1, 1));
            raytracer.Parameters["_Size"].SetValue(size);
            raytracer.Parameters["CamPos"].SetValue(camPos);

            foreach (var pass in raytracer.CurrentTechnique.Passes)
            {
                pass.Apply();

                GraphicsDevice.SetVertexBuffer(cubeSlices);
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, TextureSize * 2);
            }
        }

        private void DrawText()
        {
            string text = "WASD to move smoke\n";
            text += "Arrows to rotate";

            spriteBatch.Begin();
            spriteBatch.DrawString(textFont, text, new Vector2(30, 30), Color.White);
            spriteBatch.End();
        }

        private VertexBuffer CreateCubeSlices()
        {
            var vertices = new VertexPositionTexture[36];
            Vector3 center = new Vector3(0.5f);

            // Forward face
            vertices[0] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(0, 1));
            vertices[1] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(0, 0));
            vertices[2] = new VertexPositionTexture(new Vector3(1, 1, 0) - center, new Vector2(1, 0));
            vertices[3] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(0, 1));
            vertices[4] = new VertexPositionTexture(new Vector3(1, 1, 0) - center, new Vector2(1, 0));
            vertices[5] = new VertexPositionTexture(new Vector3(1, 0, 0) - center, new Vector2(1, 1));

            // Backward face
            vertices[6] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 1));
            vertices[7] = new VertexPositionTexture(new Vector3(0, 1, 1) - center, new Vector2(0, 0));
            vertices[8] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(1, 0));
            vertices[9] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 1));
            vertices[10] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(1, 0));
            vertices[11] = new VertexPositionTexture(new Vector3(1, 0, 1) - center, new Vector2(1, 1));

            // Top face
            vertices[12] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(0, 1));
            vertices[13] = new VertexPositionTexture(new Vector3(0, 1, 1) - center, new Vector2(0, 0));
            vertices[14] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(1, 0));
            vertices[15] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(0, 1));
            vertices[16] = new VertexPositionTexture(new Vector3(1, 1, 1) - center, new Vector2(1, 0));
            vertices[17] = new VertexPositionTexture(new Vector3(1, 1, 0) - center, new Vector2(1, 1));

            // Bottom face
            vertices[18] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(0, 1));
            vertices[19] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 0));
            vertices[20] = new VertexPositionTexture(new Vector3(1, 0, 1) - center, new Vector2(1, 0));
            vertices[21] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(0, 1));
            vertices[22] = new VertexPositionTexture(new Vector3(1, 0, 1) - center, new Vector2(1, 0));
            vertices[23] = new VertexPositionTexture(new Vector3(1, 0, 0) - center, new Vector2(1, 1));

            // Left Face
            vertices[24] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 1));
            vertices[25] = new VertexPositionTexture(new Vector3(0, 1, 1) - center, new Vector2(0, 0));
            vertices[26] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(1, 0));
            vertices[27] = new VertexPositionTexture(new Vector3(0, 0, 1) - center, new Vector2(0, 1));
            vertices[28] = new VertexPositionTexture(new Vector3(0, 1, 0) - center, new Vector2(1, 0));
            vertices[29] = new VertexPositionTexture(new Vector3(0, 0, 0) - center, new Vector2(1, 1));

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
    }
}
