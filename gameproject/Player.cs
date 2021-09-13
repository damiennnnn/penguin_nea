using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
namespace gameproject
{
    public class Player : Entity // player entity should be treated as every other entity
    {
        public Matrix View; // view matrix to use for 3d camera
        public Matrix Projection; // projection matrix to use for 3d camera;
        public Matrix Rotate;

        public float horizVel = 0f;
        public float vertVel = 0f;

        Vector3 viewPunch = Vector3.Zero;
        public Vector3 ViewVec = Vector3.Zero;
        MouseState PrevState; // previous mouse state for calculating mouse input

        public List<Block> nearby = new List<Block>();

        public float FOV = 75f;
        public float Sensitivity = 0.2f;
        float MouseSens { get => Sensitivity/MathHelper.ToRadians(FOV) ;} // 
        public Player(Vector3 pos, string model, string texture) : base(pos,model,texture){ // use base constructor as the player is an entity 
            View = Matrix.CreateLookAt(Position, new Vector3(0, 0, 0), Vector3.Up); // create view matrix, looking at centre
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FOV), Main.Resolution.Width / (float)Main.Resolution.Height, 0.001f, 25f);
            // field of view (65 degrees), current aspect ratio (window width divided by window height)
            UpdateSurroundings();
        }

        public void UpdateFOV() { Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FOV), Main.Resolution.Width / (float)Main.Resolution.Height, 0.001f, 1000f); } // update the projection matrix if the fov changes
        public void UpdateFOV(float fov) { Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fov), Main.Resolution.Width / (float)Main.Resolution.Height, 0.001f, 1000f); } // update the projection matrix if the fov changes

        bool repeatOnce = false;

        bool[] MovementDirections = new bool[4]{
            false, false, false, false }; // forward, back, left, right
        // start applying velocity in the given directions


        void KeyboardInput(GameTime gameTime)
        {
            Vector3 Cross = Vector3.Cross(Vector3.Up, WorldPosition.Forward); // calculate cross product, the direction that will be perpendicular to two other lines

            Vector3 ForwardVel = Vector3.Zero;

            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                ForwardVel += WorldPosition.Forward; // add the forward position to the player to move forward 
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                ForwardVel -= WorldPosition.Forward; // subtract the forward position to go backwards
            }

            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                ForwardVel += Cross; // add the cross product to the player position, pointing left, to move left 
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                ForwardVel -= Cross; // subtract the cross product to move right
            }

            ForwardVel *= MovementSpeed; // multiply our movement vector by the defined movement speed

            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                ForwardVel *= 2f; // double movement speed when shift is pressed, sprinting

            Velocity += (ForwardVel);
        }

        public Vector3 Vel = Vector3.Zero;

        new void HandleMovement(GameTime gameTime)
        {
            //KeyboardInput(gameTime);
            Vector3 Cross = Vector3.Cross(Vector3.Up, WorldPosition.Forward); // calculate cross product, the direction that will be perpendicular to two other lines

            Vector3 MovementDir = Vector3.Zero;

            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                MovementDir += WorldPosition.Forward; // add the forward position to the player to move forward 
            }
            if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                MovementDir -= WorldPosition.Forward; // subtract the forward position to go backwards
            }

            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                MovementDir += Cross; // add the cross product to the player position, pointing left, to move left 
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                MovementDir -= Cross; // subtract the cross product to move right
            }

            if (MovementDir != Vector3.Zero)
                MovementDir.Normalize(); // get our direction vector

            if (Keyboard.GetState().IsKeyDown(Keys.Space)) // add upwards vector to velocity for jumping
            {
                //if (!repeatOnce)
                //{
                    jumpstate = true;
                    repeatOnce = true;
                //}
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Space))
            {
                repeatOnce = false;
                jumpstate = false;
            }

/*
            Velocity = HandlePhysics(gameTime, Velocity);

            if (OnGround)
                Velocity = Physics.MoveGround(MovementDir, Velocity, gameTime);
            else
                Velocity = Physics.MoveAir(MovementDir, Velocity, gameTime);

            Position += (Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds);// multiply the movement vector by the time between updates and add to position, to compensate for any speed up or slowdown in performance and for consistent physics
            */

            base.HandleMovement(gameTime, MovementDir);
            var test = new Vector3(Velocity.X, 0f, Velocity.Z);
            horizVel = test.Length();
           
            test = new Vector3(0f, Velocity.Y, 0f);
            vertVel = test.Length();
        }

        // all for testing items/guns, will clean up
        public bool LeftMouseClick = false;
        public bool RightMouseClick = false;
        void HandleMouseInputItems(GameTime gameTime)
        {
            EyeAngles.X -= MouseSens * (Mouse.GetState().X - PrevState.X); // mouse moves by the difference between mouse positions 
            if (EyeAngles.X > 180f) EyeAngles.X = (-180f + (EyeAngles.X % 180f)); // eye angles are capped between -180 to 180 horizontal
            if (EyeAngles.X < -180f) EyeAngles.X = (180f - (EyeAngles.X % 180f));
            EyeAngles.Y -= MouseSens * (Mouse.GetState().Y - PrevState.Y);
            if (EyeAngles.Y >= 90f) EyeAngles.Y = 89f; // eye angles are capped between -89 and 89 vertical
            if (EyeAngles.Y <= -90f) EyeAngles.Y = -89f;

            Mouse.SetPosition((int)Main.WindowCentre.X, (int)Main.WindowCentre.Y);
            PrevState = Mouse.GetState();

            if (LeftMouseClick)
            {
                LeftClickEvent(gameTime);
                if (Mouse.GetState().LeftButton == ButtonState.Released)
                {
                    LeftMouseClick = false; // do an action when the left mouse button is released
                }
            }

            if (Mouse.GetState().LeftButton == ButtonState.Pressed && !LeftMouseClick)
            {
                LeftMouseClick = true;
            }
            else if (Mouse.GetState().LeftButton == ButtonState.Released)
            {

            }


            if (RightMouseClick) // do an action when the right mouse button is pressed
            {
                    RightClickEvent();
                if (Mouse.GetState().RightButton == ButtonState.Released)
                {
                    RightMouseClick = false; // do an action when the right mouse button is released
                }
            }

            if (Mouse.GetState().RightButton == ButtonState.Pressed)
                RightMouseClick = true;

        }

        public void RightClickEvent()
        {

        }

        public Vector3 CastRayFromLook(float dist)
        {
            var lookDirection = (Position + Vector3.Up + EyePosition);


            Vector3 endPoint = (lookDirection + (Rotate.Forward * dist));

            return endPoint;
        }
        public void LeftClickEvent(GameTime gameTime)
        {
            GetCurrentlyHeld().Fire(this, gameTime);
        }
        public new void Update(GameTime gameTime) // override update for player entity
        {
            if (prevChunkPos == new Vector3(0, -1, 0))
            {
                Rendering = GetSurroundingChunks();
                UpdateSurroundings();
            }


            WorldData.Chunk chunk;
            bool foundChunk = WorldData.GetChunk(Position, out chunk);
            if (foundChunk)
            {
                prevChunkPos = GetCurChunkPos();
            }
            // apply y-axis rotation to player camera, not to the world model
            if (Main.MouseInput) // only handle input if the console is closed
            {
                HandleMouseInputItems(gameTime); // eye angle/mouse movement routine
                HandleMovement(gameTime); // WASD movement for the player
            }

            foreach (var item in Inventory)
                item.Update(this, gameTime);

            DoCollision(); // share collision method with entities

            var RotateMatrix = Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(EyeAngles.X)); // create a rotation matrix based on the eye angles
            var World = Matrix.CreateTranslation(Position); // create world matrix from current position

            WorldPosition = RotateMatrix * World; // apply x-axis rotation to world model

            EyePosition = WorldPosition.Forward; // set eye position to forward direction of model

            EyePosition = Vector3.Transform(EyePosition, Matrix.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, WorldPosition.Forward), MathHelper.ToRadians(-EyeAngles.Y)));
            EyePosition.Normalize();
            var rotate2 = Matrix.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, WorldPosition.Forward), MathHelper.ToRadians(-EyeAngles.Y));

            Rotate = RotateMatrix * rotate2 * Matrix.CreateTranslation(Position + Vector3.Up);

            View = Matrix.CreateLookAt(Position + Vector3.Up, Position + Vector3.Up + EyePosition, Vector3.Up); // set view matrix to the direction where the player is pointing
            //View = Matrix.CreateLookAt(new Vector3(10, 1, 10), Position + Vector3.Up, Vector3.Up);; // set view matrix to the direction where the player is pointing
            foundChunk = WorldData.GetChunk(Position, out chunk);
            
            if (foundChunk)
            {
                var XYZ = GetCurChunkPos();
                if (XYZ != prevChunkPos)
                {
                    prevChunkPos = XYZ;
                    UpdateSurroundings(); // only update our surrounding chunks when necessary
                    Rendering = GetSurroundingChunks();
                }
            }

            if (SurroundingBoxes.Count > 0)
                GetLookingAt();
        }
        public List<Block> ImmediateNeighbours = new List<Block>();
        public Dictionary<Vector3, WorldData.Chunk> Rendering = new Dictionary<Vector3, WorldData.Chunk>();
        public BoundingBox LookingAt = new BoundingBox();

        static Vector3[] chunkDirections = new Vector3[]
{
            new Vector3(0,0,1), // forward, chunk ahead of us
            new Vector3(0,0,-1), // back, chunk behind us
            new Vector3(-1,0,0), // left, chunk to the left of us
            new Vector3(1, 0, 0), // right, chunk to the right of us
            new Vector3(-1, 0, 1), // forwardleft
            new Vector3(1, 0, 1), // forwardright
            new Vector3(-1, 0, -1), // backleft
            new Vector3(1, 0, -1) // backright
};
        public Dictionary<Vector3, WorldData.Chunk> GetSurroundingChunks()
        {
            Dictionary<Vector3, WorldData.Chunk> keyValuePairs = new Dictionary<Vector3, WorldData.Chunk>();
            Vector3 curChunkPos = GetCurChunkPos();
            if (WorldData.ChunkDictionary.TryGetValue(curChunkPos, out var curChunk))
                keyValuePairs.Add(curChunkPos, curChunk);

            int radius = Settings.RenderDistance;
            int rOver2 = (radius / 2);
            for (int x = -rOver2; x < rOver2; x++)
            {
                for (int z = -rOver2; z < rOver2; z++)
                {
                    Vector3 newPos = (curChunkPos) + new Vector3(x, 0, z);
                    if (newPos == curChunkPos) continue;

                    if (WorldData.ChunkDictionary.TryGetValue(newPos, out var chunk))
                        keyValuePairs.Add(newPos, chunk);
                }
            }    
            return keyValuePairs;
        }

        class RaySearch
        {
            public Block block;
            public BoundingBox box;
            public float dist;
        }

        public Block lookingAt;
        void GetLookingAt()
        {
            List<RaySearch> allIntersect = new List<RaySearch>();
            var nearby = ImmediateNeighbourBoxes(5);
            for (int i = 0; i < nearby.Count; i++)
            {
                
                var box = new BoundingBox(nearby[i].XYZ, nearby[i].XYZ + Vector3.One);
                Ray newRay = new Ray(Position + Vector3.Up, EyePosition);
                float? dist = box.Intersects(newRay);
                if (dist != null)
                    {
                        RaySearch intersect = new RaySearch();
                        intersect.block = nearby[i];
                        intersect.dist = (float)dist;
                        intersect.box = box;
                        allIntersect.Add(intersect);
                    }
                
            }
            if (allIntersect.Count > 0)
            {
                allIntersect = allIntersect.OrderBy(x => x.dist).ToList();
                LookingAt = allIntersect[0].box;
                lookingAt = allIntersect[0].block;  
            }
            else
            {
                LookingAt = new BoundingBox();
                lookingAt = null;
            }
        }
    }
}
