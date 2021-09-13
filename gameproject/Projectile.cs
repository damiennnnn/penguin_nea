using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Math;
namespace gameproject
{
    [Serializable]
    public class Projectile
    {
        public static bool BelowGround(Projectile proj)
        {
            return (proj.Position.Y < 0f);
        }
        public static bool IsDead(Projectile proj)
        {
            return (proj.Lifetime < 0f);
        }

        public Vector3 Position; // position in world as a vector
        public Vector3 Velocity; // velocity of projectile, containing Direction and Magnitude 
        public Vector3 Acceleration; // acceleration of projectile
        public Vector3 Forward; // forward vector of cube
        public Vector3 Scale; // scale of projectile
        public Color Colour;
        public float Lifetime = 5f; // lifetime of projectile in seconds
        public float DamageValue = 5f; // damage that the projectile will deal on collision
        public int WeapType = 0;
        public bool Cosmetic = false;
        public Entity OwnerEntity;
        public Projectile(Vector3 pos, Vector3 vel, Vector3 accel, float speed, float lifetime)
        {
            Position = pos;
            Velocity = vel * speed;
            Acceleration = accel;
            Colour = Color.Yellow;
            Lifetime = lifetime;
        }
        public Projectile(Vector3 pos, Vector3 vel, Vector3 accel, float speed, float lifetime, Color col, Vector3 eyepos, Vector3 scale, float dmg, Entity ownerEnt)
        {
            Position = pos;
            Velocity = vel * speed;
            Acceleration = accel;
            Colour = col;
            Lifetime = lifetime;
            Forward = eyepos;
            Scale = scale;
            DamageValue = dmg;
            OwnerEntity = ownerEnt;
        }
        public Projectile(Vector3 pos, Vector3 vel, Vector3 accel)
        {
            Position = pos;
            Velocity = vel;
            Acceleration = accel;
            Colour = Color.Yellow;
        }
        private bool IsCollide(Entity peng)
        {
            if (peng == null) return false;
            var min = peng.Box.Min;
            var max = peng.Box.Max;

            return ((Position.X > min.X) && (Position.X < max.X) && (Position.Y > min.Y) && (Position.Y < max.Y) && (Position.Z > min.Z) && (Position.Z < max.Z));
        }

        Random rand = new Random();
        int num = 0;
        
        float timestamp = -1f;
        bool hitAny = false;
        bool doDmg = true;

 

        public void Update(GameTime gameTime)
        {
            Velocity += (Acceleration * (float)gameTime.ElapsedGameTime.TotalSeconds);
            Position += (Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds);

            if (num == 0)
            {
                Entity hitEntity = null;

                if (!Cosmetic)
                    hitEntity = Main.Entities.ToList().FirstOrDefault(IsCollide); // find the first entity that matches the predicate

                if (hitEntity != null) // if there is a valid entity
                {
                    // healthhit shieldhit shieldcrack
                    if (timestamp > -1)
                    {
                        if (((float)gameTime.TotalGameTime.TotalSeconds - timestamp) > 0.15f)
                        {
                            Main.BasePlayer.Hit++; 
                            doDmg = true;
                            timestamp = (float)gameTime.TotalGameTime.TotalSeconds;
                            Lifetime /= 2f;
                            Scale /= 2f;
                        }
                        else
                            doDmg = false;
                    }
                    if (!doDmg) return;
                    float healthdmg = 0f;

                    if (hitEntity.ShieldHealth < 0f)
                        healthdmg = DamageValue;

                    else if ((hitEntity.ShieldHealth - DamageValue) <= 0f)
                    {
                        healthdmg = (DamageValue - hitEntity.ShieldHealth);
                        //Main.SFX["shieldcrack"].Play(0.3f, 0.5f, 0);
                        var cracksound = new Main.SoundLog();
                        cracksound.vol = 0.3f;
                        cracksound.pitch = 0.5f;
                        cracksound.pan = 0f;
                        cracksound.soundEffect = Main.SFX["shieldcrack"];
                        Main.soundLog.Add(cracksound);
                        Main.HitmarkerAlt = 0f;
                        hitEntity.ShieldHealth = 0f;
                        hitEntity.ShieldHealth -= DamageValue;
                    }
                    else if ((hitEntity.ShieldHealth - DamageValue) > 0f)
                    {
                        float pitch = ((float)rand.Next(20, 27) / 100);

                        if (timestamp > -1) {
                            var repeat = new Main.SoundLog();
                            repeat.vol = 0.005f;
                            repeat.pitch = pitch;
                            repeat.pan = 0f;
                            repeat.soundEffect = Main.SFX["shieldhit"];
                            Main.soundLog.Add(repeat);
                        }// if this is a repeated projectile ( e.g flamethrower )
                         //Main.SFX["shieldhit"].Play(0.005f, pitch, 0);
                        else
                        {
                            var repeat = new Main.SoundLog();
                            repeat.vol = 0.15f;
                            repeat.pitch = pitch;
                            repeat.pan = 0f;
                            repeat.soundEffect = Main.SFX["shieldhit"];
                            Main.soundLog.Add(repeat);
                        }
                            //Main.SFX["shieldhit"].Play(0.15f, pitch, 0);
                        hitEntity.ShieldHealth -= DamageValue;
                    }

                    if (hitEntity.ShieldHealth <= 0f)
                    {
                        float pitch = ((float)rand.Next(-10, 10) / 100);
                        //if (timestamp > -1)
                        //    Main.SFX["healthhit"].Play(0.04f, pitch, 0);
                        //else
                        var health = new Main.SoundLog();
                        health.vol = 0.3f;
                        health.pitch = pitch;
                        health.pan = 0f;
                        health.soundEffect = Main.SFX["healthhit"];
                        //Main.SFX["healthhit"].Play(0.3f, pitch, 0);
                        Main.soundLog.Add(health);
                        hitEntity.Health -= healthdmg;
                    }

                    if (hitEntity.Health < 0f)
                        Main.Entities.Remove(hitEntity); // remove the entity if health drops below 0

                    hitEntity.LastHitTime = gameTime.TotalGameTime;
                    OwnerEntity.LastHitEnt = hitEntity;
                    if (WeapType == 4) //  if the fired weapon is flamethrower/lingering
                    {
                        if (timestamp < 0)
                            timestamp = (float)(gameTime.TotalGameTime.TotalSeconds);
                        doDmg = false;
                        Velocity /= 50f;
                    }
                    else
                    {
                        Main.BasePlayer.Hit++;
                        DamageValue = 0f; //
                        Lifetime = -1f; // kill the projectile
                    }
                    hitAny = true;
                    Main.HitmarkerAlpha = 400f;
                }
            }


            Lifetime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if ((Lifetime < 0f) && (hitAny == false))
                Main.BasePlayer.Miss++;
        }

        public void Render(Matrix View, Matrix Proj)
        {
            foreach (ModelMesh Mesh in Cube.CubeModel.Meshes)
            {
                foreach (BasicEffect Effect in Mesh.Effects)
                {
                    Effect.EnableDefaultLighting();
                    Effect.AmbientLightColor = Colour.ToVector3(); // set colour of the cube
                    Effect.View = View; // use players current view 
                    Effect.Alpha = 0.9f;
                    Effect.Projection = Proj;
                    Effect.World = Matrix.CreateScale(Scale) * Matrix.CreateWorld(Position, Forward, Vector3.Up); // create a world matrix from cubes position
                    Effect.TextureEnabled = false;
                }
                Mesh.Draw();
            }
        }
    }

    public class Autoproj
    {
        public static Vector3 CreateAutoproj(Vector3 pos, Vector3 accel, float speed)
        {
            Vector3 Position = pos; // position in world as a vector
            Vector3 Velocity; // velocity of projectile, containing Direction and Magnitude 
            Vector3 Acceleration = accel; // acceleration of projectile

            double distance = Vector3.Distance(Position, Main.Projectiles[0].Position);
            int id = 0;
            for (int i = 1; i < Main.Projectiles.Count; i++)
            {
                if (distance > Vector3.Distance(Position, Main.Projectiles[i].Position))
                {
                    distance = Vector3.Distance(Position, Main.Projectiles[i].Position);
                    id = i;
                }
            }

            Projectile target = Main.Projectiles[id];

            Vector3 acc = target.Acceleration - Acceleration;
            Vector3 offset = target.Position - Position;

            IEnumerable<double> terms = new double[]
            {
                Vector3.Dot(offset,offset),                                                        //0.25(a•a)t^4
                2*Vector3.Dot(target.Velocity,offset),                                                        //(u•a)t^3
                (Vector3.Dot(target.Velocity,target.Velocity) + Vector3.Dot(acc,offset) - System.Math.Pow(speed, 2)),     //(u•u+a•p-v^2)t^2
                Vector3.Dot(target.Velocity,acc),                                                      //2(u•p)t
                0.25*Vector3.Dot(acc,acc),                                                               //p•p
            };

            Polynomial time = new Polynomial(terms);
            double[] initials = new double[8] { 200, 120, 60, 30, 15, 5, 2, 0 }; //Numbers to run root finding, this can be made slightly more efficient

            double? root = null;
            for (int i = 0; i < 8; i++)
            {
                System.Numerics.Complex r = time.FindRoot(new System.Numerics.Complex(initials[i], 0));
                if (root == null && r.Real >= 0 && r.Imaginary == 0)
                {
                    root = r.Real;
                }
                if (r.Real < root && r.Real >= 0 && r.Imaginary == 0)
                {
                    root = r.Real;
                }
            }

            //Use this time value to calculate displacement 
            if (root != null)
            {
                double r = root.GetValueOrDefault();
                Vector3 S = target.Velocity * (float)r + 0.5f * target.Acceleration * (float)r * (float)r + target.Position - Position; //S2 = U2t + 0.5a2t^2 + P2 - P1


                //Calculate the velocity vector
                Velocity = S / (float)r - 0.5f * Acceleration * (float)r; //U = S - 0.5at^2/t
            }
            else
            {
                return Vector3.Zero;
            }
            var proj = new Projectile(Position, Velocity, Acceleration);
            proj.Colour = Color.Red;
            Main.Autoprojs.Add(proj);
            return Vector3.Normalize(Velocity);
        }
    }
}
