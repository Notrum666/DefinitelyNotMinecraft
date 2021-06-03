using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace DefinitelyNotMinecraft
{
    public static class Extensions
    {
        public static Matrix4 toMatrix(this Quaternion q)
        {
            return new Matrix4(1 - 2 * q.Y * q.Y - 2 * q.Z * q.Z, 2 * q.X * q.Y - 2 * q.Z * q.W, 2 * q.X * q.Z + 2 * q.Y * q.W, 0,
                               2 * q.X * q.Y + 2 * q.Z * q.W, 1 - 2 * q.X * q.X - 2 * q.Z * q.Z, 2 * q.Y * q.Z - 2 * q.X * q.W, 0,
                               2 * q.X * q.Z - 2 * q.Y * q.W, 2 * q.Y * q.Z + 2 * q.X * q.W, 1 - 2 * q.X * q.X - 2 * q.Y * q.Y, 0,
                               0, 0, 0, 1);
        }
    }
    public enum Orientation
    {
        Front = 0,
        Back = 1,
        Right = 2,
        Left = 3,
        Up = 4,
        Down = 5
    }
    internal static class Cube
    {
        internal static Vertex[][] polygons;
    }
    public abstract class Block
    {
        public ushort id;
        public bool notCube;
        public int textureId;
        public List<TextureLocation> textureLocations = new List<TextureLocation>();
        public TextureLocation getTexture(int x, int y, int z, Orientation side, ushort secondaryId)
        {
            if (textureLocations.Count == 0)
                return new TextureLocation();
            else
                return textureLocations[0];
        }
    }
    public struct BlockID
    {
        public ushort id;
        public ushort secondaryId;
    }
    public class Chunk
    {
        private BlockID[,,] content;
        internal int X, Z;
        internal int vertexCount = 0;
        internal int vao;
        private int vbo;
        internal bool isLoaded { get; private set; }
        private bool hasBeenModified = false;
        public BlockID this[int x, int y, int z]
        {
            get { return content[x - X * Core.CHUNK_WIDTH, y, z - Z * Core.CHUNK_LENGTH]; }
            set { content[x - X * Core.CHUNK_WIDTH, y, z - Z * Core.CHUNK_LENGTH] = value; }
        }
        public Chunk(int X, int Z)
        {
            isLoaded = false;

            content = new BlockID[Core.CHUNK_WIDTH, Core.CHUNK_HEIGHT, Core.CHUNK_LENGTH];

            this.X = X;
            this.Z = Z;

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * 4, 0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * 4, 3 * 4);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 8 * 4, 5 * 4);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
        public void setHasBeenModified()
        {
            hasBeenModified = true;
        }
        public bool getHasBeenModified()
        {
            return hasBeenModified;
        }
        internal void load()
        {
            Core.GetChunk(X - 1, Z)?.setHasBeenModified();
            Core.GetChunk(X + 1, Z)?.setHasBeenModified();
            Core.GetChunk(X, Z - 1)?.setHasBeenModified();
            Core.GetChunk(X, Z + 1)?.setHasBeenModified();
            hasBeenModified = true;



            for (int i = 0; i < Core.CHUNK_WIDTH; i++)
                for (int j = 0; j < Core.CHUNK_LENGTH; j++)
                    for (int k = 0; k < 55 + 5 * Math.Sin((X * Core.CHUNK_WIDTH + i) / 16.0 * Math.PI) * Math.Sin((Z * Core.CHUNK_LENGTH + j) / 16.0 * Math.PI); k++)
                        content[i, k, j].id = 1;
                        //if (k <= 55)
                        //    content[i, k, j] = 1;
                        //else
                        //    content[i, k, j] = 2;
            isLoaded = true;
        }
        internal void unload()
        {
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
        }
        internal void update()
        {

        }
        internal void updateVAO()
        {
            List<float> data = new List<float>();
            //Model cube = (Model)ResourceManager.GetModel("Cube");
            Chunk front = Core.GetChunk(X, Z + 1);
            Chunk back = Core.GetChunk(X, Z - 1);
            Chunk right = Core.GetChunk(X + 1, Z);
            Chunk left = Core.GetChunk(X - 1, Z);
            Block curBlock;
            Vector3 blockPos = new Vector3();
            ushort secondaryId;
            void addPolygon(Orientation side, int x, int y, int z)
            {
                TextureLocation loc = curBlock.getTexture(x, y, z, side, secondaryId);
                foreach (Vertex v in Cube.polygons[(int)side])
                {
                    Vector3 pos = blockPos + v.v;
                    data.AddRange(new float[] { pos.X, pos.Y, pos.Z, loc.pos_x + v.vt.X * loc.size_x, loc.pos_y + v.vt.Y * loc.size_y, v.vn.X, v.vn.Y, v.vn.Z });
                }
            }
            for (int i = 0; i < Core.CHUNK_WIDTH; i++)
                for (int j = 0; j < Core.CHUNK_HEIGHT; j++)
                    for (int k = 0; k < Core.CHUNK_LENGTH; k++)
                    {
                        if (content[i, j, k].id != 0)
                        {
                            curBlock = Core.GetBlockByID(content[i, j, k].id);
                            if (!curBlock.notCube)
                            {
                                blockPos.X = i;
                                blockPos.Y = j;
                                blockPos.Z = k;
                                secondaryId = content[i, j, k].secondaryId;
                                if (i != 0 && (content[i - 1, j, k].id == 0 || Core.GetBlockByID(content[i - 1, j, k].id).notCube) ||
                                    i == 0 && left != null && (left.content[Core.CHUNK_WIDTH - 1, j, k].id == 0 || Core.GetBlockByID(left.content[Core.CHUNK_WIDTH - 1, j, k].id).notCube))
                                    addPolygon(Orientation.Left, i, j, k); // left 
                                if (i != Core.CHUNK_WIDTH - 1 && (content[i + 1, j, k].id == 0 || Core.GetBlockByID(content[i + 1, j, k].id).notCube) ||
                                    i == Core.CHUNK_WIDTH - 1 && right != null && (right.content[0, j, k].id == 0 || Core.GetBlockByID(right.content[0, j, k].id).notCube))
                                    addPolygon(Orientation.Right, i, j, k); // right
                                if (j == 0 || content[i, j - 1, k].id == 0 || Core.GetBlockByID(content[i, j - 1, k].id).notCube)
                                    addPolygon(Orientation.Down, i, j, k); // down
                                if (j == Core.CHUNK_HEIGHT - 1 || content[i, j + 1, k].id == 0 || Core.GetBlockByID(content[i, j + 1, k].id).notCube)
                                    addPolygon(Orientation.Up, i, j, k); // up
                                if (k != 0 && (content[i, j, k - 1].id == 0 || Core.GetBlockByID(content[i, j, k - 1].id).notCube) ||
                                    k == 0 && back != null && (back.content[i, j, Core.CHUNK_LENGTH - 1].id == 0 || Core.GetBlockByID(back.content[i, j, Core.CHUNK_LENGTH - 1].id).notCube))
                                    addPolygon(Orientation.Back, i, j, k); // back
                                if (k != Core.CHUNK_LENGTH - 1 && (content[i, j, k + 1].id == 0 || Core.GetBlockByID(content[i, j, k + 1].id).notCube) ||
                                    k == Core.CHUNK_LENGTH - 1 && front != null && (front.content[i, j, 0].id == 0 || Core.GetBlockByID(front.content[i, j, 0].id).notCube))
                                    addPolygon(Orientation.Front, i, j, k); // front
                            }
                            else
                            {
                                // TODO: Implement non-cubed blocks
                            }
                        }
                    }
            
            if (data.Count == 0)
            {
                vertexCount = 0;
                hasBeenModified = false;
                return;
            }

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * data.Count, data.ToArray(), BufferUsageHint.DynamicDraw);
            vertexCount = data.Count / 8;
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            hasBeenModified = false;
        }
    }
    public class Transform
    {
        public Vector3 position { get { return parent == null ? localPosition : (parent.model * new Vector4(localPosition, 1f)).Xyz; }
                                  set { if (parent == null) localPosition = value; else localPosition = (new Vector4(value, 1f) * parent.model).Xyz; } }
        internal Vector3 prevFramePosition;
        internal Vector3 deltaPosition;
        public Quaternion rotation { get { return parent == null ? localRotation : parent.rotation * rotation; }
                                     set { if (parent == null) localRotation = value; else localRotation = value * parent.rotation.Inverted(); } }
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 scale;
        public Vector3 forward { get { return rotation * Vector3.UnitZ; } }
        public Vector3 right { get { return rotation * -Vector3.UnitX; } }
        public Vector3 up { get { return rotation * Vector3.UnitY; } }
        public Transform parent;
        public Matrix4 model { get 
            {
                Matrix4 mat = Matrix4.CreateScale(scale) * rotation.toMatrix();
                mat.M41 += position.X;
                mat.M42 += position.Y;
                mat.M43 += position.Z;
                if (parent != null)
                    return parent.model * mat;
                else
                    return mat;
            } }
        public Transform()
        {
            localRotation = Quaternion.Identity;
            scale = new Vector3(1, 1, 1);
        }
        public Transform(Vector3 position)
        {
            localPosition = position;
            prevFramePosition = position;
            localRotation = Quaternion.Identity;
            scale = new Vector3(1, 1, 1);
        }
        public Transform(Vector3 position, Transform parent)
        {
            localPosition = position;
            prevFramePosition = position;
            localRotation = Quaternion.Identity;
            scale = new Vector3(1, 1, 1);
            this.parent = parent;
        }
        public Transform(Vector3 position, Quaternion rotation)
        {
            localPosition = position;
            prevFramePosition = position;
            localRotation = rotation;
            scale = new Vector3(1, 1, 1);
        }
        public Transform(Vector3 position, Quaternion rotation, Transform parent)
        {
            localPosition = position;
            prevFramePosition = position;
            localRotation = rotation;
            scale = new Vector3(1, 1, 1);
            this.parent = parent;
        }
        public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            localPosition = position;
            prevFramePosition = position;
            localRotation = rotation;
            this.scale = scale;
        }
        public Transform(Vector3 position, Quaternion rotation, Vector3 scale, Transform parent)
        {
            localPosition = position;
            prevFramePosition = position;
            localRotation = rotation;
            this.scale = scale;
            this.parent = parent;
        }
        public Transform(Vector3 position, Vector3 scale)
        {
            localPosition = position;
            prevFramePosition = position;
            localRotation = Quaternion.Identity;
            this.scale = scale;
        }
        public Transform(Vector3 position, Vector3 scale, Transform parent)
        {
            localPosition = position;
            prevFramePosition = position;
            localRotation = Quaternion.Identity;
            this.scale = scale;
            this.parent = parent;
        }
        public Transform(Quaternion rotation)
        {
            localRotation = rotation;
            scale = new Vector3(1, 1, 1);
        }
        public Transform(Quaternion rotation, Transform parent)
        {
            localRotation = rotation;
            scale = new Vector3(1, 1, 1);
            this.parent = parent;
        }
        public Transform(Quaternion rotation, Vector3 scale)
        {
            localRotation = rotation;
            this.scale = scale;
        }
        public Transform(Quaternion rotation, Vector3 scale, Transform parent)
        {
            localRotation = rotation;
            this.scale = scale;
            this.parent = parent;
        }
    }
    public class Mesh
    {
        public Model model;
        public Texture texture;
        public bool hide;
    }
    public enum CollisionGroup
    {
        World,
        Player,
        Mob,
        Item
    }
    public class MatrixDictionary<Key1, Key2, Value>
    {
        private Dictionary<Key1, Dictionary<Key2, Value>> dict = new Dictionary<Key1, Dictionary<Key2, Value>>();
        public Value this[Key1 key1, Key2 key2]
        {
            get 
            { 
                if (!dict.ContainsKey(key1))
                    dict[key1] = new Dictionary<Key2, Value>();
                return dict[key1][key2];
            }
            set
            {
                if (!dict.ContainsKey(key1))
                    dict[key1] = new Dictionary<Key2, Value>();
                dict[key1][key2] = value;
            }
        }
        public bool ContainsKeyPair(Key1 key1, Key2 key2)
        {
            return dict.ContainsKey(key1) && dict[key1].ContainsKey(key2);
        }
        public void Clear()
        {
            dict.Clear();
        }

    }
    public class Collider
    {

        public Vector3 offset;
        public Vector3 size;
    }
    public class Rigidbody
    {
        public float mass = 1f;
        public Vector3 velocity;
        public float drag = 0;
        public bool useGravity;
        public bool isStatic;
        public void applyForce(Vector3 force)
        {
            velocity += force * (float)Time.DeltaTime / mass;
        }
        public void applyImpulse(Vector3 impulse)
        {
            velocity += impulse / mass;
        }
    }
    public class Entity
    {
        public Transform transform { get; } = new Transform();
        public Mesh mesh;
        public Collider collider;
        public Rigidbody rigidbody;
        public bool useGravity;
        public void Update()
        {

        }
        public void FixedUpdate()
        {
            transform.prevFramePosition = transform.position;
            transform.position += transform.deltaPosition;
        }
    }
    public struct Camera
    {
        public Transform transform;
        public float FOV;
        public float resolution;
        public Matrix4 view { get { return Matrix4.LookAt(transform.position, transform.position - transform.forward, transform.up); } }
        public Matrix4 proj { get { return Matrix4.CreatePerspectiveFieldOfView(FOV, resolution, 0.01f, 1000); } }
        public Camera(Vector3 position, Quaternion rotation, float FOV, float resolution)
        {
            transform = new Transform(position, rotation);
            this.FOV = FOV;
            this.resolution = resolution;
        }
        public Camera(Vector3 position, Vector3 euler, float FOV, float resolution)
        {
            transform = new Transform(position, Quaternion.FromEulerAngles(euler));
            this.FOV = FOV;
            this.resolution = resolution;
        }
    }
    public static class Core
    {
        public static readonly int CHUNK_LENGTH = 16;
        public static readonly int CHUNK_WIDTH = 16;
        public static readonly int CHUNK_HEIGHT = 256;
        private static readonly int MAX_CHUNKS_PER_FRAME = 2;
        private static int _renderDistance = 12;
        public static int RenderDistance { get { return _renderDistance; } set { _renderDistance = value; UpdateChunks(); } }
        internal static List<Chunk> chunks = new List<Chunk>();
        private static List<Chunk> chunksToLoad = new List<Chunk>();
        private static List<Chunk> chunksToUnload = new List<Chunk>();

        private static Block[] registeredBlocks = new Block[ushort.MaxValue];

        private static int last_x, last_z;
        internal static Camera curCamera;

        internal static List<Entity> entities = new List<Entity>();

        public static MatrixDictionary<CollisionGroup, CollisionGroup, bool> CollisionRule = new MatrixDictionary<CollisionGroup, CollisionGroup, bool>();

        internal static void Init()
        {
            InitCube();
            RegisterBlock(new Blocks.Block_Stone());
            RegisterBlock(new Blocks.Block_Dirt());

            curCamera = new Camera(new Vector3(8, 62, 8), new Vector3(0f, 0f, 0f), (float)(85.0 / 180.0 * Math.PI), 1920f / 1080f);
            last_x = (int)(Math.Floor(curCamera.transform.position.X / CHUNK_WIDTH) + 1);

            ResourceManager.atlas.generateAtlas();
        }
        private static void InitCube()
        {
            Vector3[] v = new Vector3[8] { new Vector3(0, 0, 1), new Vector3(0, 1, 1),
                                           new Vector3(1, 0, 1), new Vector3(1, 1, 1),
                                           new Vector3(1, 0, 0), new Vector3(1, 1, 0),
                                           new Vector3(0, 0, 0), new Vector3(0, 1, 0) };
            Vector2[] vt = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1),
                                            new Vector2(1, 0), new Vector2(1, 1) };
            Vector3 vn = new Vector3();
            int[][] faces = new int[6][] { new int[4] { 3, 4, 2, 1 },
                                           new int[4] { 7, 8, 6, 5 },
                                           new int[4] { 5, 6, 4, 3 },
                                           new int[4] { 1, 2, 8, 7 },
                                           new int[4] { 8, 2, 4, 6 },
                                           new int[4] { 1, 7, 5, 3 } };
            Cube.polygons = new Vertex[8][];
            for (int i = 0; i < 6; i++)
            {
                Cube.polygons[i] = new Vertex[4];
                for (int j = 0; j < 4; j++)
                    Cube.polygons[i][j] = new Vertex(v[faces[i][j] - 1], vt[faces[0][j] - 1], vn);
            }
        }
        public static Entity SpawnEntity(Entity entity)
        {
            entities.Add(entity);
            return entity;
        }
        public static Block GetBlockByID(ushort id)
        {
            return registeredBlocks[id];
        }
        public static bool RegisterBlock(Block block)
        {
            if (block.GetType() == typeof(Block))
                return false;
            if (registeredBlocks[block.id] != null)
                return false;
            registeredBlocks[block.id] = block;
            return true;
        }
        // TODO: implement non-loaded chunks
        public static Chunk GetChunk(int X, int Z)
        {
            foreach (Chunk chunk in chunks)
                if (chunk.X == X && chunk.Z == Z)
                    return chunk;
            return null;
        }
        // TODO: implement non-loaded chunks
        public static Chunk[,] GetChunks(int from_X, int from_Z, int to_X, int to_Z)
        {
            void swap(ref int value1, ref int value2)
            {
                int tmp;
                tmp = value1;
                value1 = value2;
                value2 = tmp;
            }
            if (to_X < from_X)
                swap(ref from_X, ref to_X);
            if (to_Z < from_Z)
                swap(ref from_Z, ref to_Z);
            Chunk[,] chunksBlock = new Chunk[to_X - from_X + 1, to_Z - from_Z + 1];
            foreach (Chunk chunk in chunks)
                if (chunk.X >= from_X && chunk.X <= to_X && chunk.Z >= from_Z && chunk.Z <= to_Z)
                    chunksBlock[chunk.X - from_X, chunk.Z - from_Z] = chunk;
            return chunksBlock;
        }
        // TODO: implement non-loaded chunks
        public static Chunk GetChunkByPosition(float x, float z)
        {
            return GetChunk((int)Math.Floor(x / CHUNK_WIDTH), (int)Math.Floor(z / CHUNK_LENGTH));
        }
        // TODO: implement non-loaded chunks
        public static Chunk[,] GetChunksByPosition(float from_x, float from_z, float to_x, float to_z)
        {
            return GetChunks((int)Math.Floor(from_x / CHUNK_WIDTH),
                             (int)Math.Floor(from_z / CHUNK_LENGTH),
                             (int)Math.Floor(to_x / CHUNK_WIDTH),
                             (int)Math.Floor(to_z / CHUNK_LENGTH));
        }
        // TODO: implement non-loaded chunks
        public static BlockID GetBlock(int x, int y, int z)
        {
            return GetChunkByPosition(x, z)[x, y, z];
        }
        // TODO: implement non-loaded chunks
        public static BlockID[,,] GetBlocks(int from_x, int from_y, int from_z, int to_x, int to_y, int to_z)
        {
            void swap(ref int value1, ref int value2)
            {
                int tmp;
                tmp = value1;
                value1 = value2;
                value2 = tmp;
            }
            if (to_x < from_x)
                swap(ref from_x, ref to_x);
            if (to_y < from_y)
                swap(ref from_y, ref to_y);
            if (to_z < from_z)
                swap(ref from_z, ref to_z);
            Chunk[,] chunksBlock = GetChunksByPosition(from_x, from_z, to_x, to_z);
            BlockID[,,] blocks = new BlockID[to_x - from_x + 1, to_y - from_y + 1, to_z - from_z + 1];

            for (int i = 0; i < chunksBlock.GetLength(0); i++)
                for (int j = 0; j < chunksBlock.GetLength(1); j++)
                    for (int x = Math.Max(from_x - chunksBlock[i, j].X * CHUNK_WIDTH, 0); x <= to_x && x < CHUNK_WIDTH; x++)
                        for (int y = from_y; y <= to_y; y++)
                            for (int z = Math.Max(from_z - chunksBlock[i, j].Z * CHUNK_LENGTH, 0); z <= to_z && z < CHUNK_LENGTH; z++)
                                blocks[i * CHUNK_WIDTH + x - from_x, y - from_y, j * CHUNK_LENGTH + z - from_z] = chunksBlock[i, j][x, y, z];

            return blocks;
        }
        internal static void Update()
        {
            if (last_x != Math.Floor(curCamera.transform.position.X / CHUNK_WIDTH) ||
                last_z != Math.Floor(curCamera.transform.position.Z / CHUNK_LENGTH))
            {
                last_x = (int)Math.Floor(curCamera.transform.position.X / CHUNK_WIDTH);
                last_z = (int)Math.Floor(curCamera.transform.position.Z / CHUNK_LENGTH);
                UpdateChunks();
            }
            for (int i = 0; i < MAX_CHUNKS_PER_FRAME && chunksToUnload.Count > 0; i++)
            {
                chunksToUnload[0].unload();
                chunksToUnload.RemoveAt(0);
            }
            for (int i = 0; i < MAX_CHUNKS_PER_FRAME && chunksToLoad.Count > 0; i++)
            {
                chunksToLoad[0].load();
                chunksToLoad.RemoveAt(0);
            }

            foreach (Chunk chunk in chunks)
            {
                if (chunk.getHasBeenModified())
                    chunk.updateVAO();
                chunk.update();
            }
        }
        internal static void FixedUpdate()
        {
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].rigidbody != null && !entities[i].rigidbody.isStatic)
                {
                    entities[i].rigidbody.velocity *= 1f - entities[i].rigidbody.drag * (float)Time.DeltaTime;
                    if (entities[i].rigidbody.useGravity)
                        entities[i].rigidbody.velocity.Y -= (float)Time.DeltaTime * 10f;
                    entities[i].transform.localPosition += entities[i].rigidbody.velocity * (float)Time.DeltaTime;
                }
                if (entities[i].collider != null)
                {
                    for (int j = i + 1; j < entities.Count; j++)
                        if (entities[j].collider != null)
                        {
                            SolveCollision(entities[i], entities[j]);
                        }
                    SolveWorldCollision(entities[i]);
                }
                entities[i].FixedUpdate();
            }
        }
        /// <summary>
        /// Old version, no bullet problem solving
        /// </summary>
        //private static void SolveWorldCollision(Entity entity)
        //{
        //    Vector3 from = entity.transform.position + entity.collider.offset;
        //    Vector3 to = from + entity.collider.size;
        //    Vector3 blocksFrom = new Vector3((float)Math.Floor(from.X) - 1, Math.Min(Math.Max((int)Math.Floor(from.Y) - 1, 0), CHUNK_HEIGHT - 1), (float)Math.Floor(from.Z) - 1);
        //    BlockID[,,] blocks = GetBlocks((int)blocksFrom.X, (int)blocksFrom.Y, (int)blocksFrom.Z,
        //                                   (int)Math.Floor(to.X) + 1, Math.Min(Math.Max((int)Math.Floor(to.Y) + 1, 0), CHUNK_HEIGHT - 1), (int)Math.Floor(to.Z) + 1);
        //    Vector3 outVec = new Vector3();
        //    int count = 0;
        //    for (int x = 0; x < blocks.GetLength(0); x++)
        //        for (int y = 0; y < blocks.GetLength(1); y++)
        //            for (int z = 0; z < blocks.GetLength(2); z++)
        //            {
        //                if (blocks[x, y, z].id == 0)
        //                    continue;
        //                Vector3 outPositive = new Vector3(blocksFrom.X + x + 1 - from.X, blocksFrom.Y + y + 1 - from.Y, blocksFrom.Z + z + 1 - from.Z);
        //                if (outPositive.X <= 0 || outPositive.Y <= 0 || outPositive.Z <= 0)
        //                    continue;
        //                if (outPositive.X >= outPositive.Y)
        //                {
        //                    outPositive.X = 0;
        //                    if (outPositive.Z >= outPositive.Y)
        //                        outPositive.Z = 0;
        //                    else
        //                        outPositive.Y = 0;
        //                }
        //                else
        //                {
        //                    outPositive.Y = 0;
        //                    if (outPositive.Z >= outPositive.X)
        //                        outPositive.Z = 0;
        //                    else
        //                        outPositive.X = 0;
        //                }
        //                Vector3 outNegative = new Vector3(blocksFrom.X + x - to.X, blocksFrom.Y + y - to.Y, blocksFrom.Z + z - to.Z);
        //                if (outNegative.X >= 0 || outNegative.Y >= 0 || outNegative.Z >= 0)
        //                    continue;
        //                if (outNegative.X <= outNegative.Y)
        //                {
        //                    outNegative.X = 0;
        //                    if (outNegative.Z <= outNegative.Y)
        //                        outNegative.Z = 0;
        //                    else
        //                        outNegative.Y = 0;
        //                }
        //                else
        //                {
        //                    outNegative.Y = 0;
        //                    if (outNegative.Z <= outNegative.X)
        //                        outNegative.Z = 0;
        //                    else
        //                        outNegative.X = 0;
        //                }
        //                if (outPositive.LengthSquared < outNegative.LengthSquared)
        //                    outVec += outPositive;
        //                else
        //                    outVec += outNegative;
        //                count++;
        //            }
        //    if (count > 0)
        //    {
        //        entity.transform.localPosition += outVec / count;
        //        if (entity.rigidbody != null)
        //        {
        //            if (outVec.X != 0)
        //                entity.rigidbody.velocity.X = 0;
        //            if (outVec.Y != 0)
        //                entity.rigidbody.velocity.Y = 0;
        //            if (outVec.Z != 0)
        //                entity.rigidbody.velocity.Z = 0;
        //        }
        //    }
        //}
        private static void SolveWorldCollision(Entity entity)
        {
            Vector3 from = entity.transform.position + entity.collider.offset;
            Vector3 to = from + entity.collider.size;
            Vector3 fromPrev = entity.transform.prevFramePosition + entity.collider.offset;
            Vector3 toPrev = fromPrev + entity.collider.size;
            Vector3 blocksFrom = new Vector3((float)Math.Floor(Math.Min(from.X, fromPrev.X)) - 1, Math.Min(Math.Max((int)Math.Floor(Math.Min(from.Y, fromPrev.Y)) - 1, 0), CHUNK_HEIGHT - 1), (float)Math.Floor(Math.Min(from.Z, fromPrev.Z)) - 1);
            BlockID[,,] blocks = GetBlocks((int)blocksFrom.X, (int)blocksFrom.Y, (int)blocksFrom.Z,
                                           (int)Math.Floor(Math.Max(to.X, toPrev.X)) + 1, Math.Min(Math.Max((int)Math.Floor(Math.Max(to.Y, toPrev.Y)) + 1, 0), CHUNK_HEIGHT - 1), (int)Math.Floor(Math.Max(to.Z, toPrev.Z)) + 1);
            Vector3 outVec = new Vector3();
            Vector3 counts = new Vector3();
            for (int x = 0; x < blocks.GetLength(0); x++)
                for (int y = 0; y < blocks.GetLength(1); y++)
                    for (int z = 0; z < blocks.GetLength(2); z++)
                    {
                        if (blocks[x, y, z].id == 0)
                            continue;
                        float frameCollisionMoment;
                        float earliestCollisionMoment = 2f;
                        Vector3 earliestCollisionVector;
                        if (from.X - fromPrev.X != 0)
                        {
                            frameCollisionMoment = (blocksFrom.X + x + 1 - fromPrev.X) / (from.X - fromPrev.X);
                            if (frameCollisionMoment >= 0 && frameCollisionMoment <= 1 && frameCollisionMoment < earliestCollisionMoment)
                            {
                                earliestCollisionMoment = frameCollisionMoment;
                                earliestCollisionVector = Vector3.UnitX;
                            }
                        }
                        if (to.X - toPrev.X != 0)
                        {
                            frameCollisionMoment = (blocksFrom.X + x - toPrev.X) / (to.X - toPrev.X);
                            if (frameCollisionMoment >= 0 && frameCollisionMoment <= 1 && frameCollisionMoment < earliestCollisionMoment)
                            {
                                earliestCollisionMoment = frameCollisionMoment;
                                earliestCollisionVector = -Vector3.UnitX;
                            }
                        }
                        if (from.Y - fromPrev.Y != 0)
                        {
                            frameCollisionMoment = (blocksFrom.Y + y + 1 - fromPrev.Y) / (from.Y - fromPrev.Y);
                            if (frameCollisionMoment >= 0 && frameCollisionMoment <= 1 && frameCollisionMoment < earliestCollisionMoment)
                            {
                                earliestCollisionMoment = frameCollisionMoment;
                                earliestCollisionVector = Vector3.UnitY;
                            }
                        }
                        if (to.Y - toPrev.Y != 0)
                        {
                            frameCollisionMoment = (blocksFrom.Y + y - toPrev.Y) / (to.Y - toPrev.Y);
                            if (frameCollisionMoment >= 0 && frameCollisionMoment <= 1 && frameCollisionMoment < earliestCollisionMoment)
                            {
                                earliestCollisionMoment = frameCollisionMoment;
                                earliestCollisionVector = -Vector3.UnitY;
                            }
                        }
                        if (from.Z - fromPrev.Z != 0)
                        {
                            frameCollisionMoment = (blocksFrom.Z + z + 1 - fromPrev.Z) / (from.Z - fromPrev.Z);
                            if (frameCollisionMoment >= 0 && frameCollisionMoment <= 1 && frameCollisionMoment < earliestCollisionMoment)
                            {
                                earliestCollisionMoment = frameCollisionMoment;
                                earliestCollisionVector = Vector3.UnitZ;
                            }
                        }
                        if (to.Z - toPrev.Z != 0)
                        {
                            frameCollisionMoment = (blocksFrom.Z + z - toPrev.Z) / (to.Z - toPrev.Z);
                            if (frameCollisionMoment >= 0 && frameCollisionMoment <= 1 && frameCollisionMoment < earliestCollisionMoment)
                            {
                                earliestCollisionMoment = frameCollisionMoment;
                                earliestCollisionVector = -Vector3.UnitZ;
                            }
                        }
                        if (earliestCollisionMoment < 0 || earliestCollisionMoment > 1)
                            continue;
                    }
        }
        private static void SolveCollision(Entity entity1, Entity entity2)
        {
            Vector3 from1, to1, from2, to2;
            from1 = entity1.transform.position + entity1.collider.offset;
            to1 = from1 + entity1.collider.size;
            from2 = entity2.transform.position + entity2.collider.offset;
            to2 = from2 + entity2.collider.size;
            if (to1.X < from2.X || to2.X < from1.X ||
                to1.Y < from2.Y || to2.Y < from1.Y ||
                to1.Z < from2.Z || to2.Z < from1.Z)
                return;
            Vector3 force = from1 + entity1.collider.size / 2f - from2 + entity2.collider.size;
            force.Y = 0;
            if (entity1.rigidbody != null && entity2.rigidbody != null)
            {
                entity1.rigidbody.applyForce(force * entity2.rigidbody.mass / (entity1.rigidbody.mass + entity2.rigidbody.mass));
                entity2.rigidbody.applyForce(-force * entity1.rigidbody.mass / (entity1.rigidbody.mass + entity2.rigidbody.mass));
            }
            else
                if (entity1.rigidbody != null)
                    entity1.rigidbody.applyForce(force);
                else
                    if (entity2.rigidbody != null)
                    entity2.rigidbody.applyForce(-force);
        }
        private static void UpdateChunks()
        {
            Chunk[,] chunksField = new Chunk[RenderDistance * 2 + 1, RenderDistance * 2 + 1];
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].X < last_x - RenderDistance || chunks[i].X > last_x + RenderDistance ||
                    chunks[i].Z < last_z - RenderDistance || chunks[i].Z > last_z + RenderDistance)
                {
                    if (!chunksToUnload.Contains(chunks[i]))
                        chunksToUnload.Add(chunks[i]);
                    chunks.RemoveAt(i);
                    i--;
                }
                else
                    chunksField[chunks[i].X - last_x + RenderDistance, chunks[i].Z - last_z + RenderDistance] = chunks[i];
            }
            for (int i = 0; i < RenderDistance * 2 + 1; i++)
                for (int j = 0; j < RenderDistance * 2 + 1; j++)
                    if (chunksField[i, j] == null)
                    {
                        Chunk newChunk = new Chunk(last_x + i - RenderDistance, last_z + j - RenderDistance);
                        chunks.Add(newChunk);
                        chunksToLoad.Add(newChunk);
                    }
                    else if (!chunksField[i, j].isLoaded)
                        if (!chunksToLoad.Contains(chunksField[i, j]))
                            chunksToLoad.Add(chunksField[i, j]);
        }
    }
}
