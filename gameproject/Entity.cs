using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Jitter;
using Jitter.Collision;
using Jitter.LinearMath;

using Microsoft.Xna.Framework.Input;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;

namespace gameproject
{
    [Serializable]
    public class Entity
    {

        public string Name = string.Empty; // name of entity
        public string ID = "";   // unique ID for entity

        public float Health = 100f;
        public float ShieldHealth = 100f; // entity can have both health and shield health on top
        public TimeSpan LastHitTime = new TimeSpan();
        // item interaction
        public int HeldItemIndex = 0;
        public List<Gun> Inventory = new List<Gun>();

        // physics-related fields
        public Vector3 Position = Vector3.Zero; // position in world as vector
        public Matrix WorldPosition = Matrix.Identity; // position in world as matrix
        public Matrix Rotation = Matrix.Identity; // rotation around position as matrix
        public Vector3 Velocity = Vector3.Zero; // entity movement velocity
        public Vector2 EyeAngles = Vector2.Zero; // entity camera angles in degrees (x - horizontal, y - vertical)
        public Vector3 EyePosition = Vector3.Zero; // entity eye position in world
        public Vector3 Acceleration = Vector3.Zero; // entity movement acceleration
        public BoundingBox ModelBoundingBox; // bounding box for the model
        public BoundingBox Box; // entity bounding box
        public float MovementSpeed = 4f; // movement speed of the entity
        public float Scale = 1f; // scale of entity size
        public bool OnGround = false; // can the entity
                                      // or not
        public Shape shape;
        public RigidBody rigidBody;

        public Entity LastHitEnt;

        public List<Block> SurroundingBoxes = new List<Block>();
        public List<WorldData.Chunk> SurroundingChunks = new List<WorldData.Chunk>();
        public Dictionary<Vector3, Block> BlockDictionary = new Dictionary<Vector3, Block>();

        public bool[] ApplyInDirection = new bool[4] { false, false, false, false }; // 0 - up, 1 - down, 2 - left, 3 - right

        public string Model = "penguin";
        public string Texture = "penguin";

        public int Miss = 0;
        public int Hit = 0;

        public float WeaponAccuracy() {
            int total = (Miss + Hit);
            return ((float)Hit / (float)total);
        }
        public Vector3 Tint = Vector3.Zero; // colour tint
        bool Movement = false;

        public Gun GetCurrentlyHeld()
        {
            return Inventory[HeldItemIndex];
        }
        public Entity(Vector3 pos, string model, string texture, string name = "", bool Move = false)
        {
            Position = pos;
            Model = model;
            Texture = texture;
            Name = name;
            WorldPosition = Matrix.CreateWorld(pos, Vector3.Forward, Vector3.Up);
            Movement = Move;
            // generate unique id
            var guid = Guid.NewGuid();
            ID = "";
            // testing
            //CustomInterpreter.WriteLine(string.Format("new entity name: {0} id: {1}", Name, ID));
            SetupObject(); // create bounding box for entity from model
            UpdateSurroundings();
        }

        Vector3 gravity = Vector3.Zero;
        public float fallTime = 0f;

        double elapsed = double.MaxValue;
        public bool jumpstate = false;
        public bool midjump = false;


        public double timer = 0;

        public Vector3 GetCurChunkPos()
        {
            int boxX = (int)(Position.X / ChunkSetup.ChunkHorizontal);
            int boxY = (int)(Position.Y / ChunkSetup.ChunkVertical);
            int boxZ = (int)(Position.Z / ChunkSetup.ChunkHorizontal);
            Vector3 chunkPos = new Vector3(boxX, boxY, boxZ);

            return chunkPos;
        }

        public bool GetCurrentChunk(out WorldData.Chunk chunk)
        {            
            int boxX = (int)(Position.X / ChunkSetup.ChunkHorizontal);
            int boxY = (int)(Position.Y / ChunkSetup.ChunkVertical);
            int boxZ = (int)(Position.Z / ChunkSetup.ChunkHorizontal);

            Vector3 chunkPos = new Vector3(boxX, boxY, boxZ);

            //CubeMap.Chunk chunk2 = new CubeMap.Chunk() { XYZ = Vector3.Zero };

            bool found = WorldData.ChunkDictionary.TryGetValue(chunkPos, out chunk);

            return found;
        }

        public Vector3 DoJump(GameTime gameTime)
        {
            /*Vector3 JumpVector = Vector3.Zero;
            if (jumpstate && CanJump)
            {
                if (gameTime.TotalGameTime.TotalMilliseconds < elapsed)
                    elapsed = gameTime.TotalGameTime.TotalMilliseconds;
                else if ((gameTime.TotalGameTime.TotalMilliseconds - elapsed) > 100)
                {
                    elapsed = double.MaxValue;
                    jumpstate = false;
                    midjump = false;
                    return JumpVector;
                }
                midjump = true;

                JumpVector = Vector3.Up * 1000f;
            }

            return JumpVector;*/
            return Vector3.Zero;
        }


        float tVelocity = 50f; // terminal falling velocity 
        float gravityStrength = 16f; // multiplier for gravity
        float jumpHeight = 1.5f;

        public Vector3 jumpVector = Vector3.Zero;
        public Vector3 gravVector = Vector3.Zero;

        public Vector3 jumpDirection = Vector3.Zero;


        public List<BoundingBox> touching = new List<BoundingBox>();
        
        public List<Block> ImmediateNeighbourBoxes(int radius = 2)
        {
            List<Block> boxes = new List<Block>();

            for (int x = -radius; x < radius; x++)
            {
                for (int y = -radius; y < radius; y++)
                {
                    for (int z = -radius; z < radius; z++) // will check 216 surrounding blocks for collisions
                    {
                        Vector3 relativePos = Position + new Vector3(x, y, z);
                        relativePos.Round();
                        if (BlockDictionary.TryGetValue(relativePos, out var box))
                            boxes.Add(box);
                    }
                }
            }

            return boxes;
        }
        public List<Block> NearbyBlocks = new List<Block>();

        

        public Vector3 HandlePhysics(GameTime gameTime, Vector3 vel)
        {
            OnGround = false;

            gravVector = (Vector3.Down * gravityStrength);
            gravVector *= (float)gameTime.ElapsedGameTime.TotalSeconds;
            bool doneCollision = false;
            //if (vel.Length() > 0.001f) { }
            NearbyBlocks = ImmediateNeighbourBoxes();
                for (int i = 0; i < NearbyBlocks.Count; i++)
                {
                var box = new BoundingBox(NearbyBlocks[i].XYZ, NearbyBlocks[i].XYZ + Vector3.One);



                if (box.Intersects(this.Box)) //box.Intersects(this.Box)
                {
                    var boxcentre = (Vector3.Lerp(box.Min, box.Max, 0.5f));
                    touching.Add(box);
                    bool above = (this.Box.Min.Y > boxcentre.Y); // entity is above box
                    bool below = (this.Box.Max.Y < boxcentre.Y); // entity is below box
                    bool forward = (this.Box.Max.X < boxcentre.X); // box is in front of entity
                    bool back = (this.Box.Min.X > boxcentre.X); // box is behind entity
                    bool left = (this.Box.Max.Z < boxcentre.Z); // box to the left of entity
                    bool right = (this.Box.Min.Z > boxcentre.Z); // box to the right of entity

                    Vector3 entCentre = Vector3.Lerp(this.Box.Min, this.Box.Max, 0.5f);
                    Vector3 dirVector = entCentre - boxcentre;


                    if (above)
                    {
                        if (vel.Y <= 0f)
                        {
                            vel.Y = 0f;
                            if (!forward && !back && !left && !right)
                                Position -= new Vector3(0, (this.Box.Min.Y - box.Max.Y), 0);
                            OnGround = true;
                            midjump = false;
                            gravVector = Vector3.Zero;
                        }
                        //vel = new Vector3(0, (box.Max.Y - this.Box.Min.Y), 0);
                        //doneCollision = true;
                    }
                    else if (below && (!doneCollision))
                    {
                        if (vel.Y >= 0f)
                        {
                            vel.Y = 0f;
                            //vel -= new Vector3(0, (this.Box.Max.Y - box.Min.Y), 0);
                            jumpVector = Vector3.Zero;
                        }
                        //doneCollision = true;
                    }
                    else if (forward && (!doneCollision))
                    {
                        vel.X = 0f;
                        Position -= new Vector3((this.Box.Max.X - box.Min.X), 0, 0);
                        //doneCollision = true;
                    }
                    else if (back && (!doneCollision))
                    {
                        vel.X = 0f;
                        Position += new Vector3((box.Max.X - this.Box.Min.X), 0, 0);
                        //doneCollision = true;
                    }
                    else if (left && (!doneCollision))
                    {
                        vel.Z = 0f;
                        Position -= new Vector3(0, 0, (this.Box.Max.Z - box.Min.Z));
                        //doneCollision = true;
                    }
                    else if (right && (!doneCollision))
                    {
                        vel.Z = 0f;
                        Position += new Vector3(0, 0, (box.Max.Z - this.Box.Min.Z));
                        //doneCollision = true;
                    }

                }
                }
            

            if (jumpstate && OnGround)
            {
                jumpstate = false;
                midjump = true;
                OnGround = false;
                //jumpVector = Vector3.Up * 4;
                float jump = (float)System.Math.Pow((2 * gravityStrength * jumpHeight), 0.5); // square root of -(2 * gravityStrength * jumpHeight)                
                jumpVector = new Vector3(0, jump, 0);
            }


            vel += (jumpVector);
            vel += gravVector;
            jumpVector = Vector3.Zero;

            if (-(vel.Y) > tVelocity) vel.Y = -(tVelocity); // dont allow our downwards velocity to exceed our terminal velocity
            return vel;
        }

        public void SetupObject() // updates the entity bounding box from the model for collisions 
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (ModelMesh mesh in Resources.Models[Model].Meshes)
            {
                foreach (ModelMeshPart mesh_part in mesh.MeshParts)
                {
                    int vertex_stride = mesh_part.VertexBuffer.VertexDeclaration.VertexStride;
                    int vertex_buffer_size = mesh_part.NumVertices * vertex_stride;
                    float[] vertex_data = new float[vertex_buffer_size / sizeof(float)];
                    mesh_part.VertexBuffer.GetData<float>(vertex_data);

                    for (int i = 0; i < vertex_buffer_size / sizeof(float); i += vertex_stride / sizeof(float))
                    {
                        Vector3 pos = new Vector3(vertex_data[i], vertex_data[i + 1], vertex_data[i + 2]);
                        min = Vector3.Min(min, pos);
                        max = Vector3.Max(max, pos);
                    } // calculate the minimum and maximum points from a model mesh

                }
            }

            var jvec = new Vector3(max.X, max.Y + 0.5f, max.Z);
            ModelBoundingBox = new BoundingBox(min, jvec);
        }

        public void DoCollision()
        {
            var min = ModelBoundingBox.Min + Position;
            var max = ModelBoundingBox.Max + Position;
            Box = new BoundingBox(min, max); // modify the existing bounding box instead of recreating it every frame, improved performance


        }
        
        public void HandleMovement(GameTime gameTime, Vector3? MoveDir)
        {
            //KeyboardInput(gameTime);
            Vector3 MovementDir = Vector3.Zero;

            if (MoveDir.HasValue)
                MovementDir = MoveDir.Value;

            if (MovementDir != Vector3.Zero)
                MovementDir.Normalize(); // get our direction vector

            Velocity = HandlePhysics(gameTime, Velocity);

            if (OnGround)
                Velocity = Physics.MoveGround(MovementDir, Velocity, gameTime);
            else
                Velocity = Physics.MoveAir(MovementDir, Velocity, gameTime);
            Position += (Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds);// multiply the movement vector by the time between updates and add to position, to compensate for any speed up or slowdown in performance and for consistent physics
        }

        public Vector3 prevChunkPos = new Vector3(0,-1,0);
        public void Update(GameTime gameTime) {

            //if (Movement)
            //HandleMovement(gameTime); // copy of player movement code, for testing
            if (prevChunkPos == new Vector3(0, -1, 0))
                UpdateSurroundings();

            WorldData.Chunk chunk;
            bool foundChunk = WorldData.GetChunk(Position, out chunk);
            if (foundChunk)
            {
                prevChunkPos = chunk.XYZ;
            }

            if ((gameTime.TotalGameTime.TotalSeconds - LastHitTime.TotalSeconds) > 3)
            {
                if (ShieldHealth < 0f) ShieldHealth = 0f;
                ShieldHealth += (15 * (float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            if (ShieldHealth > 100) ShieldHealth = 100;

            Vector3 Cross = Vector3.Cross(Vector3.Up, WorldPosition.Forward); // calculate cross product, the direction that will be perpendicular to two other lines
            Vector3 MovementDir = Vector3.Zero;
            if (Keyboard.GetState().IsKeyDown(Keys.I))
            {
                MovementDir += WorldPosition.Forward; // add the forward position to the player to move forward 
            }
            if (Keyboard.GetState().IsKeyDown(Keys.K))
            {
                MovementDir -= WorldPosition.Forward; // subtract the forward position to go backwards
            }

            if (Keyboard.GetState().IsKeyDown(Keys.J))
            {
                MovementDir += Cross; // add the cross product to the player position, pointing left, to move left 
            }
            if (Keyboard.GetState().IsKeyDown(Keys.L))
            {
                MovementDir -= Cross; // subtract the cross product to move right
            }
            HandleMovement(gameTime, MovementDir);
            var RotateMatrix = Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(EyeAngles.X)); // create a rotation matrix based on the eye angles
            var World = Matrix.CreateTranslation(Position); // create world matrix from current position
            WorldPosition = RotateMatrix * World; // apply x-axis rotation to world model
            DoCollision(); // handle bounding box updates and do collision with the world

            foundChunk = WorldData.GetChunk(Position, out chunk);
            if (foundChunk)
            {
                if (chunk.XYZ != prevChunkPos)
                {
                    prevChunkPos = chunk.XYZ;
                    UpdateSurroundings(); // only update our surrounding chunks when necessary
                }
            }
        } // update entity every frame

        public void UpdateSurroundings()
        {
            if (WorldData.ChunkDictionary.Count == 0) return;
            WorldData.GetBoundingsAndChunks(this, out SurroundingChunks, out BlockDictionary);
            SurroundingBoxes = BlockDictionary.Values.ToList();
        }

        public bool IsInView(Player player, Matrix ViewProj)
        {
            return new BoundingFrustum(ViewProj).Intersects(Box);
        }
        public void Render(Player player, Matrix view, Matrix proj)
        {
            Vector3 diffuseDir = (Main.BasePlayer.Position + Vector3.Up);
            diffuseDir -= Position;
            diffuseDir.Normalize();
            foreach (ModelMesh Mesh in Resources.Models[Model].Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Vector3.One;
                    Effect.View = view;
                    Effect.Projection = proj;
                    Effect.World = Matrix.CreateScale(Scale, Scale, Scale) * WorldPosition;

                    Effect.LightingEnabled = true;


                    Effect.Texture = Resources.Textures[Texture];
                    Effect.TextureEnabled = true;
                }
                Mesh.Draw();
            }
        }
    }
}
