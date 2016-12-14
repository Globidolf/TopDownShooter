﻿
using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Game_Java_Port
{
    public static class GameStatus {

        public static ulong GetFirstFreeID { get {
                ulong newID = 0;
                lock(GameSubjects) {
                    if (GameSubjects.Count > 0)
                        while(GameSubjects.Any((obj) => obj.ID == newID))
                            newID++;
                    return newID;
                }
            } }

        private static System.Threading.Timer tickTimer = new System.Threading.Timer(tick);

        public static List<AttributeBase> GameSubjects { get; } = new List<AttributeBase>();
        public static List<IInteractable> GameObjects { get; } = new List<IInteractable>();
        private static List<ITickable> Tickables { get; } = new List<ITickable>();
        public static List<IRenderable> Renderables { get; } = new List<IRenderable>();

        public static Random RNG { get; set; } = new Random();

        public static DateTime TIME { get; } = DateTime.Now;
        

        //stuff that shouldn't be exposed, to prevent concurrency and/or invalidation of data.
        #region fields

        /// <summary>
        /// A list of all created menus.
        /// This way the Menus won't have to be generated repetively and it allows for some list checks.
        /// </summary>
        private static List<GameMenu> _RegisteredMenus = new List<GameMenu>();

        /// <summary>
        /// State of the currently pressed mouse buttons. Required in the game loop.
        /// </summary>
        private static List<MouseButtons> _mbstate = new List<MouseButtons>();

        /// <summary>
        /// State of the currently pressed keyboard buttons. Required in the game loop.
        /// </summary>
        private static List<Keys> _kbstate = new List<Keys>();

        internal static void MouseWheel(MouseEventArgs args) {
            onScrollEvent?.Invoke(args);
        }


        #endregion

        //custom argument classes
        #region internal classes

        /// <summary>
        /// In addition to the usual KeyEventArgs, this one contains information about the 
        /// state of the key and will update the keyboardstate.
        /// the consumed property can changed accessed after construcion to tell other receivers to ignore the press
        /// as it has been handled already.
        /// </summary>
        public class onKeyPressArgs {
            public bool Consumed { get; set; } = false;
            public bool Down { get; }
            public KeyEventArgs Data { get; }

            //hidden ctor
            private onKeyPressArgs() { }

            //public constructor with key and state
            public onKeyPressArgs(KeyEventArgs data, bool down = true) {
                Data = data;
                Down = down;
                if(Down && !_kbstate.Contains(data.KeyCode))
                    _kbstate.Add(data.KeyCode);
                else if(!Down && _kbstate.Contains(data.KeyCode))
                    _kbstate.Remove(data.KeyCode);
            }
        }

        /// <summary>
        /// a more specific version of the MouseEventArgs.
        /// As with the KeyEventArgs, has a state property (Down) and
        /// a Consumed property, which can be modified for the same purpose.
        /// </summary>
        public class onClickArgs {
            public bool Consumed { get; set; } = false;
            public Vector2 Position { get; }
            public MouseButtons Button { get; }
            public bool Down { get; }

            public onClickArgs(MouseButtons button, Vector2 pos, bool down) {
                Position = pos;
                Button = button;
                Down = down;
            }
        }

        #endregion

        // onclick and onkey. basics for a game...
        #region events

        /// <summary>
        /// Global onclick event. delegates the click to each and every listener that requires it.
        /// </summary>
        public static event Action<onClickArgs> onClickEvent;

        public static event Action<MouseEventArgs> onScrollEvent;

        /// <summary>
        /// Global onKey event. delegates the keypress to each and every listener that requires it.
        /// </summary>
        public static event Action<onKeyPressArgs> onKeyChangeEvent;

        /// <summary>
        /// Global onKey event. delegates the keypress to each and every listener that requires it.
        /// </summary>
        public static event Action<onKeyPressArgs> onKeyEvent;

        #endregion

        // a lot
        #region methods

        /// <summary>
        /// Checks if a mousebutton is currently pressed.
        /// </summary>
        /// <param name="mb">The button to do the check for</param>
        /// <returns>true if pressed, false in all the many other cases.</returns>
        public static bool getButtonState(MouseButtons mb) {
            return _mbstate.Contains(mb);
        }

        public static bool getKeyState(Keys key) {
            return _kbstate.Contains(key);
        }

        public static bool getInputState(Controls control) {
            if(_kbstate.Contains((Keys)GameVars.ControlMapping[control]))
                return true;
            if(_mbstate.Contains((MouseButtons)GameVars.ControlMapping[control]))
                return true;
            return false;
        }

        public static Controls getInputState() {
            Controls state = Controls.none;
            foreach(Controls input in Enum.GetValues(typeof(Controls))) {
                if(input == Controls.none)
                    continue;
                if(_kbstate.Contains((Keys)GameVars.ControlMapping[input]) ||
                    _mbstate.Contains((MouseButtons)GameVars.ControlMapping[input]))
                    state |= input;
            }
            return state;
        }

        /// <summary>
        /// Do not call without parental supervision.
        /// lol jk just dont call, ok?
        /// </summary>
        public static void SetKeyState(KeyEventArgs args, bool Down = true) {
            if(Down && !_kbstate.Contains(args.KeyCode))
                _kbstate.Add(args.KeyCode);
            else if(!Down && _kbstate.Contains(args.KeyCode))
                _kbstate.Remove(args.KeyCode);
            else {
                onKeyEvent?.Invoke(new onKeyPressArgs(args, Down));
                return;
            }
            onKeyEvent?.Invoke(new onKeyPressArgs(args, Down));
            onKeyChangeEvent?.Invoke(new onKeyPressArgs(args, Down));
        }

        /// <summary>
        /// gets the menu with the given name
        /// </summary>
        /// <param name="Name">the name of the menu to look for</param>
        /// <returns>the menu looked for or null if it wasn't found</returns>
        public static GameMenu getMenu(string Name) {
            return _RegisteredMenus.Find((Menu) => { return Menu.Name == Name; });
        }

        /// <summary>
        /// Registers the menu in an internal list.
        /// </summary>
        /// <param name="menu">the menu to register</param>
        public static void addMenu(GameMenu menu) {
            if(!_RegisteredMenus.Contains(menu))
                _RegisteredMenus.Add(menu);
            else
                throw new Exception("A menu was Registered twice. This happens when a fucktard develops a game.");
        } // especially proud of the above line


        /// <summary>
        /// Do not call without parental supervision.
        /// lol jk just dont call, ok?
        /// </summary>
        public static void SetMouseState(MouseEventArgs args, bool Down = true) {

            Matrix3x2 transform = Program._RenderTarget.Transform;

            Vector2 pos = new Vector2(args.X, args.Y) * transform.TranslationVector;
            

            if(Down && !_mbstate.Contains(args.Button))
                _mbstate.Add(args.Button);
            else if(!Down && _mbstate.Contains(args.Button))
                _mbstate.Remove(args.Button);

            onClickEvent?.Invoke(new onClickArgs(args.Button, MousePos, Down));
            //Console.WriteLine("Button " + args.Button.ToString() + " is now " + (Down ? "Down" : "Up"));
        }

        /// <summary>
        /// Checks if the given menu is registered.
        /// </summary>
        /// <param name="request">The menu object to look for</param>
        /// <returns>True if the menu was found, false otherwise</returns>
        public static bool hasMenu(GameMenu request) { return _RegisteredMenus.Contains(request); }
        /// <summary>
        /// Checks if there is a menu with the given name registered
        /// </summary>
        /// <param name="Name">The name to look for</param>
        /// <returns>True if a menu with the name was found, false otherwise</returns>
        public static bool hasMenu(string Name) { return _RegisteredMenus.Exists((Menu) => Menu.Name == Name); }

        #endregion

        /// <summary>
        /// Mouse Position is changed by the onMouseMove event of the game. implemented in the GameScreen.
        /// </summary>
        public static Vector2 MousePos { get; set; }

        // display

        /// <summary>
        /// returns the width of the GameScreen.
        /// </summary>
        public static int ScreenWidth { get { return Program.width; } }
        /// <summary>
        /// returns the height of the GameScreen.
        /// </summary>
        public static int ScreenHeight { get { return Program.height; } }

        /// <summary>
        /// The measurestring method is required, we need an instance to call it.... well and the instance needs a bitmap, so...
        /// </summary>
        public static System.Drawing.Graphics TextRenderer { get; } = System.Drawing.Graphics.FromImage(new System.Drawing.Bitmap(1, 1));

        #region Menus

        // menu settings

        /// <summary>
        /// Pen for the non-text non-border menu parts
        /// </summary>
        public static SolidColorBrush MenuPen { get; } = new SolidColorBrush(Program._RenderTarget, Color.Lerp(Color.Crimson, Color.Black, 0.4f));
        /// <summary>
        /// Pen for the menu borders
        /// </summary>
        public static SolidColorBrush MenuBorderPen { get; } = MenuPen;
        /// <summary>
        /// Brush for the hover effect of the menu
        /// </summary>
        public static SolidColorBrush MenuHoverBrush { get; } = new SolidColorBrush(Program._RenderTarget, Color.Lerp(Color.Crimson, Color.Black, 0.75f)); //CustomMaths.fromArgb(0xff, 0x17, 0x4e, 0x30)
        /// <summary>
        /// Brush for the text of the menu
        /// </summary>
        public static SolidColorBrush MenuTextBrush { get; } = new SolidColorBrush(Program._RenderTarget, CustomMaths.fromArgb(255, 0xff, 0xff, 0xff));//new Pen(Color.FromArgb(0x27, 0xae, 0x60)).Brush;
        /// <summary>
        /// Font for the menu
        /// </summary>
        public static TextFormat MenuFont { get; } = new TextFormat(Program.DW_Factory, System.Drawing.FontFamily.Families[0].Name, 12);
        /// <summary>
        /// Padding for the menu
        /// </summary>
        public static int MenuPadding { get; } = 15;

        // checks

        /// <summary>
        /// If there is any menu that is open this will return true. otherwise false.
        /// </summary>
        public static bool isMenuOpen { get { return _RegisteredMenus.Exists((menu) => { return menu.isOpen; }); } }

        #endregion

        //Runtime

        private static bool _Running;
        public static bool Running { get { return _Running; } set {
                if (value == true && _Running == false) {
                    _Running = value;
                    tickTimer.Change(0, (int)(1000 / GameVars.defaultGTPS));
                } else {
                    _Running = value;
                    tickTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        private static bool _Paused;
        public static bool Paused { get {
                return isMenuOpen || _Paused;
            }
            set {
                _Paused = value;
            }
        }


        public static void removeTickable(ITickable obj) {
            if(Tickables.Contains(obj))
                lock(Tickables) {
                    Tickables.Remove(obj);
                }
        }

        public static void addTickable(ITickable obj) {
            lock(Tickables) {
                Tickables.Add(obj);
            }
        }

        public static void clearTickables() {
            lock(Tickables) {
                Tickables.Clear();
            }
        }

        public static void removeRenderable(IRenderable obj) {
            if(Renderables.Contains(obj))
                lock(Renderables) {
                    Renderables.Remove(obj);
                }
        }

        public static void addRenderable(IRenderable obj) {
            lock(Renderables) {
                Renderables.Add(obj);
            }
        }

        public static void clearRenderables() {
            lock(Renderables) {
                Renderables.Clear();
            }
        }

        public static void tick(object fu) {
            MatrixExtensions.Tick();
            _RegisteredMenus.ForEach((menu) =>
            {
                if(menu.isOpen)
                    menu.Tick();
            });
            if(!Paused) {
                ITickable[] dataCopy;
                lock(Tickables) {
                    dataCopy = Tickables.ToArray();
                }
                foreach(ITickable tickable in dataCopy) {
                    if (tickable != null)
                        tickable.Tick();
                }
            }
        }


        public static byte[] Serialize() {
            List<byte> data = new List<byte>();

            data.AddRange(RNG.saveRNG());
            data.AddRange(NameGen.NameRandomizer.saveRNG());

            lock(GameSubjects) {

                data.AddRange(BitConverter.GetBytes(GameSubjects.Count));

                foreach(AttributeBase obj in GameSubjects) {
                    data.AddRange(((NPC)obj).serialize());
                }
            }

            return data.ToArray();
        }

        public static byte[] serializeStates() {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(GameSubjects.Count));
            lock(GameSubjects) {
                GameSubjects.ForEach((obj) => data.AddRange(obj.serializeState()));
            }
            return data.ToArray();
        }

        public static void reset() {
            if(Game.instance._client != null) {
                Game.instance._client.Listen = false;
                Game.instance._client = null;
            }
            if(Game.instance.GameHost != null) {
                Game.instance.GameHost.Listen = false;
                Game.instance.GameHost.ClientPlayer.Clear();
                Game.instance.GameHost.ClientList.Clear();
                Game.instance.GameHost = null;
            }
            lock(GameSubjects)
                GameSubjects.Clear();
            clearTickables();
            
            clearRenderables();
            addRenderable(Game.instance);
            addTickable(Game.instance);

            Background back = new Background(dataLoader.get("GameBG.bmp"), settings: Background.Settings.Fill_Screen | Background.Settings.Parallax);
            back.ExtendX = ExtendMode.Wrap;
            back.ExtendY = ExtendMode.Wrap;
            GameMenu.MainMenu.open();

        }

        /// <summary>
        /// Stops all threads and then the game.
        /// Will implement some forced data saves to make future life hell for the player.
        /// with this i mean that the game remembers wrongly exiting it and makes it possible
        /// for it to respond accordingly to the player. to bitch about it.
        /// </summary>
        public static void exit() {
            Program.form.Close();
        }

        public static void finalize() {
            reset();
        }
    }
}