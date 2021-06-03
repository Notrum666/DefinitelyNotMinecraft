using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DefinitelyNotMinecraft.Entities
{
    class Entity_Arrow : Entity
    {
        public Entity_Arrow()
        {
            mesh = new Mesh();
            mesh.model = ResourceManager.LoadModel("Models\\arrow.obj");
            mesh.texture = ResourceManager.LoadTexture("Textures\\Entities\\arrow.png");

            collider = new Collider();
            collider.offset.X = -0.25f;
            collider.offset.Y = -0.25f;
            collider.offset.Z = -0.25f;
            collider.size.X = 0.5f;
            collider.size.Y = 0.5f;
            collider.size.Z = 0.5f;

            rigidbody = new Rigidbody();
            rigidbody.mass = 1;
            rigidbody.isStatic = false;
            rigidbody.useGravity = true;
        }
    }
}
