using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using static TpLibrary.DZR;

namespace TpLibrary
{
    public class DZR
    {
        public List<Actor> ActorList = new List<Actor>();
        public List<Door> DoorList = new List<Door>();
        public List<TgscInfo> Scob = new List<TgscInfo>();


        public List<Chunk> Chunks = new List<Chunk>();

        public DZR() { }

        public DZR(string filePath)
        {
            Read(new BinaryDataReader(File.OpenRead(filePath)));
        }

        public DZR(Stream stream)
        {
            Read(new BinaryDataReader(stream));
        }

        public void Export(string filePath)
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public void Import(string filePath)
        {
        }

        public void Save(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                this.Write(new BinaryDataWriter(fs));
            }
        }

        public void Save(Stream stream)
        {
            this.Write(new BinaryDataWriter(stream));
        }

        public void Read(BinaryDataReader reader)
        {
            reader.ByteOrder = ByteOrder.BigEndian;
            uint num = reader.ReadUInt32();

            Chunks.Clear();
            for (int i = 0; i < num; i++)
            {
                Chunk chunk = new Chunk();
                chunk.Magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                chunk.Count = reader.ReadUInt32();
                chunk.Offset = reader.ReadUInt32();
                Chunks.Add(chunk);
            }

            //Store raw data into byte arrays to keep unloaded data intact
            var ordered = Chunks.OrderBy(x => x.Offset).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                var offset = ordered[i].Offset;
                var size = reader.BaseStream.Length - offset;
                if (i < ordered.Count - 1)
                    size = ordered[i + 1].Offset - offset;

                reader.Seek(offset, SeekOrigin.Begin);
                ordered[i].Data = reader.ReadBytes((int)size);
            }
            //Load stage data
            StageLoader(reader);
        }

        private void StageLoader(BinaryDataReader reader)
        {
            LoadStageData(reader, new Dictionary<string, Action<BinaryDataReader>>()
            {
                { "ACTR", LoadActor},
                { "TGOB", LoadActor},

                { "TGSC", LoadScob},

                { "Door", LoadDoors},
                { "SCOB", LoadScob},
            });
            ActorLayerLoader(reader);
        }

        private void ActorLayerLoader(BinaryDataReader reader)
        {
            string[] actrLayer = new string[] { "ACT0", "ACT1", "ACT2", "ACT3", "ACT4", "ACT5", "ACT6", "ACT7", "ACT8", "ACT9", "ACTa", "ACTb", "ACTc", "ACTd", "ACTe" };
            string[] scobLayer = new string[] { "SCO0", "SCO1", "SCO2", "SCO3", "SCO4", "SCO5", "SCO6", "SCO7", "SCO8", "SCO9", "SCOa", "SCOb", "SCOc", "SCOd", "SCOe" };
            string[] doorLayer = new string[] { "Doo0", "Doo1", "Doo2", "Doo3", "Doo4", "Doo5", "Doo6", "Doo7", "Doo8", "Doo9", "Dooa", "Doob", "Dooc", "Dood", "Dooe" };

            for (int i = 0; i < 15; i++)
            {
                LoadStageData(reader, new Dictionary<string, Action<BinaryDataReader, int>>()
                {
                    { actrLayer[i], LoadActor},
                    { scobLayer[i], LoadScob},
                    { doorLayer[i], LoadDoors},
                }, i);
            }
        }

        private void LoadStageData(BinaryDataReader reader,
            Dictionary<string, Action<BinaryDataReader>> data)
        {
            foreach (var item in data)
            {
                var chunk = Chunks.FirstOrDefault(x => x.Magic == item.Key);
                if (chunk != null)
                {
                    reader.Seek(chunk.Offset, SeekOrigin.Begin);
                    item.Value.Invoke(reader);
                }
            }
        }

        private void LoadStageData(BinaryDataReader reader,
            Dictionary<string, Action<BinaryDataReader, int>> data, int layer)
        {
            foreach (var item in data)
            {
                var chunk = Chunks.FirstOrDefault(x => x.Magic == item.Key);
                if (chunk != null)
                {
                    reader.Seek(chunk.Offset, SeekOrigin.Begin);
                    item.Value.Invoke(reader, layer);
                }
            }
        }

        private void LoadActor(BinaryDataReader reader, int layer) {
            this.ActorList.Add(new Actor(reader) { Layer = layer, });
        }

        private void LoadDoors(BinaryDataReader reader) {
            this.DoorList.Add(new Door(reader) { Layer = -1, });
        }

        private void LoadDoors(BinaryDataReader reader, int layer) {
            this.DoorList.Add(new Door(reader) { Layer = layer, });
        }

        private void LoadScob(BinaryDataReader reader) {
            this.Scob.Add(new TgscInfo(reader) { Layer = -1, });
        }

        private void LoadScob(BinaryDataReader reader, int layer) {
            this.Scob.Add(new TgscInfo(reader) { Layer = layer, });
        }

        private void LoadActor(BinaryDataReader reader) {
            this.ActorList.Add(new Actor(reader) { Layer = -1, });
        }

        private void SaveData()
        {
            string[] actrLayer = new string[] { "ACT0", "ACT1", "ACT2", "ACT3", "ACT4", "ACT5", "ACT6", "ACT7", "ACT8", "ACT9", "ACTa", "ACTb", "ACTc", "ACTd", "ACTe" };
            string[] scobLayer = new string[] { "SCO0", "SCO1", "SCO2", "SCO3", "SCO4", "SCO5", "SCO6", "SCO7", "SCO8", "SCO9", "SCOa", "SCOb", "SCOc", "SCOd", "SCOe" };
            string[] doorLayer = new string[] { "Doo0", "Doo1", "Doo2", "Doo3", "Doo4", "Doo5", "Doo6", "Doo7", "Doo8", "Doo9", "Dooa", "Doob", "Dooc", "Dood", "Dooe" };

            var actors = ActorList.Where(x => x.Layer == -1);

            for (int i = 0; i < 15; i++)
            {
                var actors_layer = ActorList.Where(x => x.Layer == i);

            }
        }

        public void Write(BinaryDataWriter writer)
        {
            writer.ByteOrder = ByteOrder.BigEndian;
            writer.Write(this.Chunks.Count);
            foreach (var chunk in this.Chunks)
            {
                writer.Write(Encoding.ASCII.GetBytes(chunk.Magic));
                writer.Write(chunk.Count);
                writer.Write(0); //offset for later
            }

            var ordered = Chunks.OrderBy(x => x.Offset).ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var index = Chunks.IndexOf(ordered[i]);

                var pos = writer.BaseStream.Position;

                writer.Seek(4 + (12 * index) + 8, SeekOrigin.Begin);
                writer.Write((uint)pos);

                writer.Seek(pos, SeekOrigin.Begin);

                switch (ordered[i].Magic)
                {
                    default:
                        writer.Write(ordered[i].Data);
                        break;
                }

                AlignBytes(writer, 4);
            }
        }

        public void AlignBytes(BinaryDataWriter writer, int alignment, byte value = 0xFF)
        {
            var startPos = writer.Position;
            long position = writer.Seek((-writer.Position % alignment + alignment) % alignment, SeekOrigin.Current);

            writer.Seek(startPos, System.IO.SeekOrigin.Begin);
            while (writer.Position != position)
            {
                writer.Write(value);
            }
        }

        public class Actor
        {
            public string Name;
            public byte[] Parameters;
            public Vector3 Position;
            public Vector3 Rotation;
            public short EnemyID;

            public int Layer = -1;

            public Actor() { }

            public Actor(BinaryDataReader reader) { Read(reader); }

            public virtual void Read(BinaryDataReader reader)
            {
                Name = Encoding.ASCII.GetString(reader.ReadBytes(8)).Replace("\0", "");
                Parameters = reader.ReadBytes(4);
                Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Rotation = new Vector3(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16()).ToDegrees();
                EnemyID = reader.ReadInt16();
            }

            public virtual void Write(BinaryDataWriter writer)
            {
                var pos = writer.BaseStream.Position;
                writer.Write(Encoding.ASCII.GetBytes(Name));
                writer.Seek(pos + 8, SeekOrigin.Begin);

                var rot = Rotation.FromDegrees();

                writer.Write(this.Parameters);
                writer.Write(this.Position.X);
                writer.Write(this.Position.Y);
                writer.Write(this.Position.Z);
                writer.Write((short)rot.X);
                writer.Write((short)rot.Y);
                writer.Write((short)rot.Z);
                writer.Write(EnemyID);
            }
        }

        public class TgscInfo : Actor
        {
            public Vector3 Scale;

            public TgscInfo() { }
            public TgscInfo(BinaryDataReader reader) { Read(reader); }

            public override void Read(BinaryDataReader reader)
            {
                base.Read(reader);
                Scale = new Vector3(reader.ReadByte() / 10.0f, reader.ReadByte() / 10.0f, reader.ReadByte() / 10.0f);
            }

            public override void Write(BinaryDataWriter writer)
            {
                base.Write(writer);
                writer.Write((byte)(Scale.X * 10));
                writer.Write((byte)(Scale.Y * 10));
                writer.Write((byte)(Scale.Z * 10));
            }
        }

        public class Door : TgscInfo
        {
            public Door() { }
            public Door(BinaryDataReader reader) { Read(reader); }

            public override void Read(BinaryDataReader reader)
            {
                base.Read(reader);
            }

            public override void Write(BinaryDataWriter writer)
            {
                base.Write(writer);
            }
        }

        public class Chunk
        {
            public string Magic = "";
            public uint Offset;
            public uint Count;

            public byte[] Data;
        }
    }
}
