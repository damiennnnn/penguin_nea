using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Jitter;
using Jitter.Collision;
using Jitter.Dynamics;
using Jitter.LinearMath;
using Microsoft.Xna.Framework.Input;
namespace gameproject
{
    public static class Physics // methods that are reusable throughout 
    {
        public static float Friction = 10f;
        public static float AirAccel = 10f;
        public static float GroundAccel = 50f;
        public static float MaxVel = 10f;
        public static float MaxAirVel = 8.5f;

        public static Vector3 Accelerate(Vector3 dir, Vector3 prevVel, float accel, float maxVel, GameTime gameTime)
        {
            float projVel = Vector3.Dot(prevVel, dir);
            float accelVel = accel * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if ((projVel + accelVel) > maxVel)
                accelVel = (maxVel - projVel);

            return prevVel + (dir * accelVel);
        }

        public static Vector3 MoveGround(Vector3 dir, Vector3 prevVel, GameTime gameTime) // aceelerate with friction
        {
            float speed = prevVel.Length();

            if (speed != 0)
            {
                float drop = (speed * Friction * (float)gameTime.ElapsedGameTime.TotalSeconds);
                prevVel *= (System.Math.Max(speed - drop, 0) / speed);
            }
            return Accelerate(dir, prevVel, GroundAccel, MaxVel, gameTime);
        }

        public static Vector3 MoveAir(Vector3 dir, Vector3 prevVel, GameTime gameTime) // accelerate without friction
        {
            return Accelerate(dir, prevVel, AirAccel, MaxAirVel, gameTime);
        }

        public static Vector3 JitterToXNA(JVector vec) // jitter jvector to xna/monogame vector3
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }
        public static JVector XNAToJitter(Vector3 vec) // xna/monogame vector3 to jitter jvector
        {
            return new JVector(vec.X, vec.Y, vec.Z);
        }
        public static Matrix ToMatrix(JMatrix matrix)
        {
            return new Matrix(
                matrix.M11, matrix.M12, matrix.M13, 0.0f,
                matrix.M21, matrix.M22, matrix.M23, 0.0f,
                matrix.M31, matrix.M32, matrix.M33, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
        }
    }
}
