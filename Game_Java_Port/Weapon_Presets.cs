using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {

    //When adding Weapons, keep in mind to add them to all three locations:
    // 1. The Preset Enumeration
    // 2. The Static Get-Only Property
    // 3. The _BaseWeapon Class Indexer
    //You can add multiple names for the weapon, but they need to have the same value.
    public enum WeapPreset {
        Boomerang,
        Throwing_Knives, Knives = Throwing_Knives,
        Rock,
        SubMachineGun, SMG = SubMachineGun,
        Sniper_Rifle, Sniper = Sniper_Rifle,
        RocketLauncher,
        Mace,
        Spear,
        Greatsword,
        Sword,
        Electro_Sphere, Electro = Electro_Sphere,
        Assault_Rifle, Assault = Assault_Rifle, AR = Assault,
        Acid, Acid_Sprayer = Acid,
        Shotgun,
        Revolver,
        Pistol
    }
    public partial class Weapon {

        #region Throwing Weapons

        private static Weapon Boomerang {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Boomerang";

                temp.Behaviour = BulletBehaviour.Returning;
                temp.WType = WeaponType.Throwable;
                temp._Slot = Interface.EquipSlot.Weapon1H;

                temp.Precision = 0.7f;
                temp.Damage = 7;
                temp.Range = 1200;
                temp.AttackSpeed = 1;

                temp.BulletsPerShot = 1;
                temp.BulletHitCount = 1;
                temp.ReloadSpeed = 0;
                temp.ClipSize = 12;
                temp.BulletSpeed = 350;

                return temp;
            }
        }

        private static Weapon ThrowingKnives {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Throwing Knives";

                temp.Behaviour = BulletBehaviour.Piercing;
                temp.WType = WeaponType.Throwable;
                temp._Slot = Interface.EquipSlot.Weapon1H;

                temp.Precision = 0.95f;
                temp.Damage = 3;
                temp.Range = 400;
                temp.AttackSpeed = 3;

                temp.BulletsPerShot = 3;
                temp.BulletHitCount = 4;
                temp.ReloadSpeed = 0;
                temp.ClipSize = 12;
                temp.BulletSpeed = 350;

                return temp;
            }
        }

        private static Weapon Rock {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Rock";

                temp.Behaviour = BulletBehaviour.Bounce;
                temp.WType = WeaponType.Throwable;
                temp._Slot = Interface.EquipSlot.Weapon1H;

                temp.Precision = 0.95f;
                temp.Damage = 5;
                temp.Range = 500;
                temp.AttackSpeed = 2;

                temp.BulletsPerShot = 3;
                temp.BulletHitCount = 4;
                temp.ReloadSpeed = 0;
                temp.ClipSize = 12;
                temp.BulletSpeed = 350;

                return temp;
            }
        }

        #endregion

        private static Weapon SubMachineGun {
            get {
                Weapon temp = new Weapon();
                temp.Name = "SMG";

                temp.Behaviour = BulletBehaviour.Normal;
                temp.WType = WeaponType.SubMachineGun;
                temp._Slot = Interface.EquipSlot.Weapon1H;

                temp.Precision = 0.8f;
                temp.Damage = 1;
                temp.Range = 350;
                temp.AttackSpeed = 12;

                temp.BulletsPerShot = 1;
                temp.BulletHitCount = 1;
                temp.ReloadSpeed = 800;
                temp.ClipSize = 60;
                temp.BulletSpeed = 900;

                return temp;
            }
        }

        private static Weapon SniperRifle {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Sniper Rifle";

                temp.Behaviour = BulletBehaviour.Piercing;
                temp.WType = WeaponType.SniperRifle;
                temp._Slot = Interface.EquipSlot.Weapon2H;

                temp.Precision = 1;
                temp.Damage = 10f;
                temp.Range = 900;
                temp.AttackSpeed = 0.2f;

                temp.BulletsPerShot = 1;
                temp.BulletHitCount = 6;
                temp.ReloadSpeed = 3500;
                temp.ClipSize = 5;
                temp.BulletSpeed = 7500;

                return temp;
            }
        }

        private static Weapon Rocketlauncher {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Rocket Launcher";

                temp.Behaviour = BulletBehaviour.Explosive | BulletBehaviour.Knockback;
                temp.WType = WeaponType.RocketLauncher;
                temp._Slot = Interface.EquipSlot.Weapon2H;

                temp.Precision = 0.96f;
                temp.Damage = 9f;
                temp.Range = 600;
                temp.AttackSpeed = 0.8f;

                temp.BulletsPerShot = 1;
                temp.BulletHitCount = 1;
                temp.ReloadSpeed = 3500;
                temp.ClipSize = 1;
                temp.BulletSpeed = 600;

                return temp;
            }
        }

        #region Melee Weapons

        private static Weapon Mace {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Mace";

                temp.Behaviour = BulletBehaviour.Knockback;
                temp.WType = WeaponType.Melee;
                temp._Slot = Interface.EquipSlot.Weapon2H;

                temp.Precision = 0.75f;
                temp.Damage = 9;
                temp.Range = temp.MeleeRangeExtension = 130;
                temp.AttackSpeed = 0.5f;

                temp.BulletHitCount = 1;

                return temp;
            }
        }

        private static Weapon Spear {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Spear";

                temp.Behaviour = BulletBehaviour.Knockback;
                temp.WType = WeaponType.Melee;
                temp._Slot = Interface.EquipSlot.Weapon2H;

                temp.Precision = 0.94f;
                temp.Damage = 7;
                temp.Range = temp.MeleeRangeExtension = 250;
                temp.AttackSpeed = 0.4f;

                temp.BulletHitCount = 7;

                return temp;
            }
        }

        private static Weapon Greatsword {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Greatsword";

                temp.Behaviour = BulletBehaviour.Knockback;
                temp.WType = WeaponType.Melee;
                temp._Slot = Interface.EquipSlot.Weapon2H;

                temp.Precision = 0.6f;
                temp.Damage = 6;
                temp.Range = temp.MeleeRangeExtension = 170;
                temp.AttackSpeed = 0.3f;

                temp.BulletHitCount = 7;

                return temp;
            }
        }

        private static Weapon Sword {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Sword";

                temp.Behaviour = BulletBehaviour.Knockback;
                temp.WType = WeaponType.Melee;
                temp._Slot = Interface.EquipSlot.Weapon1H;

                temp.Precision = 0.6f;
                temp.Damage = 4;
                temp.Range = temp.MeleeRangeExtension = 100;
                temp.AttackSpeed = 0.7f;

                temp.BulletHitCount = 3;

                return temp;
            }
        }

        #endregion

        private static Weapon ElectroSphere {
            get {
                Weapon temp = new Weapon();
                temp.Name = "ElectroSphere";

                temp.Behaviour = BulletBehaviour.Beam | BulletBehaviour.Tracking | BulletBehaviour.Piercing;
                temp.WType = WeaponType.Electricity;
                temp._Slot = Interface.EquipSlot.Weapon2H;

                temp.Precision = 0;
                temp.Damage = 1;
                temp.Range = 200;
                temp.AttackSpeed = 1;

                temp.BulletsPerShot = 3;
                temp.BulletHitCount = uint.MaxValue;
                temp.ReloadSpeed = 2000;
                temp.ClipSize = 100;
                temp.BulletSpeed = 0;

                return temp;
            }
        }

        private static Weapon AssaultRifle {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Assault Rifle";

                temp.Behaviour = BulletBehaviour.Knockback;
                temp.WType = WeaponType.AssaultRifle;
                temp._Slot = Interface.EquipSlot.Weapon1H;

                temp.Precision = 0.9f;
                temp.Damage = 2;
                temp.Range = 400;
                temp.AttackSpeed = 10;

                temp.BulletsPerShot = 1;
                temp.BulletHitCount = 2;
                temp.ReloadSpeed = 2000;
                temp.ClipSize = 30;
                temp.BulletSpeed = 1000;

                return temp;
            }
        }

        private static Weapon Acidsprayer {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Acid Sprayer";

                temp.Behaviour = BulletBehaviour.Piercing | BulletBehaviour.MultiHit;
                temp.WType = WeaponType.Acid;
                temp._Slot = Interface.EquipSlot.Weapon2H;

                temp.Precision = 0;
                temp.Damage = 0.05f;
                temp.Range = 200;
                temp.AttackSpeed = 25;

                temp.BulletsPerShot = 2;
                temp.BulletHitCount = uint.MaxValue;
                temp.ReloadSpeed = 4000;
                temp.ClipSize = 1000;
                temp.BulletSpeed = 25;

                return temp;
            }
        }

        private static Weapon Shotgun {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Shotgun";

                temp.Behaviour = BulletBehaviour.Knockback;
                temp.WType = WeaponType.Shotgun;
                temp._Slot = Interface.EquipSlot.Weapon2H;

                temp.Precision = 0.7f;
                temp.Damage = 1f;
                temp.Range = 180;
                temp.AttackSpeed = 0.75f;

                temp.BulletsPerShot = 6;
                temp.BulletHitCount = 1;
                temp.ReloadSpeed = 1500;
                temp.ClipSize = 4;
                temp.BulletSpeed = 800;

                return temp;
            }
        }

        private static Weapon Revolver {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Revolver";

                temp.Behaviour = BulletBehaviour.Normal;
                temp.WType = WeaponType.Revolver;
                temp._Slot = Interface.EquipSlot.Weapon1H;

                temp.Precision = 0.86f;
                temp.Damage = 7.5f;
                temp.Range = 250;
                temp.AttackSpeed = 12; //approx what humans manage to click/s over longer durations. prevent total op by autoclicker

                temp.BulletsPerShot = 1;
                temp.BulletHitCount = 2;
                temp.ReloadSpeed = 2500;
                temp.ClipSize = 6;
                temp.BulletSpeed = 1000;

                return temp;
            }
        }

        private static Weapon Pistol {
            get {
                Weapon temp = new Weapon();
                temp.Name = "Pistol";

                temp.Behaviour = BulletBehaviour.Normal;
                temp.WType = WeaponType.Pistol;
                temp._Slot = Interface.EquipSlot.Weapon1H;

                temp.Precision = 0.9f;
                temp.Damage = 3;
                temp.Range = 200;
                temp.AttackSpeed = 1;

                temp.BulletsPerShot = 1;
                temp.BulletHitCount = 1;
                temp.ReloadSpeed = 2000;
                temp.ClipSize = 12;
                temp.BulletSpeed = 750;

                return temp;
            }
        }

        public static readonly _BaseWeapon BaseWeapons = new _BaseWeapon();

        /// <summary>
        /// Indexer class. do not use. instead use 'BaseWeapon'.
        /// </summary>
        public class _BaseWeapon {
            public static int Count { get { return Enum.GetValues(typeof(WeapPreset)).Cast<WeapPreset>().Distinct().Count(); } }
            
            public Weapon Random(Random RNG) {
                return BaseWeapons[RNG.Next(Count)];
            }

            public Weapon this[int index] {
                get {
                    return this[(WeapPreset)index];
                }
            }

            public Weapon this[WeapPreset index] {
                get {
                    switch(index) {
                        case WeapPreset.Throwing_Knives: return ThrowingKnives;
                        case WeapPreset.Sword: return Sword;
                        case WeapPreset.SubMachineGun: return SubMachineGun;
                        case WeapPreset.Spear: return Spear;
                        case WeapPreset.Sniper_Rifle: return SniperRifle;
                        case WeapPreset.Shotgun: return Shotgun;
                        case WeapPreset.RocketLauncher: return Rocketlauncher;
                        case WeapPreset.Rock: return Rock;
                        case WeapPreset.Revolver: return Revolver;
                        case WeapPreset.Pistol: return Pistol;
                        case WeapPreset.Mace: return Mace;
                        case WeapPreset.Greatsword: return Greatsword;
                        case WeapPreset.Electro_Sphere: return ElectroSphere;
                        case WeapPreset.Boomerang: return Boomerang;
                        case WeapPreset.Assault_Rifle: return AssaultRifle;
                        case WeapPreset.Acid_Sprayer: return Acidsprayer;
                        default: return null;
                    }
                }
            }
        }
    }
}
