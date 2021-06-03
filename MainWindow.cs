using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System.Threading;

namespace DefinitelyNotMinecraft
{
    public class MainWindow : GameWindow
    {
        private Shader shader;
        private Stopwatch timer;
        private double physicsUpdateAccumulator;
        public MainWindow()
        {
            Width = 1280;
            Height = 720;
            WindowState = WindowState.Maximized;
            CursorVisible = false;
            CursorGrabbed = true;
            Title = "Definitely not Minecraft";
            VSync = VSyncMode.Off;
        }
        protected override void OnLoad(EventArgs e)
        {
            physicsUpdateAccumulator = 0;

            shader = ResourceManager.GetShader(ResourceManager.LoadShaders("Shaders\\default.vert", "Shaders\\default.frag"));

            Closed += OnClosed;

            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            //GL.Enable(EnableCap.Texture2D);
            //GL.Enable(EnableCap.Blend);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            timer = new Stopwatch();
            timer.Start();
        }
        private void OnClosed(object sender, EventArgs e)
        {
            Exit();
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            Time.DeltaTime = e.Time;
            Time.TotalTime += e.Time;

            physicsUpdateAccumulator += e.Time;

            Input.OnUpdateFrame();
            float speed = 2f * (float)Time.DeltaTime;
            if (Input.IsKeyDown(Key.LShift))
                speed *= 15f;
            if (Input.IsKeyPressed(Key.Escape))
                Exit();
            if (Input.IsKeyDown(Key.W))
                Core.curCamera.transform.localPosition -= Core.curCamera.transform.forward * speed;
            if (Input.IsKeyDown(Key.S))
                Core.curCamera.transform.localPosition += Core.curCamera.transform.forward * speed;
            if (Input.IsKeyDown(Key.D))
                Core.curCamera.transform.localPosition -= Core.curCamera.transform.right * speed;
            if (Input.IsKeyDown(Key.A))
                Core.curCamera.transform.localPosition += Core.curCamera.transform.right * speed;
            if (Input.IsKeyDown(Key.Space))
                Core.curCamera.transform.localPosition.Y += speed;
            if (Input.IsKeyDown(Key.ControlLeft))
                Core.curCamera.transform.localPosition.Y -= speed;
            if (Input.IsKeyPressed(Key.E))
            {
                Entity arrow = new Entities.Entity_Arrow();
                arrow.transform.localPosition = Core.curCamera.transform.position;
                Core.SpawnEntity(arrow);
            }
            Vector2 mouseDelta = Input.GetMouseDelta() / 1000f;
            Core.curCamera.transform.localRotation = Quaternion.FromAxisAngle(Vector3.UnitY, -mouseDelta.X) * Quaternion.FromAxisAngle(-Core.curCamera.transform.right, -mouseDelta.Y) * Core.curCamera.transform.rotation;

            Time.DeltaTime = Time.FixedDeltaTime;
            while (physicsUpdateAccumulator >= Time.FixedDeltaTime)
            {
                Core.FixedUpdate();
                physicsUpdateAccumulator -= Time.FixedDeltaTime;
            }
            Time.DeltaTime = e.Time;
            Core.Update();
        }
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.ClearColor(new Color4(0.2f, 0.2f, 0.2f, 0.2f));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shader.id);

            Matrix4 proj = Core.curCamera.proj;
            Matrix4 view = Core.curCamera.view;
            Matrix4 model;
            GL.UniformMatrix4(shader.locations["proj"], false, ref proj);
            GL.UniformMatrix4(shader.locations["view"], false, ref view);

            GL.BindTexture(TextureTarget.Texture2D, ResourceManager.atlas.id);

            GL.Enable(EnableCap.CullFace);

            foreach (Chunk chunk in Core.chunks)
            {
                if (chunk.vertexCount > 0)
                {
                    model = Matrix4.CreateTranslation(chunk.X * Core.CHUNK_WIDTH, 0, chunk.Z * Core.CHUNK_LENGTH);
                    GL.UniformMatrix4(shader.locations["model"], false, ref model);
                    GL.BindVertexArray(chunk.vao);
                    GL.DrawArrays(PrimitiveType.Quads, 0, chunk.vertexCount);
                }
            }

            GL.Disable(EnableCap.CullFace);

            foreach (Entity entity in Core.entities)
            {
                if (entity.mesh != null && !entity.mesh.hide)
                {
                    model = entity.transform.model;
                    GL.UniformMatrix4(shader.locations["model"], false, ref model);
                    GL.BindVertexArray(entity.mesh.model.VAO);
                    GL.BindTexture(TextureTarget.Texture2D, entity.mesh.texture.id);
                    GL.DrawArrays(PrimitiveType.Quads, 0, entity.mesh.model.vertexCount);
                }
            }

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            SwapBuffers();
        }
        public override void Exit()
        {
            ResourceManager.OnExit();
            base.Exit();
        }
        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
        }
    }
}
