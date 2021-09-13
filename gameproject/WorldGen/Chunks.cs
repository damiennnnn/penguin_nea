using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gameproject
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Mesh
    {
        public VertexPositionNormalTexture[] Vertices { get; set; }
    }

    public static class ChunkSetup
    {
        public static int ChunkHorizontal = 16;
        public static int ChunkVertical = 256;
    }

    public static class WorldData
    {
        public static List<Chunk> Chunks = new List<Chunk>();
        public static List<Chunk> RenderingList = new List<Chunk>();
        public static Dictionary<Vector3, Chunk> ChunkDictionary = new Dictionary<Vector3, Chunk>();
        public static int CubeCount;

        private static readonly Vector3[] chunkDirections =
        {
            new Vector3(0, 0, 1), // forward, chunk ahead of us
            new Vector3(0, 0, -1), // back, chunk behind us
            new Vector3(-1, 0, 0), // left, chunk to the left of us
            new Vector3(1, 0, 0), // right, chunk to the right of us
            new Vector3(-1, 0, 1), // forwardleft
            new Vector3(1, 0, 1), // forwardright
            new Vector3(-1, 0, -1), // backleft
            new Vector3(1, 0, -1) // backright
        };


        public static Vector3[] dirs =
        {
            Vector3.Forward,
            Vector3.Backward,
            Vector3.Right,
            Vector3.Left,
            Vector3.Up,
            Vector3.Down
        };

        public static bool GetChunk(Vector3 Position, out Chunk foundChunk)
        {
            foundChunk = null;
            var chunkX = (int) (Position.X / ChunkSetup.ChunkHorizontal);
            var chunkY = (int) (Position.Y / ChunkSetup.ChunkVertical);
            var chunkZ = (int) (Position.Z / ChunkSetup.ChunkHorizontal);
            var ChunkPos = new Vector3(chunkX, chunkY, chunkZ);

            return
                ChunkDictionary.TryGetValue(ChunkPos,
                    out foundChunk); // returns false if the chunk doesnt exist, returns true if it does
        }

        public static void Clear()
        {
            Chunks.Clear();
            ChunkDictionary.Clear();
            CubeCount = 0;
        }

        public static List<Block> GetBlocksAndNeighbours(Entity entity)
        {
            var nearbyChunks = GetChunksAndNeighbours(entity);
            var blockList = new List<Block>();
            for (var i = 0; i < nearbyChunks.Count; i++) blockList.AddRange(nearbyChunks[i].Blocks.Values.ToList());
            return blockList;
        }

        public static List<Chunk>
            GetChunksAndNeighbours(Entity entity) // returns a list with the current chunk and all neighbour chunks
        {
            Chunk curChunk;
            var surroundings = new List<Chunk>();
            if (entity.GetCurrentChunk(out curChunk))
            {
                var curChunkPos = entity.GetCurChunkPos();

                surroundings.Add(curChunk); // add the players current chunk

                for (var i = 0;
                    i < 7;
                    i++) // there will only be 8 surrounding chunks, and 9 chunks in total (including the current one)
                {
                    var newChunkPos = curChunkPos + chunkDirections[i];
                    Chunk chunk;
                    if (ChunkDictionary.TryGetValue(newChunkPos, out chunk))
                        surroundings.Add(chunk);
                }
            }

            return surroundings;
            //Vector3 foward = Vector3
        }


        public static void GetBoundingsAndChunks(Entity entity, out List<Chunk> chunks,
            out Dictionary<Vector3, Block> boundingBoxes) // get bounding boxes of neighbouring chunks and the chunks as a list
        {
            var neighbours = GetChunksAndNeighbours(entity);
            var boxesVec = new Dictionary<Vector3, Block>();
            for (var i = 0; i < neighbours.Count; i++)
                foreach (var KeyVal in neighbours[i].Blocks)
                    boxesVec.Add(KeyVal.Key, KeyVal.Value);
            neighbours.RemoveAll(x => x == null);
            boundingBoxes = boxesVec;
            chunks = neighbours;
        }

        public static void CalcChunkCollision(Chunk chunk)
        {
            /* if (Main.PhysSystem.RigidBodies.Contains(chunk.chunkCollis))
                 Main.PhysSystem.RemoveBody(chunk.chunkCollis);
 
             foreach (var pos in chunk.Blocks.Keys)
             {
                 var shape = new CompoundShape.TransformedShape(new BoxShape(1, 1, 1), JMatrix.Identity, Physics.XNAToJitter(pos));
 
                 //chunk.compound.Add(shape);
 
             }
 
             var compoundShape = new CompoundShape(chunk.compound);
             chunk.chunkCollis = new RigidBody(compoundShape);
             chunk.chunkCollis.IsStatic = true;
             */
            // Main.PhysSystem.AddBody(chunk.chunkCollis);
        }

        public static void SetupWorld(ConcurrentBag<(Vector3, BlockID)> pos, GraphicsDevice gDevice, float lastProgress)
        {
            var concurDir = new ConcurrentDictionary<Vector3, Chunk>();

            var watch = new Stopwatch();

            watch.Start();
            float count = pos.Count;
            var cur = 0f;
            Parallel.ForEach(pos, Tup =>
            {
                var xyz = Tup.Item1;
                var id = Tup.Item2;
                CubeCount++;

                var boxX = (int) (xyz.X / ChunkSetup.ChunkHorizontal);
                var boxY = (int) (xyz.Y / ChunkSetup.ChunkVertical);
                var boxZ = (int) (xyz.Z / ChunkSetup.ChunkHorizontal);
                var chunkpos = new Vector3(boxX, boxY, boxZ);
                Chunk chunk;
                //if ((boxX % 2) == 0) id = "plank";
                var block = new Block(id, xyz);

                if (concurDir.TryGetValue(chunkpos, out chunk))
                {
                    // chunk already exists in which to store our block
                    chunk.Blocks[xyz] = block;
                }
                else
                {
                    var newChunk = new Chunk(); // 
                    newChunk.XYZ = chunkpos; // else create a new chunk with the required position
                    block.ChunkPosition = chunkpos;
                    newChunk.Blocks.TryAdd(xyz, block);
                    concurDir.TryAdd(chunkpos, newChunk);
                }

                cur++;
                Main.WorldGenProgress = lastProgress + cur / count * 100;
            });
            ChunkDictionary = concurDir.ToDictionary(entry => entry.Key,
                entry => entry.Value);
            concurDir.Clear();
            watch.Stop();
            Console.WriteLine("chunk split time: " + watch.Elapsed.TotalSeconds);
            watch.Reset();

            var curProg = Main.WorldGenProgress + 0f;

            var blocks = new List<Block>();
            var BlockList = new Dictionary<Vector3, Block>();


            watch.Start();

            count = ChunkDictionary.Count;
            cur = 0f;
            Parallel.ForEach(ChunkDictionary, keyvalpair =>
            {
                var verts = new List<VertexPositionNormalTexture>();
                var chunkPos = keyvalpair.Key;
                var chunk = keyvalpair.Value;
                foreach (var block in chunk.Blocks)
                {
                    var blockPos = block.Key;

                    Vector3[] vecs =
                    {
                        blockPos + Vector3.Forward,
                        blockPos + Vector3.Backward,
                        blockPos + Vector3.Right,
                        blockPos + Vector3.Left,
                        blockPos + Vector3.Up,
                        blockPos + Vector3.Down
                    };
                    bool[] facesToRender =
                    {
                        true, // forward
                        true, // back
                        true, // right
                        true, // left
                        true, // top
                        true // bottom
                    };
                    var occluded = true;
                    for (var i = 0; i < 6; i++)
                    {
                        var surrounding = chunkPos + dirs[i];

                        var blockInPos = false;
                        ChunkDictionary.TryGetValue(surrounding, out var chunk1);

                        var isWater = false;

                        Block neighbour;

                        if (chunk.Blocks.TryGetValue(vecs[i], out neighbour))
                        {
                            isWater = neighbour.BlockType == BlockID.Water;
                            blockInPos = !isWater;
                        }
                        else if (chunk1 == null)
                        {
                        }
                        else if (chunk1.Blocks.TryGetValue(vecs[i], out neighbour))
                        {
                            isWater = neighbour.BlockType == BlockID.Water;
                            blockInPos = !isWater;
                        }


                        //if (chunk1 != null)
                        //    blockInPos = (chunk.Blocks.ContainsKey(vecs[i]) || chunk1.Blocks.ContainsKey(vecs[i]));
                        //else
                        //    blockInPos = chunk.Blocks.ContainsKey(vecs[i]);

                        if (!blockInPos)
                            occluded = false; // if the block isnt completely occluded

                        facesToRender[i] = !blockInPos; // we dont want to render that face if there is a block in front
                    }

                    if (!occluded)
                    {
                        var mesh = block.Value.CreateMesh(facesToRender);
                        verts.AddRange(mesh);
                    }
                }

                chunk.ChunkMesh = new Mesh {Vertices = verts.ToArray()};
                //RenderingList.Add(chunk);
                cur++;
                Main.WorldGenProgress = curProg + cur / count * 100;
            });
            /*
            foreach (var keyvalpair in ChunkDictionary)
            {
                List<VertexPositionNormalTexture> verts = new List<VertexPositionNormalTexture>();
                var chunkPos = keyvalpair.Key;
                var chunk = keyvalpair.Value;
                foreach (var block in chunk.Blocks)
                {
                    var blockPos = block.Key;

                    Vector3[] vecs = new Vector3[]
{
                        blockPos + Vector3.Forward,
                        blockPos + Vector3.Backward,
                        blockPos + Vector3.Right,
                        blockPos + Vector3.Left,
                        blockPos + Vector3.Up,
                        blockPos + Vector3.Down
};
                    bool[] facesToRender = new bool[] // a bool for each direction we will be checking
                    {
                    true, // forward
                    true, // back
                    true, // right
                    true, // left
                    true, // top
                    true // bottom
                    };
                    bool occluded = true;
                    for (int i = 0; i < 6; i++)
                    {
                        var surrounding = chunkPos + dirs[i];

                        bool blockInPos = false;
                        ChunkDictionary.TryGetValue(surrounding, out var chunk1);

                        if (chunk1 != null)
                            blockInPos = (chunk.Blocks.ContainsKey(vecs[i]) || chunk1.Blocks.ContainsKey(vecs[i]));
                        else
                            blockInPos = chunk.Blocks.ContainsKey(vecs[i]);

                        if (!blockInPos)
                            occluded = false; // if the block isnt completely occluded

                        facesToRender[i] = (!blockInPos); // we dont want to render that face if there is a block in front
                    }
                    if (!occluded)
                    {
                        var mesh = block.Value.CreateMesh(facesToRender, gDevice);
                        verts.AddRange(mesh);
                    }
                }
                chunk.ChunkMesh = new Mesh { Vertices = verts.ToArray() };
                RenderingList.Add(chunk);
            }
            */
            watch.Stop();
            Console.WriteLine("occludecheck time: " + watch.Elapsed.TotalSeconds);

/*
            foreach (var blockvec in BlockList)
            {
                var blockPos = blockvec.Key;
                var block = blockvec.Value;
                Vector3[] vecs = new Vector3[]
                {
                        blockPos + Vector3.Forward,
                        blockPos + Vector3.Backward,
                        blockPos + Vector3.Right,
                        blockPos + Vector3.Left,
                        blockPos + Vector3.Up,
                        blockPos + Vector3.Down
                };
                bool[] facesToRender = new bool[] // a bool for each direction we will be checking
                {
                    true, // forward
                    true, // back
                    true, // right
                    true, // left
                    true, // top
                    true // bottom
                };
                bool occluded = true;
                for (int i = 0; i < 6; i++)
                {
                    bool blockInPos = BlockList.ContainsKey(vecs[i]);
                    if (!blockInPos)
                        occluded = false; // if the block isnt completely occluded

                    facesToRender[i] = (!blockInPos); // we dont want to render that face if there is a block in front
                }
                if (!occluded)
                {
                    //block.GenerateGeometry(gDevice, facesToRender[0], facesToRender[1], facesToRender[2], facesToRender[3], facesToRender[4], facesToRender[5]);
                    block.CreateMesh(facesToRender, gDevice);
                    //block.InitialiseInstance(gDevice);
                    blocks.Add(block);
                }

            }

*/
            //RenderingList = blocks;
        }

        public class Chunk
        {
            public ConcurrentDictionary<Vector3, Block> Blocks = new ConcurrentDictionary<Vector3, Block>();

            public Vector3 XYZ;

            //public List<BoundingBox> Boxes = new List<BoundingBox>();
            //public List<Block> Blocks = new List<Block>();
            public Mesh ChunkMesh { get; set; }
            //public List<CompoundShape.TransformedShape> compound = new List<CompoundShape.TransformedShape>();
            //public RigidBody chunkCollis;

            public void Draw(GraphicsDevice graphicsDevice)
            {
                try
                {
                    if (ChunkMesh.Vertices.Length > 0)
                        using (var buffer = new VertexBuffer(
                            graphicsDevice,
                            VertexPositionNormalTexture.VertexDeclaration,
                            ChunkMesh.Vertices.Length, // Vertices
                            BufferUsage.WriteOnly))
                        {
                            // Load the buffer
                            buffer.SetData(ChunkMesh.Vertices);
                            // Send the vertex buffer to the device
                            graphicsDevice.SetVertexBuffer(buffer);
                            // Draw the primitives from the vertex buffer to the device as triangles
                            graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, ChunkMesh.Vertices.Length / 3);
                        }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
 
            }
        }
    }

    public class WorldD : DrawableGameComponent
    {
        #region FIELDS

        private int cubeOutlines = 0;
        private readonly string cmdstr = "debug_cubeoutlines";

        public int DrawCubeOutlines
        {
            get => (int) Global.ConsoleVars[cmdstr].val;
            set => Global.ConsoleVars[cmdstr].val = value;
        }

        public int DrawEntityBoxes
        {
            get => (int) Global.ConsoleVars["debug_entityboxes"].val;
            set => Global.ConsoleVars["debug_entityboxes"].val = value;
        }

        private Texture2D texture;
        private Effect effect;

        private VertexDeclaration instanceVertexDeclaration;

        private DynamicVertexBuffer instanceBuffer;
        private DynamicVertexBuffer geometryBuffer;
        private IndexBuffer indexBuffer;

        private VertexBufferBinding[] bindings;
        private CubeInfo[] instances;

        private struct CubeInfo
        {
            public Vector4 World;
            public Vector2 AtlasCoordinate;
        }

        public int sizeX;
        public int sizeZ;

        #endregion

        #region PROPERTIES

        public Matrix View { get; set; }

        public Matrix Projection { get; set; }


        public List<BoundingBox> BoundingBoxes = new List<BoundingBox>();
        public List<Vector3> BoxPositions = new List<Vector3>();
        public BoundingBox[] boundingBoxes;
        public BoundingBox FloorBox { get; set; }

        public int InstanceCount()
        {
            return BoxPositions.Count;
        }

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        ///     Creates a new CubeMap.
        /// </summary>
        /// <param name="game">Parent game instance.</param>
        /// <param name="sizeX">Map size on X.</param>
        /// <param name="sizeZ">Map size on Z.</param>
        public WorldD(Game game, List<Vector3> Positions)
            : base(game)
        {
            BoxPositions = Positions;
            boundingBoxes = new BoundingBox[BoxPositions.Count];
        }

        public void RegisterVars()
        {
            Global.RegisterVariable(cmdstr, 0, "Draws the bounding box outlines for the cubes");
            Global.RegisterVariable("debug_entityboxes", 0, "Draws the bounding box outlines for the entities");
        }

        #endregion

        #region METHODS

        /// <summary>
        ///     Initialize the VertexBuffer declaration for one cube instance.
        /// </summary>
        private void InitializeInstanceVertexBuffer()
        {
            var _instanceStreamElements = new VertexElement[2];

            // Position
            _instanceStreamElements[0] = new VertexElement(0, VertexElementFormat.Vector4,
                VertexElementUsage.Position, 1);

            // Texture coordinate
            _instanceStreamElements[1] = new VertexElement(sizeof(float) * 4, VertexElementFormat.Vector2,
                VertexElementUsage.TextureCoordinate, 1);

            instanceVertexDeclaration = new VertexDeclaration(_instanceStreamElements);
        }

        /// <summary>
        /// Initialize all the cube instance. (sizeX * sizeZ)
        /// </summary>

        /*
        private void InitializeInstances()
        {
            this.instances = new CubeInfo[InstanceCount()];
            var instance = new List<CubeInfo>();

            foreach (Vector3 pos in BoxPositions)
            {
                var cubeinfo = new CubeInfo() { World = new Vector4(new Vector3(pos.X * 2f, pos.Y *2f, pos.Z *2f), 1f), AtlasCoordinate = new Vector2(0, 0) };
                Vector3 vec = new Vector3(pos.X , pos.Y , pos.Z );
                Vector3 max = vec + (Vector3.One);
                BoundingBox box = new BoundingBox(vec, max);
                BoundingBoxes.Add(box);

                int boxX = (int)(pos.X / 16);
                int boxY = (int)(pos.Y / 256);
                int boxZ = (int)(pos.Z / 16);
                Vector3 chunkpos = new Vector3(boxX, boxY, boxZ);
                Chunk chunk;
                if ( ChunkDictionary.TryGetValue(chunkpos, out chunk)) // chunk already exists in which to store our block
                    ChunkDictionary[chunkpos].Boxes.Add(box); // add our block to the chunk
                else
                {
                    Chunk newChunk = new Chunk(); // 
                    newChunk.XYZ = chunkpos; // else create a new chunk with the required position
                    newChunk.Boxes.Add(box);
                    ChunkDictionary.Add(chunkpos, newChunk);
                }
                instance.Add(cubeinfo);
                
            }

            System.Console.WriteLine(string.Format("chunk count: {0}", ChunkDictionary.Count()));
            // Set the instace data to the instanceBuffer.
     
            this.instanceBuffer = new DynamicVertexBuffer(this.GraphicsDevice, instanceVertexDeclaration, InstanceCount(), BufferUsage.WriteOnly);
            this.instanceBuffer.SetData(instance.ToArray());
        }
        */
        /// <summary>
        ///     Generate the common cube geometry. (Only one cube)
        /// </summary>
        private void GenerateCommonGeometry()
        {
            var _vertices = new VertexPositionTexture[24];

            #region filling vertices

            // top face of block
            _vertices[0].Position = new Vector3(-1, 1, -1) + Vector3.One;
            _vertices[0].TextureCoordinate = new Vector2(0, 0);
            _vertices[1].Position = new Vector3(1, 1, -1) + Vector3.One;
            _vertices[1].TextureCoordinate = new Vector2(1, 0);
            _vertices[2].Position = new Vector3(-1, 1, 1) + Vector3.One;
            _vertices[2].TextureCoordinate = new Vector2(0, 1);
            _vertices[3].Position = new Vector3(1, 1, 1) + Vector3.One;
            _vertices[3].TextureCoordinate = new Vector2(1, 1);

            // bottom face of block
            _vertices[4].Position = new Vector3(-1, -1, 1) + Vector3.One;
            _vertices[4].TextureCoordinate = new Vector2(0, 0);
            _vertices[5].Position = new Vector3(1, -1, 1) + Vector3.One;
            _vertices[5].TextureCoordinate = new Vector2(1, 0);
            _vertices[6].Position = new Vector3(-1, -1, -1) + Vector3.One;
            _vertices[6].TextureCoordinate = new Vector2(0, 1);
            _vertices[7].Position = new Vector3(1, -1, -1) + Vector3.One;
            _vertices[7].TextureCoordinate = new Vector2(1, 1);

            // right face of block
            _vertices[8].Position = new Vector3(-1, 1, -1) + Vector3.One;
            _vertices[8].TextureCoordinate = new Vector2(0, 0);
            _vertices[9].Position = new Vector3(-1, 1, 1) + Vector3.One;
            _vertices[9].TextureCoordinate = new Vector2(1, 0);
            _vertices[10].Position = new Vector3(-1, -1, -1) + Vector3.One;
            _vertices[10].TextureCoordinate = new Vector2(0, 1);
            _vertices[11].Position = new Vector3(-1, -1, 1) + Vector3.One;
            _vertices[11].TextureCoordinate = new Vector2(1, 1);

            // back face of block
            _vertices[12].Position = new Vector3(-1, 1, 1) + Vector3.One;
            _vertices[12].TextureCoordinate = new Vector2(0, 0);
            _vertices[13].Position = new Vector3(1, 1, 1) + Vector3.One;
            _vertices[13].TextureCoordinate = new Vector2(1, 0);
            _vertices[14].Position = new Vector3(-1, -1, 1) + Vector3.One;
            _vertices[14].TextureCoordinate = new Vector2(0, 1);
            _vertices[15].Position = new Vector3(1, -1, 1) + Vector3.One;
            _vertices[15].TextureCoordinate = new Vector2(1, 1);
            // left face of block
            _vertices[16].Position = new Vector3(1, 1, 1) + Vector3.One;
            _vertices[16].TextureCoordinate = new Vector2(0, 0);
            _vertices[17].Position = new Vector3(1, 1, -1) + Vector3.One;
            _vertices[17].TextureCoordinate = new Vector2(1, 0);
            _vertices[18].Position = new Vector3(1, -1, 1) + Vector3.One;
            _vertices[18].TextureCoordinate = new Vector2(0, 1);
            _vertices[19].Position = new Vector3(1, -1, -1) + Vector3.One;
            _vertices[19].TextureCoordinate = new Vector2(1, 1);
            // front face of block
            /*_vertices[20].Position = new Vector3(1, 1, -1) + Vector3.One;
            _vertices[20].TextureCoordinate = new Vector2(0, 0);
            _vertices[21].Position = new Vector3(-1, 1, -1) + Vector3.One;
            _vertices[21].TextureCoordinate = new Vector2(1, 0);
            _vertices[22].Position = new Vector3(1, -1, -1) + Vector3.One;
            _vertices[22].TextureCoordinate = new Vector2(0, 1);
            _vertices[23].Position = new Vector3(-1, -1, -1) + Vector3.One;
            _vertices[23].TextureCoordinate = new Vector2(1, 1);*/

            #endregion

            geometryBuffer = new DynamicVertexBuffer(GraphicsDevice, VertexPositionTexture.VertexDeclaration,
                24, BufferUsage.WriteOnly);
            geometryBuffer.SetData(_vertices);

            #region filling indices

            var _indices = new int[36];
            _indices[0] = 0;
            _indices[1] = 1;
            _indices[2] = 2;
            _indices[3] = 1;
            _indices[4] = 3;
            _indices[5] = 2;

            _indices[6] = 4;
            _indices[7] = 5;
            _indices[8] = 6;
            _indices[9] = 5;
            _indices[10] = 7;
            _indices[11] = 6;

            _indices[12] = 8;
            _indices[13] = 9;
            _indices[14] = 10;
            _indices[15] = 9;
            _indices[16] = 11;
            _indices[17] = 10;

            _indices[18] = 12;
            _indices[19] = 13;
            _indices[20] = 14;
            _indices[21] = 13;
            _indices[22] = 15;
            _indices[23] = 14;

            _indices[24] = 16;
            _indices[25] = 17;
            _indices[26] = 18;
            _indices[27] = 17;
            _indices[28] = 19;
            _indices[29] = 18;

            _indices[30] = 20;
            _indices[31] = 21;
            _indices[32] = 22;
            _indices[33] = 21;
            _indices[34] = 23;
            _indices[35] = 22;

            #endregion

            indexBuffer = new IndexBuffer(GraphicsDevice, typeof(int), 36, BufferUsage.WriteOnly);
            indexBuffer.SetData(_indices);
        }

        #endregion

        #region OVERRIDE METHODS

        /// <summary>
        ///     Initialize the CubeMap.
        /// </summary>
        public override void Initialize()
        {
            InitializeInstanceVertexBuffer();
            GenerateCommonGeometry();
            //this.InitializeInstances();

            // Creates the binding between the geometry and the instances.

            bindings = new VertexBufferBinding[2];
            bindings[0] = new VertexBufferBinding(geometryBuffer);
            bindings[1] = new VertexBufferBinding(instanceBuffer, 0, 1);

            base.Initialize();
        }

        /// <summary>
        ///     Load the CubeMap effect and texture.
        /// </summary>
        protected override void LoadContent()
        {
            effect = Game.Content.Load<Effect>("instance_effect");
            texture = Game.Content.Load<Texture2D>("mcgrass");

            base.LoadContent();
        }

        /// <summary>
        ///     Update the CubeMap logic.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        /// <summary>
        ///     Draw the cube map using one single vertexbuffer.
        /// </summary>
        /// <param name="gameTime"></param>
        public void DrawAll(GameTime gameTime)
        {
            // Set the effect technique and parameters

            effect.CurrentTechnique = effect.Techniques["Instancing"];
            effect.Parameters["WorldViewProjection"].SetValue(Main.BasePlayer.View * Main.BasePlayer.Projection);
            effect.Parameters["cubeTexture"].SetValue(texture);

            // Set the indices in the graphics device.
            GraphicsDevice.Indices = indexBuffer;

            // Apply the current technique pass.
            effect.CurrentTechnique.Passes[0].Apply();

            // Set the vertex buffer and draw the instanced primitives.
            GraphicsDevice.SetVertexBuffers(bindings);
            GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 24, 0, 12, InstanceCount());

            //base.Draw(gameTime);
        }

        #endregion
    }
}