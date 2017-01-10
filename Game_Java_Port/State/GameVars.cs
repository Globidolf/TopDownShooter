using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Game_Java_Port {

    class GameVars {
        public static Random ClientRNG = new Random();
        public const float defaultFPS = 59;
        public const float defaultGTPS = 60;

        public const float WeaponInfoMargin = 5;

        public const bool debugMode = true;

        public const uint scrollOffset = 50;
        public const int messageLifeTime = 3000;

        public const float pickupRange = 200;
        public const float pickupMouseRange = 50;

        public static Dictionary<Controls, int> ControlMapping { get; set; } = new Dictionary<Controls, int>() {
                {Controls.move_left,        (int)Keys.A },
                {Controls.move_up,          (int)Keys.W },
                {Controls.move_right,       (int)Keys.D },
                {Controls.move_down,        (int)Keys.S },
                {Controls.run,              (int)Keys.ShiftKey },
                {Controls.walk,             (int)Keys.ControlKey },
                {Controls.interact,         (int)MouseButtons.Right },
                {Controls.open_pausemenu,   (int)Keys.Escape },
                {Controls.open_inventory,   (int)Keys.E },
                {Controls.fire,             (int)MouseButtons.Left },
                {Controls.reload,          (int)Keys.R }
            };


        public static Dictionary<ItemType, Color> RarityColors { get; } = new Dictionary<ItemType, Color>() {
            {ItemType.Set,          CustomMaths.fromArgb(0xff,0x4a,0xe4,0x4a) },
            {ItemType.Quest,        CustomMaths.fromArgb(0xff,0x4a,0x4a,0xe4) },
            {ItemType.Special,      CustomMaths.fromArgb(0xff,0x4a,0xe4,0xe4) },
            {ItemType.Garbage,      CustomMaths.fromArgb(0xff,0xaa,0xaa,0xaa) },
            {ItemType.Common,       CustomMaths.fromArgb(0xff,0xee,0xee,0xee) },
            {ItemType.Uncommon,     CustomMaths.fromArgb(0xff,0xaa,0xee,0xaa) },
            {ItemType.Rare,         CustomMaths.fromArgb(0xff,0xaa,0xaa,0xee) },
            {ItemType.Epic,         CustomMaths.fromArgb(0xff,0xee,0xaa,0xee) },
            {ItemType.Legendary,    CustomMaths.fromArgb(0xff,0xee,0xdd,0x55) },
            {ItemType.Pearlescent,  CustomMaths.fromArgb(0xff,0x55,0xee,0xdd) },
            {ItemType.DevItem,      CustomMaths.fromArgb(0xff,0x00,0x22,0x33) },
            {ItemType.Gold,         CustomMaths.fromArgb(0xff,0xdd,0xdd,0x22) }
        };

        public static Color Random {
            get {
                return Color.FromRgba(ClientRNG.Next());
            }
        }
    }

    public enum Rank {
        Furniture,
        Trash,
        Common,
        Elite,
        Rare,
        Leader,
        Miniboss,
        Boss,
        God,
        Player
    }

    public enum Attribute {
        Vitality,
        Strength,
        Dexterity,
        Agility,
        Intelligence,
        Wisdom,
        Luck
    }

    public enum Effect {
        Damage_Threshold,
        Damage_Absorption,
        Damage_Reflection,
        Spike_Damage
    }

    [Flags]
    public enum BulletBehaviour : byte {
        Normal =    0x00,
        Tracking =  0x01,
        Piercing =  0x01 << 1,
        Beam =      0x01 << 2,
        MultiHit =  0x01 << 3,
        Explosive = 0x10,
        Knockback = 0x10 << 1,
        Bounce =    0x10 << 2,
        Returning = 0x10 << 3
    }

    public enum WeaponType {
        Melee,
        Throwable,
        Pistol,
        Revolver,
        Shotgun,
        SubMachineGun,
        AssaultRifle,
        SniperRifle,
        RocketLauncher,
        Electricity,
        Acid,
    }

    [Flags]
    public enum ItemType : byte {
        Set =           0x01,
        Quest =         0x01 << 1,
        Special =       0x01 << 2,
        Reserved =      0x01 << 3,
        Garbage =       0x00,
        Common =        0x10,
        Uncommon =      0x20, // 0x10 << 1
        Rare =          0x30,
        Epic =          0x40, // 0x10 << 2
        Legendary =     0x50,
        Pearlescent =   0x60,
        DevItem =       0x70,
        Gold =          0x10 << 3
    }

    [Flags]
    public enum Controls : short {
        none =          0x0000,
        move_up =       0x0001,
        move_down =     0x0001 << 1,
        move_left =     0x0001 << 2,
        move_right =    0x0001 << 3,
        fire =          0x0010,
        reload =       0x0010 << 1,
        run =           0x0010 << 2,
        walk =          0x0010 << 3,
        interact =      0x0100,
        open_pausemenu= 0x0100 << 1,
        open_inventory= 0x0100 << 2,
    }
}
