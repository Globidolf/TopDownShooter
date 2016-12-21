using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.Interface {
    public interface IUsable {
        uint charges { get; set; }

        Tooltip ActionInfo { get; }

        void Use(CharacterBase on);
    }
}
