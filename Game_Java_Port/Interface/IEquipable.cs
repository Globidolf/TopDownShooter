using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.Interface {
    public enum EquipSlot {
        Weapon1H,
        Weapon2H,
        Power,
        Helmet,
        Chest,
        Legs,
        Arm,
        Feet,
        Finger,
        Hand
    }
    public interface IEquipable {
        EquipSlot Slot { get; }
        void Equip(AttributeBase on);
    }
}
