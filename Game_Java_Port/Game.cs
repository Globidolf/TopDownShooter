using Game_Java_Port.Interface;
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
    class Game : ITickable, IRenderable {

        public enum GameState {
            Menu,
            SinglePlayer,
            Host,
            Client
        }
        
        public static GameState state {
            get {
                if(instance._player == null)
                    return GameState.Menu;
                if(instance.GameHost != null)
                    return GameState.Host;
                if(instance._client != null)
                    return GameState.Client;
                return GameState.SinglePlayer;
            }
        }

        public static readonly Game instance = new Game();
        
        public Host GameHost;

        public bool statechanged = false;

        public List<string> _messages = new List<string>();

        public GameClient _client;
        public NPC _player;

        public AngleSingle LastAimDirection = new AngleSingle();
        private Controls _lastState = Controls.none;

        RectangleF _Area;

        public RectangleF Area { get {
                if (_Area == RectangleF.Empty ||_Area.Height != ScreenHeight || _Area.Width != ScreenWidth)
                    _Area = new RectangleF(0, 0, ScreenWidth, ScreenHeight);
                return _Area;
            } set { _Area = value; } }

        public Vector2 Location {
            get {
                    return new Vector2(ScreenWidth / 2, ScreenHeight / 2);
            }
            set { }
        }

        public void addMessage(string msg) {
            lock(_messages)
                _messages.Add(msg);
            Timer timer = null;
            timer = new Timer((obj) =>
            {
                lock(_messages)
                    _messages.Remove(msg);
                timer.Dispose();
            });
            timer.Change(GameVars.messageLifeTime,Timeout.Infinite);
        }

        private bool justPressed(Controls input) {
            if(!_lastState.HasFlag(input) && getInputState().HasFlag(input)) {
                return true;
            }
            return false;
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
            //pre-lock check (prevent exception)
            if (instance._player != null) {
                lock(instance._player) lock (GameSubjects) {
                    //post lock check (actual validation)
                    if(instance._player != null && instance._player.Health <= 0 && GameSubjects.Contains(instance._player)) {
                        NPC tempPlayer = instance._player;
                        Timer timer = null;
                        instance.addMessage("You died. Lost all Exp. Respawning in 5 seconds...");
                        tempPlayer.Location = (instance.GameHost != null ? GameHost._HostGenRNG : RNG).NextVector2(new Vector2(-10000, -10000), new Vector2(10000, 10000));
                        tempPlayer.Health = tempPlayer.MaxHealth;
                        tempPlayer.Exp = 0;
                        tempPlayer.removeFromGame();
                        timer = new Timer((obj) =>
                        {
                            tempPlayer.addToGame();
                            timer.Dispose();
                        });
                        timer.Change(5000, Timeout.Infinite);
                    }
                }
            }
            lock(GameSubjects) {
                //singleplayer Game
                if(_client == null && _player != null) {
                    if(RNG.Next((int)GameVars.defaultGTPS * GameSubjects.Count / 3) == 0) {
                        NPC rndSpwn = new NPC(_player.Level, add: false);
                        float range = rndSpwn.EquippedWeaponR != null ? rndSpwn.RWeaponRange : rndSpwn.RMeleeRange;
                        rndSpwn.Location = new Vector2(
                            (RNG.Next(2) == 0 ? -1 : 1) * (ScreenWidth / 2 + range) + _player.Location.X,
                            (RNG.Next(2) == 0 ? -1 : 1) * (ScreenHeight / 2 + range) + _player.Location.Y);
                        rndSpwn.addToGame();
                    }
                } else if(GameHost != null) {
                    GameSubjects.FindAll((subj) => subj.Team == FactionNames.Players).ForEach((subj) =>
                    {
                        if(GameHost._HostGenRNG.Next((int)GameVars.defaultGTPS * GameSubjects.Count / 3) == 0) {
                            NPC rndSpwn = new NPC(_player.Level, add: false);
                            float range = rndSpwn.EquippedWeaponR != null ? rndSpwn.RWeaponRange : rndSpwn.RMeleeRange;
                            rndSpwn.Location = new Vector2(
                                (GameHost._HostGenRNG.Next(2) == 0 ? -1 : 1) * (ScreenWidth / 2 + range) + subj.Location.X,
                                (GameHost._HostGenRNG.Next(2) == 0 ? -1 : 1) * (ScreenHeight / 2 + range) + subj.Location.Y);
                            _client.send(GameClient.CommandType.add, rndSpwn.serialize());
                        }
                    });
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
            cmd.AddRange(GetBytes((short)getInputState()));
            return cmd.ToArray();
        }
        
        public byte[] SerializeAimDir() {
            List<byte> cmd = new List<byte>();

            cmd.AddRange(GetBytes(_player.ID));
            cmd.AddRange(GetBytes(_player.AimDirection.Radians));
            return cmd.ToArray();
        }

        public void draw(RenderTarget rt) {

            List<string> temp = new List<string>();
            lock(_messages) {
                temp.AddRange(_messages);
            }
            temp.Reverse();
            



            int i = 0;

            Color menucolor = (Color)(Color4)MenuTextBrush.Color;

            // double cast resets alpha channel
            MenuTextBrush.Color = (Color)(Color3)Color4.Negate(MenuTextBrush.Color);

            MenuTextBrush.Opacity = 0.8f;

            rt.FillRectangle(new RectangleF(20, ScreenHeight - Math.Min(temp.Count * 20,400), ScreenWidth / 2 - 20, Math.Min(temp.Count * 20, 400)), MenuTextBrush);


            MenuTextBrush.Color = menucolor;
            MenuTextBrush.Opacity = 1;

            foreach(string msg in temp) {
                if(i * 20 < 400)
                    rt.DrawText(msg, MenuFont, new RectangleF(20, ScreenHeight - ++i * 20, ScreenWidth/2 - 20, 400 - (i-1) * 20),MenuTextBrush);
            }
        }
    }
}
