using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game_Java_Port.Interface;
using SharpDX.Direct2D1;
using SharpDX;
using Game_Java_Port.Logics;

namespace Game_Java_Port {
    public sealed partial class Weapon : ItemBase, IEquipable, ISerializable<Weapon> {


        public float MeleeRangeExtension { get; private set; }
        public float Range { get; private set; }
        public float AttackSpeed { get; private set; }
        public float Precision { get; set; }
        public float Damage { get; private set; }
        public float BulletSpeed { get; private set; }

        public uint Level { get; private set; } = 1;
        public uint AmmoPerShot { get; private set; }
        public uint BulletsPerShot { get; private set; }
        public uint ClipSize { get; private set; }
        public uint ReloadSpeed { get; private set; }
        public uint BulletHitCount { get; private set; }
        public uint Ammo { get; private set; }

        private float _CoolDown;
        private float _Reload;

        public WeaponType WType { get; private set; }
        public BulletBehaviour Behaviour { get; private set; }

        public int Seed { get; private set; }

        private ItemType _Rarity = ItemType.Common;

        public override ItemType Rarity { get { return _Rarity; } }

        private uint calcPrice() {
            float price = 50 * (float)Math.Pow(Level, 1.1f);

            switch(_Rarity) {
                case ItemType.Garbage:
                    price *= 0.2f;
                    break;
                case ItemType.Common:
                default:
                    break;
                case ItemType.Uncommon:
                    price *= 1.4f;
                    break;
                case ItemType.Rare:
                    price *= 2.2f;
                    break;
                case ItemType.Epic:
                    price *= 4.2f;
                    break;
                case ItemType.Legendary:
                    price *= 10f;
                    break;
                case ItemType.Unique:
                    price *= 50f;
                    break;
                case ItemType.DevItem:
                    price *= 300f;
                    break;
            }
            return (uint)price;
        }

        public override uint BasePrice {
            get {
                return calcPrice();
            }
        }

        private EquipSlot _Slot = EquipSlot.Weapon2H;
        public EquipSlot Slot { get { return _Slot; } }

        private Weapon() { RenderData = new RenderData { AnimationFrameCount = new Point(2,2), mdl = Model.Square }; }
        
        public Weapon(uint level, int? seed = null, WeapPreset? wt = null, ItemType? rarity = null, bool add = true) {
            Level = level;

            if(seed == null) {
                if(Game.instance._client == null)
                    Seed = GameStatus.RNG.Next();
                else if(Game.instance.GameHost != null)
                    Seed = Game.instance.GameHost._HostGenRNG.Next();
                else
                    throw new InvalidOperationException("Cannot create undefined weapon as a client. You propably tried to cheat, you bastard");
            } else
                Seed = (int)seed;

            //Game.instance.addMessage(Seed.ToString());

            _RNG = new Random(Seed);

            GenType = 0;

            if(wt != null) {
                GenType = 1;
                if(rarity != null)
                    GenType = 3;
            } else if(rarity != null)
                GenType = 2;

            // determines the weapon type and scales the values to match the type. also takes in the levels for slight in-and decreases of the values.
            #region base calculation

            Weapon Base = wt == null ? BaseWeapons.Random(_RNG) : BaseWeapons[(WeapPreset)wt];

            Name = Base.Name;
			/*
            drawType = Base.drawType;
            image = Base.image;
			*/
			RenderData = new RenderData
			{
				mdl = Model.Square,
				ResID = Base.RenderData.ResID
			};
            WType = Base.WType;
            
            float __weakLevelReduction = (float)Math.Pow(0.99f, level - 1);

            float __strongLevelBonus = (float)Math.Pow(level,0.9f);
            float __weakLevelBonus = (float)Math.Pow(level, 1d/3d);

            float DamageMult = 1;

            float ApsMult = _RNG.NextFloat(0.75f, 1.25f);

            float PrecMult = 1;

            if (Base.Precision > 0)
                PrecMult = _RNG.NextFloat(0.75f,1.25f);

            float BlSpdMult = _RNG.NextFloat(0.75f, 1.25f);
            float RangeMult = _RNG.NextFloat(0.75f, 1.25f);
            DamageMult *= 1 / ApsMult;
            DamageMult *= 1 / PrecMult;
            DamageMult *= 1 / BlSpdMult;
            DamageMult *= 1 / RangeMult;

            float ReloadMult = _RNG.NextFloat(0.75f, 1.25f);
            float ClipMult = 1 / ReloadMult;

            float BulletCountMult = 1;

            while (_RNG.Next(5) == 0) {
                BulletCountMult += 1;
            }

            DamageMult /= BulletCountMult * 0.9f;
            
            AttackSpeed = Base.AttackSpeed * ApsMult;

            Precision = Base.Precision * PrecMult;

            Damage = Base.Damage * DamageMult * __strongLevelBonus;

            BulletSpeed = Base.BulletSpeed * BlSpdMult;

            Range = Base.Range * RangeMult * __weakLevelBonus;

            if(WType == WeaponType.Melee)
                MeleeRangeExtension = Range;
            else
                MeleeRangeExtension = 0;

            BulletsPerShot = (uint)(Base.BulletsPerShot * BulletCountMult);

            AmmoPerShot = (uint)BulletCountMult;

            Behaviour = Base.Behaviour;

            BulletHitCount = Base.BulletHitCount;

            ReloadSpeed = (uint)(Base.ReloadSpeed * ReloadMult * __weakLevelReduction);

            ClipSize = (uint)(Base.ClipSize * ClipMult * __weakLevelBonus);
            

            #endregion

            // determines the weapon rarity gives additional properties to it.
            #region mod calculation

            if(rarity == null) {


                Dictionary<ItemType, float> Chances = new Dictionary<ItemType, float>();
                float chancesum = 0;
                Chances.Add(ItemType.Garbage, chancesum);
                chancesum += 800;
                Chances.Add(ItemType.Common, chancesum);
                chancesum += 1200;
                Chances.Add(ItemType.Uncommon, chancesum);
                chancesum += 600;
                Chances.Add(ItemType.Rare, chancesum);
                chancesum += 150;
                Chances.Add(ItemType.Epic, chancesum);
                chancesum += 50;
                Chances.Add(ItemType.Legendary, chancesum);
                chancesum += 10;
                Chances.Add(ItemType.Unique, chancesum);
                chancesum += 1;
                Chances.Add(ItemType.DevItem, chancesum);
                chancesum += 0.01f;

                float _rarity = (float)_RNG.NextDouble() * chancesum;
                _Rarity = Chances.Last((pair) => pair.Value < _rarity).Key;
            } else
                _Rarity = (ItemType)rarity;

            uint mods = 0;

            switch(_Rarity) {
                // no special mod, just make all stats worse! :D
                case ItemType.Garbage:
                    float __strongMalus = 0.8f + (float)_RNG.NextDouble() * 0.1f;
                    float __strongMalusPlus = 1.1f + (float)_RNG.NextDouble() * 0.1f;
                    AttackSpeed *= __strongMalus;
                    if(ClipSize > 1)
                        ClipSize -= Math.Max(1, (uint)(ClipSize * 0.8));

                    if(ClipSize > AmmoPerShot + 1)
                        AmmoPerShot++;

                    Range *= __strongMalus;
                    Precision *= __strongMalus;
                    ReloadSpeed = (uint)(ReloadSpeed * __strongMalusPlus);

                    if((Behaviour & BulletBehaviour.MultiHit) == BulletBehaviour.MultiHit)
                        BulletSpeed *= __strongMalusPlus;
                    else
                        BulletSpeed *= __strongMalus;

                    break;

                    // add modifiers depending on rarity
                case ItemType.Uncommon:
                    mods = 1;
                    break;
                case ItemType.Rare:
                    mods = 3;
                    break;
                case ItemType.Epic:
                    mods = 5;
                    break;
                case ItemType.Legendary:
                    mods = 8;
                    break;
                case ItemType.Unique:
                    mods = 10;
                    break;
                case ItemType.DevItem:
                    mods = 20;
                    break;
                case ItemType.Common:
                default:
                break;
            }

            List<int> addedmods = new List<int>();


            // add the actual effects
            while (mods > 0) {
                switch(_RNG.Next(17)) {
                    // add tracking effect
                    case 0:
                        // bullet will track the closest enemy not hit yet

                        if(WType == WeaponType.Melee)
                            mods++;
                        else if(Behaviour.isStackable(BulletBehaviour.Tracking)) {
                            Behaviour |= BulletBehaviour.Tracking;
                            Range *= 1.1f;
                            addedmods.Add(0);
                        } else
                            mods++;
                        break;
                    // add beam effect
                    case 1:
                        //bullet will no longer be a bullet, but will fire a straight line towards the aim position (instant hit)

                        if(WType == WeaponType.Melee)
                            mods++;
                        else if(Behaviour.isStackable(BulletBehaviour.Beam)) {
                            Behaviour |= BulletBehaviour.Beam;
                            addedmods.Add(1);
                        } else
                            mods++;
                        break;
                    case 2:
                        //bullet will pierce enemies (damaging only once

                        if(WType == WeaponType.Melee)
                            mods++;
                        else if(Behaviour.isStackable(BulletBehaviour.Piercing)) {
                            Behaviour |= BulletBehaviour.Piercing;
                            if(BulletHitCount < uint.MaxValue / 1.5f)
                                BulletHitCount += (uint)Math.Max(1, BulletHitCount * 1.5f);
                            addedmods.Add(2);
                        } else
                            mods++;
                        break;
                    case 3:
                        //bullet will track the user, making a boomerang effect

                        if(WType == WeaponType.Melee)
                            mods++;
                        //low chance as this is mostly useless (1 of 10 compared to other mods)
                        else if(Behaviour.isStackable(BulletBehaviour.Returning) && _RNG.Next(10) == 0) {
                            Behaviour |= BulletBehaviour.Returning;
                            Range *= 4;
                            addedmods.Add(3);
                        } else
                            mods++;
                        break;
                    case 4:
                        //bullet explodes upon impact

                        if(WType == WeaponType.Melee)
                            mods++;
                        else if(Behaviour.isStackable(BulletBehaviour.Explosive)) {
                            Behaviour |= BulletBehaviour.Explosive;
                            BulletHitCount += (uint)Math.Max(100, BulletHitCount * 0.8f);
                            addedmods.Add(4);
                        } else
                            mods++;
                        break;
                    case 5:
                        //bullet bounces off enemies and walls
                        if(WType == WeaponType.Melee)
                            mods++;
                        else if(Behaviour.isStackable(BulletBehaviour.Bounce)) {
                            Behaviour |= BulletBehaviour.Bounce;
                            BulletHitCount += (uint)Math.Max(1, BulletHitCount * 1.5f);
                            addedmods.Add(5);
                        } else
                            mods++;
                        break;
                    case 6:
                        //multihit is a modifier of piercing, when it is active, one bullet can hit the same target multiple times

                        if(WType == WeaponType.Melee)
                            mods++;
                        else if(Behaviour.isStackable(BulletBehaviour.MultiHit)) {
                            Behaviour |= BulletBehaviour.MultiHit;
                            addedmods.Add(6);
                        } else
                            mods++;
                        break;
                    case 7:
                        // add a quarter of the current projectiles to the weapon (min 1)
                        if(WType == WeaponType.Melee)
                            mods++;
                        else {
                            BulletsPerShot += Math.Max(1, BulletsPerShot / 4);
                            addedmods.Add(7);
                        }
                        break;
                    case 8:
                        // add a quarter of the current potential hits to the weapon (min 1)
                        if(WType == WeaponType.Melee || (Behaviour & (BulletBehaviour.Bounce | BulletBehaviour.Explosive | BulletBehaviour.Piercing)) != BulletBehaviour.Normal) {
                            BulletHitCount += Math.Max(1, BulletHitCount / 4);
                            addedmods.Add(7);
                        } else
                            mods++;
                        break;
                    case 9:
                        // increase reload speed
                        if(WType == WeaponType.Melee)
                            mods++;
                        else {
                            ReloadSpeed = (uint)(ReloadSpeed * 0.8f);
                            addedmods.Add(7);
                        }
                        break;
                    case 10:
                        // increase clip size
                        if(WType == WeaponType.Melee)
                            mods++;
                        else {
                            ClipSize = (uint)Math.Max((ClipSize * 1.25f),ClipSize + 1);
                            addedmods.Add(7);
                        }
                        break;
                    case 11:
                        // increase precision / melee attack-area
                        if(WType == WeaponType.Electricity)
                            mods++;
                        else if(WType == WeaponType.Melee) {
                            Precision *= 0.8f;
                            addedmods.Add(7);
                        } else {
                            Precision = 1 - (1 - Precision) * 0.8f;
                            addedmods.Add(7);
                        }
                        break;
                    case 12:
                        // Increase Weapon Range
                        Range *= 1.2f;
                        if(WType == WeaponType.Melee)
                            MeleeRangeExtension = Range;
                        addedmods.Add(7);
                        break;
                    case 13:
                        // increase / decrease bullet speed
                        if(WType == WeaponType.Melee || (Behaviour & BulletBehaviour.Beam) != BulletBehaviour.Normal)
                            mods++;
                        else if(WType == WeaponType.Acid) {
                            BulletSpeed *= 0.9f;
                            addedmods.Add(7);
                        } else {
                            BulletSpeed *= 1.1f;
                            addedmods.Add(7);
                        }
                        break;
                    case 14:
                        // decrease ammo consumption
                        if(AmmoPerShot > 1) {
                            AmmoPerShot--;
                            addedmods.Add(7);
                        } else
                            mods++;
                        break;
                    case 15:
                        // increase damage
                        Damage *= 1.1f;
                        addedmods.Add(7);
                        break;
                    case 16:
                        // increase attackspeed
                        AttackSpeed = AttackSpeed * 1.1f;
                        addedmods.Add(7);
                        break;
                }
                mods--;
            }

            int submods = addedmods.Count((val) => val == 7);

            string suffix = "";
            string affix = "";

            if(addedmods.Contains(1)) {
                // seeks target and unloads all damage onto it.
                if(addedmods.Contains(0) && addedmods.Contains(2) && addedmods.Contains(6))
                    suffix = "Static";
                // unloads all damage if hit
                else if(addedmods.Contains(2) && addedmods.Contains(6))
                    suffix = "Lightning";
                // seeks targets and switches on hit
                else if(addedmods.Contains(0) && addedmods.Contains(2))
                    suffix = "Chaining";
                // pierces
                else if(addedmods.Contains(2))
                    suffix = "Rail-";
                // seeks its target
                else if(addedmods.Contains(0))
                    suffix = "Electric";
                else
                    suffix = "Laser-";
            } else {
                // seeks target and continously damages it.
                if(addedmods.Contains(0) && addedmods.Contains(2) && addedmods.Contains(6))
                    suffix = "Haunting";
                // continously damages on hit
                else if(addedmods.Contains(2) && addedmods.Contains(6))
                    suffix = "Ripping";
                // seeks targets and switches on hit
                else if(addedmods.Contains(0) && addedmods.Contains(2))
                    suffix = "Travelling";
                // pierces
                else if(addedmods.Contains(2))
                    suffix = "Piercing";
                // seeks its target
                else if(addedmods.Contains(0))
                    suffix = "Seeking";
            }

            if(addedmods.Contains(4)) {
                if(suffix == "")
                    suffix = "Explosive";
                else
                    affix = "destruction";
            }

            if(affix != "" && (addedmods.Contains(3) || addedmods.Contains(5)))
                affix += " and ";

            if(addedmods.Contains(3) && addedmods.Contains(5))
                affix += "chaos";
            else {
                if(addedmods.Contains(3))
                    affix += "loyality";
                if(addedmods.Contains(5))
                    affix += "deflection";
            }



            Name = (suffix == "" ? "" : (suffix.EndsWith("-") ? suffix : suffix + " ")) + Name + (affix == "" ? "" : " of " + affix) + (submods > 0 ? " +" + submods : "");

            #endregion

            Ammo = ClipSize;

            // todo: unique mods

            init();

            if(add)
                AddToGame();
        }

        public void Reload() {
            if(Ammo < ClipSize && _Reload <= 0) {
                _Reload += ReloadSpeed / 1000f;
                if(Owner.AmmoStorage[WType] >= ClipSize - Ammo) {
                    Owner.AmmoStorage[WType] -= ClipSize - Ammo;
                    Ammo = ClipSize;
                } else {
                    Ammo += Owner.AmmoStorage[WType];
                    Owner.AmmoStorage[WType] = 0;
                }
            }
        }

        public void Fire() {
            if(Ammo > ClipSize)
                Ammo = 0;
            if(_CoolDown <= 0 && _Reload <= 0) {
                if(WType == WeaponType.Melee) {
                    //melee attack
                    List<CharacterBase> targets = new List<CharacterBase>();

                    lock(GameStatus.GameSubjects)
                        targets.AddRange(GameStatus.GameSubjects.OrderBy((targ) => Owner.AimDirection.difference(Owner.Location.angleTo(targ.Location), true)));

                    targets.Remove(Owner);

                    targets.RemoveAll((targ) =>
                    {
                        if(Vector2.Distance(targ.Location, Owner.Location) - targ.Size / 2 > Range)
                            return true;
                        if(Owner.PrecisionR != 0 && Owner.Location.angleTo(targ.Location).isInBetween(
                                new AngleSingle(Owner.AimDirection.Revolutions + (1 - Owner.PrecisionR)/2, AngleType.Revolution),
                                new AngleSingle(Owner.AimDirection.Revolutions - (1 - Owner.PrecisionR)/2, AngleType.Revolution)))
                            return true;
                        return false;
                    });
                    uint i = BulletHitCount;
                    foreach(CharacterBase targ in targets) {
                        lock(targ) {
                            if(i <= 0)
                                break;
                            else
                                i--;
                                Owner.Attack(targ, Owner.MeleeDamageR);
                                if(Behaviour.HasFlag(BulletBehaviour.Knockback)) {

                                    targ.MovementVector += Owner.Location.angleTo(targ.Location).toVector() * Owner.MeleeDamageR;
                                    //targ.MovementVector.X += -(float)Math.Cos(Owner.Location.angleTo(targ.Location).Radians) * Math.Min(80000 / GameVars.TicksPerSecond, ((Owner.MeleeDamageR / targ.MaxHealth)) * 10000 / GameVars.TicksPerSecond);
                                    //targ.MovementVector.Y += -(float)Math.Sin(Owner.Location.angleTo(targ.Location).Radians) * Math.Min(80000 / GameVars.TicksPerSecond, ((Owner.MeleeDamageR / targ.MaxHealth)) * 10000 / GameVars.TicksPerSecond);
                                }
                        }
                    }

                } else if(Ammo > 0) {
                    Ammo -= AmmoPerShot;
                    List<CharacterBase> consumed = new List<CharacterBase>();
                    for(int i = 0; i < BulletsPerShot; i++) {
                        new Bullet(this, _RNG.Next(), consumed);
                    }
                }

                if(Ammo == 0 && WType == WeaponType.Throwable) {
                    Drop();
                    lock(this)
                        Dispose();
                }

                _CoolDown += 1 / AttackSpeed;
            }
        }
        
        public override void Tick() {
            base.Tick();
            if (_CoolDown > 0)
                _CoolDown -= GameStatus.TimeMultiplier;
            if(_Reload > 0)
                _Reload -= GameStatus.TimeMultiplier;
        }

        public override Tooltip ItemInfo {
            get {
                if (itemInfo == null) {
                    itemInfo = new WeaponTooltip(this,
                    Validation: () => ItemBaseInfoValidation() && Owner == null,
                    ticksInternal: true);
                }
                return itemInfo;
            }
        }

        Serializer<Weapon> ISerializable<Weapon>.Serializer {
            get { return Serializers.WeaponSerializer.Instance; }
        }

        public override void draw(DeviceContext rt) {
            base.draw(rt);
			/*
            if (!Game.state.HasFlag(Game.GameState.Menu) && Owner == Game.instance._player && _Reload > 0) {
                int width = 150;
                RectangleF pos = new RectangleF(-width/2,0,width,20);
                RectangleF sub = new RectangleF(-width/2,0,width* (ReloadSpeed - _Reload * 1000) / ReloadSpeed, 20);
				/*
                pos.Location += Game.instance.Location;
                sub.Location += Game.instance.Location;
                weaponPen.Color = Color.SmoothStep(Color.Yellow, Color.Black, 0.5f);
                rt.FillRectangle(pos, GameStatus.BGBrush);
                weaponPen.Color = Color.Yellow;
                rt.FillRectangle(sub, weaponPen);
                weaponPen.Color = Color.Black;
                rt.DrawRectangle(pos, weaponPen);
                pos.Location += 2;
                //SpriteFont.DEFAULT.directDrawText("Reloading...", pos, rt);
                //rt.DrawText("Reloading...", GameStatus.MenuFont, pos, weaponPen);
            }
		*/
        }

        public void Equip(CharacterBase on) {

            if(on.EquippedWeaponL != this && on.EquippedWeaponR != this) {

                if(Slot == EquipSlot.Weapon2H)
                    on.EquippedWeaponL = on.EquippedWeaponR = this;
                else if(on.EquippedWeaponR == null || on.EquippedWeaponL != null)
                    on.EquippedWeaponR = this;
                else
                    on.EquippedWeaponL = this;
            }
        }


    } // end class
} // end namespace
