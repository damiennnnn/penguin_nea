using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using QuakeConsole;
using Microsoft.Xna.Framework.Audio;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Utility;
using MonoGame.Extended;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Timers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace gameproject
{
    public class GameState
    {
        [Serializable]
        public class Block
        {
            public BoundingBox Bounding;
            public Vector3 Position;
            public string Type = "dirt";
        }
        [Serializable]
        public class Chunk
        {
            public Vector3 XYZ;
            public List<Block> Blocks = new List<Block>();
            public Vector3 Min;
            public Vector3 Max;
        }
        public class GameWorld {
            public List<BoundingBox> BoundingBoxes = new List<BoundingBox>();
            public List<Vector3> BoxPositions = new List<Vector3>();
            public List<Chunk> Chunks = new List<Chunk>();

            public List<Projectile> Projectiles = new List<Projectile>(); // list of projectiles spawned in the world
            public List<Projectile> Autoprojs = new List<Projectile>(); // list of autoprojectiles (unused)
            public List<Entity> Entities = new List<Entity>(); // list of entities in the world

        }

        public GameWorld World; // world to use 

        public bool Initialised = false;
        public double Tickrate = (1000 / 64);
        EventBasedNetListener Listener = new EventBasedNetListener();
        NetManager Server;

        List<NetPeer> Peers = new List<NetPeer>();
        Timer Ticker;

        [Serializable]
        public class State{
            public List<Chunk> Chunks = new List<Chunk>();
            public List<Projectile> Projectiles = new List<Projectile>(); // list of projectiles spawned in the world
            public List<Entity> Entities = new List<Entity>(); // list of entities in the world
        }



        public GameState()
        {
            Server = new NetManager(Listener);
            Server.Start(35545);

            Listener.PeerConnectedEvent += PeerConnectedEvent;
            Listener.ConnectionRequestEvent += ConnectionRequestEvent;
            
            WorldSetup();

            Ticker = new Timer();
            Ticker.Interval = Tickrate;
            Ticker.Elapsed += Tick;
            Ticker.Start();
            Initialised = true;
        }
        TimeSpan totalTimeSpan = new TimeSpan();
        private void Tick(object sender, ElapsedEventArgs e)
        {
            Initialised = true; // our game server is now running

            totalTimeSpan.Add(TimeSpan.FromMilliseconds(Tickrate)); // add our ticks to our timespan
            GameTime gameTime = new GameTime(totalTimeSpan, TimeSpan.FromMilliseconds(Tickrate));

            foreach (var Entity in World.Entities)
                Entity.Update(gameTime);

            foreach (var Projectile in World.Projectiles)
                Projectile.Update(gameTime);


            NetDataWriter writer = new NetDataWriter();

            State curState = new State()
            {
                Chunks = World.Chunks,
                Entities = World.Entities,
                Projectiles = World.Projectiles
            };
            
            Server.PollEvents();
            Server.SendToAll(Serialiser.Serialize(curState), DeliveryMethod.Sequenced);
            //Ticker.Change(Tickrate, Timeout.Infinite);
            //Server.SendToAll();
        }

        private void ConnectionRequestEvent(ConnectionRequest request)
        {
            request.AcceptIfKey("penguinsim");
        }


        private void PeerConnectedEvent(NetPeer peer)
        {
            Guid id = Guid.NewGuid(); // generate new id
            NetDataWriter writer = new NetDataWriter(); 
            writer.Put(id.ToString()); 
            peer.Send(writer, DeliveryMethod.ReliableOrdered); // send this id to our new client with reliability

            Entity playerEnt = new Entity(new Vector3(0, 0, 0), "penguin", "penguin", "Player");
            playerEnt.ID = id.ToString();
            World.Entities.Add(playerEnt);

            Peers.Add(peer);
        }

        public static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        void HandleNewConnection()
        {
            Entity playerEnt = new Entity(new Vector3(0, 0, 0), "penguin", "penguin", "Player"); // create a new entity
            playerEnt.ID = "";
        }

        void HandleDisconnect()
        {
        }

        void WorldSetup()
        {
            World = new GameWorld();
            World.Entities.Add(new Entity(new Vector3(0, 0, 0), "penguin", "penguin", "margie"));
            World.Entities.Add(new Entity(new Vector3(2, 0, 0), "penguin", "penguin", "joe"));
            World.Entities.Add(new Entity(new Vector3(4, 0, 0), "penguin", "penguin", "hingadinga"));
            World.Entities.Add(new Entity(new Vector3(6, 0, 0), "penguin", "penguin", "urmum"));
            World.Entities.Add(new Entity(new Vector3(8, 0, 0), "penguin", "penguin", "BoJo"));



            List<Vector3> Pos = new List<Vector3>();
            for (int i = -50; i < 50; i++)
            {
                for (int z = -50; z < 50; z++)
                    Pos.Add(new Vector3(i, -1, z));
            }

            Pos.Add(new Vector3(0, 5, 0));
            Pos.Add(new Vector3(5, 0, 5));

            World.BoxPositions = Pos;
            foreach (var pos in Pos)
            {
                Block block = new Block();
                Vector3 min = new Vector3(pos.X, pos.Y, pos.Z); // calculate bounding box for collision calculations
                Vector3 max = min + Vector3.One;

                BoundingBox Box = new BoundingBox(min, max);
                block.Bounding = Box;
                block.Position = pos;
                // calculate which chunk the block is stored in
                int boxX = (int)System.Math.Round(pos.X / 16); // each chunk is 16x16x16
                int boxY = (int)System.Math.Round(pos.Y / 16); // 
                int boxZ = (int)System.Math.Round(pos.Z / 16); // 
                Vector3 ChunkPos = new Vector3(boxX, boxY, boxZ);

                if (World.Chunks.Exists(x => x.XYZ == ChunkPos)) // if a chunk already exists with our chunk pos
                {
                    World.Chunks.Find(x => x.XYZ == ChunkPos).Blocks.Add(block); // add our block to that chunk
                    //Vector3 ChunkPosToReal = (ChunkPos * 16);
                    //World.Chunks.Find(x => x.XYZ == ChunkPos).Min = ChunkPosToReal;
                    //World.Chunks.Find(x => x.XYZ == ChunkPos).Max = ChunkPosToReal + new Vector3(16, 16, 16);
                }
                else
                {
                    Chunk newChunk = new Chunk(); // set up a new chunk
                    newChunk.XYZ = ChunkPos; // set its position
                    newChunk.Blocks.Add(block); // add our block
                    newChunk.Min = (ChunkPos * 16);
                    newChunk.Max = ((ChunkPos * 16) + new Vector3(16, 16, 16));
                    World.Chunks.Add(newChunk); // add our chunk to the chunk list
                }
                // sort
                World.Chunks = World.Chunks.OrderBy(v => v.XYZ.X).ToList();
                World.Chunks = World.Chunks.OrderBy(v => v.XYZ.Y).ToList();
                World.Chunks = World.Chunks.OrderBy(v => v.XYZ.Z).ToList();
                //



            }

        }
    }
}
