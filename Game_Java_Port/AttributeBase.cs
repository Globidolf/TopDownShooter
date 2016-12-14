using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    public abstract class AttributeBase : IRenderable, ITickable {

        #region base values

        public const float BaseHealth = 60;
        public const float BaseMovementSpeed = 600;
        public const float BasePrecision = 1f;
        public const float BaseMeleeDamage = 0.1f;
        public const float BaseMeleeRange = 30;
        public const float BaseMeleeSpeed = 0.5f;
        public const float BaseAcceleration = 45f;
        public const float BaseExpExponent = 1.05f;

        public const uint BaseAttributeValue = 5;
        public const uint BaseAttributePoints = 20;
        public const uint BaseAttributePointsPerLevel = 2;

        public const uint BaseExpToLevel = 100;
        public const uint BaseExpIncPerLevel = 10;

        #endregion

        public enum DrawType {
            None,
            Circle,
            Rectangle,
            Polygon,
            Image
        }


        private float _CoolDown = 0;

        public Faction Team = FactionNames.Environment;

        public abstract DrawType drawType { get; }
        public SolidColorBrush Pencil { get; set; } = new SolidColorBrush(Program._RenderTarget, Color.FromRgba(0xFF00FFFF));
        public bool IsMoving { get; set; }
        public virtual RectangleF Area { get; set; }
        public Vector2 Location {
            get { return Area.Center; }
            set {
                RectangleF rect = Area;
                rect.Location = value - new Vector2(Area.Width / 2, Area.Height / 2);
                Area = rect;
            }
        }
        private bool _dead { get; set; } = false;
        public Bitmap Image { get; set; }

        public List<Vector2> Polygon { get; } = new List<Vector2>();

        private Vector2[] laser;

        public Vector2 MovementVector = new Vector2();
        public Vector2 DirectionVector = new Vector2();
        public AngleSingle AimDirection { get; set; }

        public Vector2 Target = new Vector2();

        private float _complexSize = 0;
        private object comparsionobject = new RectangleF();

        public float Size {
            get {
                switch(drawType) {
                    case DrawType.Circle:
                    case DrawType.None:
                        return Area.Width;
                    case DrawType.Rectangle:
                    case DrawType.Image:
                        if(!(comparsionobject is RectangleF) || (RectangleF)comparsionobject != Area) {
                            _complexSize = Vector2.Distance(Area.TopLeft, Area.BottomRight);
                            comparsionobject = Area;
                        }
                        return _complexSize;
                    case DrawType.Polygon:
                        if(!(comparsionobject is List<Vector2>) || (List<Vector2>)comparsionobject != Polygon) {
                            float longestdist = 0;
                            foreach(Vector2 p1 in Polygon)
                                foreach(Vector2 p2 in Polygon) {
                                    if((p1 - p2).LengthSquared() > longestdist)
                                        longestdist = (p1 - p2).LengthSquared();
                                }
                            _complexSize = (float)Math.Sqrt(longestdist);
                            comparsionobject = Polygon;
                        }
                        return _complexSize;
                    default:
                        return 0;
                }
            }
            set {
                RectangleF rect = Area;
                switch(drawType) {
                    case DrawType.Circle:
                    case DrawType.None:
                        rect = Area;

                        rect.Width = rect.Height = value;
                        Area = rect;
                        break;
                    case DrawType.Rectangle:
                    case DrawType.Image:

                        _complexSize = value;

                        rect = Area;

                        rect.Width = rect.Height = (float)Math.Sqrt(Math.Pow(value, 2) / 2);

                        comparsionobject = Area = rect;
                        break;
                    case DrawType.Polygon:

                        float scale = value / _complexSize;

                        _complexSize = value;

                        for(int i = 0; i < Polygon.Count; i++)
                            Polygon[i] *= scale;

                        break;
                }
            }
        }

        public string Name { get; set; } = NameGen.RandomName;

        uint AttackCoolDown { get; set; }

        public Dictionary<WeaponType, uint> AmmoStorage { get; } = generateAmmoStorage();

        public Dictionary<EquipSlot, IEquipable> Equipment { get; } = new Dictionary<EquipSlot, IEquipable>();

        public IEquipable getEquipedItem(EquipSlot slot, bool left = false) {
            if(slot == EquipSlot.Weapon1H || slot == EquipSlot.Weapon2H)
                return left ? EquippedWeaponL : EquippedWeaponR;
            if(Equipment.ContainsKey(slot))
                return Equipment[slot];
            return null;
        }

        private Weapon _EquippedWeaponR;
        private Weapon _EquippedWeaponL;

        public Weapon EquippedWeaponR {
            get { return _EquippedWeaponR; }
            set {
                if(_EquippedWeaponR != null && _EquippedWeaponR.Slot == EquipSlot.Weapon2H)
                    _EquippedWeaponL = null;
                if(value == null) {
                    _EquippedWeaponR = null;
                } else if(Inventory.Contains(value)) {
                    _EquippedWeaponR = value;
                    if(value.Slot == EquipSlot.Weapon2H)
                        _EquippedWeaponL = value;
                    else if(value == _EquippedWeaponL)
                        _EquippedWeaponL = null;
                }
            }
        }
        public Weapon EquippedWeaponL {
            get { return _EquippedWeaponL; }
            set {
                if(_EquippedWeaponL != null && _EquippedWeaponL.Slot == EquipSlot.Weapon2H)
                    _EquippedWeaponR = null;
                if(value == null) {
                    _EquippedWeaponL = null;
                } else if(Inventory.Contains(value)) {
                    _EquippedWeaponL = value;
                    if(value.Slot == EquipSlot.Weapon2H)
                        _EquippedWeaponR = value;
                    else if(value == _EquippedWeaponR)
                        _EquippedWeaponR = null;
                }
            }
        }

        public abstract Rank Rank { get; }

        private ulong _ID;

        public ulong ID {
            get { return _ID; }
            set {
                if(GameStatus.GameSubjects.Any((subj) => subj.ID == value))
                    throw new ArgumentOutOfRangeException("value", value, "Object with requested ID already Registered!");
                _ID = value;
            }
        }

        protected float PowerMod {
            get {
                switch(Rank) {
                    case Rank.Furniture:
                        return 0.01f;
                    case Rank.Trash:
                        return 0.05f;
                    case Rank.Common:
                        return 0.1f;
                    case Rank.Elite:
                        return 0.75f;
                    case Rank.Rare:
                        return 1.5f;
                    case Rank.Leader:
                        return 2f;
                    case Rank.Miniboss:
                        return 3f;
                    case Rank.Boss:
                        return 4.5f;
                    case Rank.God:
                        return 10f;
                    case Rank.Player:
                        return 1f;
                    default:
                        return 0f;
                }
            }
        }

        public float Health { get; set; }
        public float MaxHealth { get { return HealthMult * BaseHealth * PowerMod; } }
        public float Precision { get { return Math.Min(1f, PrecisionMult * BasePrecision * (EquippedWeaponR == null ? 1 : EquippedWeaponR.Precision)); } }
        public float MeleeDamage { get { return MeleeMult * (BaseMeleeDamage + (EquippedWeaponR == null ? 0 : EquippedWeaponR.Damage)); } }
        public float MeleeSpeed { get { return MeleeSpeedMult * BaseMeleeSpeed * (EquippedWeaponR == null ? 1 : EquippedWeaponR.AttackSpeed); } }
        public float RMeleeRange { get { return BaseMeleeRange * PowerMod + (EquippedWeaponR == null ? 0 : EquippedWeaponR.MeleeRangeExtension); } }
        public float LMeleeRange { get { return BaseMeleeRange * PowerMod + (EquippedWeaponL == null ? 0 : EquippedWeaponL.MeleeRangeExtension); } }
        public float RWeaponRange { get { return EquippedWeaponR == null ? 0 : EquippedWeaponR.Range; } }
        public float LWeaponRange { get { return EquippedWeaponL == null ? 0 : EquippedWeaponL.Range; } }
        public float Acceleration { get { return SpeedMult * BaseAcceleration; } }
        public float MaxMovementSpeed { get { return SpeedMult * BaseMovementSpeed * Math.Max(0.5f, Math.Min(2, PowerMod)); } }
        public List<ItemBase> Inventory { get; } = new List<ItemBase>();

        private uint _Exp;
        public uint Exp {
            get {
                return _Exp;
            }
            set {
                uint temp = value;
                while(temp >= ExpToLvlUp) {
                    temp -= ExpToLvlUp;
                    Level++;
                }
                _Exp = temp;
            }
        }

        public uint ExpToLvlUp {
            get {
                return (uint)((BaseExpToLevel + BaseExpIncPerLevel * (Level - 1)) * Math.Pow(BaseExpExponent, (Level - 1)));
            }
        }

        public uint ExpLeft { get { return ExpToLvlUp - Exp; } }

        private uint _Level;
        public uint Level {
            get {
                return _Level;
            }
            set {
                uint difference = value - _Level;
                _Level = value;
                AttributePoints += difference * BaseAttributePointsPerLevel;
            }
        }

        private uint _AttributePoints;

        public uint AttributePoints {
            get { return _AttributePoints; }
            set {
                if(_AttributePoints != 0 && value == 0)
                    Health = MaxHealth;
                _AttributePoints = value;
            }
        }

        public uint Vit { get { return Attributes[Attribute.Vitality]; } }
        public uint Str { get { return Attributes[Attribute.Strength]; } }
        public uint Dex { get { return Attributes[Attribute.Dexterity]; } }
        public uint Agi { get { return Attributes[Attribute.Agility]; } }
        public uint Int { get { return Attributes[Attribute.Intelligence]; } }
        public uint Wis { get { return Attributes[Attribute.Wisdom]; } }
        public uint Luc { get { return Attributes[Attribute.Luck]; } }

        #region statmultipliers

        private uint _statsum = 0;

        private uint statsum {
            get {
                return Vit + Str +
                       Dex + Agi +
                       Int + Wis +
                       Luc;
            }
        }

        public static float getHPMult(uint Vit, uint Str, uint Dex, uint Agi, uint Int, uint Wis, uint Luc) {
            return (Math.Min(Vit, 20) * 5 + ((Vit > 20) ? (Vit - 20) * 2 : 0) +
                                Math.Min(Str, 10) * 3 + ((Str > 10) ? (Str - 10) * 1 : 0) +
                                Math.Min(Dex, 20) * 1 +
                                Math.Min(Agi, 20) * 1 +
                                Math.Min(Int, 20) * 1 +
                                Math.Min(Wis, 20) * 1 +
                                Math.Min(Luc, 20) * 1) * 0.03f;
        }

        public static float getSPDMult(uint Str, uint Dex, uint Agi) {
            return 0.5f + 0.003f * (
                                Str * 1 +
                                Math.Min(Dex, 20) * 4 +
                                    ((Dex > 20) ? (Dex - 20) * 1 : 0) +
                                Math.Min(Agi, 20) * 8 +
                                    ((Agi > 20) ? (Agi - 20) * 2 : 0));
        }

        public static float getMDMGMult(uint Str, uint Dex) {
            return 0.75f + 0.002f * (
                                Math.Min(Str, 20) * 6 +
                                    ((Str > 20) ? (Str - 20) * 3 : 0) +
                                Math.Min(Dex, 20) * 4 +
                                    ((Dex > 20) ? (Dex - 20) * 2 : 0));
        }

        public static float getPRCMult(uint Dex, uint Agi) {
            return 0.8f + 0.002f * (
                                Math.Min(Dex, 20) * 6 +
                                    ((Dex > 20) ? (Dex - 20) * 2 : 0) +
                                Math.Min(Agi, 20) * 4 +
                                    ((Agi > 20) ? (Agi - 20) * 1 : 0));
        }

        public static float getRDMGMult(uint Dex, uint Agi) {
            return 0.75f + 0.002f * (
                                Math.Min(Dex, 20) * 6 +
                                    ((Dex > 20) ? (Dex - 20) * 3 : 0) +
                                Math.Min(Agi, 20) * 4 +
                                    ((Agi > 20) ? (Agi - 20) * 2 : 0));
        }

        public static float getMSPDMult(uint Str, uint Dex, uint Agi) {
            return 0.3f + 0.006f * (
                                Str * 1 +
                                Math.Min(Dex, 20) * 4 +
                                    ((Dex > 20) ? (Dex - 20) * 1 : 0) +
                                Math.Min(Agi, 20) * 8 +
                                    ((Agi > 20) ? (Agi - 20) * 2 : 0));
        }

        private void recalc() {
            if(_statsum != statsum) {
                _statsum = statsum;
                _hpmult = getHPMult(Vit, Str, Dex, Agi, Int, Wis, Luc);
                _spdmult = getSPDMult(Str, Dex, Agi);
                _mdmgmult = getMDMGMult(Str, Dex);
                _precmult = getPRCMult(Dex, Agi);
                _rangmult = getRDMGMult(Dex, Agi);
                _mspdmult = getMSPDMult(Str, Dex, Agi);
                ;
            }
        }

        private float _hpmult = 0;

        public float HealthMult {
            get {
                recalc();
                return _hpmult;
            }
        }

        private float _spdmult = 0;

        public float SpeedMult {
            get {
                recalc();
                return _spdmult;
            }
        }
        private float _mdmgmult = 0;
        public float MeleeMult {
            get {
                recalc();
                return _mdmgmult;
            }
        }

        private float _precmult = 0;

        public float PrecisionMult {
            get {
                recalc();
                return _precmult;
            }
        }
        private float _rangmult = 0;
        public float RangedMult {
            get {
                recalc();
                return _rangmult;
            }
        }
        float _mspdmult = 0;
        public float MeleeSpeedMult {
            get {
                recalc();
                return _mspdmult;
            }
        }

        #endregion


        protected List<Tuple<Effect, float>> Effects { get; } = new List<Tuple<Effect, float>>();

        private static Dictionary<Attribute, uint> generateBaseAttributes() {
            Dictionary<Attribute, uint> temp = new Dictionary<Attribute, uint>();

            foreach(Attribute Attrib in Enum.GetValues(typeof(Attribute))) {
                temp.Add(Attrib, BaseAttributeValue);
            }
            return temp;
        }

        private static Dictionary<WeaponType, uint> generateAmmoStorage(float Base = 1) {
            Dictionary<WeaponType, uint> temp = new Dictionary<WeaponType, uint>();

            temp.Add(WeaponType.Acid, (uint)(Base * 3000));
            temp.Add(WeaponType.AssaultRifle, (uint)(Base * 120));
            temp.Add(WeaponType.Electricity, (uint)(Base * 2000));
            temp.Add(WeaponType.Melee, 0);
            temp.Add(WeaponType.Pistol, (uint)(Base * 60));
            temp.Add(WeaponType.Revolver, (uint)(Base * 30));
            temp.Add(WeaponType.RocketLauncher, (uint)(Base * 5));
            temp.Add(WeaponType.Shotgun, (uint)(Base * 50));
            temp.Add(WeaponType.SniperRifle, (uint)(Base * 12));
            temp.Add(WeaponType.SubMachineGun, (uint)(Base * 500));
            temp.Add(WeaponType.Throwable, 0);

            return temp;
        }

        public Dictionary<Attribute, uint> Attributes { get; set; } = generateBaseAttributes();
        public Random RNG { get; internal set; }

        public void AutoAssignAttributePoints() {
            if(AttributePoints > 0) {
                uint[] stats = new uint[Enum.GetValues(typeof(Attribute)).Length];
                while(AttributePoints > 0) {
                    stats[RNG.Next(stats.Length)]++;
                    AttributePoints--;
                }
                for(int i = 0; i < stats.Length; i++)
                    Attributes[(Attribute)Enum.GetValues(typeof(Attribute)).GetValue(i)] += stats[i];
            }
        }

        public void init(uint level = 1) {
            Level = level;
            AttributePoints = BaseAttributePoints + BaseAttributePointsPerLevel * (Level - 1);
        }

        protected void LevelUp(uint levels = 1) {
            Level += levels;
            Health = MaxHealth;
        }

        protected void Killed(AttributeBase by) {
            lock(this)
                if(!_dead) {
                    new Background(dataLoader.get("corpse.bmp"), Area.Center, 30, Background.Settings.Parallax);
                    if(Game.instance._client == null) {
                        Game.instance.addMessage(Name + " was killed by " + by.Name + ".");
                        foreach(ItemBase item in Inventory.ToArray()) {
                            item.Drop();
                        }
                        removeFromGame();
                    } else if(Game.instance.GameHost != null) {
                        Game.instance._client.send(GameClient.CommandType.message, (Name + " was killed by " + by.Name + ".").serialize());
                        if(Rank != Rank.Player)
                            Game.instance._client.send(GameClient.CommandType.remove, BitConverter.GetBytes(ID));
                    }
                    _dead = true;
                }
        }

        public virtual void Attack(AttributeBase target, float damage, bool reflection = false) {
            float totalDamage = damage;

            float threshold = 0;
            float reflect = 0;
            float absorption = 1;   //reversed for calculation
            float spike = 0;

            target.Effects.ForEach((effect) =>
            {
                switch(effect.Item1) {
                    case Effect.Damage_Threshold:
                        threshold += effect.Item2;
                        break;
                    case Effect.Damage_Absorption:
                        absorption *= (1 - effect.Item2);
                        break;
                    case Effect.Damage_Reflection:
                        reflect += effect.Item2;
                        break;
                    case Effect.Spike_Damage:
                        spike += effect.Item2;
                        break;
                    default:
                        //NYI
                        break;
                }
            });

            if(!reflection)
                target.Attack(this, damage * reflect + spike, true);

            if(threshold < totalDamage) {

                totalDamage -= threshold;   //threshold takes priority to make nullification harder
                totalDamage *= absorption;  //scale with the absorption

                if(totalDamage >= target.Health) {
                    target.Health = 0;
                    target.Killed(this);
                    Exp += (uint)Math.Ceiling(Math.Pow(1.1f, Math.Min(Level + 5, target.Level)) * 10 * Math.Pow(target.PowerMod, 1.1));
                    //Exp += (uint)Math.Ceiling(target.Level * 0.6f * Math.Min(Level + 2, target.Level) * target.PowerMod);
                    if(Rank != Rank.Player) {
                        AutoAssignAttributePoints();
                    }
                } else {
                    target.Health -= totalDamage;
                    if(target is NPC) {
                        ((NPC)target).Agressor = this;
                    }
                }
            } // end of if()

        } // end of Attack()

        public void Fire(bool L = false) {
            if((L ? EquippedWeaponL : EquippedWeaponR) == null) {
                if(_CoolDown <= 0) {
                    //melee attack
                    List<AttributeBase> targets = new List<AttributeBase>();

                    lock(GameStatus.GameSubjects)
                        targets.AddRange(GameStatus.GameSubjects.OrderBy((targ) => AimDirection.offset(Area.Center.angleTo(targ.Area.Center), true)));

                    targets.Remove(this);

                    targets.RemoveAll((targ) =>
                    {
                        if(Vector2.Distance(targ.Location, Location.absolute()) - targ.Size / 2 > RMeleeRange)
                            return true;
                        if(Precision != 0 && Location.angleTo(targ.Location).isInBetween(
                                new AngleSingle(AimDirection.Revolutions + (1 - Precision) / 2, AngleType.Revolution),
                                new AngleSingle(AimDirection.Revolutions - (1 - Precision) / 2, AngleType.Revolution)))
                            return true;
                        return false;
                    });
                    if(targets.Count > 0) {
                        Attack(targets.First(), MeleeDamage);
                    }
                    _CoolDown += 1 / MeleeSpeed;
                }
            } else
                (L ? EquippedWeaponL : EquippedWeaponR).Fire();
        }


        public virtual void draw(RenderTarget rt) {
            if(Game.instance._player != null) {



                Vector2 relativePos = Location + MatrixExtensions.PVTranslation;
                Vector2 relativeTarget = Target + MatrixExtensions.PVTranslation;

                Vector2 otherSideRelative = Location + (Location - Target) + MatrixExtensions.PVTranslation;

                Color4 tempColor = Pencil.Color;


                switch(drawType) {
                    case DrawType.Rectangle:
                        Area.Offset(MatrixExtensions.PVTranslation);
                        rt.FillRectangle(Area, Pencil);

                        Area.Offset(-MatrixExtensions.PVTranslation);
                        break;
                    case DrawType.Image:
                        Area.Offset(MatrixExtensions.PVTranslation);
                        rt.DrawBitmap(Image, Area, 1, BitmapInterpolationMode.Linear);
                        Area.Offset(-MatrixExtensions.PVTranslation);
                        break;
                    case DrawType.Circle:
                        rt.FillEllipse(new Ellipse(relativePos, Size / 2, Size / 2), Pencil);
                        break;
                    case DrawType.None:
                    default:
                        break;
                }

                switch(Game.instance._player.Team.Dispositions.ContainsKey(Team) ? Game.instance._player.Team.Dispositions[Team] : Game.instance._player.Team.DefaultDisposition) {
                    case Faction.Disposition.Allied:
                        Pencil.Color = Color.Green;
                        break;
                    case Faction.Disposition.Enemy:
                        Pencil.Color = Color.Red;
                        break;
                    case Faction.Disposition.Fear:
                        Pencil.Color = Color.Purple;
                        break;
                    case Faction.Disposition.Neutral:
                        Pencil.Color = Color.LightGray;
                        break;
                }

                switch(drawType) {
                    case DrawType.Polygon:
                        Polygon.Aggregate((prev, current) =>
                        {
                            rt.DrawLine(prev, current, Pencil);
                            return current;
                        });

                        break;
                    case DrawType.Rectangle:
                        Area.Offset(MatrixExtensions.PVTranslation);
                        Area.Inflate(0.5f, 0.5f);
                        rt.FillRectangle(Area, Pencil);
                        Area.Inflate(2, 2);

                        Area.Offset(-MatrixExtensions.PVTranslation);
                        break;
                    case DrawType.Circle:
                        rt.FillEllipse(new Ellipse(relativePos, Size / 4, Size / 4), Pencil);
                        break;
                    case DrawType.None:
                    default:
                        break;
                }

                Pencil.Color = Team.FactionColor;

                switch(drawType) {
                    case DrawType.Rectangle:
                        Area.Offset(MatrixExtensions.PVTranslation);
                        rt.DrawRectangle(Area, Pencil);

                        Area.Offset(-MatrixExtensions.PVTranslation);
                        break;
                    case DrawType.Circle:
                        rt.DrawEllipse(new Ellipse(relativePos, Size / 2, Size / 2), Pencil);
                        break;
                    case DrawType.Polygon:
                    case DrawType.Image:
                    case DrawType.None:
                    default:
                        break;
                }

                Pencil.Color = tempColor;

                float range = Math.Max(RWeaponRange, RMeleeRange);

                List<GradientStop> lgs = new List<GradientStop>();

                GradientStop temp = new GradientStop();

                temp.Color = Color.Red;
                temp.Position = 0;
                lgs.Add(temp);
                temp = new GradientStop();

                temp.Color = new Color(Color.Red.ToVector3(), 0.1f);
                temp.Position = 1;
                lgs.Add(temp);

                using(GradientStopCollection gsc = new GradientStopCollection(rt, lgs.ToArray())) {

                    LinearGradientBrushProperties lgbp = new LinearGradientBrushProperties() {
                        StartPoint = otherSideRelative,
                        EndPoint = relativeTarget
                    };

                    using(LinearGradientBrush lgb = new LinearGradientBrush(rt, lgbp, gsc)) {

                        if(Precision == 0) {
                            rt.DrawEllipse(new Ellipse(relativePos, range, range), lgb);
                        } else {
                            if(laser != null)
                                lock(laser) {
                                    if(laser != null) {
                                        rt.DrawLine(laser[0], laser[1], lgb);
                                        if(laser.Length == 3)
                                            rt.DrawLine(laser[0], laser[2], lgb);
                                    }
                                }
                        }
                    }
                }
            }
        }


        public float radToDeg(float rad) {
            return (float)(rad / 2 / Math.PI * 360);
        }

        public virtual void Tick() {
            if(_CoolDown > 0)
                _CoolDown -= GameVars.defaultGTPS / 1000;

            if(IsMoving) {
                applyAcceleration();
            }

            slowDown();

            RectangleF temp = Area;
            temp.Location += MovementVector / GameVars.defaultGTPS;
            Area = temp;

            float range = Math.Max(RWeaponRange, RMeleeRange);
            Target = Location + AimDirection.toVector() * range;

            if(laser != null) {
                lock(laser) {
                    generatelaserlines(range);
                }
            } else
                generatelaserlines(range);

        }

        private void generatelaserlines(float range) {

            if(Precision < 1) {

                laser = new Vector2[3];

                laser[0] = Location + MatrixExtensions.PVTranslation;
                laser[1] = laser[0].move(new AngleSingle(AimDirection.Revolutions + (1 - Precision) / 2, AngleType.Revolution), range);
                laser[2] = laser[0].move(new AngleSingle(AimDirection.Revolutions - (1 - Precision) / 2, AngleType.Revolution), range);


            } else {
                laser = new Vector2[2];
                laser[0] = Location + MatrixExtensions.PVTranslation;
                laser[1] = Target + MatrixExtensions.PVTranslation;
            }
        }

        private void applyAcceleration() {
            Vector2 nextMovementVector = MovementVector + DirectionVector * Acceleration;

            if(nextMovementVector.LengthSquared() > MaxMovementSpeed * MaxMovementSpeed) {
                nextMovementVector.Normalize();
                nextMovementVector *= MaxMovementSpeed;
            }
            MovementVector = nextMovementVector;

        }

        private void slowDown() {

            Vector2 nextMovementVector = MovementVector;

            int X = !IsMoving || DirectionVector.X == 0 || Math.Sign(DirectionVector.X) != Math.Sign(nextMovementVector.X) ? Math.Sign(nextMovementVector.X) : 0;
            int Y = !IsMoving || DirectionVector.Y == 0 || Math.Sign(DirectionVector.Y) != Math.Sign(nextMovementVector.Y) ? Math.Sign(nextMovementVector.Y) : 0;



            nextMovementVector -= Acceleration * new Vector2(X, Y);

            if(Math.Sign(nextMovementVector.X) == -X)
                nextMovementVector.X = 0;
            if(Math.Sign(nextMovementVector.Y) == -Y)
                nextMovementVector.Y = 0;

            MovementVector = nextMovementVector;

        }

        virtual public void removeFromGame() {
            GameStatus.removeTickable(this);
            GameStatus.removeRenderable(this);
            lock(GameStatus.GameSubjects)
                GameStatus.GameSubjects.Remove(this);
        }

        virtual public void addToGame() {
            GameStatus.addTickable(this);
            GameStatus.addRenderable(this);
            lock(GameStatus.GameSubjects)
                GameStatus.GameSubjects.Add(this);
        }

        public void setState(byte[] buffer, ref int pos) {
            if(this != Game.instance._player) {
                RectangleF temp = Area;
                temp.Location = new Vector2(buffer.getFloat(ref pos), buffer.getFloat(ref pos));
                Area = temp;
                MovementVector.X = buffer.getFloat(ref pos);
                MovementVector.Y = buffer.getFloat(ref pos);
                AimDirection = new AngleSingle(buffer.getFloat(ref pos), AngleType.Radian);
            } else {
                pos += CustomMaths.floatsize * 5;
            }
            Health = buffer.getFloat(ref pos);
            int count = buffer.getInt(ref pos);
            for(int i = 0; i < count; i++) {
                Random temp = buffer.loadRNG(ref pos);
                if(temp != null)
                    Inventory[i]._RNG = temp;
            }
        }

        public void setWeaponRandomState(byte[] buffer, ref int pos) {
            if(EquippedWeaponR != null && this != Game.instance._player)
                EquippedWeaponR._RNG = buffer.loadRNG(ref pos);
            else
                pos += CustomMaths.rngsize;
        }

        public byte[] getWeaponRandomState() {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(ID));
            data.AddRange(EquippedWeaponR._RNG.saveRNG());
            if(EquippedWeaponR != null)
                return data.ToArray();
            return null;
        }

        public byte[] serializeState() {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(ID));
            data.AddRange(BitConverter.GetBytes(Area.Center.X));
            data.AddRange(BitConverter.GetBytes(Area.Center.Y));
            data.AddRange(BitConverter.GetBytes(MovementVector.X));
            data.AddRange(BitConverter.GetBytes(MovementVector.Y));
            data.AddRange(BitConverter.GetBytes(AimDirection.Radians));
            data.AddRange(BitConverter.GetBytes(Health));
            data.AddRange(BitConverter.GetBytes(Inventory.Count));
            for(int i = 0; i < Inventory.Count; i++) {
                data.AddRange(Inventory[i]._RNG.saveRNG());
            }


            return data.ToArray();
        }

        public void drawUI(RenderTarget rt) {

            float padding = 5;

            float pos = padding;

            float barwidth = 200;
            float barheight = 20;

            rt.DrawText(Name, GameStatus.MenuFont, new RectangleF(padding, pos, Game.instance.Area.Width - padding * 2, Game.instance.Area.Height - pos), Pencil);

            pos += 12 + 2 * padding;

            RectangleF barRegion = new RectangleF(padding, pos, barwidth, barheight);
            RectangleF barSubRegion = new RectangleF(padding, pos, barwidth * Health / MaxHealth, barheight);

            pos += barheight + 2 * padding;

            Color4 temp = Pencil.Color;

            Pencil.Color = Color.DarkRed;

            rt.FillRectangle(barRegion, Pencil);

            Pencil.Color = Color.Red;

            rt.FillRectangle(barSubRegion, Pencil);

            Pencil.Color = Color.White;

            rt.DrawRectangle(barRegion, Pencil);

            Pencil.Color = Color.White;

            rt.DrawText(Health.ToString("0.##") + " / " + MaxHealth.ToString("0.##"), GameStatus.MenuFont, barRegion, Pencil);

            barRegion = new RectangleF(padding, pos, barwidth, barheight);
            barSubRegion = new RectangleF(padding, pos, barwidth * Exp / ExpToLvlUp, barheight);

            pos += barheight + 2 * padding;

            Pencil.Color = Color.DarkBlue;

            rt.FillRectangle(barRegion, Pencil);

            Pencil.Color = Color.Blue;

            rt.FillRectangle(barSubRegion, Pencil);

            Pencil.Color = Color.White;

            rt.DrawRectangle(barRegion, Pencil);

            Pencil.Color = Color.White;

            rt.DrawText(Exp.ToString("0.##") + " / " + ExpToLvlUp.ToString("0.##"), GameStatus.MenuFont, barRegion, Pencil);

            if(EquippedWeaponL != null && EquippedWeaponL.ClipSize > 0) {
                barRegion = new RectangleF(padding, pos, barwidth, barheight);
                barSubRegion = new RectangleF(padding, pos, barwidth * EquippedWeaponL.Ammo / EquippedWeaponL.ClipSize, barheight);

                pos += barheight + 2 * padding;

                Pencil.Color = Color.SmoothStep(Color.Yellow, Color.Black, 0.5f);

                rt.FillRectangle(barRegion, Pencil);

                Pencil.Color = Color.Yellow;

                rt.FillRectangle(barSubRegion, Pencil);

                Pencil.Color = Color.White;

                rt.DrawRectangle(barRegion, Pencil);

                Pencil.Color = Color.Black;

                rt.DrawText(EquippedWeaponL.Ammo.ToString("0.##") + " / " + EquippedWeaponL.ClipSize.ToString("0.##") + " (" + AmmoStorage[EquippedWeaponL.WType] + ")", GameStatus.MenuFont, barRegion, Pencil);
            }

            if(EquippedWeaponL != EquippedWeaponR && EquippedWeaponR != null && EquippedWeaponR.ClipSize > 0) {
                barRegion = new RectangleF(padding, pos, barwidth, barheight);
                barSubRegion = new RectangleF(padding, pos, barwidth * EquippedWeaponR.Ammo / EquippedWeaponR.ClipSize, barheight);

                pos += barheight + 2 * padding;

                Pencil.Color = Color.SmoothStep(Color.Yellow, Color.Black, 0.5f);

                rt.FillRectangle(barRegion, Pencil);

                Pencil.Color = Color.Yellow;

                rt.FillRectangle(barSubRegion, Pencil);

                Pencil.Color = Color.White;

                rt.DrawRectangle(barRegion, Pencil);

                Pencil.Color = Color.Black;

                rt.DrawText(EquippedWeaponR.Ammo.ToString("0.##") + " / " + EquippedWeaponR.ClipSize.ToString("0.##") + " (" + AmmoStorage[EquippedWeaponR.WType] + ")", GameStatus.MenuFont, barRegion, Pencil);
            }

            Pencil.Color = temp;
        }
    }
}
