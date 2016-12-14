using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Game_Java_Port.GameStatus;
using static System.BitConverter;

namespace Game_Java_Port {
    public class NPC : AttributeBase, IInteractable {

        public Controls _lastState = Controls.none;

        public Action<NPC> AI { get; set; }

        public AttributeBase Agressor;

        public AttributeBase Interactor;

        public Controls inputstate = Controls.none;

        public float ViewRadius { get; private set; }

        public override DrawType drawType { get { return DrawType.Circle; } }

        public override Rank Rank { get; }

        public int Seed { get; set; }
        public AngleSingle LastAimDirection { get; internal set; }

        public string ActionDescription {
            get { return "Tell: " + Team.InteractionConduct.ToString() + " me!"; }
        }

        private RectangleF hpRect;
        private RectangleF hpLeftRect;
        public bool justPressed(Controls input) {
            if(!_lastState.HasFlag(input) && inputstate.HasFlag(input)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Unique constructor. only non-writeable rank is set.
        /// </summary>
        /// <param name="rank">Rank for this NPC</param>
        private NPC(Rank rank) {
            Rank = rank;
            if(Rank == Rank.Player)
                AI = AI_Library.PlayerSim;
        }


        public NPC(string Name,
            uint Vit = 1, uint Str = 1, uint Dex = 1,
            uint Agi = 1, uint Int = 1, uint Wis = 1,
            uint Luc = 1, bool directAdd = true,
            ulong? ID = null) {

            Seed = -1;

            if(ID == null)
                this.ID = GetFirstFreeID;
            else
                this.ID = (ulong)ID;

            this.Name = Name;

            Rank = Rank.Player;

            Team = Faction.Players;

            init();

            Attributes[Attribute.Vitality] = Vit;
            Attributes[Attribute.Strength] = Str;
            Attributes[Attribute.Dexterity] = Dex;
            Attributes[Attribute.Agility] = Agi;
            Attributes[Attribute.Intelligence] = Int;
            Attributes[Attribute.Wisdom] = Wis;
            Attributes[Attribute.Luck] = Luc;

            RectangleF __pos = new RectangleF();

            __pos.Width = __pos.Height = 10;

            Area = __pos;
            __pos.Location = Game.instance.Location;
            Health = MaxHealth;
            //weaponcodes
            /* returning acid 68348639
             * tracking smg 2056088395
             * epic sg 1415295922
             */
            if(false) {
                Weapon temp = new Weapon(Level, wt: WeapPreset.Pistol, rarity: ItemType.Uncommon);
                temp.PickUp(this);
                EquippedWeaponR = temp;
            }
            Pencil.Color = Color.Blue;
            if(directAdd) {
                addToGame();
                AI = AI_Library.PlayerSim;
            }
        }

        public NPC(uint level = 1, int? seed = null, ulong? ID = null, bool add = true) {

            if(ID == null)
                this.ID = GetFirstFreeID;
            else
                this.ID = (ulong)ID;

            do
                Seed = seed == null ? GameStatus.RNG.Next() : (int)seed;
            while(Seed == -1);

            RNG = new Random(Seed);

            init(level);

            Dictionary<Rank, float> Chances = new Dictionary<Rank, float>();
            float chancesum = 0;

            AimDirection = new AngleSingle(RNG.NextFloat(0, 1), AngleType.Revolution);
            DirectionVector.X = (float)(RNG.NextDouble() * 2 - 1);
            DirectionVector.Y = (float)(RNG.NextDouble() * 2 - 1);

            Chances.Add(Rank.Furniture, chancesum);
            chancesum += 50;
            Chances.Add(Rank.Trash, chancesum);
            chancesum += 120;
            Chances.Add(Rank.Common, chancesum);
            chancesum += 80;
            Chances.Add(Rank.Elite, chancesum);
            chancesum += 30;
            Chances.Add(Rank.Rare, chancesum);
            chancesum += 10;
            Chances.Add(Rank.Leader, chancesum);
            chancesum += 6;
            Chances.Add(Rank.Boss, chancesum);
            chancesum += 2f;
            Chances.Add(Rank.God, chancesum);
            chancesum += 0.5f;

            float rank = RNG.NextFloat(0, chancesum);

            Rank = Chances.Last((pair) => pair.Value < rank).Key;

            Team = RNG.getValidFaction();

            AI = AI_Library.NPC_AI;



            if(Rank > Rank.Furniture) {
                Weapon w = new Weapon(level + (uint)(Rank - Rank.Trash));
                w.PickUp(this);
                w.Equip(this);
            }
            float minSize = 12;
            float viewmult = 2.6f;
            switch(Rank) {
                case Rank.Furniture:
                    Size = minSize + 6 + (float)RNG.NextDouble() * 24;
                    Pencil.Color = Color.LightPink;
                    Team = Faction.Environment;
                    AI = null;
                    break;
                case Rank.Trash:
                    Size = minSize + 4 + (float)RNG.NextDouble() * 2;
                    Pencil.Color = Color.Gray;
                    ViewRadius = 200 * viewmult;
                    break;
                case Rank.Common:
                    Size = minSize + 8 + (float)RNG.NextDouble() * 4;
                    Pencil.Color = Color.White;
                    ViewRadius = 220 * viewmult;
                    break;
                case Rank.Elite:
                    Size = minSize + 12 + (float)RNG.NextDouble() * 4;
                    Pencil.Color = Color.Blue;
                    ViewRadius = 250 * viewmult;
                    break;
                case Rank.Rare:
                    Size = minSize + 16 + (float)RNG.NextDouble() * 4;
                    Pencil.Color = Color.Yellow;
                    ViewRadius = 300 * viewmult;
                    break;
                case Rank.Leader:
                    Size = minSize + 20 + (float)RNG.NextDouble() * 5;
                    Pencil.Color = Color.Orange;
                    ViewRadius = 400 * viewmult;
                    break;
                case Rank.Boss:
                    Size = minSize + 30 + (float)RNG.NextDouble() * 30;
                    Pencil.Color = Color.Red;
                    ViewRadius = 500 * viewmult;
                    break;
                case Rank.Player:
                    Pencil.Color = Color.Green;
                    Team = Faction.Players;
                    break;
            }
            AutoAssignAttributePoints();
            Health = MaxHealth;
            if(add)
                addToGame();
        }

        float removeCounter = 60;

        public override void Tick() {
            Vector2 relativePos = Location + MatrixExtensions.PVTranslation;
            float distanceToPlayers = float.PositiveInfinity;

            lock(GameSubjects) {
                if(GameSubjects.Any((subj) => subj.Team == FactionNames.Players)) {
                    GameSubjects.ForEach((subj) =>
                    {
                        float temp;
                        if((temp = Vector2.DistanceSquared(Location, subj.Location)) < distanceToPlayers)
                            distanceToPlayers = temp;
                    });
                }
            }

            hpRect = new RectangleF(relativePos.X - 50, relativePos.Y - Size - 35, 100, 20);
            hpLeftRect = new RectangleF(relativePos.X - 50, relativePos.Y - Size - 35, hpRect.Width / MaxHealth * Health, hpRect.Height);

            if(distanceToPlayers < (ScreenHeight * ScreenHeight + ScreenWidth * ScreenWidth) / 2) {
                removeCounter = 60;
                AI?.Invoke(this);
                base.Tick();
            } else {
                removeCounter -= 1 / GameVars.defaultGTPS;
                if(removeCounter <= 0) {
                    removeFromGame();
                }
            }
        }



        public override void draw(RenderTarget rt) {
            base.draw(rt);

            if(false) {
                rt.FillRectangle(new RectangleF(400, 400, 400, 400), MenuHoverBrush);
                string test = "";
                foreach(var atr in Attributes) {
                    test += atr.Key + ": " + atr.Value + "\n";
                }
                test += Location;
                rt.DrawText(test, MenuFont, new RectangleF(400, 400, 400, 400), MenuBorderPen);
            }

            if(this != Game.instance._player) {
                if(Game.instance._player != null) {
                    Color4 temp = Pencil.Color;

                    // prevent concurrent exceptions
                    RectangleF hpRect = this.hpRect;
                    Pencil.Color = Color.DarkRed;
                    rt.FillRectangle(hpRect, Pencil);
                    Pencil.Color = Color.Red;
                    rt.FillRectangle(hpLeftRect, Pencil);
                    Pencil.Color = Color.White;
                    rt.DrawRectangle(hpRect, Pencil);
                    Pencil.Color = temp;
                    rt.DrawText(Health.ToString("0.##") + " / " + MaxHealth.ToString("0.##"), MenuFont, hpRect, Pencil);
                    hpRect.Offset(0, -20);

                    string displaystring = Name + " (Level " + Level + " " + Rank.ToString() + ") [" + Team.ToString() + "]";

                    lock(TextRenderer)
                        hpRect.Width = TextRenderer.MeasureString(displaystring, GameMenu._menufont).Width * 0.8f;
                    Pencil.Color = Color.Black;
                    rt.FillRectangle(hpRect, Pencil);
                    Pencil.Color = temp;
                    rt.DrawText(displaystring, MenuFont, hpRect, Pencil);

                }
            } else
                drawUI(rt);
        }

        public void setInputState(byte[] buffer, ref int pos) {
            if(this != Game.instance._player)
                inputstate = buffer.getEnumShort<Controls>(ref pos);
            else
                pos += CustomMaths.shortsize;
        }

        public static NPC Deserialize(byte[] buffer, ref int pos) {
            NPC temp;

            int Seed = buffer.getInt(ref pos);
            if(Seed != -1) {
                temp = new NPC(buffer.getUInt(ref pos), Seed, add: false);
            } else {
                temp = new NPC(buffer.getEnumByte<Rank>(ref pos));
                temp.Seed = Seed;
                temp.Level = buffer.getUInt(ref pos);
                temp.Size = buffer.getFloat(ref pos);
                foreach(Attribute attr in Enum.GetValues(typeof(Attribute))) {
                    temp.Attributes[attr] = buffer.getUInt(ref pos);
                }
            }
            temp.ID = buffer.getULong(ref pos);
            temp.Pencil.Color = Color.FromRgba(buffer.getInt(ref pos));
            temp.Location = new Vector2(buffer.getFloat(ref pos), buffer.getFloat(ref pos));
            temp.MovementVector.X = buffer.getFloat(ref pos);
            temp.MovementVector.Y = buffer.getFloat(ref pos);
            temp.DirectionVector.X = buffer.getFloat(ref pos);
            temp.DirectionVector.Y = buffer.getFloat(ref pos);
            temp.AimDirection = new AngleSingle(buffer.getFloat(ref pos), AngleType.Radian);
            temp.Health = buffer.getFloat(ref pos);
            temp.Exp = buffer.getUInt(ref pos);
            temp.Name = buffer.getString(ref pos);
            int inventoryItems = buffer.getInt(ref pos);
            int equippedWeapon = buffer.getInt(ref pos);

            for(int i = 0; i < inventoryItems; i++) {
                ItemBase.deSerialize(buffer, ref pos).PickUp(temp);
            }

            if(equippedWeapon >= 0) {
                temp.EquippedWeaponR = (Weapon)temp.Inventory[equippedWeapon];
            }

            return temp;
        }

        public byte[] serialize() {
            List<byte> data = new List<byte>();

            data.AddRange(GetBytes(Seed));
            if(Seed != -1) {
                data.AddRange(GetBytes(Level));
            } else {
                data.Add((byte)Rank);
                data.AddRange(GetBytes(Level));
                data.AddRange(GetBytes(Size));
                foreach(Attribute attr in Enum.GetValues(typeof(Attribute))) {
                    data.AddRange(GetBytes(Attributes[attr]));
                }
            }
            data.AddRange(GetBytes(ID));
            data.AddRange(GetBytes(((Color4)Pencil.Color).ToRgba()));
            data.AddRange(GetBytes(Location.X));
            data.AddRange(GetBytes(Location.Y));
            data.AddRange(GetBytes(MovementVector.X));
            data.AddRange(GetBytes(MovementVector.Y));
            data.AddRange(GetBytes(DirectionVector.X));
            data.AddRange(GetBytes(DirectionVector.Y));
            data.AddRange(GetBytes(AimDirection.Radians));
            data.AddRange(GetBytes(Health));
            data.AddRange(GetBytes(Exp));
            data.AddRange(Name == null ? GetBytes(0) : Name.serialize());

            data.AddRange(GetBytes(Inventory.Count));
            if(EquippedWeaponR == null)
                data.AddRange(GetBytes(-1));
            else
                data.AddRange(GetBytes(Inventory.IndexOf(EquippedWeaponR)));

            foreach(ItemBase item in Inventory) {
                data.AddRange(item.serialize());
            }

            return data.ToArray();
        }

        public void interact(NPC interactor) {
            Interactor = interactor;
        }

        public override void addToGame() {
            base.addToGame();
            lock(GameObjects)
                GameObjects.Add(this);
        }

        public override void removeFromGame() {
            base.removeFromGame();
            lock(GameObjects)
                GameObjects.Remove(this);
        }
    }
}
