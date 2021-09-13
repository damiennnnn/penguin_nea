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
    public class Sky
    {
        public static Model CloudModel;
        public float[] map;
        static string cmdstr = "rain_on";
        public bool ShouldRain()
        {
            int raining = (int)Global.ConsoleVars[cmdstr].val;
            bool shouldRain = (raining > 0);

            return shouldRain;
        }

        public Sky(float[] Map)
        {
            map = Map;
            rand = new Random();

        }

        public static float offset = 0;
        public static Random rand; 
        public float prevGameTime = 0; 
        public void ProjGen(Player player, GameTime gameTime)
        {
           
            float curGameTime = (float)gameTime.TotalGameTime.TotalSeconds;
            if ((curGameTime - prevGameTime) < 0.2f) return;

            prevGameTime = (float)gameTime.TotalGameTime.TotalSeconds;
            Perlin.Noise2d.Reseed();
            for (float x = 0; x < 100; x++)
            {
                for (float z = 0; z < 100; z++)
                {
                    var chance = rand.NextDouble();
                    if (chance > 0.0075f) continue;
                    var scale = new Vector3(0.025f, 0.2f, 0.025f);
                    var proj = new Projectile(Main.BasePlayer.Position + new Vector3(x -50, 60 + ((float)chance * 100), z - 50 ), Vector3.Zero, new Vector3(0,-9.8f,0), 20f, 5f, Color.Cyan, Vector3.Forward, scale, 0f, player) ;
                    proj.Cosmetic = true;
                    Main.Projectiles.Add(proj);
                }
            }
            offset += 0.001f;
            if ((offset + 100) > 300) offset = 0;
        }

    }
}
