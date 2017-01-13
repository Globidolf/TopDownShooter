using Game_Java_Port.Interface;
using Game_Java_Port.Logics;
using Game_Java_Port.Serializers;
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
    public class NPC : CharacterBase, IInteractable, ISerializable<NPC> {

        public Controls _lastState = Controls.none;

        public Action<NPC> AI { get; set; }

        public override GenerationType GenType { get { return GenerationType.NPC; } }

        public CharacterBase Agressor;

        public CharacterBase Interactor;

        public Controls inputstate = Controls.none;

        public float ViewRadius { get; private set; }

        public override DrawType drawType { get; set; } = DrawType.Circle;

        public override Rank Rank { get; set; }

        public int Seed { get; set; }
        public AngleSingle LastAimDirection { get; internal set; }

        private Tooltip _ActionInfo;

        public Tooltip ActionInfo {
            get {
                if (_ActionInfo == null)
                    _ActionInfo = new Tooltip("Tell: " + Team.InteractionConduct.ToString() + " me!", Validation: () => this.drawActionInfo(), ticksInternal: true);
                return _ActionInfo;
            }
        }
        
        new public Serializer<NPC> Serializer { get { return NPCSerializer.Instance; } }

        private RectangleF hpRect;
        private RectangleF hpLeftRect;
        public bool justPressed(Controls input) {
            if(!_lastState.HasFlag(input) && inputstate.HasFlag(input)) {
                return true;
            }
            return false;
        }
        
        public NPC() { }

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

            Image = dataLoader.get("player");

            drawType = DrawType.Image;
            

            Area = new RectangleF(Game.instance.Location.X, Game.instance.Location.Y, Image.PixelSize.Width, Image.PixelSize.Height);
            Health = MaxHealth;

            Pencil.Color = Color.Blue;


            if(directAdd) {
                Program.DebugLog.Add("Adding Subject " + ID + ". NPC(...a lot...).");
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
            if(add) {
                Program.DebugLog.Add("Adding Subject " + ID + ". NPC(uint, int?, ulong?, bool).");
                addToGame();
            }
        }

        float removeCounter = 5;

        public override void Tick() {
            bool isdead = Corpses.ContainsKey(this);


            if(!isdead) {
                Vector2 relativePos = Location + MatrixExtensions.PVTranslation;
                float distanceToPlayers = float.PositiveInfinity;
                
                    if(GameSubjects.Any((subj) => subj.Team == FactionNames.Players)) 
                        GameSubjects.FindAll((subj) => subj.Team == FactionNames.Players).ForEach((subj) =>
                        {
                            float temp;
                            if((temp = Vector2.DistanceSquared(Location, subj.Location)) < distanceToPlayers)
                                distanceToPlayers = temp;
                        });
                displaystring = Name + " (Level " + Level + " " + Rank.ToString() + ") [" + Team.ToString() + "]";

                Size2 measuredSize = SpriteFont.DEFAULT.MeasureString(displaystring);
                hpRect = new RectangleF(relativePos.X - measuredSize.Width / 2, relativePos.Y - Size - 35, measuredSize.Width, measuredSize.Height);

                /*
                using(SharpDX.DirectWrite.TextLayout tl = new SharpDX.DirectWrite.TextLayout(Program.DW_Factory, displaystring, MenuFont, 1000, 1000))
                    hpRect = new RectangleF(relativePos.X - tl.Metrics.Width / 2, relativePos.Y - Size - 35, tl.Metrics.Width, tl.Metrics.Height);
                */

                hpLeftRect = new RectangleF(hpRect.X, hpRect.Y, hpRect.Width / MaxHealth * Health, hpRect.Height);

                if(distanceToPlayers < (ScreenHeight * ScreenHeight + ScreenWidth * ScreenWidth)) {
                    removeCounter = 5;
                    AI?.Invoke(this);
                    base.Tick();
                    if(Team.InteractionConduct != Faction.Conduct.Ignore) {
                        ActionInfo.Tick();
                        if (this.drawActionInfo())
                            Cursor.CursorType = CursorTypes.Interact;
                    }
                } else {
                    removeCounter -= TimeMultiplier;
                    if(removeCounter <= 0) {
                            Corpses.Add(this,0);
                        switch(Game.state) {
                            case Game.GameState.Host | Game.GameState.Multiplayer:
                                Program.DebugLog.Add("Sending Remove Req: " + ID + ". NPC.Tick().");
                                Game.instance._client.send(GameClient.CommandType.remove, GetBytes(ID));
                                break;
                            case Game.GameState.Normal:
                                Program.DebugLog.Add("Removing Subject " + ID + ". NPC.Tick().");
                                    removeFromGame();
                                break;
                        } // end switch
                    } // end if
                } // end else
            } // end if
        }

        private string displaystring = "";

        public override void draw(RenderTarget rt) {
                if (!disposed && removeCounter == 5){

                    base.draw(rt);

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

                            SpriteFont.DEFAULT.directDrawText(Health.ToString("0.##") + " / " + MaxHealth.ToString("0.##"), hpRect, rt);

                            //rt.DrawText(Health.ToString("0.##") + " / " + MaxHealth.ToString("0.##"), MenuFont, hpRect, Pencil);
                            hpRect.Offset(0, -20);
                            
                            Pencil.Color = Color.Black;
                            rt.FillRectangle(hpRect, Pencil);
                            Pencil.Color = temp;

                            SpriteFont.DEFAULT.directDrawText(displaystring, hpRect, rt);

                            //rt.DrawText(displaystring, MenuFont, hpRect, Pencil);

                        }
                    } else
                        drawUI(rt);
                }
        }

        bool disposed = false;

        public override void despawn() {
            base.despawn();
            if(_ActionInfo != null)
                _ActionInfo.Hide();
                GameObjects.Remove(this);
        }

        protected override void Respawn() {
            base.Respawn();
            if(_ActionInfo != null)
                _ActionInfo.Show();
                GameObjects.Add(this);
        }

        protected override void Dispose(bool disposing) {
            if(!disposed) {
                if(_ActionInfo != null)
                    _ActionInfo.Dispose();
            }
            disposed = true;
            base.Dispose(disposing);
        }

        public void setInputState(byte[] buffer, ref int pos) {
            if(this != Game.instance._player)
                inputstate = buffer.getEnumShort<Controls>(ref pos);
            else
                pos += CustomMaths.shortsize;
        }
        
        public void interact(CharacterBase interactor) {
            Interactor = interactor;
        }

        public override void addToGame() {
            base.addToGame();
                GameObjects.Add(this);
        }
    }
}
