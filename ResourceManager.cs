using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using System.Windows.Forms;

namespace DefinitelyNotMinecraft
{
    public struct Vertex
    {
        public Vector3 v;
        public Vector2 vt;
        public Vector3 vn;
        public Vertex(Vector3 v, Vector2 vt, Vector3 vn)
        {
            this.v = v;
            this.vt = vt;
            this.vn = vn;
        }
    }
    public class Model
    {
        public Vertex[][] polygons { get; private set; }
        public int vertexCount { get; private set; }
        internal int VAO;
        public Model(Vertex[][] polygons)
        {
            this.polygons = polygons;

            List<float> data = new List<float>();
            foreach (Vertex[] poly in polygons)
                foreach (Vertex v in poly)
                    data.AddRange(new float[] { v.v.X, v.v.Y, v.v.Z, v.vt.X, v.vt.Y, v.vn.X, v.vn.Y, v.vn.Z });

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);
            int VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * 4, 0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * 4, 3 * 4);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 8 * 4, 5 * 4);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * 4, data.ToArray(), BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            vertexCount = data.Count / 8;
        }
    }
    public class Shader
    {
        public int id;
        public Dictionary<string, int> locations;
        public Shader(int id)
        {
            this.id = id;
            locations = new Dictionary<string, int>();
            int uniformsCount;
            GL.GetProgram(id, GetProgramParameterName.ActiveUniforms, out uniformsCount);
            for (int i = 0; i < uniformsCount; i++)
            {
                string uniformName = GL.GetActiveUniform(id, i, out _, out _);
                locations[uniformName] = GL.GetUniformLocation(id, uniformName);
            }
        }
    }
    public class TextureLocation
    {
        public float pos_x, pos_y, size_x, size_y;
        public TextureLocation()
        {
            pos_x = 0f;
            pos_y = 0f;
            size_x = 1f;
            size_y = 1f;
        }
        public TextureLocation(float pos_x, float pos_y, float size_x, float size_y)
        {
            this.pos_x = pos_x;
            this.pos_y = pos_y;
            this.size_x = size_x;
            this.size_y = size_y;
        }
    }
    public class Texture
    {
        public int id;
        public Texture(int id)
        {
            this.id = id;
        }
    }
    internal class TextureAtlas
    {
        internal int id;
        internal int width = 0, height = 0;
        public List<TextureLocation> locations = new List<TextureLocation>();
        public List<Bitmap> textures = new List<Bitmap>();
        private static readonly int PIXELS_BETWEEN_TEXTURES = 2;
        private static readonly int MAX_WIDTH = 512;
        private struct TextureLocationInPixels
        {
            public int pos_x, pos_y, size_x, size_y;
            public TextureLocationInPixels(int pos_x, int pos_y, int size_x, int size_y)
            {
                this.pos_x = pos_x;
                this.pos_y = pos_y;
                this.size_x = size_x;
                this.size_y = size_y;
            }
        }
        public TextureAtlas()
        {
            id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMinFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new int[] { (int)TextureWrapMode.ClampToBorder });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new int[] { (int)TextureWrapMode.ClampToBorder });
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        public TextureLocation addTexture(Bitmap texture)
        {
            textures.Add(texture);
            TextureLocation location = new TextureLocation();
            locations.Add(location);
            return location;
        }
        internal void generateAtlas()
        {
            if (textures.Count == 0)
                return;
            height = textures[0].Height;
            width = textures[0].Width;
            List<TextureLocationInPixels> pixelLocations = new List<TextureLocationInPixels>();
            for (int i = 0; i < textures.Count; i++)
            {
                TextureLocationInPixels possibleLocation = new TextureLocationInPixels(0, 0, textures[i].Width, textures[i].Height);
                bool findLocation = false;
                while (!findLocation)
                {
                    bool overlaps = false;
                    foreach (TextureLocationInPixels location in pixelLocations)
                        if (!(possibleLocation.pos_x + possibleLocation.size_x + PIXELS_BETWEEN_TEXTURES <= location.pos_x || // to the left
                             possibleLocation.pos_y + possibleLocation.size_y + PIXELS_BETWEEN_TEXTURES <= location.pos_y || // above
                             possibleLocation.pos_x >= location.pos_x + location.size_x + PIXELS_BETWEEN_TEXTURES || // to the right
                             possibleLocation.pos_y >= location.pos_y + location.size_y + PIXELS_BETWEEN_TEXTURES)) // under
                        {
                            possibleLocation.pos_x = location.pos_x + location.size_x + PIXELS_BETWEEN_TEXTURES;
                            overlaps = true;
                            break;
                        }
                    if (overlaps)
                    {
                        if (possibleLocation.pos_x + possibleLocation.size_x > MAX_WIDTH)
                        {
                            possibleLocation.pos_x = 0;
                            possibleLocation.pos_y += 1;
                        }
                        if (possibleLocation.pos_y + possibleLocation.size_y > height)
                            height = possibleLocation.pos_y + possibleLocation.size_y;
                    }
                    else
                        findLocation = true;
                }
                if (possibleLocation.pos_x + possibleLocation.size_x > width)
                    width = possibleLocation.pos_x + possibleLocation.size_x;
                pixelLocations.Add(possibleLocation);
            }
            byte[] data = new byte[height * width * 4];
            for (int k = 0; k < locations.Count; k++)
            {
                for (int i = 0; i < pixelLocations[k].size_y; i++)
                    for (int j = 0; j < pixelLocations[k].size_x; j++)
                    {
                        Color pixel = textures[k].GetPixel(j, i);
                        data[((pixelLocations[k].pos_y + i) * width + pixelLocations[k].pos_x + j) * 4] = pixel.R;
                        data[((pixelLocations[k].pos_y + i) * width + pixelLocations[k].pos_x + j) * 4 + 1] = pixel.G;
                        data[((pixelLocations[k].pos_y + i) * width + pixelLocations[k].pos_x + j) * 4 + 2] = pixel.B;
                        data[((pixelLocations[k].pos_y + i) * width + pixelLocations[k].pos_x + j) * 4 + 3] = pixel.A;
                    }
                locations[k].pos_x = pixelLocations[k].pos_x / (float)width;
                locations[k].pos_y = pixelLocations[k].pos_y / (float)height;
                locations[k].size_x = pixelLocations[k].size_x / (float)width;
                locations[k].size_y = pixelLocations[k].size_y / (float)height;
            }
            GL.BindTexture(TextureTarget.Texture2D, id);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
    public static class ResourceManager
    {
        private static string resourcesPath = "Resources\\";
        private static List<Shader> shaders = new List<Shader>();
        private static Dictionary<string, Model> models = new Dictionary<string, Model>();
        private static List<Texture> textures = new List<Texture>();
        internal static TextureAtlas atlas = new TextureAtlas();
        public static int LoadShaders(string vertexShaderPath, string fragmentShaderPath)
        {
            vertexShaderPath = resourcesPath + vertexShaderPath;
            fragmentShaderPath = resourcesPath + fragmentShaderPath;
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(vertexShaderPath)) || !File.Exists(vertexShaderPath))
                    throw new FileNotFoundException("Vertex shader file not found", vertexShaderPath);
                if (!Directory.Exists(Path.GetDirectoryName(fragmentShaderPath)) || !File.Exists(fragmentShaderPath))
                    throw new FileNotFoundException("Fragment shader file not found", fragmentShaderPath);
                var vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertexShader, File.ReadAllText(vertexShaderPath));
                GL.CompileShader(vertexShader);
                int result;
                GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out result);
                if (result == 0)
                    throw new Exception("Vertex shader compilation error: " + GL.GetShaderInfoLog(vertexShader));
                else
                    Logger.Log("Vertex shader compiled.", LogType.Debug);

                var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, File.ReadAllText(fragmentShaderPath));
                GL.CompileShader(fragmentShader);
                GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out result);
                if (result == 0)
                    throw new Exception("Fragment shader compilation error: " + GL.GetShaderInfoLog(fragmentShader));
                else
                    Logger.Log("Fragment shader compiled.", LogType.Debug);

                var program = GL.CreateProgram();
                GL.AttachShader(program, vertexShader);
                GL.AttachShader(program, fragmentShader);
                GL.LinkProgram(program);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out result);
                if (result == 0)
                    throw new Exception("Program linking error: " + GL.GetProgramInfoLog(program));
                else
                    Logger.Log("Program linked.", LogType.Debug);

                GL.DetachShader(program, vertexShader);
                GL.DetachShader(program, fragmentShader);
                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);

                Shader shader = new Shader(program);
                shaders.Add(shader);
                return shaders.Count - 1;
            }
            catch (Exception e)
            {
                Logger.Log(e);
                MessageBox.Show("Error occurred during shaders loading, see logs for detailed information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return -1;
            }
        }
        public static Model LoadModel(string path, bool clockwise = false)
        {
            try
            {
                List<Vector3> coords = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<Vector3> normals = new List<Vector3>();

                List<Vertex[]> polygons = new List<Vertex[]>();

                string[] text = File.ReadAllLines(resourcesPath + path);
                foreach (string line in text)
                {
                    string[] words = line.Trim().Replace('.', ',').Split(' ');
                    if (words.Length > 0)
                    {
                        switch (words[0])
                        {
                            case "v":
                                coords.Add(new Vector3(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3])));
                                break;
                            case "vt":
                                uvs.Add(new Vector2(float.Parse(words[1]), float.Parse(words[2])));
                                break;
                            case "vn":
                                normals.Add(new Vector3(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3])));
                                break;
                            case "f":
                                Vertex[] polygon = new Vertex[words.Length - 1];
                                for (int i = 1; i < words.Length; i++)
                                {
                                    string[] values = words[i].Split('/');
                                    polygon[i - 1].v = coords[int.Parse(values[0]) - 1];
                                    polygon[i - 1].vt = values.Length > 1 ? uvs[int.Parse(values[1]) - 1] : new Vector2(0f, 0f);
                                    polygon[i - 1].vn = values.Length > 2 ? normals[int.Parse(values[2]) - 1] : new Vector3(0f, 0f, 0f);
                                }
                                if (clockwise)
                                    polygon = polygon.Reverse().ToArray();
                                polygons.Add(polygon);
                                break;
                            default:
                                break;
                        }
                    }
                }

                Model model = new Model(polygons.ToArray());

                return models[Path.GetFileNameWithoutExtension(path)] = model;
            }
            catch (Exception e)
            {
                Logger.Log(e);
                MessageBox.Show("Error occurred during model loading, see logs for detailed information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return null;
            }
        }
        public static Texture LoadTexture(string path, bool flipVertically = true)
        {
            try
            {
                path = resourcesPath + path;
                if (!File.Exists(path))
                    throw new FileNotFoundException("Texture file not found", path);
                Bitmap bmp = new Bitmap(path);
                if (flipVertically)
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                byte[] data = new byte[bmp.Height * bmp.Width * 4];
                for (int i = 0; i < bmp.Height; i++)
                    for (int j = 0; j < bmp.Width; j++)
                    {
                        Color curPixel = bmp.GetPixel(j, i);
                        data[(i * bmp.Width + j) * 4] = curPixel.R;
                        data[(i * bmp.Width + j) * 4 + 1] = curPixel.G;
                        data[(i * bmp.Width + j) * 4 + 2] = curPixel.B;
                        data[(i * bmp.Width + j) * 4 + 3] = curPixel.A;
                    }

                int id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new int[] { (int)TextureMinFilter.Nearest });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new int[] { (int)TextureMagFilter.Nearest });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new int[] { (int)TextureWrapMode.ClampToBorder });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new int[] { (int)TextureWrapMode.ClampToBorder });
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, 0);

                Texture texture = new Texture(id);
                textures.Add(texture);
                return texture;
            }
            catch (Exception e)
            {
                Logger.Log(e);
                MessageBox.Show("Error occurred during image loading, see logs for detailed information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return null;
            }
        }
        public static TextureLocation LoadTextureToAtlas(string path, bool flipVertically = true)
        {
            try
            {
                path = resourcesPath + path;
                if (!File.Exists(path))
                    throw new FileNotFoundException("Texture file not found", path);
                Bitmap bmp = new Bitmap(path);
                if (flipVertically)
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                return atlas.addTexture(bmp);
            }
            catch (Exception e)
            {
                Logger.Log(e);
                MessageBox.Show("Error occurred during image loading, see logs for detailed information.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
                return null;
            }
        }
        internal static void Init()
        {

        }
        public static Texture GetTexture(int index)
        {
            if (textures.Count > index)
                return textures[index];
            else
                return null;
        }
        public static Model GetModel(string name)
        {
            if (models.ContainsKey(name))
                return models[name];
            else
                return null;
        }
        public static Shader GetShader(int index)
        {
            if (index < shaders.Count && index >= 0)
                return shaders[index];
            else
                return null;
        }
        internal static void OnExit()
        {
            foreach (Shader shader in shaders)
                GL.DeleteProgram(shader.id);
            foreach (Texture texture in textures)
                GL.DeleteTexture(texture.id);
            if (atlas != null && atlas.id != 0)
                GL.DeleteTexture(atlas.id);
        }
    }
}
