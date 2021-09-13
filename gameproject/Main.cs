using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Jitter;
using Jitter.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Myra;
using Myra.Graphics2D.UI;
using Perlin;
using QuakeConsole;

namespace gameproject
{
    public class Main : Game
    {
        // semantic versioning?
        public static int Major = 2;
        public static int Minor = 0;
        public static int Patch = 0;

        public static CollisionSystem CollisSystem = new CollisionSystemSAP();
        public static JitterWorld PhysSystem;
        public static GameTime gameTime;

        public static (int Width, int Height) Resolution = (1280, 720);
        public static Vector2 WindowCentre = Vector2.Zero;
        public static Random RNG = new Random();

        public static bool MouseInput = true;
        public static ConsoleComponent Console; // text console for manipulating variables/inputting commands at runtime
        public static CustomInterpreter Interpreter = new CustomInterpreter();

        public static Dictionary<string, Texture2D>
            Textures = new Dictionary<string, Texture2D>(); // dictionary for storing textures

        public static Dictionary<string, SoundEffect>
            SFX = new Dictionary<string, SoundEffect>(); // dictionary for storing sound effects

        public static Cube cube;
        public static List<Projectile> Projectiles = new List<Projectile>(); // list of projectiles spawned in the world
        public static List<Projectile> Autoprojs = new List<Projectile>(); // list of autoprojectiles (unused)
        public static List<Entity> Entities = new List<Entity>(); // list of entities in the world

        public static Thread ServerThread; // separate thread that the internal server is ran on 

        public static ConcurrentBag<SoundLog>
            soundLog =
                new ConcurrentBag<SoundLog>(); // the sound log is accessed in parallel threads, concurrentbag is threadsafe


        public static Player BasePlayer; // current player entity
        public static GraphicsDevice gDevice;
        public static float HitmarkerAlpha;
        public static float HitmarkerAlt = 255f;

        private static readonly string cmdstr = "rain";
        //public float BlockScale { get => (float)Global.ConsoleVars[cmdstr].val; set { Global.ConsoleVars[cmdstr].val = value; } }

        public static List<Block> Blocks = new List<Block>();

        private static Texture2D HealthRect;

        public static float WorldGenProgress;

        private static int world_size = 300;
        private static int WorldSeed;
        private static string WorldSeedStr = "";
        private static float[,,] perlintoplayer;


        public static Vector3 test = Vector3.Zero;
        public static int RenderingCubes;
        public static int RenderingSides = 0;

        public Desktop _desktop;
        private readonly GraphicsDeviceManager _graphics;
        public Sky CloudSky;
        public Vector3 co = new Vector3(0, 1.3f, 0);
        public Crosshair CrosshairPlayer; // crosshair
        public BasicEffect Effect;
        private TimeSpan ElapsedWorldThreadTime;
        private SpriteFont Font;

        public FrameCount FPS = new FrameCount(); // frames per second counter

        private ThreadStart genThreadStart;
        private float hsv = 0.01f;


        private readonly Keys[] InventoryKeys =
        {
            Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0
        };


        private readonly bool isGameRunning = true;

        public Dictionary<string, Model> Models = new Dictionary<string, Model>(); // dictionary for storing 3d models

        private bool mouserelease;

        private readonly int physcount = 0;
        private SpriteBatch SprBatch;
        private bool startOnce;


        private Stopwatch threadTimer;
        public bool ToggleUI = true;
        private TimeSpan total;
        private bool uitoggle;
        private Thread updateThread;
        public List<Cube> World = new List<Cube>(); // world is constructed out of cubes
        private Thread worldGenThread;

        public bool worldInitialised; // is the world generated

        public List<Vector3> Worldpos = new List<Vector3>();

        public Main()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            IsFixedTimeStep = false; // allow the game to update as fast as the hardware allows

            IsMouseVisible = false; // hide mouse cursor

            Console = new ConsoleComponent(this);
            Console.Padding = 10f;
            Console.InputPrefixColor = Color.MonoGameOrange;

            Console.InputPrefix = ">";
            Console.FontColor = Color.White;
            Console.Interpreter = Interpreter;
            SetupInterpreter();
            Components.Add(Console);
        }

        public static float GetBlockScale()
        {
            return (float) (int) Global.ConsoleVars[cmdstr].val / 1000;
        }

        private void GenerateNoiseMap(int width, int height, int octaves, out float[] map)
        {
            var data = new float[width * height];

            /// track min and max noise value. Used to normalize the result to the 0 to 1.0 range.
            var min = float.MaxValue;
            var max = float.MinValue;

            /// rebuild the permutation table to get a different noise pattern. 
            /// Leave this out if you want to play with changing the number of octaves while 
            /// maintaining the same overall pattern.
            Noise2d.Reseed();

            var frequency = 1f;
            var amplitude = 0.5f;
            //var persistence = 0.25f;

            for (var octave = 0; octave < octaves; octave++)
            {
                /// parallel loop - easy and fast.
                Parallel.For(0
                    , width * height
                    , offset =>
                    {
                        var i = offset % width;
                        var j = offset / width;
                        var noise = Noise2d.Noise(i * frequency * 1f / width, j * frequency * 1f / height);
                        noise = data[j * width + i] += noise * amplitude;

                        min = System.Math.Min(min, noise);
                        max = System.Math.Max(max, noise);
                    }
                );

                frequency *= 2;
                amplitude /= 2;
            }

            map = data;
        }

        public void
            PerlinNoiseWorldGenerate() // perlin noise generation creates a pseudorandom, more natural appearance
        {
            WorldData.Clear();
            var timer = new Stopwatch();
            timer.Start();

            /*for (int i = 0; i < world_size; i++)
            {
                for (int z = 0; z < world_size; z++)
                {
                    float noise = Perlin.Noise2d.Noise(i, z);

                    int YVal = -1;
                    YVal = (int)(noise * 10);
                   // YVal = (int)(System.Math.Round(System.Math.Pow(noise, 2)));
                    for (int y = -16; y < YVal; y++)
                    {
                        Pos.Add(new Vector3(i - (world_size/2), y, z - (world_size / 2)));
                    }
                }
            }*/
            float[] map;
            //GenerateNoiseMap(world_size, world_size, 1, out map);
            System.Console.WriteLine("Enter world_size (default 300): ");
            int.TryParse(System.Console.ReadLine(), out world_size);

            System.Console.WriteLine("Enter seed: ");
            WorldSeedStr = System.Console.ReadLine();
            var seed = WorldSeedStr.GetHashCode();
            System.Console.Clear();
            NoiseMaker.Reseed(seed);
            WorldSeed = seed;
            GenerateNoiseMap(300, 300, 10, out var map2);
            CloudSky = new Sky(map2);
            var Pos = new List<(Vector3, BlockID)>();
            // grass block layer

            float count = world_size * world_size; // 
            var cur = 0f;
            perlintoplayer = new float[world_size, world_size, 5];
            var newblocks = new ConcurrentBag<(Vector3, BlockID)>();
            var min = 0f;
            var max = 1f;
            Parallel.For(-(world_size / 2), world_size / 2, x => // parallelise world generation, big performance improvement
            {
                for (var z = -(world_size / 2); z < world_size / 2; z++)
                {
                    for (var y = 1; y < 2; y++) // levels 1 -> 2
                    {
                        var vec = new Vector3(x, y, z);
                        var noise = NoiseMaker.Noise(vec / 100, 2, ref min, ref max);
                        perlintoplayer[x + world_size / 2, z + world_size / 2, 0] = noise;
                        noise *= 10f;
                        if (noise < 0.3f) continue;
                        vec.Round();

                        newblocks.Add((vec, BlockID.Grass));
                    }

                    for (var y = 0; y < 1; y++) // levels 0 -> 1
                    {
                        var vec = new Vector3(x, y, z);
                        var noise = NoiseMaker.Noise(vec / 100, 2, ref min, ref max);
                        perlintoplayer[x + world_size / 2, z + world_size / 2, 1] = noise;
                        noise *= 10f;
                        if (noise < 0.05f) continue;
                        vec.Round();

                        newblocks.Add((vec, BlockID.Grass));
                    }

                    for (var y = -2; y < 0; y++) // levels -2 -> 0
                    {
                        var vec = new Vector3(x, y, z);
                        var noise = NoiseMaker.Noise(vec / 100, 20, ref min, ref max);
                        perlintoplayer[x + world_size / 2, z + world_size / 2, 2] = noise;
                        vec.Round();
                        if (noise < 0.001f)
                            newblocks.Add((vec, BlockID.Water));
                        else
                            newblocks.Add((vec, BlockID.Grass));
                    }

                    for (var y = -4; y < -2; y++) // levels -4 -> -2 
                    {
                        var vec = new Vector3(x, y, z);
                        var noise = NoiseMaker.Noise(vec / 100, 35, ref min, ref max);
                        if (noise < 0.004f) continue;
                        vec.Round();

                        newblocks.Add((vec, BlockID.Grass));
                    }

                    for (var y = -6; y < -4; y++) // levels -6 -> -4 
                    {
                        var vec = new Vector3(x, y, z);
                        var noise = NoiseMaker.Noise(vec / 100, 4, ref min, ref max);
                        perlintoplayer[x + world_size / 2, z + world_size / 2, 3] = noise;
                        if (noise < 0.002f) continue;
                        vec.Round();

                        newblocks.Add((vec, BlockID.Stone));
                    }

                    for (var y = -8; y < -6; y++) // levels -8 -> -6
                    {
                        var vec = new Vector3(x, y, z);
                        var noise = NoiseMaker.Noise(vec / 100, 25, ref min, ref max);
                        if (noise < 0.008f) continue;
                        vec.Round();

                        newblocks.Add((vec, BlockID.Stone));
                    }

                    for (var y = -16; y < -8; y++) // levels -12 -> -8
                    {
                        var vec = new Vector3(x, y, z);
                        var noise = NoiseMaker.Noise(vec / 100, 5, ref min, ref max);
                        if (noise < 0.002f) continue;
                        vec.Round();

                        newblocks.Add((vec, BlockID.Stone));
                    }

                    for (var y = -18; y < -16; y++) // levels -12 -> -8
                    {
                        var vec = new Vector3(x, y, z);

                        vec.Round();

                        newblocks.Add((vec, BlockID.Bedrock));
                    }

                    cur++;
                    WorldGenProgress = cur / count * 100f;
                }
            });

            /*for (int x = 0; x < world_size; x++)
            {
                for (int z = 0; z < world_size; z++)
                {
                    for (int y = -8; y < 0; y++)
                    {
                        Vector3 vec = new Vector3(x, y, z);
                        float min = 0f;
                        float max = 1f;
                        float noise = NoiseMaker.Noise(vec / 100, 25, ref min, ref max);
                        if (noise < 0.01f) continue;
                        vec.Round();
                        positions.Add(vec);
                    }
                }
            }*/

            timer.Stop();
            System.Console.WriteLine("world gen time: " + timer.Elapsed.TotalSeconds);

            newblocks.Add((new Vector3(0, 28f, 0), BlockID.Iron));
            newblocks.Add((new Vector3(2f, 1f, 2f), BlockID.Iron));


            WorldData.SetupWorld(newblocks, GraphicsDevice, WorldGenProgress);
            System.Console.WriteLine("block count: {0}", newblocks.Count);
            newblocks = null;
            worldInitialised = true;
            GC.Collect();
        }


        private void SetupInterpreter()
        {
            Interpreter.AddCommand("exit", Exit);
        }

        protected override void Initialize()
        {
            // initialise
            _graphics.PreferredBackBufferWidth = Resolution.Width;
            _graphics.PreferredBackBufferHeight = Resolution.Height;
            _graphics.SynchronizeWithVerticalRetrace = false; // disable vsync (syncing with monitor refresh rate)
            IsFixedTimeStep = false;

            _graphics.ApplyChanges();

            using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher("select * from Win32_Processor"),
                win32CompSys = new ManagementObjectSearcher("select * from Win32_ComputerSystem"),
                win32Memory = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
            {
                foreach (ManagementObject obj in win32Proc.Get())
                {
                    var clockSpeed = obj["MaxClockSpeed"].ToString();
                    var procName = obj["Name"].ToString();
                    var manufacturer = obj["Manufacturer"].ToString();
                    var version = obj["Version"].ToString();

                    System.Console.WriteLine(procName);
                    System.Console.WriteLine(clockSpeed);
                    System.Console.WriteLine(manufacturer);
                    System.Console.WriteLine(version);
                }

                double totalRamCapacity = 0;
                foreach (ManagementObject obj in win32Memory.Get())
                    totalRamCapacity += Convert.ToDouble(obj.GetPropertyValue("Capacity"));
                totalRamCapacity /= 1073741824; // convert to gigabytes from bytes
                System.Console.WriteLine("Total RAM: {0}GB", totalRamCapacity);
                using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        System.Console.WriteLine("Name  -  " + obj["Name"]);
                        System.Console.WriteLine("DeviceID  -  " + obj["DeviceID"]);
                        System.Console.WriteLine("AdapterRAM  -  " + obj["AdapterRAM"]);
                        System.Console.WriteLine("AdapterDACType  -  " + obj["AdapterDACType"]);
                        System.Console.WriteLine("Monochrome  -  " + obj["Monochrome"]);
                        System.Console.WriteLine("DriverVersion  -  " + obj["DriverVersion"]);
                        System.Console.WriteLine("VideoProcessor  -  " + obj["VideoProcessor"]);
                        System.Console.WriteLine("VideoArchitecture  -  " + obj["VideoArchitecture"]);
                        System.Console.WriteLine("VideoMemoryType  -  " + obj["VideoMemoryType"]);
                    }
                }
            }

            HealthRect = new Texture2D(GraphicsDevice, 1, 1);
            HealthRect.SetData(new[] {Color.White});

            gDevice = GraphicsDevice;
            Effect = new BasicEffect(GraphicsDevice);
            Effect.LightingEnabled = false;
            Effect.TextureEnabled = false;
            Effect.VertexColorEnabled = true;

            WindowCentre = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
            Mouse.SetPosition((int) WindowCentre.X, (int) WindowCentre.Y);

            // load models and textures from disk
            Models.Add("penguin", Content.Load<Model>("PenguinBaseMesh"));
            Models.Add("rifle", Content.Load<Model>("ump47"));
            Models.Add("shotgun", Content.Load<Model>("Quad_Shotgun"));
            Models.Add("sword", Content.Load<Model>("magicsword"));
            Models.Add("biggun", Content.Load<Model>("Cyborg_Weapon"));
            Models.Add("flamethrow", Content.Load<Model>("flamethrow"));


            Textures.Add("penguin", Content.Load<Texture2D>("PenguinTexture"));
            Textures.Add("plank", Content.Load<Texture2D>("mcwood"));
            Textures.Add("grass", Content.Load<Texture2D>("mcgrass"));
            Textures.Add("cool", Content.Load<Texture2D>("cool"));
            Textures.Add("texturemap",
                Content.Load<Texture2D>(
                    "terrain")); // texture map file that stores all the block textures that we will need 


            SFX.Add("gunshot", Content.Load<SoundEffect>("laser"));
            SFX.Add("shotgunshot", Content.Load<SoundEffect>("ShotGunO"));
            SFX.Add("flamesfx", Content.Load<SoundEffect>("flamesfx"));
            SFX.Add("healthhit", Content.Load<SoundEffect>("healthhit"));
            SFX.Add("shieldhit", Content.Load<SoundEffect>("shieldhit"));
            SFX.Add("shieldcrack", Content.Load<SoundEffect>("shieldcrack"));

            Renderer.SetupShader(Content.Load<Effect>("blockLighting"));


            Cube.CubeModel = Content.Load<Model>("simplecube");
            Cube.WorldModel = Content.Load<Model>("simplecube");
            Sky.CloudModel = Content.Load<Model>("simplecube");

            Font = Content.Load<SpriteFont>("DefaultFont"); // load default font for 2d text rendering

            Resources.Models = Models;
            Resources.Textures = Textures;

            BasePlayer = new Player(new Vector3(20, 5f, 20), "penguin", "penguin");
            Global.RegisterVariable(cmdstr, 1000, "Scale factor for block rendering.");
            //ServerThread = new Thread(serverThreading);
            //ServerThread.Start();

            #region entitysetup

            // rifle = new Vector3(0.10f, -0.11f, -0.12f)
            // shotgun = new Vector3(0.15f, -0.15f, -0.20f)
            // biggun = new Vector3(0.10f, -0.11f, -0.12f)


            var rifle = new Rifle(Models["rifle"], new Vector3(0.08f, -0.106f, -0.12f));
            rifle.Colour = Color.Blue;
            rifle.UseSound = SFX["gunshot"];
            var bigg = new Biggun(Models["biggun"], new Vector3(0.10f, -0.11f, -0.12f));
            bigg.UseSound = SFX["gunshot"];
            var shotgun = new Shotgun(Models["shotgun"], new Vector3(0.10f, -0.11f, -0.12f));
            shotgun.UseSound = SFX["shotgunshot"];
            var flamethrow = new Flamethrower(Models["flamethrow"], new Vector3(0.0985f, -0.181f, -0.12f));
            flamethrow.UseSound = SFX["flamesfx"];

            var empty = new Empty(Models["rifle"], Vector3.Zero);

            BasePlayer.Inventory.Add(empty);
            BasePlayer.Inventory.Add(rifle);
            BasePlayer.Inventory.Add(bigg);
            BasePlayer.Inventory.Add(shotgun);
            BasePlayer.Inventory.Add(flamethrow);

            Entities.Add(new Entity(new Vector3(0, 2, 0), "penguin", "penguin", "cheeky"));
            Entities.Add(new Entity(new Vector3(2, 2, 0), "penguin", "penguin", "joe"));
            Entities.Add(new Entity(new Vector3(4, 2, 0), "penguin", "penguin", "mama"));
            Entities.Add(new Entity(new Vector3(6, 2, 0), "penguin", "penguin", "urmum"));
            var ent = new Entity(new Vector3(8, 2, 0), "penguin", "penguin", "BoJo");
            ent.Health = 1000f;

            Entities.Add(ent);

            for (var i = 10; i < 20; i += 2)
                Entities.Add(new Entity(new Vector3(i, 2, i), "penguin", "penguin", "pingu pingu"));

            #endregion

            Crosshair.gDevice = GraphicsDevice;
            CrosshairPlayer = new Crosshair();
            Global.RegisterVariable("rain_on", 0, "Toggle rain effects.");


            var s = new SamplerState
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                Filter = TextureFilter.Point
            };

            GraphicsDevice.SamplerStates[0] = s;
            base.Initialize();
        }

        private static void serverThreading()
        {
            var internalServer = new GameState();
        }

        protected override void LoadContent()
        {
            SprBatch = new SpriteBatch(GraphicsDevice);
            MyraSetup();
        }

        protected override void Update(GameTime gameTime)
        {
            Main.gameTime = gameTime;
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                updateThread.Abort();
                Exit();
            }


            if (Keyboard.GetState().IsKeyDown(Keys.OemTilde))
                Console.ToggleOpenClose(); // toggle the console on ~ key press
            if (!worldInitialised && !startOnce)
            {
                genThreadStart = PerlinNoiseWorldGenerate;
                worldGenThread = new Thread(genThreadStart); // generate a new thread for our world generation
                worldGenThread.Start(); // start the thread
                startOnce = true; // make sure we only do this once
            }
            else if (worldInitialised && startOnce)
            {
                worldGenThread.Abort();
                genThreadStart = DoUpdate;
                updateThread = new Thread(genThreadStart);
                threadTimer = new Stopwatch();
                total = new TimeSpan();
                updateThread.Start();
                startOnce = false;
            } // update the world only when it exists

            base.Update(gameTime);
        }

        public void DoUpdate()
        {
            while (isGameRunning)
            {
                threadTimer.Start();
                var gameTime = new GameTime(total, ElapsedWorldThreadTime);

                if (Keyboard.GetState().IsKeyDown(Keys.P))
                {
                    if (!mouserelease)
                    {
                        MouseInput = !MouseInput;
                        IsMouseVisible = !MouseInput;
                    }

                    mouserelease = true;
                }

                if (Keyboard.GetState().IsKeyUp(Keys.P)) mouserelease = false;


                if (Keyboard.GetState().IsKeyDown(Keys.O))
                {
                    if (!uitoggle) ToggleUI = !ToggleUI;

                    uitoggle = true;
                }

                if (Keyboard.GetState().IsKeyUp(Keys.O)) uitoggle = false;
                if (Mouse.GetState().RightButton == ButtonState.Pressed)
                    BasePlayer.UpdateFOV(25f);
                if (Mouse.GetState().RightButton == ButtonState.Released)
                    BasePlayer.UpdateFOV();

                if (Keyboard.GetState().IsKeyDown(Keys.R))
                    BasePlayer.GetCurrentlyHeld().Reload();

                if (BasePlayer.GetCurrentlyHeld().ActionTimer < -999f)
                    for (var i = 0; i < InventoryKeys.Length; i++)
                        if (Keyboard.GetState().IsKeyDown(InventoryKeys[i]))
                            if (BasePlayer.Inventory.ElementAtOrDefault(i) != null)
                                BasePlayer.HeldItemIndex = i;

                Projectiles.RemoveAll(Projectile.IsDead);
                // update logic every frame
                BasePlayer.Update(gameTime); // update player entity

                if (CloudSky.ShouldRain())
                    CloudSky.ProjGen(BasePlayer, gameTime);

                //foreach (var proj in Projectiles)
                //    proj.Update(gameTime);

                //foreach (var ent in Entities)
                //    ent.Update(gameTime);
                Parallel.ForEach(Projectiles, proj => { proj.Update(gameTime); });

                Parallel.ForEach(Entities, ent => // update the entities in a parallel loop
                {
                    if (ent != null)
                        ent.Update(gameTime);
                });

                foreach (var sound in soundLog) sound.PlaySound();

                soundLog = new ConcurrentBag<SoundLog>();
                HitmarkerAlpha -= 500 * (float) ElapsedWorldThreadTime.TotalSeconds;
                if (HitmarkerAlpha < 1f) HitmarkerAlpha = 0f;
                HitmarkerAlt += 200 * (float) ElapsedWorldThreadTime.TotalSeconds;
                if (HitmarkerAlt > 255f) HitmarkerAlt = 255f;

                var Tickrate =
                    (int) (7 - threadTimer
                        .ElapsedMilliseconds); 
                if (Tickrate < 0) Tickrate = 0;
                Thread.Sleep(Tickrate); // have a set tick rate and remove the elapsed time to keep a fixed rate 


                threadTimer.Stop();
                total += threadTimer.Elapsed;
                ElapsedWorldThreadTime = threadTimer.Elapsed;
                threadTimer.Reset();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            hsv += 0.10f;
            int r, g, b;
            HSV.HsvToRgb(hsv, 1, 1, out r, out g, out b);
            var col = new Color(r, g, b);
            if (!worldInitialised)
            {
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                GraphicsDevice.Clear(new Color(0, 20, 150));
                SprBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                SprBatch.DrawString(Font, "Generating world...", new Vector2(30, 30), col);
                SprBatch.Draw(HealthRect, new Vector2(30, 60), null, Color.Black, 0f, Vector2.Zero,
                    new Vector2(300f, 20f), SpriteEffects.None, 0f);

                HSV.HsvToRgb(WorldGenProgress / 5f, 1, 1, out var r2, out var g2, out var b2);
                var col2 = new Color(r2, g2, b2);
                SprBatch.Draw(HealthRect, new Vector2(30, 60), null, col2, 0f, Vector2.Zero,
                    new Vector2(WorldGenProgress, 20f), SpriteEffects.None, 0f);

                var worldgenstageMsg = "";
                if (WorldGenProgress <= 100f)
                    worldgenstageMsg = "Generating blocks from noise...";
                else if (WorldGenProgress > 100f && WorldGenProgress <= 200f)
                    worldgenstageMsg = "Splitting blocks into chunks...";
                else
                    worldgenstageMsg = "Generating rendering meshes...";

                SprBatch.DrawString(Font, worldgenstageMsg, new Vector2(30, 90), Color.White);
                SprBatch.DrawString(Font, string.Format("{0}x{0}", world_size), new Vector2(30, 120), Color.DarkRed);
                SprBatch.DrawString(Font, WorldSeedStr + " " + WorldSeed, new Vector2(30, 150), Color.Red);

                SprBatch.Draw(HealthRect, new Vector2(30, 180), null, Color.Black, 0f, Vector2.Zero,
                    new Vector2(300f, 300f), SpriteEffects.None, 0f);
                for (var x = 0; x < world_size; x += 5)
                for (var y = 0; y < world_size; y += 5)
                {
                    if (perlintoplayer == null) continue;
                    SprBatch.Draw(HealthRect, new Vector2(30 + x, 180 + y), null,
                        Color.DarkGray * (perlintoplayer[x, y, 3] * 10), 0f, Vector2.Zero, new Vector2(5f, 5f),
                        SpriteEffects.None, 0f);
                    SprBatch.Draw(HealthRect, new Vector2(30 + x, 180 + y), null,
                        Color.DarkGreen * (perlintoplayer[x, y, 2] * 10), 0f, Vector2.Zero, new Vector2(5f, 5f),
                        SpriteEffects.None, 0f);
                    SprBatch.Draw(HealthRect, new Vector2(30 + x, 180 + y), null,
                        Color.Green * (perlintoplayer[x, y, 1] * 10), 0f, Vector2.Zero, new Vector2(5f, 5f),
                        SpriteEffects.None, 0f);
                    SprBatch.Draw(HealthRect, new Vector2(30 + x, 180 + y), null,
                        Color.LightGreen * (perlintoplayer[x, y, 0] * 10), 0f, Vector2.Zero, new Vector2(5f, 5f),
                        SpriteEffects.None, 0f);
                }


                SprBatch.End();
            }
            else
            {
                var world = BasePlayer.WorldPosition;
                var view = BasePlayer.View;
                var proj = BasePlayer.Projection;
                var viewProjection = view * proj;
                var rendering = BasePlayer.Rendering;


                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                GraphicsDevice.Clear(new Color(0, 20, 150));

                // draw every frame
                var delta = (float) gameTime.ElapsedGameTime.TotalSeconds;
                FPS.Update(delta);

                //ChunkWorld.DrawAll(gameTime);
                var rs = new RasterizerState();
                rs.CullMode = CullMode.CullCounterClockwiseFace;
                GraphicsDevice.RasterizerState = rs;

                Renderer.DrawChunks(GraphicsDevice, world, view, proj, rendering);
                //foreach (var block in BasePlayer.NearbyBlocks)
                //{
                //    if (block.Buffer == null) continue;
                //    Debug.DrawBoundingBox(block.Buffer, Effect, GraphicsDevice, BasePlayer.View, BasePlayer.Projection);
                //}
                //BasePlayer.Render(BasePlayer);
                foreach (var Projectile in Projectiles.ToList()) // render each projectile within the projectiles list
                    Projectile.Render(view, proj);


                BasePlayer.GetCurrentlyHeld().Render(BasePlayer);

                //Sky.Render(BasePlayer, new Vector3(10, 1, 10), new Vector3(0, 40, 0));

                // we can use this repeatedly instead of generating one for each entity
                foreach (var Entity in Entities.ToList())
                    if (Entity.IsInView(BasePlayer, viewProjection))
                        Entity.Render(BasePlayer, view, proj); // render each individual entity
                //if ((int)Global.ConsoleVars["debug_entityboxes"].val != 1)
                //    continue;
                //var buffer = Debug.CreateBoundingBoxBuffers(Entity.Box, GraphicsDevice); // generate the buffers to render for the entities bounding box
                //Debug.DrawBoundingBox(buffer, Effect, GraphicsDevice, BasePlayer.View, BasePlayer.Projection); // render bounding box

                var LookingAtBuffer = Debug.CreateBoundingBoxBuffers(BasePlayer.LookingAt, GraphicsDevice);
                Debug.DrawBoundingBox(LookingAtBuffer, Effect, GraphicsDevice, BasePlayer.View, BasePlayer.Projection);


                SprBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                if (ToggleUI)
                {
                    SprBatch.DrawString(Font, FPS.AverageFramesPerSecond.ToString(), new Vector2(10, 10),
                        Color.Yellow); // fps counter
                    SprBatch.DrawString(Font, gameTime.TotalGameTime.TotalSeconds.ToString(), new Vector2(10, 30),
                        Color.Yellow); // time between each Draw() call
                    SprBatch.DrawString(Font, Projectiles.Count.ToString(), new Vector2(10, 50),
                        Color.Orange); // number of projectiles alive
                    SprBatch.DrawString(Font, string.Format("timestamp: {0}", gameTime.TotalGameTime.Ticks.ToString()),
                        new Vector2(40, 50), Color.Red);
                    SprBatch.DrawString(Font, BasePlayer.GetCurrentlyHeld().ActionTimer.ToString(), new Vector2(10, 70),
                        Color.Purple);
                    SprBatch.DrawString(Font, string.Format("seed: {0}", WorldSeed), new Vector2(10, 90),
                        Color.MediumPurple);
                    var updatespersec = 1 / ElapsedWorldThreadTime.TotalSeconds;
                    if (double.IsInfinity(updatespersec)) updatespersec = 0;

                    SprBatch.DrawString(Font,
                        string.Format("render tick: {0}ms physics tick: {1}ms tickrate: {2}",
                            gameTime.ElapsedGameTime.TotalMilliseconds, ElapsedWorldThreadTime.TotalMilliseconds,
                            updatespersec), new Vector2(10, 110), Color.Red);
                    // currently held item and ammo count
                    SprBatch.DrawString(Font,
                        string.Format("pos: {0} horizVel: {1} vertVel: {2}", BasePlayer.Position, BasePlayer.horizVel,
                            BasePlayer.vertVel), new Vector2(10, 130), Color.Orange);

                    SprBatch.DrawString(Font,
                        string.Format("can jump: {0} midjump: {1} jump vec: {2}", BasePlayer.OnGround,
                            BasePlayer.midjump, BasePlayer.jumpVector), new Vector2(10, 170), Color.Purple);
                    SprBatch.DrawString(Font, string.Format("vel: {0}", BasePlayer.Velocity), new Vector2(10, 190),
                        Color.MediumPurple);
                    SprBatch.DrawString(Font,
                        string.Format("rendering: {0} body count: {1}", RenderingCubes, physcount),
                        new Vector2(10, 210), Color.MediumPurple);
                    for (var i = 0; i < BasePlayer.SurroundingChunks.Count(); i++)
                        SprBatch.DrawString(Font, string.Format("{0}", BasePlayer.SurroundingChunks[i].XYZ.ToString()),
                            new Vector2(10, 230 + i * 20), Color.BlueViolet);

                    SprBatch.DrawString(Font, "penguin simulator 2021", new Vector2(140, 10), col); // 
                    SprBatch.DrawString(Font, string.Format("ver: {0}.{1}.{2}", Major, Minor, Patch),
                        new Vector2(140, 30), col); // 

                    SprBatch.Draw(HealthRect, new Vector2(20, 680), null, Color.Black, 0f, Vector2.Zero,
                        new Vector2(200f, 20f), SpriteEffects.None, 0f);

                    var test = BasePlayer.GetCurrentlyHeld();
                    var type = test.GetType().ToString();
                    var name = type.Split('.')[1];
                    var scale = 0f;
                    if (test.MaxProjCount > 0) scale = 200f * (test.ProjectileCount / (float) test.MaxProjCount);
                    SprBatch.DrawString(Font, string.Format("{0}", name), new Vector2(20, 620), col);
                    SprBatch.DrawString(Font, string.Format("{0}", BasePlayer.WeaponAccuracy() * 100f),
                        new Vector2(20, 640), col);
                    SprBatch.DrawString(Font, string.Format("{0}/{1}", test.ProjectileCount, test.MaxProjCount),
                        new Vector2(20, 660), col);
                    SprBatch.Draw(HealthRect, new Vector2(20, 680), null, test.ProjectileColour, 0f, Vector2.Zero,
                        new Vector2(scale, 20f), SpriteEffects.None, 0f);
                }

                #region chunkBounds

                var renderEntInfo = true;
                for (var i = 0; i < Entities.Count; i++)
                {
                    if (!renderEntInfo) continue;

                    var penguinpos = GraphicsDevice.Viewport.Project(Entities[i].Position + Vector3.Up,
                        BasePlayer.Projection, BasePlayer.View, Matrix.Identity);
                    var penguinpos2 = GraphicsDevice.Viewport.Project(Entities[i].Position + co, BasePlayer.Projection,
                        BasePlayer.View, Matrix.Identity);
                    var heading = Entities[i].Position - BasePlayer.Position;
                    var dist = Vector3.Distance(BasePlayer.Position, Entities[i].Position);
                    if (dist > 35f) continue;

                    if (Vector3.Dot(heading, BasePlayer.WorldPosition.Forward) > 0)
                    {
                        SprBatch.DrawString(Font, Entities[i].Name, new Vector2(penguinpos.X, penguinpos.Y),
                            Color.Yellow, 0, Vector2.Zero, 4 / dist, SpriteEffects.None, 1f);
                        if (Entities[i] != BasePlayer.LastHitEnt) continue;
                        SprBatch.Draw(HealthRect, new Vector2(penguinpos2.X, penguinpos2.Y), null,
                            new Color(0, 0, 0, HitmarkerAlpha / 400), 0f, Vector2.Zero,
                            new Vector2(100f * (1 / dist), 5f * (4 / dist)), SpriteEffects.None,
                            0f); // draw black background of health bar
                        SprBatch.Draw(HealthRect, new Vector2(penguinpos2.X, penguinpos2.Y), null,
                            new Color(255, 0, 0, HitmarkerAlpha / 400), 0f, Vector2.Zero,
                            new Vector2(Entities[i].Health * (1 / dist), 5f * (4 / dist)), SpriteEffects.None,
                            0f); // draw health in red
                        SprBatch.Draw(HealthRect, new Vector2(penguinpos2.X, penguinpos2.Y), null,
                            new Color(0, 255, 255, HitmarkerAlpha / 400), 0f, Vector2.Zero,
                            new Vector2(Entities[i].ShieldHealth * (1 / dist), 5f * (4 / dist)), SpriteEffects.None,
                            0f); // draw shield in blue

                        //SprBatch.DrawString(Font, Entities[i].Health.ToString(), new Vector2(penguinpos2.X, penguinpos2.Y), Color.Red, 0, Vector2.Zero, 4 / dist, SpriteEffects.None, 1f);
                    }
                }


                /*
                Vector3 transla = GraphicsDevice.Viewport.Project(BasePlayer.GetCurrentlyHeld().Translation, BasePlayer.Projection, BasePlayer.View, Matrix.Identity);
                SprBatch.DrawString(Font, "-", new Vector2(transla.X, transla.Y), Color.Pink);

                for (int i = 0; i < cubeMap.Chunks.Count; i++)
                {

                    Vector3 chunkpos = cubeMap.Chunks[i].Min;
                    Vector3 prevpos = cubeMap.Chunks[i].Max;

                    float dist = (Vector3.Distance(BasePlayer.Position, chunkpos));
                    float dist2 = (Vector3.Distance(BasePlayer.Position, prevpos));
                    var heading = (chunkpos - BasePlayer.Position);
                    var heading2 = (prevpos - BasePlayer.Position);
                    Vector3 w2c = GraphicsDevice.Viewport.Project(chunkpos, BasePlayer.Projection, BasePlayer.View, Matrix.Identity);
                    Vector3 w2c2 = GraphicsDevice.Viewport.Project(prevpos, BasePlayer.Projection, BasePlayer.View, Matrix.Identity);

                    if ((Vector3.Dot(heading, BasePlayer.WorldPosition.Forward)) > 0)
                        SprBatch.DrawString(Font, i.ToString(), new Vector2(w2c.X, w2c.Y), Color.Red, 0, Vector2.Zero, 8 / dist, SpriteEffects.None, 1f);
                    if ((Vector3.Dot(heading2, BasePlayer.WorldPosition.Forward)) > 0)
                        SprBatch.DrawString(Font, i.ToString(), new Vector2(w2c2.X, w2c2.Y), Color.LightSkyBlue, 0, Vector2.Zero, 8 / dist2, SpriteEffects.None, 1f);
                }*/

                #endregion

                var hitmarkercol = new Color(HitmarkerAlt / 255f, HitmarkerAlt / 255f, 1f);
                hitmarkercol *= HitmarkerAlpha / 255f;
                SprBatch.DrawLine(WindowCentre - new Vector2(10, 10), WindowCentre - new Vector2(5, 5), hitmarkercol,
                    2f);
                SprBatch.DrawLine(WindowCentre - new Vector2(10, -10), WindowCentre - new Vector2(5, -5), hitmarkercol,
                    2f);

                SprBatch.DrawLine(WindowCentre + new Vector2(10, 10), WindowCentre + new Vector2(5, 5), hitmarkercol,
                    2f);
                SprBatch.DrawLine(WindowCentre - new Vector2(-10, 10), WindowCentre - new Vector2(-5, 5), hitmarkercol,
                    2f);

                SprBatch.Draw(CrosshairPlayer.CrosshairVertical, CrosshairPlayer.CrosshairPosition[0], Color.White);
                SprBatch.Draw(CrosshairPlayer.CrosshairVertical, CrosshairPlayer.CrosshairPosition[1], Color.White);
                SprBatch.Draw(CrosshairPlayer.CrosshairHorizontal, CrosshairPlayer.CrosshairPosition[2], Color.White);
                SprBatch.Draw(CrosshairPlayer.CrosshairHorizontal, CrosshairPlayer.CrosshairPosition[3], Color.White);

                SprBatch.End();
                if (!ToggleUI)
                    _desktop.Render();

                RenderingCubes = 0;
            }

            base.Draw(gameTime);
        }

        public class SoundLog
        {
            public float pan;
            public float pitch;
            public SoundEffect soundEffect;
            public float vol;

            public void PlaySound()
            {
                soundEffect.Play(vol, pitch, pan);
            }
        }

        #region UISetup

        private float val = 0.0025f;
        private Widget zLabel;

        private void MyraSetup()
        {
            MyraEnvironment.Game = this;

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            for (var i = 0; i < BasePlayer.Inventory.Count; i++)
            {
                var button = new TextButton
                {
                    GridRow = i + 1,
                    GridColumn = 1,
                    Text = BasePlayer.Inventory[i].ToString(),
                    Id = i.ToString()
                };
                button.TouchDown += invButtonClick;
                grid.Widgets.Add(button);
            }

            var regenButton = new TextButton
            {
                GridRow = 1,
                GridColumn = 2,
                Text = "New World"
            };
            regenButton.TouchDown += newWorldButton;
            grid.Widgets.Add(regenButton);

            var resetButton = new TextButton
            {
                GridRow = 2,
                GridColumn = 2,
                Text = "Reset Position"
            };
            resetButton.TouchDown += resetPosButton;
            grid.Widgets.Add(resetButton);

            // Add it to the desktop
            _desktop = new Desktop();
            _desktop.Root = grid;
        }

        private void resetPosButton(object sender, EventArgs e)
        {
            BasePlayer.Position = new Vector3(0, 1, 0);
            BasePlayer.Velocity = Vector3.Zero;
        }

        private void ResetWorld()
        {
            updateThread.Abort();
            System.Console.Clear();
            WorldGenProgress = 0;
            worldInitialised = false;
            startOnce = false;
            BasePlayer.Position = new Vector3(0, 1, 0);
            BasePlayer.Velocity = Vector3.Zero;
            BasePlayer.prevChunkPos = new Vector3(-1, -1, -1);
        }

        private void newWorldButton(object sender, EventArgs e)
        {
            ResetWorld();
        }

        private void invButtonClick(object sender, EventArgs e)
        {
            var invindex = int.Parse(((TextButton) sender).Id);
            BasePlayer.HeldItemIndex = invindex;
        }

        #endregion
    }
}