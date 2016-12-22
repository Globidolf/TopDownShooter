using System;
using System.Linq;
using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using Game_Java_Port.Serializers;

namespace Game_Java_Port {
    public abstract class ItemBase : IRenderable, ITickable, IDisposable, IInteractable, ISerializable<ItemBase>, IIndexable {

        private bool disposed = false;

        public Random _RNG;

        private ulong _ID;

        public ulong ID { get { return _ID; } }

        public const float InfoDisplayPlayerDist = 500;

        public const float InfoDisplayCursorDist = 20;

        public const float BaseSize = 8;
        public const float BaseWeight = 1;
        
        public const float BaseBuyMultiplier = 2;
        public const float BaseSellMultiplier = 0.5f;

        public const uint DespawnTime = 60;

        public uint UnOwnedTime { get; private set; }

        public abstract ItemType Rarity { get; }
        private SolidColorBrush Pencil { get; set; } = new SolidColorBrush(Program._RenderTarget, Color.Transparent);
        internal Bitmap image;
        public string Name { get; set; } = "Unknown Item";

        public virtual float Size { get { return BaseSize; } set { } }
        public virtual float Weight { get { return BaseWeight; } }

        private RectangleF _Area = new RectangleF();

        public Vector2 Location {
            get {
                return Area.Center;
            }
            set {
                RectangleF temp = Area;
                temp.Location = temp.Center - temp.Location + value;
                Area = temp;
            }
        }

        public RectangleF Area { get {
                return Owner != null ? Owner.Area : _Area;
            } set { _Area = value; } }
        
        protected Tooltip itemInfo;

        public string ItemInfoText { get {
                return ItemInfo.Text;
            } set {
                ItemInfo.Text = value;
            }
        }

        protected string ItemBaseInfo { get {
                return Name + "\nValue: " + SellPrice;
            } }

        protected Func<bool> ItemBaseInfoValidation {
            get {
                return () => Game.instance._player != null &&
                                Vector2.DistanceSquared(Game.instance._player.Location, Location) < InfoDisplayPlayerDist * InfoDisplayPlayerDist &&
                                Vector2.DistanceSquared(Location + MatrixExtensions.PVTranslation, GameStatus.MousePos) < InfoDisplayCursorDist * InfoDisplayCursorDist;
            }
        }

        public virtual Tooltip ItemInfo {
            get {
                if(itemInfo == null) {
                    itemInfo = new Tooltip(ItemBaseInfo,
                        Validation: ItemBaseInfoValidation,
                        ticksInternal: true);
                }
                return itemInfo;
            }
        }

        public byte GenType { get; set; }

        public abstract uint BasePrice { get; }

        public uint BuyPrice { get { return (uint)Math.Ceiling(BasePrice * BaseBuyMultiplier); } }
        public uint SellPrice { get { return (uint)Math.Ceiling(BasePrice * BaseSellMultiplier); } }

        public CharacterBase Owner { get; set; }

        public Serializer<ItemBase> Serializer {  get { return ItemSerializer.Instance; } }

        public int Z { get; set; } = 1;

        public DrawType drawType { get; set; } = DrawType.Circle;

        public void PickUp(CharacterBase by) {
            UnOwnedTime = 0;
            if(Owner == null) {
                lock(GameStatus.GameObjects)
                    GameStatus.GameObjects.Remove(this);
                Owner = by;
                Owner.Inventory.Add(this);


                //Weapon has owner, attempt to take it anyway
            } else {
                #region Steal Attempt

                // base steal chance, subject to change as this whole partition
                float stealChance = by.Level * 2.5f;

                // make relevant stats matter.
                stealChance += (by.Str - Owner.Str) + (by.Dex - Owner.Dex) + (by.Agi - Owner.Agi) + (by.Luc - Owner.Luc);

                #region Calculation Example
                //example: level 5 rogue steals from level 10 mage:
                //rogue:            12.5 level base,    6str    8dex    8agi    5luc
                //mage:             75 level base,      3str    3dex    5agi    4luc
                //                                     +3   =3 +5   =8 +3   =11+1   =12
                //stat difference:  12 in favor of rogue (+)

                //chance 24.5 to 75, or ~32% for an attempt at a low def charater with a high atk one. level makes a huge difference.

                //example reversed: level 10 mage steals from level 5 rogue:
                //mage:             25 level base,      3str    3dex    5agi    4luc
                //rogue:            37.5 level base,    6str    8dex    8agi    5luc
                //                                     -3  =-3 -5  =-8 -3  =-11-1  =-12
                //stat difference:  12 in favor of rogue (-)

                //chance 13 to 37.5, or ~35% for an attempt at a high def charater with a low atk one. only the level gives chances of success

                #endregion

                if(stealChance > by.RNG.Next((int)(Owner.Level * 7.5f))) {
                    Owner.Inventory.Remove(this);
                    if(Owner.EquippedWeaponR == this)
                        Owner.EquippedWeaponR = null;

                    Owner = by;
                    Owner.Inventory.Add(this);
                } else {
                    Owner.Attack(by, Owner.MeleeDamageR, true);
                }

                #endregion
            }
        }

        public void Drop() {
            if(Owner != null) {
                _Area = Owner.Area;

                if(Owner.EquippedWeaponR == this)
                    Owner.EquippedWeaponR = null;
                Owner.Inventory.Remove(this);
                Owner = null;
                lock(GameStatus.GameObjects)
                    GameStatus.GameObjects.Add(this);
            }
        }

        RectangleF _D_ImgRect;
        Ellipse _D_Ellipse;
        Vector2 _D_RelativePos;

        Tooltip _NameToolTip;

        Tooltip NameTooltip { get {
                if (_NameToolTip == null) {
                    _NameToolTip = new Tooltip(Name,
                        () => new Vector2(Location.X + MatrixExtensions.PVTranslation.X, Location.Y + MatrixExtensions.PVTranslation.Y - _NameToolTip.Area.Height - 20),
                        () => Owner == null,
                        true);
                }
                return _NameToolTip;
            } }

        Tooltip _ActionTooltip;
        public Tooltip ActionInfo { get {
                if (_ActionTooltip == null) {
                    _ActionTooltip = new Tooltip("Pick up",
                        () => new Vector2(Location.X + MatrixExtensions.PVTranslation.X, Location.Y + MatrixExtensions.PVTranslation.Y - _ActionTooltip.Area.Height - _NameToolTip.Area.Height - 20),
                        () => this.drawActionInfo() && Owner == null,
                        true);
                }
                return _ActionTooltip;
            } }

        public virtual void draw(RenderTarget rt) {
            if(Owner == null) {


                lock(this) {

                    if(!Pencil.IsDisposed) {
                        if(Pencil.Color == (Color4)Color.Transparent) {
                            Pencil.Color = GameVars.RarityColors[Rarity];
                        }

                        switch(drawType) {
                            case DrawType.Circle:
                                rt.FillEllipse(_D_Ellipse, Pencil);
                                break;
                            case DrawType.Image:
                                rt.DrawBitmap(image, _D_ImgRect, 1, BitmapInterpolationMode.Linear);
                                break;
                        }
                    }
                }
            }
        }



        public virtual void Tick() {
            if(Owner == null) {
                UnOwnedTime++;

                NameTooltip.Tick();
                ActionInfo.Tick();
                ItemInfo.Tick();

                if (this.drawActionInfo())
                    GameStatus.Cursor.CursorType = CursorTypes.Inventory_Add;


                _D_RelativePos = Location + MatrixExtensions.PVTranslation;

                switch(drawType) {
                    case DrawType.Rectangle:
                    case DrawType.Image:
                        _D_ImgRect = new RectangleF(
                            _D_RelativePos.X - image.PixelSize.Width / 2,
                            _D_RelativePos.Y - image.PixelSize.Height / 2,
                            image.PixelSize.Width,
                            image.PixelSize.Height);
                        break;
                    case DrawType.Circle:
                        _D_Ellipse = new Ellipse(_D_RelativePos, Size / 2, Size / 2);
                        break;
                }

                if(UnOwnedTime > DespawnTime * GameVars.defaultGTPS) {
                    lock(this) 
                        Dispose();
                    
                }
            }

        }

        
        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            if(disposed)
                return;
            if(disposing) {
                lock(GameStatus.GameObjects)
                    GameStatus.GameObjects.Remove(this);
                GameStatus.removeRenderable(this);
                GameStatus.removeTickable(this);
                Pencil.Dispose();
                ItemInfo.Dispose();
                ActionInfo.Dispose();
                NameTooltip.Dispose();
            }
            disposed = true;
        }

        public virtual void AddToGame() {
            GameStatus.addRenderable(this);
            GameStatus.addTickable(this);
            if (Owner != null)
                lock(GameStatus.GameObjects)
                    GameStatus.GameObjects.Add(this);
        }

        public void interact(CharacterBase who) {
            PickUp(who);
            if (this is IEquipable) {
                if(who.getEquipedItem(((IEquipable)this).Slot) == null)
                    ((IEquipable)this).Equip(who);
                else if(((IEquipable)this).Slot == EquipSlot.Weapon1H && who.getEquipedItem(EquipSlot.Weapon1H, true) == null)
                    ((IEquipable)this).Equip(who);
            }
            NameTooltip.Tick();
            ActionInfo.Tick();
            ItemInfo.Tick();
        }
        
    }
}
