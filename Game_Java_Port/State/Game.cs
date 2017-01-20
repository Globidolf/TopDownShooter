using Game_Java_Port.Interface;
using Game_Java_Port.Logics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Game_Java_Port.GameStatus;
using static System.BitConverter;

namespace Game_Java_Port {
    class Game : ITickable, IRenderable, IDisposable {

        [Flags]
        public enum GameState {
            Normal =        0,
            Paused =        1,
            Menu =          1 << 1,
            Multiplayer =   1 << 2,
            Host =          1 << 3
        }
        
        public static GameState state {
            get {
                GameState _state = GameState.Normal;
                if(instance._player == null)
                    _state = GameState.Menu;
                else {
                    if(Paused)
                        _state |= GameState.Paused;
                    if(instance._client != null) {
                        _state |= GameState.Multiplayer;
                        if(instance.GameHost != null)
                            _state |= GameState.Host;
                    }
                }

                return _state;
            }
        }

        public static readonly Game instance = new Game();
        
        public Host GameHost;

        public bool statechanged = false;

        public Dictionary<string, float> _messages = new Dictionary<string, float>();

        public GameClient _client;
        public NPC _player;

        public AngleSingle LastAimDirection = new AngleSingle();

        RectangleF _Area;

        public RectangleF Area { get {
                if (_Area == RectangleF.Empty ||_Area.Height != ScreenHeight || _Area.Width != ScreenWidth)
                    _Area = new RectangleF(0, 0, ScreenWidth, ScreenHeight);
                return _Area;
            } set { _Area = value.Floor(); } }

        public Vector2 Location {
            get {
                    return new Vector2(ScreenWidth / 2, ScreenHeight / 2);
            }
            set { }
        }

        public int Z { get; set; }

        public DrawType drawType { get; set; } = DrawType.Rectangle;

        public void addMessage(string msg) {
            if(msg == null)
                throw new ArgumentException("Message must not be null!", "msg");
            if(_messages.ContainsKey(msg)) {
                int i = 2;
                while(_messages.ContainsKey(msg + "(" + i + ")"))
                    i++;
                _messages.Add(msg + "(" + i + ")", GameVars.messageLifeTime);
            } else
                _messages.Add(msg, GameVars.messageLifeTime);
        }
        public void Host(int port) {
            GameHost = new Host(port);
            Connect("localhost", port);
            addMessage("listening on port " + port);
        }



        public void Connect(string IP, int port) {
            _client = new GameClient(IP, port);
        }



        public void Tick() {

            if(!disposed) {
                for(int i = 0; i < _messages.Count; i++) {
                    _messages[_messages.Keys.ElementAt(i)] -= TimeMultiplier;
                    if (_messages[_messages.Keys.ElementAt(i)] <= 0) {
                        _messages.Remove(_messages.Keys.ElementAt(i));
                    }
                }
                switch(state) {
                    case GameState.Normal:
                        int count;
                        count = GameSubjects.Count;

                        if(RNG.NextFloat(0, 0.3f / TimeMultiplier) <= 1) {
                            NPC rndSpwn = new NPC(_player.Level, add: false);
                            float range = rndSpwn.EquippedWeaponR != null ? rndSpwn.WeaponRangeR : rndSpwn.MeleeRangeR;
                            range = rndSpwn.EquippedWeaponL != null ? Math.Max(range, rndSpwn.WeaponRangeL) : range;
                            range = Math.Max(range, rndSpwn.Size);
                            rndSpwn.Location = new Vector2(
                                (RNG.Next(2) == 0 ? -1 : 1) * (ScreenWidth / 2 + range) + _player.Location.X,
                                (RNG.Next(2) == 0 ? -1 : 1) * (ScreenHeight / 2 + range) + _player.Location.Y);
                            Program.DebugLog.Add("Adding Subject " + rndSpwn.ID + ". Game.Tick().");
                            rndSpwn.addToGame();
                        }
                        break;
                    case GameState.Host | GameState.Multiplayer:
                        List<CharacterBase> players;
                        players = GameSubjects.FindAll((subj) => subj.Team == FactionNames.Players);
                        players.ForEach((subj) =>
                        {
                            if(GameHost._HostGenRNG.NextFloat(0, 3 / TimeMultiplier) <= 1) {
                                NPC rndSpwn = new NPC(subj.Level, add: false);
                                float range = rndSpwn.EquippedWeaponR != null ? rndSpwn.WeaponRangeR : rndSpwn.MeleeRangeR;
                                rndSpwn.Location = new Vector2(
                                    (GameHost._HostGenRNG.Next(2) == 0 ? -1 : 1) * (ScreenWidth / 2 + range) + subj.Location.X,
                                    (GameHost._HostGenRNG.Next(2) == 0 ? -1 : 1) * (ScreenHeight / 2 + range) + subj.Location.Y);
                                _client.send(GameClient.CommandType.add, Serializers.NPCSerializer.Serial(rndSpwn));
                            }
                        });
                        break;
                }
            }
        }

        public byte[] SerializePos() {
            List<byte> cmd = new List<byte>();

            cmd.AddRange(GetBytes(_player.ID));
            cmd.AddRange(GetBytes(_player.Area.Center.X));
            cmd.AddRange(GetBytes(_player.Area.Center.Y));
            return cmd.ToArray();
        }
        
        public byte[] SerializeSpeed() {
            List<byte> cmd = new List<byte>();

            cmd.AddRange(GetBytes(_player.ID));
            cmd.AddRange(GetBytes(_player.MovementVector.X));
            cmd.AddRange(GetBytes(_player.MovementVector.Y));
            return cmd.ToArray();
        }

        public byte[] SerializeInputState() {
            List<byte> cmd = new List<byte>();

            
            cmd.AddRange(GetBytes(_player.ID));
            cmd.AddRange(GetBytes((short)_player.inputstate));
            return cmd.ToArray();
        }
        
        public byte[] SerializeAimDir() {
            List<byte> cmd = new List<byte>();

            cmd.AddRange(GetBytes(_player.ID));
            cmd.AddRange(GetBytes(_player.AimDirection.Radians));
            return cmd.ToArray();
        }

        public void draw(DeviceContext rt) {

            List<string> temp = new List<string>();
             if (!disposed) {
                temp.AddRange(_messages.Keys);
            }
            temp.Reverse();
            



            int i = 0;

            Color menucolor = (Color)(Color4)MenuTextBrush.Color;

            // double cast resets alpha channel
            MenuTextBrush.Color = (Color)(Color3)Color4.Negate(MenuTextBrush.Color);

            MenuTextBrush.Opacity = 0.8f;

            //rt.FillRectangle(new RectangleF(20, ScreenHeight - Math.Min(temp.Count * 20,400), ScreenWidth / 2 - 20, Math.Min(temp.Count * 20, 400)), MenuTextBrush);


            MenuTextBrush.Color = menucolor;
            MenuTextBrush.Opacity = 1;

            foreach(string msg in temp) {
                if(i * 20 < 400 && msg != null)
                    SpriteFont.DEFAULT.directDrawText(msg, new RectangleF(20, ScreenHeight - ++i * 20, ScreenWidth / 2 - 20, 400 - (i - 1) * 20), rt);
                //rt.DrawText(msg, MenuFont, new RectangleF(20, ScreenHeight - ++i * 20, ScreenWidth/2 - 20, 400 - (i-1) * 20),MenuTextBrush);
            }
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    if(GameHost != null) {
                        GameHost.Listen = false;
                            GameHost.Dispose();
                    }
                    if(_client != null) {
                        _client.Listen = false;
                            _client.Dispose();
                    }
                    _messages = null;
                    _player = null;
                }
                disposed = true;
            }
        }
        public void Dispose() {
            Dispose(true);
        }
        #endregion
    }
}
