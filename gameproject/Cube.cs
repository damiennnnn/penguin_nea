using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gameproject
{
    
    public class Cube // cube object that the world is constructed with 
    {
        public static Model CubeModel; // every cube should be sharing the same model, no need to store for each individual cube
        public static Model WorldModel; // cube world rendering
        public static ModelMesh CubeMesh; // all cubes will share the same model mesh
        public static BasicEffect Effect; // all cubers will share the same basiceffect
        public Matrix Translation; // store position as matrix for rendering
        public static Color Colour = Color.White; // colour that the cube uses
        public Vector3 Position; // cubes position in the world
        public BoundingBox Bounding; // bounding box for collision (not implemented yet)

        public static void ChangeColour(Color col)
        {
            Colour = col;
        }

        public Cube(Vector3 Pos) // option of defining your own colour or using a default
        {
            Position = Pos;
            Translation = Matrix.CreateTranslation(Pos);
        }
        public Cube(Vector3 Pos, Color Col)
        {
            Position = Pos;
            Colour = Col;
        }
        public void Render(Player player, Vector3 pos, float scale = 1f)
        {
            foreach (ModelMesh Mesh in CubeModel.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3(); // set colour of the cube
                    Effect.View = player.View; // use players current view 
                    Effect.Projection = player.Projection;
                    Effect.World = Matrix.CreateScale(scale, scale, scale) * Matrix.CreateTranslation(pos); // create a world matrix from cubes position
                }
                Mesh.Draw();
            }
        }
        public static Matrix[] matrices;

        /*public void Render(Player player, bool test)
        {
            Cube.Effect.EnableDefaultLighting();
            Cube.Effect.AmbientLightColor = Color.White.ToVector3();
            Cube.Effect.View = player.View;
            Cube.Effect.Projection = player.Projection;
            Cube.Effect.TextureEnabled = true;
            Cube.Effect.Texture = Main.Textures["grass"];
            int length = matrices.Length - 1;
            for (int i = 0; i < length; i++)
            {k
                Effect.World = matrices[i];
                CubeMesh.Draw();
            }

        }*/

        public void Render(Player player, bool test)
        {

        }
        public void Render(Player player, Vector3 pos, Vector3 forward)
        {
            foreach (ModelMesh Mesh in CubeModel.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3(); // set colour of the cube
                    Effect.View = player.View; // use players current view 
                    Effect.Projection = player.Projection;
                    Effect.World = Matrix.CreateScale(0.5f) * Matrix.CreateWorld(pos, forward, Vector3.Up) ; // create a world matrix from cubes position
                    //Effect.TextureEnabled = true;
                    //Effect.Texture = Main.Textures["grass"];
                }
                Mesh.Draw();
            }
        }
        public void Render(Player player, Vector3 pos)
        {
            foreach (ModelMesh Mesh in CubeModel.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3(); // set colour of the cube
                    Effect.View = player.View; // use players current view 
                    Effect.Projection = player.Projection;
                    Effect.World = Matrix.CreateTranslation(pos); // create a world matrix from cubes position
                    Effect.TextureEnabled = true;
                    Effect.Texture = Main.Textures["grass"];
                }
                Mesh.Draw();
            }
        }

        public void Render(Player player)
        {
            foreach (ModelMesh Mesh in CubeModel.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3(); // set colour of the cube
                    Effect.View = player.View; // use players current view 
                    Effect.Projection = player.Projection;
                    Effect.World = Matrix.CreateTranslation(Position); // create a world matrix from cubes position
                }
                Mesh.Draw();
            }
        }
    }
}
