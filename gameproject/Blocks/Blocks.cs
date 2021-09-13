using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace gameproject
{

    public class Block
    {
        public BlockData Data;
        public Vector3 XYZ;
        public Vector3 CentrePos;
        public Vector3 ChunkPosition;
        public BlockID BlockType; 
        struct CubeInfo
        {
            public Vector4 World;
            public Vector2 AtlasCoordinate;
        };
        Texture2D blockText;

        public Block(BlockID id, Vector3 xyz)
        {
            XYZ = xyz;
            Vector3 vec = new Vector3(xyz.X, xyz.Y, xyz.Z);
            Vector3 max = vec + (Vector3.One);
            CentrePos = Vector3.Lerp(vec, max, 0.5f);
            BlockType = id;
        }

        public bool IsInView(Matrix ViewProj)
        {
            //bool frustrumCollision = new BoundingFrustum(ViewProj).Intersects(CollisionBox);
            return false;
            //return frustrumCollision;
        }
        // Normal vectors for each face (needed for lighting / display)
        private static readonly Vector3 normalFront = new Vector3(0.0f, 0.0f, 1.0f);
        private static readonly Vector3 normalBack = new Vector3(0.0f, 0.0f, -1.0f);
        private static readonly Vector3 normalTop = new Vector3(0.0f, 1.0f, 0.0f);
        private static readonly Vector3 normalBottom = new Vector3(0.0f, -1.0f, 0.0f);
        private static readonly Vector3 normalLeft = new Vector3(-1.0f, 0.0f, 0.0f);
        private static readonly Vector3 normalRight = new Vector3(1.0f, 0.0f, 0.0f);

        public bool Visible()
        {
            return IsInView(Main.BasePlayer.View * Main.BasePlayer.Projection);
        }

        public VertexPositionNormalTexture[] blockVerts;
        public List<VertexPositionNormalTexture> CreateMesh(bool[] facesToRender)
        {
            List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();
            Vector3 Position = XYZ;

                        if (true) // We still want to render them to see them.
                        {
                            Vector3 size = new Vector3(1, 1, 1) / 2;

                            Vector3 realPos = (Position);
                            //Vector3 realPos = new Vector3(Position.X, Position.Y, Position.Z) + ChunkPosition; // Array position
                            Vector3 cubePos = new Vector3(realPos.X, realPos.Y, realPos.Z) + size; // View position

                            // Top face
                            Vector3 topLeftFront = cubePos + new Vector3(-1.0f, 1.0f, -1.0f) * size;
                            Vector3 topLeftBack = cubePos + new Vector3(-1.0f, 1.0f, 1.0f) * size;
                            Vector3 topRightFront = cubePos + new Vector3(1.0f, 1.0f, -1.0f) * size;
                            Vector3 topRightBack = cubePos + new Vector3(1.0f, 1.0f, 1.0f) * size;

                            // Calculate the cubePos of the vertices on the bottom face.
                            Vector3 btmLeftFront = cubePos + new Vector3(-1.0f, -1.0f, -1.0f) * size;
                            Vector3 btmLeftBack = cubePos + new Vector3(-1.0f, -1.0f, 1.0f) * size;
                            Vector3 btmRightFront = cubePos + new Vector3(1.0f, -1.0f, -1.0f) * size;
                            Vector3 btmRightBack = cubePos + new Vector3(1.0f, -1.0f, 1.0f) * size;

                            /* Start of Vertices */
                            if (facesToRender[4])
                            {
                                UVMap? uvMap = RenderExtensions.CreateUVMapping(BlockType, Direction.Top);
                                if (uvMap != null)
                                {
                                    UVMap uv = uvMap.GetValueOrDefault();
                                    // Add the vertices for the TOP face.
                                    vertices.Add(new VertexPositionNormalTexture(topLeftFront, normalTop, uv.BottomLeft));
                                    vertices.Add(new VertexPositionNormalTexture(topRightBack, normalTop, uv.TopRight));
                                    vertices.Add(new VertexPositionNormalTexture(topLeftBack, normalTop, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(topLeftFront, normalTop, uv.BottomLeft));
                                    vertices.Add(new VertexPositionNormalTexture(topRightFront, normalTop, uv.BottomRight));
                                    vertices.Add(new VertexPositionNormalTexture(topRightBack, normalTop, uv.TopRight));
                                }
                            }
                            //if (!this[x, y - 1, z).Visible)
                            if (facesToRender[5])
                            {
                                UVMap? uvMap = RenderExtensions.CreateUVMapping(BlockType, Direction.Bottom);
                                if (uvMap != null)
                                {
                                    UVMap uv = uvMap.GetValueOrDefault();
                                    // Add the vertices for the BOTTOM face.
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftFront, normalBottom, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftBack, normalBottom, uv.BottomLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmRightBack, normalBottom, uv.BottomRight));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftFront, normalBottom, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmRightBack, normalBottom, uv.BottomRight));
                                    vertices.Add(new VertexPositionNormalTexture(btmRightFront, normalBottom, uv.TopRight));
                                }
                            }
                            //if (!this[x, y, z + 1).Visible)
                            if (facesToRender[1])
                            {
                                UVMap? uvMap = RenderExtensions.CreateUVMapping(BlockType, Direction.Back);
                                if (uvMap != null)
                                {
                                    UVMap uv = uvMap.GetValueOrDefault();
                                    // Add the vertices for the BACK face.
                                    vertices.Add(new VertexPositionNormalTexture(topLeftBack, normalBack, uv.TopRight));
                                    vertices.Add(new VertexPositionNormalTexture(topRightBack, normalBack, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftBack, normalBack, uv.BottomRight));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftBack, normalBack, uv.BottomRight));
                                    vertices.Add(new VertexPositionNormalTexture(topRightBack, normalBack, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmRightBack, normalBack, uv.BottomLeft));
                                }
                            }
                            //if (!this[x, y, z - 1).Visible)
                            if (facesToRender[0])
                            {
                                UVMap? uvMap = RenderExtensions.CreateUVMapping(BlockType, Direction.Front);
                                if (uvMap != null)
                                {
                                    UVMap uv = uvMap.GetValueOrDefault();
                                    // Add the vertices for the FRONT face.
                                    vertices.Add(new VertexPositionNormalTexture(topLeftFront, normalFront, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftFront, normalFront, uv.BottomLeft));
                                    vertices.Add(new VertexPositionNormalTexture(topRightFront, normalFront, uv.TopRight));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftFront, normalFront, uv.BottomLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmRightFront, normalFront, uv.BottomRight));
                                    vertices.Add(new VertexPositionNormalTexture(topRightFront, normalFront, uv.TopRight));
                                }
                            }
                            if (facesToRender[3])
                            {
                                UVMap? uvMap = RenderExtensions.CreateUVMapping(BlockType, Direction.Left);
                                if (uvMap != null)
                                {
                                    UVMap uv = uvMap.GetValueOrDefault();
                                    // Add the vertices for the LEFT face.
                                    vertices.Add(new VertexPositionNormalTexture(topLeftFront, normalLeft, uv.TopRight));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftBack, normalLeft, uv.BottomLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftFront, normalLeft, uv.BottomRight));
                                    vertices.Add(new VertexPositionNormalTexture(topLeftBack, normalLeft, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmLeftBack, normalLeft, uv.BottomLeft));
                                    vertices.Add(new VertexPositionNormalTexture(topLeftFront, normalLeft, uv.TopRight));
                                }
                            }
                            if (facesToRender[2])
                            {
                                UVMap? uvMap = RenderExtensions.CreateUVMapping(BlockType, Direction.Right);
                                if (uvMap != null)
                                {
                                    UVMap uv = uvMap.GetValueOrDefault();
                                    // Add the vertices for the RIGHT face. 
                                    vertices.Add(new VertexPositionNormalTexture(topRightFront, normalRight, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmRightFront, normalRight, uv.BottomLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmRightBack, normalRight, uv.BottomRight));
                                    vertices.Add(new VertexPositionNormalTexture(topRightBack, normalRight, uv.TopRight));
                                    vertices.Add(new VertexPositionNormalTexture(topRightFront, normalRight, uv.TopLeft));
                                    vertices.Add(new VertexPositionNormalTexture(btmRightBack, normalRight, uv.BottomRight));
                                }
                            }
                            /* End of Vertices */
                        }
            blockVerts = vertices.ToArray();
            return vertices;
        }
    }

    public static class Renderer
    {
        public static void DrawAll(List<Block> Blocks, GraphicsDevice gDevice, BasicEffect Effect)
        {
            Model model = Cube.WorldModel;
            Matrix viewProj = Main.BasePlayer.View * Main.BasePlayer.Projection;
            var effect = Resources.Effects["instance_effect"];
            effect.CurrentTechnique = effect.Techniques["Textured"];
            effect.Parameters["View"].SetValue(Main.BasePlayer.View);
            effect.Parameters["Projection"].SetValue(Main.BasePlayer.Projection);
            effect.Parameters["ViewVector"].SetValue(Main.BasePlayer.EyePosition);
            effect.Parameters["AmbientColor"].SetValue(Color.White.ToVector4());
            effect.Parameters["AmbientIntensity"].SetValue(0.5f);
            effect.Parameters["DiffuseColor"].SetValue(Color.White.ToVector4());
            effect.Parameters["DiffuseIntensity"].SetValue(0.5f);
            effect.Parameters["Shininess"].SetValue(20.0f);
            effect.Parameters["SpecularColor"].SetValue(Color.White.ToVector4());
            effect.Parameters["SpecularIntensity"].SetValue(0.05f);

            var baseEffect2 = new BasicEffect(gDevice);
            baseEffect2.AmbientLightColor = Vector3.One * 0.2f;

            baseEffect2.View = Main.BasePlayer.View;
            baseEffect2.Projection = Main.BasePlayer.Projection;
            baseEffect2.LightingEnabled = true;
            for (int i = 0; i < Blocks.Count; i++)
            {
                if (!Blocks[i].IsInView(viewProj))
                    continue;



                Vector3 diffuseDir = (Main.BasePlayer.Position + Vector3.Up);
                diffuseDir -= Blocks[i].XYZ;
                diffuseDir.Normalize();

                //effect.Parameters["World"].SetValue(Blocks[i].World);
                //effect.Parameters["WorldInverseTranspose"].SetValue(Blocks[i].InverseWorld);
                //effect.Parameters["ModelTexture"].SetValue(Blocks[i].GetBlockTexture());
                //effect.Parameters["DiffuseLightDirection"].SetValue(diffuseDir);
                //effect.CurrentTechnique.Passes[0].Apply();
                foreach (ModelMesh mesh in model.Meshes)
                {
                    foreach (BasicEffect baseEffect in mesh.Effects)
                    {
                        baseEffect.EnableDefaultLighting();
                        baseEffect.AmbientLightColor = Vector3.One * 0.1f;
                        baseEffect.TextureEnabled = true;
                        //baseEffect.Texture = Blocks[i].GetBlockTexture();
                        baseEffect.View = Main.BasePlayer.View;
                        baseEffect.Projection = Main.BasePlayer.Projection;
                        baseEffect.Alpha = 1f;
                        baseEffect.LightingEnabled = true;
                        baseEffect.DirectionalLight0.Enabled = true;
                        baseEffect.DirectionalLight0.DiffuseColor =
                                                    Vector3.Clamp((Vector3.One / Vector3.Distance(Main.BasePlayer.Position, Blocks[i].CentrePos)) * 3, Vector3.One * 0.1f, Vector3.One);
                        baseEffect.DirectionalLight0.Direction =
                                 -diffuseDir;
                        baseEffect.DirectionalLight0.SpecularColor = Vector3.One * 0.8f;
                        baseEffect.World = Matrix.CreateTranslation(Blocks[i].XYZ + new Vector3(0.5f, 0f, 0.5f));
                    }

                    mesh.Draw();
                }


                //baseEffect.CurrentTechnique.Passes[0].Apply();
                //gDevice.SetVertexBuffer(Blocks[i].geometryBuffer);
                //gDevice.Indices = Blocks[i].indexBuffer;
                //gDevice.DrawPrimitives(PrimitiveType.TriangleList,0, 12);
                //Debug.DrawBoundingBox(Blocks[i].Buffer, Effect, gDevice, Main.BasePlayer.View, Main.BasePlayer.Projection);
                Main.RenderingCubes++;
            }
        }

        static BasicEffect effect = null;
        static Stopwatch stopwatch = new Stopwatch();
        static Effect ambiEffect;
        static EffectParameter sunLightCol;
        static EffectParameter sunLightDir;
        static EffectParameter sunLightIntensity;
        static EffectParameter cameraPosition;
        static EffectParameter lightingEffectPointLightPosition;
        static EffectParameter lightingEffectPointLightColour;
        static EffectParameter lightingEffectPointLightIntensity;
        static EffectParameter lightingEffectPointLightRadius;
        static EffectParameter lightingEffectMaxLightsRendered;


        static EffectParameter currentTexture;
        static EffectParameter world;
        static EffectParameter worldViewProj;
        static Vector3[] lightPositions = new Vector3[]
        {
            new Vector3(0,5,0)
        };
        static float[] lightRadius = new float[]
        {
            5f
        };
        static float[] lightIntensities = new float[]
        {
            5f
        };
        static Vector3[] lightColours = new Vector3[]
        {
            Color.Red.ToVector3()
        };
        static Vector3[] eyePosition = new Vector3[]
        {
            Vector3.Zero
        };
        public static void SetupShader(Effect effect)
        {
            ambiEffect = effect;
            sunLightCol = ambiEffect.Parameters["SunLightColor"];
            sunLightDir = ambiEffect.Parameters["SunLightDirection"];
            sunLightIntensity = ambiEffect.Parameters["SunLightIntensity"];

            cameraPosition = ambiEffect.Parameters["CamCamPosition"];
            lightingEffectPointLightPosition = ambiEffect.Parameters["PointLightPosition"];
            lightingEffectPointLightColour = ambiEffect.Parameters["PointLightColor"];
            lightingEffectPointLightIntensity = ambiEffect.Parameters["PointLightIntensity"];

            lightingEffectPointLightRadius = ambiEffect.Parameters["PointLightRadius"];
            lightingEffectMaxLightsRendered = ambiEffect.Parameters["MaxLightsRendered"];
            currentTexture = ambiEffect.Parameters["DiffuseTexture"];

            world = ambiEffect.Parameters["World"];
            worldViewProj = ambiEffect.Parameters["WorldViewProj"];
        }

        public static void AmbiShader()
        {
            //world.SetValue(w);
            //worldViewProj.SetValue(w * view * projection);



            sunLightCol.SetValue(Color.White.ToVector3());
            sunLightDir.SetValue(Vector3.Zero);
            sunLightIntensity.SetValue(0.8f);

            lightingEffectPointLightPosition.SetValue(lightPositions);
            lightingEffectPointLightRadius.SetValue(lightRadius);
            lightingEffectPointLightIntensity.SetValue(lightIntensities);
            lightingEffectPointLightColour.SetValue(lightColours);
            lightingEffectMaxLightsRendered.SetValue(1);

            //Vector3 eyePos = Vector3.Zero;
            //if (Main.BasePlayer.EyePosition != null) eyePos = Main.BasePlayer.EyePosition;
            //eyePosition[0] = eyePos;
            //cameraPosition.SetValue(eyePosition);
            //ambiEffect.Parameters["DiffuseTexture"].SetValue(Main.Textures["texturemap"]);
            ambiEffect.CurrentTechnique.Passes[0].Apply();
        }

        static Vector3 corner = new Vector3(16, 16, 16);
        public static void DrawChunks(GraphicsDevice gDevice, Matrix w, Matrix view, Matrix projection, Dictionary<Vector3, WorldData.Chunk> Rendering)
        {
            SamplerState s = new SamplerState()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                Filter = TextureFilter.Point,

            };

            gDevice.SamplerStates[0] = s;
            if (effect == null) effect = new BasicEffect(gDevice);

            effect.Texture = Main.Textures["texturemap"];
            effect.World = Matrix.Identity;
            effect.Projection = projection;
            
            effect.View = view;
            effect.AmbientLightColor = Vector3.One * 0.1f;
            effect.TextureEnabled = true;
            effect.EnableDefaultLighting();
            effect.SpecularColor = Color.Transparent.ToVector3();
            effect.FogEnabled = true;
            effect.FogColor = Color.DimGray.ToVector3();
            effect.FogStart = 25f;
            effect.FogEnd = 65f;
            effect.CurrentTechnique.Passes[0].Apply();
            var frustum = new BoundingFrustum( view * projection );
            stopwatch.Start();

            foreach (var keyvalpair in Rendering)
            {
                //var box = new BoundingBox(keyvalpair.Key, keyvalpair.Key + corner);
                //if (frustum.Intersects(box))
                    keyvalpair.Value.Draw(gDevice);
            }


            stopwatch.Stop();
            Console.SetCursorPosition(0, 4);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("chunk rendering (ms): " + stopwatch.ElapsedMilliseconds + "ms         ");
            Console.ResetColor();
            stopwatch.Reset();

        }
    }


    public class BlockData
    {
        public BlockID Identifier;

        public BlockData(BlockID id)
        {
            Identifier = id;
        }
    }
}
