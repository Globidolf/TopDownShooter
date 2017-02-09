
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

        private static ulong nextID = 0;

        public static double LastTick, CurrentTick, MsPassed;

        public static ulong GetFirstFreeID { get {
                return nextID++;
            } }


        public static CustomCursor Cursor;

        //private static System.Threading.Timer tickTimer = new System.Threading.Timer(tick);

        /// <summary>
        /// Pair of a CharacterBase and an uint. CharacterBase is a GameSubject with HP at 0. uint is the amount of ticks that have passed since it's death.
        /// </summary>
        public static Dictionary<CharacterBase, float> Corpses { get; } = new Dictionary<CharacterBase, float>();
        public static List<CharacterBase> GameSubjects { get; } = new List<CharacterBase>();
        public static List<IInteractable> GameObjects { get; } = new List<IInteractable>();
        private static List<ITickable> Tickables { get; } = new List<ITickable>();
        public static List<IRenderable> Renderables { get; } = new List<IRenderable>();

        public static Random RNG { get; set; } = new Random();

        /// <summary>
        /// Tells how many seconds have passed since the last tick.
        /// <para>
        /// To scale values over time, simply multiply.
        /// </para><para>
        /// To scale rates - values per second - divide it with this value
        /// </para></summary>
        public static float TimeMultiplier { get { return (float)(CurrentTick - LastTick) / 1000; } }

        public static Controls justPressed = Controls.none;

        private static Controls pendingPresses = Controls.none;


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
            onScrollEvent?.Invoke(null, args);
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
        public class onKeyPressArgs : EventArgs {
            public bool Consumed { get; set; } = false;
            public bool Down { get; }
            public KeyEventArgs Data { get; }

            //hidden ctor
            private onKeyPressArgs() { }

            //public constructor with key and state
            public onKeyPressArgs(KeyEventArgs data, bool down = true) {
                Data = data;
                Down = down;
            }
        }

        /// <summary>
        /// a more specific version of the MouseEventArgs.
        /// As with the KeyEventArgs, has a state property (Down) and
        /// a Consumed property, which can be modified for the same purpose.
        /// </summary>
        public class onClickArgs : EventArgs {
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
        public static event EventHandler<onClickArgs> onClickEvent;

        public static event EventHandler<MouseEventArgs> onScrollEvent;

        /// <summary>
        /// Global onKey event. delegates the keypress to each and every listener that requires it.
        /// </summary>
        public static event EventHandler<onKeyPressArgs> onKeyChangeEvent;

        /// <summary>
        /// Global onKey event. delegates the keypress to each and every listener that requires it.
        /// </summary>
        public static event EventHandler<onKeyPressArgs> onKeyEvent;

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
            onKeyPressArgs args2 = new onKeyPressArgs(args, Down);
            onKeyEvent?.Invoke(null, args2);
            if(Down && !_kbstate.Contains(args.KeyCode)) {
                onKeyChangeEvent?.Invoke(null, args2);
                if(!args2.Consumed) {
                    if(GameVars.ControlMapping.ContainsValue(args.KeyValue))
                        pendingPresses |= GameVars.ControlMapping.First((pair) => pair.Value == args.KeyValue).Key;
                    _kbstate.Add(args.KeyCode);
                }
            } else if(!Down && _kbstate.Contains(args.KeyCode)) {
                onKeyChangeEvent?.Invoke(null, args2);
                if(!args2.Consumed)
                    _kbstate.Remove(args.KeyCode);
            }

            //prevents defocus
            if(args.Alt) {
                args.Handled = true;
                args.SuppressKeyPress = true;
            }
            if(Down) {
                //Manual fullscreen
                if(args.Modifiers == Keys.Alt && args.KeyCode == Keys.Enter) {
                    Program.PrepareToggleFullscreen();
                }
            }
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

            Matrix3x2 transform = Matrix3x2.Transformation(Program.ScreenScale.X, Program.ScreenScale.Y, 0, 0 ,0 );

            Vector2 pos = new Vector2(args.X, args.Y) * transform.TranslationVector;
            
                if(Down && !_mbstate.Contains(args.Button))
                    _mbstate.Add(args.Button);
                else if(!Down && _mbstate.Contains(args.Button))
                    _mbstate.Remove(args.Button);

                onClickEvent?.Invoke(null, new onClickArgs(args.Button, MousePos, Down));
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

        #region Menus

        // menu settings
        
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
                if(value == true && _Running == false) {
                    _Running = value;
                    /*
                    tickThread = new Thread(tickloop);
                    tickThread.Priority = ThreadPriority.BelowNormal;
                    tickThread.Start();
                    */
                } else {
                    _Running = value;
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
                    Tickables.Remove(obj);
        }

        public static void addTickable(ITickable obj) {
                Tickables.Add(obj);
        }

        public static void clearTickables() {
                Tickables.Clear();
        }
		/*
        public static void removeRenderable(IRenderable obj) {
            if(Renderables.Contains(obj))
                    Renderables.Remove(obj);
        }

        public static void addRenderable(IRenderable obj) {
                Renderables.Add(obj);
                //Renderables.Sort(new Comparison<IRenderable>((left, right) => left.Z.CompareTo(right.Z) ));
        }

        public static void clearRenderables() {
            IRenderable[] temp;
                temp = Renderables.ToArray();

            Array.ForEach(temp, (rend) =>
            {
                if(rend is Background)
                    ((Background)rend).Dispose();
            });
            
                Renderables.Clear();
        }
		*/
        public static void tick(bool init) {
            justPressed = pendingPresses;
            pendingPresses = Controls.none;

            Cursor.CursorType = CursorTypes.Normal;
            //Cursor.Tick();
            

            //Cursor.Tick();
            

            if(!Paused || init) {
                ITickable[] Tickables_Copy;
                    Tickables_Copy = Tickables.ToArray();

                foreach(ITickable tickable in Tickables_Copy) {
                    // each tick locks itself when ticking so the next tick has to wait for the previous to complete.
                    if(tickable != null)
                        tickable.Tick();
                }
                // remove all corpses after one minute
                    CharacterBase[] keys = Corpses.Keys.ToArray();

                    foreach(CharacterBase key in keys)
                        Corpses[key] += TimeMultiplier;
                
                    List<KeyValuePair<CharacterBase, float>> Corpses_Copy = Corpses.ToList();
                    Corpses_Copy.FindAll((pair) => pair.Value > 60).ForEach((pair) => Corpses.Remove(pair.Key));
                MatrixExtensions.Tick();
            }
            

            //Cursor.Tick();

            //Cursor.Apply();

            justPressed = Controls.none;
        }

        private static async Task delay(int maxPass = 10) {
            if (MsPassed < maxPass) {
                await Task.Delay(maxPass - (int)MsPassed);
                CurrentTick = Program.stopwatch.Elapsed.TotalMilliseconds;
                MsPassed = CurrentTick - LastTick;
            }
        }


        public static byte[] Serialize() {
            List<byte> data = new List<byte>();

            data.AddRange(RNG.saveRNG());
            data.AddRange(NameGen.NameRandomizer.saveRNG());
            

                data.AddRange(BitConverter.GetBytes(GameSubjects.Count));

                GameSubjects.ForEach((subj) =>
                {
                    subj.Serializer.Serialize(subj);
                });

            return data.ToArray();
        }

        public static byte[] serializeStates() {
            List<byte> data = new List<byte>();

            data.AddRange(BitConverter.GetBytes(GameSubjects.Count));
                GameSubjects.ForEach((obj) => data.AddRange(obj.serializeState()));
            return data.ToArray();
        }

		public static void reset() {

			if (Game.instance._client != null) {
				Game.instance._client.Listen = false;
				Game.instance._client = null;
			}
			if (Game.instance.GameHost != null) {
				Game.instance.GameHost.Listen = false;
				Game.instance.GameHost.ClientPlayer.Clear();
				Game.instance.GameHost.ClientList.Clear();
				Game.instance.GameHost = null;
			}

			CharacterBase[] copy;
			copy = GameSubjects.ToArray();
			Array.ForEach(copy, (subj) => {
				Program.DebugLog.Add("Removing Subject " + subj.ID + ". GameStatus.reset().");
				subj.removeFromGame();
			}
			);
			GameSubjects.Clear();


			GameObjects.Clear();

			Renderer.clear();
			//clearRenderables();

			clearTickables();

			Game.instance._player = null;

			init();
		}

        public static void init() {
            //addRenderable(Game.instance);
            addTickable(Game.instance);
			Cursor.register();
            //addRenderable(Cursor);
            //addTickable(Cursor);

			Background_Tiled back = new Background_Tiled(
				dataLoader.getResID("bg_grass1_8_8"),
				dataLoader.getResID("bg_rock_2_2"),
				new Point(2,2),
				Area: new RectangleF(0,0,Program.width,Program.height),
				settings: Background.Settings.Parallax | Background.Settings.Repeat);// new Background_Tiled(Tileset.BG_Rock, Area: Game.instance.Area, settings: Background.Settings.Parallax | Background.Settings.Fill_Area);

            GameMenu.MainMenu.open();

            tick(true);
        }

        /// <summary>
        /// Stops all threads and then the game.
        /// Will implement some forced data saves to make future life hell for the player.
        /// with this i mean that the game remembers wrongly exiting it and makes it possible
        /// for it to respond accordingly to the player. to bitch about it.
        /// </summary>
        public static void exit() {
            Running = false;
            Program.form.Close();
        }

        public static void finalize() {
            Running = false;
            reset();
        }
    }
}