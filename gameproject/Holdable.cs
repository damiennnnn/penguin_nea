using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
namespace gameproject
{
    public enum Types
    {
        shotgun = 0,
        sword,
        rifle,
        biggun
    }

    public class Gun {
        public Gun(Model model, Vector3 relative)
        {
            Position = relative;
            Model = model;
        }

        public Model Model; // model to use for rendering 
        public Vector3 Position; // position of viewmodel relative to centre of view

        public Color Colour = Color.White;
        public Matrix WorldMatrix = Matrix.Identity; // matrix used for rendering and applying rotations
        public Vector3 Translation; // translation of the world matrix as a vector

        public Color ProjectileColour; // colour of projectile fired
        public float ProjectileVel; // velocity of the projectile

        public int ProjectileCount; // count of projectiles in reserve
        public int MaxProjCount; // maximum projectile count
        public SoundEffect UseSound; // sound effect to play on fire

        public float Accuracy; // accuracy of the weapon, randomised spread
        public float RecoilFactor; // recoil factor of the weapon, weapon kick
        public float RecoilPunch; // current recoil punch
        public float RecoilLimit; // max recoil punch
        public float TimeBetweenShots; // determine fire rate of weapon
        public float ActionTimer = -1000f;
        public float Scale = 0.025f;
        public float Rotation = 90f;
        public float RotationY = 0f;
        public virtual void Update(Player player, GameTime gameTime)
        {

        }
        public virtual void Render(Player player)
        {
            foreach (ModelMesh Mesh in Model.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3();
                    Effect.View = player.View;
                    Effect.Projection = player.Projection;
                    Effect.World = WorldMatrix;
                }
                Mesh.Draw();
            }
        }
        public virtual void HandleMatrix(Player player) { }
        public virtual void Fire(Player player, GameTime gameTime) {

        }
        public void Reload()
        {
            ProjectileCount = MaxProjCount;
        }
    }

    public class Rifle : Gun {

        public override void Update(Player player, GameTime gameTime)
        {
            ActionTimer -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            RecoilPunch -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (RecoilPunch < 0f) RecoilPunch = 0f;
            if (ActionTimer < -1000f) ActionTimer = -1000f;
            HandleMatrix(player);
        }
        public override void Render(Player player)
        {
            HandleMatrix(player);
            foreach (ModelMesh Mesh in Model.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3();
                    Effect.View = player.View;
                    Effect.Projection = player.Projection;
                    Effect.World = WorldMatrix;
                }
                Mesh.Draw();
            }
        }
        const float swayFactor = 0.0015f;

        public override void HandleMatrix(Player player) // setting up the world matrix for rendering 
        {
            float RecoilMovement = ActionTimer;
            if (RecoilMovement < 0f) RecoilMovement *= 0.03f;


            WorldMatrix = Matrix.CreateScale(0.025f, 0.025f, 0.025f) * Matrix.CreateRotationX(MathHelper.ToRadians(0f));
            WorldMatrix *= Matrix.CreateRotationY(MathHelper.ToRadians(90f));
            WorldMatrix *= Matrix.CreateRotationX(MathHelper.ToRadians(RecoilMovement / 30f));
            
            Vector3 sway = player.Velocity; // sway, item appears to move along with entity movement
            sway *= swayFactor;

            WorldMatrix *= Matrix.CreateTranslation(Position 
                + new Vector3(0f, RecoilMovement * 0.00010f, RecoilMovement * 0.00010f));

            WorldMatrix *= player.Rotate;
            WorldMatrix *= Matrix.CreateTranslation(-sway);
            Translation = WorldMatrix.Translation; // create a vector3 from our world matrix 
        }

        public override void Fire(Player player, GameTime gameTime)
        {
            float elapsedMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds; // 
            if (ProjectileCount <= 0) return;
            if (ActionTimer > 0) return;
            
            float[] randoms = new float[3];

            for (int i = 0; i < 3; i++)
                randoms[i] = (float)Main.RNG.NextDouble() * Accuracy; // generate random spread

            var vec = new Vector3(randoms[0] - (Accuracy / 2f), randoms[1] - (Accuracy / 2f), randoms[2] - (Accuracy / 2f));

            HandleMatrix(Main.BasePlayer);
            Main.BasePlayer.EyeAngles.Y += 0.4f;
            float x = (float)Main.RNG.NextDouble();
            x -= (x / 2);
            x /= 4;
            Main.BasePlayer.EyeAngles.X += x;
            var scale = new Vector3(0.025f, 0.025f, 0.025f);

            Projectile proj = new Projectile(
                Translation + (Vector3.Up * 0.05f),
                player.EyePosition + (vec * 0.2f),
                new Vector3(0, -5f, 0),
                ProjectileVel, 2f,
                ProjectileColour,
                player.EyePosition,
                scale,
                12f,
                player);
            
            Main.Projectiles.Add(proj); // create a new projectile from player position and eye position

            float actionTime = (TimeBetweenShots - elapsedMs);
            if (actionTime < 0f) actionTime = 0f;
            ActionTimer = actionTime;

            var recoil = (RecoilFactor);
            /*
            if ((player.EyeAngles.Y + recoil) >= 90f) 
                player.EyeAngles.Y = 89f;
            else
                player.EyeAngles.Y += recoil;

            RecoilPunch += recoil;
            */

            var rand = (float)Main.RNG.Next(-100, 100);
            rand /= 1000f;

            var pitch = (1 / ((float)ProjectileCount) * 0.9f) + rand; // slight modulation to weapon fire pitch
            UseSound.Play(0.2f, pitch, 1.0f);
            ProjectileCount -= 1;

            HandleMatrix(player);
        }
        
        public Rifle(Model model, Vector3 relative) : base(model, relative) // call base constructor from here, do things specific to this type
        {
            Accuracy = 0.04f;
            ProjectileColour = Color.Cyan;
            TimeBetweenShots = 75; // milliseconds
            MaxProjCount = 40;
            ProjectileCount = 40;
            RecoilFactor = 0.064f;
            RecoilLimit = 8000f;
            ProjectileVel = 120f;
        }
    }


    public class Biggun : Gun {


        public override void Update(Player player, GameTime gameTime)
        {
            ActionTimer -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            RecoilPunch -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (RecoilPunch < 0f) RecoilPunch = 0f;
            if (ActionTimer < -1000f) ActionTimer = -1000f;
            HandleMatrix(player);
        }
        public override void Render(Player player)
        {
            HandleMatrix(player);
            foreach (ModelMesh Mesh in Model.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3();
                    Effect.View = player.View;
                    Effect.Projection = player.Projection;
                    Effect.World = WorldMatrix;
                }
                Mesh.Draw();
            }
        }
        const float swayFactor = 0.002f;
        public override void HandleMatrix(Player player)
        {
            float RecoilMovement = ActionTimer;
            if (RecoilMovement < 0f) RecoilMovement *= 0.03f;

            WorldMatrix = Matrix.CreateScale(0.20f, 0.40f, 0.40f) * Matrix.CreateRotationX(MathHelper.ToRadians(0f));
            WorldMatrix *= Matrix.CreateRotationY(MathHelper.ToRadians(0f));
            WorldMatrix *= Matrix.CreateRotationX(MathHelper.ToRadians(RecoilMovement / 30f));
            Vector3 sway = player.Velocity;
            sway *= swayFactor;

            WorldMatrix *= Matrix.CreateTranslation(Position
                + new Vector3(0f, RecoilMovement * 0.00010f, RecoilMovement * 0.00010f));

            WorldMatrix *= player.Rotate;
            WorldMatrix *= Matrix.CreateTranslation(-sway);

            Translation = WorldMatrix.Translation;
        }

        public override void Fire(Player player, GameTime gameTime)
        {
            float elapsedMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (ProjectileCount <= 0) return;
            if (ActionTimer > 0) return;

            float[] randoms = new float[3];

            for (int i = 0; i < 3; i++)
                randoms[i] = (float)Main.RNG.NextDouble() * Accuracy; // generate random spread

            var vec = new Vector3(randoms[0] - (Accuracy / 2f), randoms[1] - (Accuracy / 2f), randoms[2] - (Accuracy / 2f));

            HandleMatrix(Main.BasePlayer);
            Main.BasePlayer.EyeAngles.Y += 0.6f;

            var scale = new Vector3(0.02f, 0.02f, 0.8f);
            scale *= 2f;
            Projectile proj = new Projectile(Translation + (Vector3.Up * 0.05f), player.EyePosition + (vec * 0.2f), new Vector3(0, -0f, 0), ProjectileVel, 5f, ProjectileColour, player.EyePosition, scale, 101f, player);
            Main.Projectiles.Add(proj);

            float actionTime = (TimeBetweenShots - elapsedMs);
            if (actionTime < 0f) actionTime = 0f;
            ActionTimer = actionTime;

            var recoil = (elapsedMs * RecoilFactor);

            if ((player.EyeAngles.Y + recoil) >= 90f)
                player.EyeAngles.Y = 89f;
            else
                player.EyeAngles.Y += recoil;

            RecoilPunch += recoil;


            var rand = (float)Main.RNG.NextDouble();
            rand *= 0.5f;
            rand /= 5f;

            var pitch = (-1f + ((1f / ProjectileCount) * 0.4f)); // increase the pitch of the firing sound as the ammo decreases
            UseSound.Play(0.2f, pitch, 1.0f);
            ProjectileCount -= 1;

            HandleMatrix(player);
        }

        public Biggun(Model model, Vector3 relative) : base(model, relative) // call base constructor from here, do things specific to this type
        {
            Accuracy = 0.0f;
            ProjectileColour = Color.Red;
            TimeBetweenShots = 1200; // milliseconds
            MaxProjCount = 12;
            ProjectileCount = 12;
            RecoilFactor = 2f; // per shot recoil
            RecoilLimit = 4000f;
            ProjectileVel = 50f;
            Colour = Color.Black;
        }
    }

    public class Shotgun : Gun
    {

        const float swayFactor = 0.002f;
        public override void Update(Player player, GameTime gameTime)
        {
            ActionTimer -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            RecoilPunch -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (RecoilPunch < 0f) RecoilPunch = 0f;
            if (ActionTimer < -1000f) ActionTimer = -1000f;
            HandleMatrix(player);
        }
        public override void Render(Player player)
        {
            HandleMatrix(player);
            foreach (ModelMesh Mesh in Model.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3();
                    Effect.View = player.View;
                    Effect.Projection = player.Projection;
                    Effect.World = WorldMatrix;
                }
                Mesh.Draw();
            }
        }
        public override void HandleMatrix(Player player)
        {
            float RecoilMovement = ActionTimer;
            if (RecoilMovement < 0f) RecoilMovement *= 0.03f;

            WorldMatrix = Matrix.CreateScale(0.6f, 0.6f, 0.6f) * Matrix.CreateRotationX(MathHelper.ToRadians(0f));
            WorldMatrix *= Matrix.CreateRotationY(MathHelper.ToRadians(90f));
            WorldMatrix *= Matrix.CreateRotationX(MathHelper.ToRadians(RecoilMovement / 15f));
            WorldMatrix *= Matrix.CreateTranslation(Position + new Vector3(0f, RecoilMovement * 0.00005f, RecoilMovement * 0.00035f));
            Vector3 sway = player.Velocity;
            sway *= swayFactor;

            WorldMatrix *= player.Rotate;
            WorldMatrix *= Matrix.CreateTranslation(-sway);
            Translation = WorldMatrix.Translation;
        }

        public override void Fire(Player player, GameTime gameTime)
        {
            float elapsedMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (ProjectileCount <= 0) return;
            if (ActionTimer > 0) return;

            float[] randoms = new float[3];

            for (int z = 0; z < 12; z++)
            {
                for (int i = 0; i < 3; i++)
                    randoms[i] = (float)Main.RNG.NextDouble() * Accuracy; // generate random spread

                var vec = new Vector3(randoms[0] - (Accuracy / 2f), randoms[1] - (Accuracy / 2f), randoms[2] - (Accuracy / 2f));

                HandleMatrix(Main.BasePlayer);
                Main.BasePlayer.EyeAngles.Y += 0.3f;

                var scale = new Vector3(0.1f, 0.1f, 0.220f);

                Projectile proj = new Projectile(Translation + (Vector3.Up * 0.05f), player.EyePosition + (vec * 0.2f), new Vector3(0, -0f, 0), ProjectileVel, 1f, ProjectileColour, player.EyePosition, scale, 8.42f, player);
                Main.Projectiles.Add(proj);
            }

            float actionTime = (TimeBetweenShots - elapsedMs);
            if (actionTime < 0f) actionTime = 0f;
            ActionTimer = actionTime;

            var recoil = (elapsedMs * RecoilFactor);

            if ((player.EyeAngles.Y + recoil) >= 90f)
                player.EyeAngles.Y = 89f;
            else
                player.EyeAngles.Y += recoil;

            RecoilPunch += recoil;


            var rand = (float)Main.RNG.NextDouble();
            rand *= 0.5f;
            rand /= 5f;

            //var pitch = (-1f + ((1f / ProjectileCount) * 0.6f)); // increase the pitch of the firing sound as the ammo decreases
            
            UseSound.Play(0.6f, -0.9f, 1.0f);
            ProjectileCount -= 1;

            HandleMatrix(player);
        }

        public Shotgun(Model model, Vector3 relative) : base(model, relative) // call base constructor from here, do things specific to this type
        {
            Accuracy = 0.6f;
            ProjectileColour = Color.Orange;
            TimeBetweenShots = 1000; // milliseconds
            MaxProjCount = 6;
            ProjectileCount = 6;
            RecoilFactor = 3f; // per shot recoil
            RecoilLimit = 4000f;
            ProjectileVel = 120f;
            Colour = Color.White;
        }
    }

    public class Flamethrower : Gun
    {

        public override void Update(Player player, GameTime gameTime)
        {
            ActionTimer -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            RecoilPunch -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (RecoilPunch < 0f) RecoilPunch = 0f;
            if (ActionTimer < -1000f) ActionTimer = -1000f;
            HandleMatrix(player);
        }
        public override void Render(Player player)
        {
            HandleMatrix(player);
            foreach (ModelMesh Mesh in Model.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3();
                    Effect.View = player.View;
                    Effect.Projection = player.Projection;
                    Effect.World = WorldMatrix;
                }
                Mesh.Draw();
            }
        }
        const float swayFactor = 0.0015f;


        public override void HandleMatrix(Player player)
        {
            float RecoilMovement = ActionTimer;
            if (RecoilMovement < 0f) RecoilMovement *= 0.03f;

            WorldMatrix = Matrix.CreateScale(Scale, Scale, Scale) * Matrix.CreateRotationX(MathHelper.ToRadians(Rotation));
            WorldMatrix *= Matrix.CreateRotationY(MathHelper.ToRadians(RotationY));
            WorldMatrix *= Matrix.CreateRotationX(MathHelper.ToRadians(0f));

            Vector3 sway = player.Velocity;
            sway *= swayFactor;

            float zMovement = (RecoilMovement * 0.0005f);
            if (zMovement < 0f) zMovement = 0f;

            WorldMatrix *= Matrix.CreateTranslation(Position
                + new Vector3(0f, 0f, zMovement));


            WorldMatrix *= player.Rotate;
            WorldMatrix *= Matrix.CreateTranslation(-sway);
            Translation = WorldMatrix.Translation;
        }

        public override void Fire(Player player, GameTime gameTime)
        {
            float elapsedMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (ProjectileCount <= 0) return;
            if (ActionTimer > 0) return;

            float[] randoms = new float[6];



            HandleMatrix(Main.BasePlayer);
            Main.BasePlayer.EyeAngles.Y += RecoilFactor;
            float rand = 0f;
            for (int i = 0; i < 16; i++)
            {
                for (int x = 0; x < 3; x++)
                    randoms[x] = (float)Main.RNG.NextDouble() * Accuracy; // generate random spread

                var vec = new Vector3(randoms[0] - (Accuracy / 2f), randoms[1] - (Accuracy / 2f), randoms[2] - (Accuracy / 2f));

                rand = (float)Main.RNG.Next(-100, 100);
                rand /= 1000f;

                var scale = new Vector3(0.025f, 0.025f, 0.025f);
                scale += (new Vector3(rand, rand, rand) / 5f);
                var col = ProjectileColour.ToVector3();
                col += new Vector3(rand * 2, rand * 2, rand * 2);
                var randcol = new Color(col);

                Projectile proj = new Projectile(Translation + (Vector3.Up * 0.05f), player.EyePosition + (vec * 0.2f), new Vector3(0, -2.25f, 0), ProjectileVel, 0.55f, randcol, player.EyePosition, scale, 0.075f, player);
                proj.WeapType = 4;
                Main.Projectiles.Add(proj);
            }
            float actionTime = (TimeBetweenShots - elapsedMs);
            if (actionTime < 0f) actionTime = 0f;
            ActionTimer = actionTime;

            var recoil = (RecoilFactor);
            
            if ((player.EyeAngles.Y + recoil) >= 90f) 
                player.EyeAngles.Y = 89f;
            else
                player.EyeAngles.Y += recoil;

            RecoilPunch += recoil;

            rand = (float)Main.RNG.Next(-100, 100);
            rand /= 1000f;
            var pitch = (1 / ((float)ProjectileCount) * 0.9f) + rand;
            UseSound.Play(0.1f, pitch, 0.0f);
            ProjectileCount -= 3;

            if (ProjectileCount < 0) ProjectileCount = 0;
            HandleMatrix(player);
        }

        public Flamethrower(Model model, Vector3 relative) : base(model, relative) // call base constructor from here, do things specific to this type
        {
            Accuracy = 1f;
            ProjectileColour = Color.OrangeRed;
            TimeBetweenShots = 45; // milliseconds
            MaxProjCount = 240;
            ProjectileCount = 240;
            RecoilFactor = 0.05f;
            RecoilLimit = 8000f;
            ProjectileVel = 15f;
            Scale = 0.00633f;
            Rotation = -90f;
            RotationY = 0f;
        }
    }

    public class Empty : Gun
    {

        public override void Update(Player player, GameTime gameTime)
        {
            ActionTimer -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            RecoilPunch -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            if (RecoilPunch < 0f) RecoilPunch = 0f;
            if (ActionTimer < -1000f) ActionTimer = -1000f;
            HandleMatrix(player);
        }
        public override void Render(Player player)
        {
            HandleMatrix(player);
            return; // dont need to render anything as this represents no item 
            foreach (ModelMesh Mesh in Model.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3();
                    Effect.View = player.View;
                    Effect.Projection = player.Projection;
                    Effect.World = WorldMatrix;
                }
                Mesh.Draw();
            }
        }
        const float swayFactor = 0.0015f;
        public override void HandleMatrix(Player player)
        {
            float RecoilMovement = ActionTimer;
            if (RecoilMovement < 0f) RecoilMovement *= 0.03f;
            WorldMatrix = Matrix.CreateScale(0.025f, 0.025f, 0.025f) * Matrix.CreateRotationX(MathHelper.ToRadians(0f));
            WorldMatrix *= Matrix.CreateRotationY(MathHelper.ToRadians(90f));
            WorldMatrix *= Matrix.CreateRotationX(MathHelper.ToRadians(RecoilMovement / 30f));
            Vector3 sway = player.Velocity;
            sway *= swayFactor;

            WorldMatrix *= Matrix.CreateTranslation(Position
                + new Vector3(0f, RecoilMovement * 0.00010f, RecoilMovement * 0.00010f));

            WorldMatrix *= player.Rotate;
            WorldMatrix *= Matrix.CreateTranslation(-sway);
            Translation = WorldMatrix.Translation;
        }

        public override void Fire(Player player, GameTime gameTime)
        {
            float elapsedMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            //if (ProjectileCount <= 0) return;
            if (ActionTimer > 0) return;

            if (player.lookingAt != null)
            {
                var chunk = player.SurroundingChunks.Find(x => x.Blocks.Values.Contains(player.lookingAt));
                if (chunk == null) return;
                chunk.Blocks.TryRemove(player.lookingAt.XYZ, out var blocdk);
                
                player.NearbyBlocks.Remove(player.lookingAt);
                player.BlockDictionary.Remove(player.lookingAt.XYZ);
                List<VertexPositionNormalTexture> verts = new List<VertexPositionNormalTexture>();
                var chunkPos = player.GetCurChunkPos();
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
                        var surrounding = chunkPos + WorldData.dirs[i];

                        bool blockInPos = false;
                        WorldData.ChunkDictionary.TryGetValue(surrounding, out var chunk1);

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
                        var mesh = block.Value.CreateMesh(facesToRender);
                        verts.AddRange(mesh);
                    }
                }

                chunk.ChunkMesh = new Mesh { Vertices = verts.ToArray() };
                player.lookingAt = null;
            }

            float actionTime = (TimeBetweenShots - elapsedMs);
            if (actionTime < 0f) actionTime = 0f;
            ActionTimer = actionTime;



            HandleMatrix(player);
        }

        public Empty(Model model, Vector3 relative) : base(model, relative) // call base constructor from here, do things specific to this type
        {
            Accuracy = 0.00f;
            ProjectileColour = Color.White;
            TimeBetweenShots = 100; // milliseconds
            MaxProjCount = 0;
            ProjectileCount = 0;
            RecoilFactor = 0f;
            RecoilLimit = 0;
            ProjectileVel = 0;
        }
    }


    /*
    public class Holdable // items which can be held in the players hand (e.g. weapons)
    {
        public static int HeldItemIndex = 0;
        public static Holdable GetCurrentlyHeld()
        {
            return Main.Items[HeldItemIndex];
        }

        public Types Type; // type of holdable item
        public Model Model; // model of item
        public Vector3 Position; // position relative to player camera (viewmodel position)

        public Color Colour = Color.White; // tint
        public Matrix mat = Matrix.Identity; // matrix used to apply rotations/modify position, also used for rendering
        public Vector3 Translation; // translation of the world matrix, gives a position used for projectile calculations
        public Color BulletCol = Color.Orange; // colour of fired projectile
        public float BulletVel = 240f; // default velocity
        public int AmmoCount = 60; // ammo count
        public SoundEffect UseSound; // sound effect to play on use of the item

        int shotcount = 1;
        float acc = 0.1f;

        public Holdable(Model model, Vector3 relativepos, Types type)
        {
            Type = type; // type of holdable item (e.g sword, gun)
            Model = model; // model to use for rendering
            Position = relativepos; // position relative to the camera 

            switch (Type)
            {
                case Types.rifle: shotcount = 1; acc = 0.1f; BulletVel = 120f; Colour = Color.DarkCyan; BulletCol = Color.Cyan; break;
                case Types.shotgun: shotcount = 12; acc = 0.8f; BulletVel = 60f; Colour = Color.White; BulletCol = Color.Orange; break;
                case Types.biggun: shotcount = 3; acc = 0.0f; BulletVel = 40f; Colour = Color.White; BulletCol = Color.Red; break;
            }
        }
        public void Fire()
        {
            for (int i = 0; i < shotcount; i++)
            {
                if (AmmoCount <= 0) return;

                var rand = (float)Main.RNG.NextDouble();
                var rand2 = (float)Main.RNG.NextDouble();
                var rand3 = (float)Main.RNG.NextDouble();

                rand *= acc;
                rand2 *= acc;
                rand3 *= acc;

                var vec = new Vector3(rand - (acc / 2f), rand2 - (acc / 2f), rand3 - (acc / 2f));

                GetPos(Main.BasePlayer);
                Main.BasePlayer.EyeAngles.Y += 0.3f;

                var scale = new Vector3(0.025f, 0.025f, 0.025f);
                if (GetCurrentlyHeld().Type == Types.biggun)
                    scale = new Vector3(0.03f, 0.03f, 0.06f);
                Main.Projectiles.Add(new Projectile(Translation + (Vector3.Up * 0.05f), Main.BasePlayer.EyePosition + (vec * 0.2f), new Vector3(0, -0f, 0), BulletVel, 60f, BulletCol, Main.BasePlayer.EyePosition, scale));
                var test = (0.0006f * Main.BasePlayer.recoilTimer);
                if (test > (0.0006f * 1000f)) test = (0.0006f * 1000f);

                Main.BasePlayer.EyeAngles.Y += test;

                var pitch = 0f;
                var volume = 0.2f;
                if (Holdable.GetCurrentlyHeld().Type == Types.shotgun)
                {
                    pitch = -0.5f;
                    volume = 0.05f;
                }
                else if (GetCurrentlyHeld().Type == Types.biggun)
                {
                    pitch = -0.5f;
                }
                else
                {
                    pitch = (1 / ((float)Main.Items[0].AmmoCount) * 0.9f);
                    var rand4 = (float)Main.RNG.NextDouble();
                    rand4 -= 0.5f;
                    rand4 /= 5f;
                    pitch += rand4;
                }
                Holdable.GetCurrentlyHeld().UseSound.Play(volume, pitch, 1.0f);
                Holdable.GetCurrentlyHeld().AmmoCount -= 1;
            }
        }

        public void Interact()
        {

        }

        /*                

        private void doRiflePos(float t)
        {
            mat = Matrix.CreateScale(0.20f, 0.40f, 0.40f) * Matrix.CreateRotationX(MathHelper.ToRadians(0f));
            mat *= Matrix.CreateRotationY(MathHelper.ToRadians(-90f));
            mat *= Matrix.CreateRotationX(MathHelper.ToRadians(t / 30f));
            mat *= Matrix.CreateTranslation(Position + new Vector3(0f, t * 0.00010f, t * 0.00030f));
        }
        private void doShotgunPos(float t)
        {
            mat = Matrix.CreateScale(0.6f, 0.6f, 0.6f) * Matrix.CreateRotationX(MathHelper.ToRadians(0f));
            mat *= Matrix.CreateRotationY(MathHelper.ToRadians(90f));
            mat *= Matrix.CreateRotationX(MathHelper.ToRadians(t / 15f));
            mat *= Matrix.CreateTranslation(Position + new Vector3(0f, t * 0.00005f, t * 0.00035f));
        }

        private void doSwordPos(float t)
        {
            mat = Matrix.CreateScale(0.03f, 0.03f, 0.03f) * Matrix.CreateRotationX(MathHelper.ToRadians(-90f));
            mat *= Matrix.CreateRotationZ(MathHelper.ToRadians(180f));
            mat *= Matrix.CreateRotationY(MathHelper.ToRadians(-50f));
            mat *= Matrix.CreateTranslation(Position + new Vector3(0f, t * 0.00025f, 0f));
        }

        private void doBigPos(float t)
        {
            mat = Matrix.CreateScale(0.20f, 0.40f, 0.40f) * Matrix.CreateRotationX(MathHelper.ToRadians(0f));
            mat *= Matrix.CreateRotationY(MathHelper.ToRadians(0f));
            mat *= Matrix.CreateRotationX(MathHelper.ToRadians(t / 30f));
            mat *= Matrix.CreateTranslation(Position + new Vector3(0f, t * 0.00010f, t * 0.00030f));
        }

        public void GetPos(Player player)
        {
            var t = player.ActionTimer;
            if (t < 0f) t = 0;
            if (t > 400f) t -= (t - 400);

            if (AmmoCount <= 0) t = 0;
            switch (Type)
            {
                case Types.rifle: doRiflePos(t); break;
                case Types.shotgun: doShotgunPos(t); break;
                case Types.sword: doSwordPos(t);  break;
                case Types.biggun: doBigPos(t); break;
            }

            mat *= player.Rotate;
            Translation = mat.Translation;
        }

        public void Render(Player player, Color col)
        {
            GetPos(player);
            foreach (ModelMesh Mesh in Model.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3();
                    Effect.View = player.View;
                    Effect.Projection = player.Projection;
                    Effect.World = mat;
                    //Effect.Texture = Main.Textures["cool"];
                    //Effect.TextureEnabled = true;
                }
                Mesh.Draw();
            }
        }

    }*/
}
