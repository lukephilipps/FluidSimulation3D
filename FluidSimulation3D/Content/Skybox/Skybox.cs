using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FluidSimulation3D
{
    public class Skybox
    {
        private Model skybox;
        public TextureCube skybox_texture;
        private Effect skybox_effect;
        private float size = 3f;

        public Skybox(string[] skybox_textures, ContentManager content, GraphicsDevice g, string model_path, string shader_path)
        {
            skybox = content.Load<Model>(model_path);
            skybox_effect = content.Load<Effect>(shader_path);

            // Assumes all textures are the same width/height (they should be)
            Texture2D tempTexture = content.Load<Texture2D>(skybox_textures[0]);
            skybox_texture = new TextureCube(g, tempTexture.Width, false, SurfaceFormat.Color);
            byte[] data = new byte[tempTexture.Width * tempTexture.Height * 4];

            tempTexture.GetData<byte>(data);
            skybox_texture.SetData<byte>(CubeMapFace.NegativeX, data);

            tempTexture = content.Load<Texture2D>(skybox_textures[1]);
            tempTexture.GetData<byte>(data);
            skybox_texture.SetData<byte>(CubeMapFace.PositiveX, data);

            tempTexture = content.Load<Texture2D>(skybox_textures[2]);
            tempTexture.GetData<byte>(data);
            skybox_texture.SetData<byte>(CubeMapFace.NegativeY, data);

            tempTexture = content.Load<Texture2D>(skybox_textures[3]);
            tempTexture.GetData<byte>(data);
            skybox_texture.SetData<byte>(CubeMapFace.PositiveY, data);

            tempTexture = content.Load<Texture2D>(skybox_textures[4]);
            tempTexture.GetData<byte>(data);
            skybox_texture.SetData<byte>(CubeMapFace.NegativeZ, data);

            tempTexture = content.Load<Texture2D>(skybox_textures[5]);
            tempTexture.GetData<byte>(data);
            skybox_texture.SetData<byte>(CubeMapFace.PositiveZ, data);
        }

        public void Draw(Matrix view, Matrix projection, Vector3 camera_position)
        {
            foreach (EffectPass pass in skybox_effect.CurrentTechnique.Passes)
            {
                foreach (ModelMesh mesh in skybox.Meshes)
                {
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        part.Effect = skybox_effect;
                        part.Effect.Parameters["World"].SetValue(Matrix.CreateScale(size) * Matrix.CreateTranslation(camera_position));
                        part.Effect.Parameters["View"].SetValue(view);
                        part.Effect.Parameters["Projection"].SetValue(projection);
                        part.Effect.Parameters["SkyBoxTexture"].SetValue(skybox_texture);
                        part.Effect.Parameters["CameraPosition"].SetValue(camera_position);
                    }
                    mesh.Draw();
                }
            }
        }
    }
}
