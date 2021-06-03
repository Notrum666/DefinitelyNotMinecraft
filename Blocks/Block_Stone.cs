using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefinitelyNotMinecraft.Blocks
{
    public class Block_Stone : Block
    {
        public Block_Stone()
        {
            id = 1;
            textureLocations.Add(ResourceManager.LoadTextureToAtlas("Textures\\Blocks\\stone.png"));
        }
    }
}
