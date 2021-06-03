using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace DefinitelyNotMinecraft
{
    class Program
    {
        static void Main(string[] args)
        {
            MainWindow window = new MainWindow(); // load

            Logger.Init(); // 1st
            ResourceManager.Init(); // 2nd
            Core.Init(); // 3rd
            Input.Init(); // 4th

            window.Run(); // last
        }
    }
}
