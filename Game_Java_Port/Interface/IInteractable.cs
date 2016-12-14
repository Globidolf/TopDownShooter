using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.Interface {
    public interface IInteractable {
        Vector2 Location { get; set; }
        string ActionDescription { get; }
        void interact(NPC who);
    }
}
