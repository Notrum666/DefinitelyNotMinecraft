using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefinitelyNotMinecraft.Blocks
{
    public class Block_Dirt : Block
    {
        public Block_Dirt()
        {
            id = 2;
            textureLocations.Add(ResourceManager.LoadTextureToAtlas("Textures\\Blocks\\dirt.png"));
        }
    }
}
