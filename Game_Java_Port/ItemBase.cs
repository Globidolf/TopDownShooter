using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game_Java_Port.Interface;
using static Game_Java_Port.CustomMaths;
using static System.BitConverter;
using SharpDX;
using SharpDX.Direct2D1;

namespace Game_Java_Port {
    public abstract class ItemBase : IRenderable, ITickable, IDisposable, IInteractable {

        public Random _RNG;

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
        
        public int ItemInfoLines { get {
                return ItemInfo.Count((c) => c == '\n') + 1;
            } }

        public abstract string ItemInfo { get; }

        protected byte gen { get; set; }

        public abstract uint BasePrice { get; }
        public uint BuyPrice { get { return (uint)(BasePrice * BaseBuyMultiplier); } }
        public uint SellPrice { get { return (uint)(BasePrice * BaseSellMultiplier); } }

        public AttributeBase Owner { get; set; }

        public string ActionDescription {
            get { return "Pick up"; }
        }

        public void PickUp(AttributeBase by) {
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
                    Owner.Attack(by, Owner.MeleeDamage, true);
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

        Ellipse ellipse;
        Vector2 relativePos;
        RectangleF textpos;
        float textSize = 0;

        public virtual void draw(RenderTarget rt) {
            if(Owner == null) {
                //Vector2 relativePos = PointF.Add(PointF.Subtract(Location, new SizeF(Game.instance._player.Location)), new SizeF(GameStatus.ScreenWidth / 2, GameStatus.ScreenHeight / 2));
                lock(this) {
                    if(!Pencil.IsDisposed) {
                        if(Pencil.Color == (Color4)Color.Transparent) {
                            Pencil.Color = GameVars.RarityColors[Rarity];
                        }
                        Color4 temp = Pencil.Color;
                        rt.FillEllipse(ellipse, Pencil);
                        Pencil.Color = Color.Black;
                        
                        Pencil.Opacity = 0.8f;
                        rt.FillRectangle(textpos, Pencil);
                        Pencil.Color = temp;
                        Pencil.Opacity = 1;
                        textpos.X += 2;
                        rt.DrawText(Name, GameStatus.MenuFont, textpos, Pencil);
                        textpos.X -= 2;
                        if (Game.instance._player != null) {
                            if(Vector2.DistanceSquared(Game.instance._player.Location, Location) < InfoDisplayPlayerDist * InfoDisplayPlayerDist &&
                                Vector2.DistanceSquared(Location + MatrixExtensions.PVTranslation, GameStatus.MousePos) < InfoDisplayCursorDist * InfoDisplayCursorDist) {
                                RectangleF pos = new RectangleF(GameStatus.MousePos.X - 150, GameStatus.MousePos.Y - Size / 2 - 20, 300, 20);
                                Pencil.Color = Color.Black;
                                Pencil.Opacity = 0.8f;
                                rt.FillRectangle(pos, Pencil);
                                Pencil.Color = temp;
                                Pencil.Opacity = 1f;
                                pos.X += 2;
                                pos.Width -= 4;
                                rt.DrawText(ActionDescription, GameStatus.MenuFont, pos, Pencil);
                            }
                        }
                    }
                }
            }
        }



        public virtual void Tick() {
            if(Owner == null) {
                UnOwnedTime++;
                if (textSize == 0) {
                    lock(GameStatus.TextRenderer)
                        textSize = GameStatus.TextRenderer.MeasureString(Name, GameMenu._menufont).Width * 0.8f;
                }
                relativePos = Location + MatrixExtensions.PVTranslation;
                textpos = new RectangleF(relativePos.X + Size / 2, relativePos.Y - 12, textSize, 20);
                ellipse = new Ellipse(relativePos, Size / 2, Size / 2);
                if(UnOwnedTime > DespawnTime * GameVars.defaultGTPS) {
                    lock(this) {
                        Dispose();
                    }
                }
            }

        }

        public static ItemBase deSerialize(byte[] buffer, ref int pos) {
            ItemBase temp;
            byte itemtype = buffer.getByte(ref pos);

            switch(itemtype) {
                //weapon
                case 0:
                    temp = new Weapon(buffer.getUInt(ref pos), buffer.getInt(ref pos));
                    break;
                //weapon with predefined type
                case 1:
                    temp = new Weapon(buffer.getUInt(ref pos), buffer.getInt(ref pos), buffer.getEnumByte<WeapPreset>(ref pos));
                    break;
                //weapon with predefined rarity
                case 2:
                    temp = new Weapon(buffer.getUInt(ref pos), buffer.getInt(ref pos), rarity: buffer.getEnumByte<ItemType>(ref pos));
                    break;
                //weapon with predefined type and rarity
                case 3:
                    temp = new Weapon(buffer.getUInt(ref pos), buffer.getInt(ref pos), buffer.getEnumByte<WeapPreset>(ref pos), buffer.getEnumByte<ItemType>(ref pos));
                    break;
                //unknown
                default:
                    temp = null;
                    break;
            }

            return temp;
        }

        public byte[] serialize() {
            List<byte> data = new List<byte>();

            data.Add(gen);

            if(this is Weapon) {
                data.AddRange(GetBytes(((Weapon)this).Level));
                data.AddRange(GetBytes(((Weapon)this).Seed));
                if(gen == 1 || gen == 3)
                    data.Add((byte)((Weapon)this).WType);
                if(gen == 2 || gen == 3) {
                    data.Add((byte)((Weapon)this).Rarity);
                }
            }

            return data.ToArray();
        }

        /// <summary>
        /// Call base.Dispose() if overriding.
        /// Not doing so will result in a memory leak.
        /// </summary>
        public virtual void Dispose() {
            lock (GameStatus.GameObjects)
                GameStatus.GameObjects.Remove(this);
            GameStatus.removeRenderable(this);
            GameStatus.removeTickable(this);
            Pencil.Dispose();
        }

        public virtual void AddToGame() {
            GameStatus.addRenderable(this);
            GameStatus.addTickable(this);
            if (Owner != null)
                lock(GameStatus.GameObjects)
                    GameStatus.GameObjects.Add(this);
        }

        public void interact(NPC who) {
            PickUp(who);
            if (this is IEquipable) {
                if(who.getEquipedItem(((IEquipable)this).Slot) == null)
                    ((IEquipable)this).Equip(who);
                else if(((IEquipable)this).Slot == EquipSlot.Weapon1H && who.getEquipedItem(EquipSlot.Weapon1H, true) == null)
                    ((IEquipable)this).Equip(who);
            }
        }
        
    }
}
