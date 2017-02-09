using System;
using System.Collections.Generic;
using Game_Java_Port.Interface;
using System.Linq;

using static Game_Java_Port.GameStatus;
using static System.Windows.Forms.MouseButtons;
using SharpDX;
using Game_Java_Port.Logics;

namespace Game_Java_Port {
    public partial class GameMenu : IRenderable {

        public const int ScrollBarWidth = 8;
		public const int MenuMargin = 64;

		public const int ElementHeight = 16;
		public const int ElementMargin = 8;
		public const int GroupMargin = 4;

		public bool update = true;

		public void updateRenderData() {
			if (update) {
				update = false;
				#region height
				_height = MenuMargin * 2;
				Elements.FindAll((ele) => ele.Container == null).ForEach((ele) => {
					_height += ElementMargin + (int) ele.Height;
				});
				_trueHeight = _height;
				_height = Math.Min(
					ScreenHeight,
					_height);
				if (_ScrollOffset < 0)
					_ScrollOffset = 0;
				if (_ScrollOffset > _trueHeight - _height)
					_ScrollOffset = _trueHeight - _height;

				#endregion

				#region width

				//default width
				resizeStrings();
				_Width = (int) (
					(Elements.Any() ? Elements.Max((ele) => ele.Area.Width) : 0)
					+ 2 * MenuMargin) - (tooLarge ? ScrollBarWidth : 0);

				// max width
				if (Elements.Any(ele => ele.GetType().Name.StartsWith("Regulator") ||
					(ele is MenuElementListBase && ((MenuElementListBase) ele).Children.Any(ele2 => ele2.GetType().Name.StartsWith("Regulator"))))
				) _Width = ScreenWidth - (tooLarge ? ScrollBarWidth : 0);
				#endregion

				_X = ScreenWidth / 2 - _Width / 2;
				_Y = ScreenHeight / 2 - _height / 2;
				RectangleF temp = new RectangleF(_X, _Y, _Width + (tooLarge ? ScrollBarWidth : 0), Height);
				if (_Area != temp) {
					_Area = temp;
					update = true;
					RenderData.mdl.VertexBuffer.ApplyRectangle(_Area);
					Elements.ForEach(e => e.update = true);
				}
			}
		}

        #region fields

        public event EventHandler onContinue;

        public event EventHandler onOpen;

        public event EventHandler<onClickArgs> OnClick;
        private bool _isOpen;
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        internal int TextYOffset { get { return 1; } }
        internal int TextXOffset { get { return 5; } }

        #endregion
        #region properties
        
        public string Name { get; private set; } = "Unnamed Menu";

        List<MenuElementBase> Elements { get; } = new List<MenuElementBase>();

        public bool isOpen {
            get {
                return _isOpen;
            }
            set {
                if(value)
                    open();
                else
                    close();
            }
        }

        private int _ScrollOffset = 0;
        private int _trueHeight = 0;
        private int _height = 0;
        private int _Y = MenuPadding;
        private int _X = MenuPadding;

        public int ScrollOffset { get { return _ScrollOffset; } }

        int Height {
            get {
                return _height;
            }
        }

        int _Width = 0;

        bool tooLarge {
            get {
                if(ScreenHeight - 2 * MenuPadding < _trueHeight)
                    return true;
                return false;
            }
        }

        private RectangleF _Area;
        public RectangleF Area {
            get {
                return _Area;
            }
            set {
				if (_Area != value) {
					_Area = value;
					update = true;
				}
			}
        }
		private int _LargestStringSize = 0; 
		public int LargestStringSize
		{
			get { return _LargestStringSize; }
			set {
				if (_LargestStringSize != value) {
					_LargestStringSize = value;
					update = true;
				}
			}
		}

		private int[] _LrgstRegNumSz = {0,0,0};
		public int[] LrgstRegNumSz
		{
			get { return _LrgstRegNumSz; }
			set {
				if(_LrgstRegNumSz != value) {
					_LrgstRegNumSz = value;
					update = true;
				}
			}
		}

		/*
        public int Z { get; set; } = 1000;

        public DrawType drawType { get; set; } = DrawType.Rectangle;
		*/

		public RenderData RenderData { get; set; }

        #endregion
        #region constants

        private const bool _log = false;
        #endregion
        #region Generation
        #region constructor

        /// <summary>
        /// This class will use presets. no need for a public constructor.
        /// </summary>
        private GameMenu() {
            onScrollEvent += (obj, args) =>
            {
                _ScrollOffset -= args.Delta / 10;
				update = true;
            };
            onClickEvent += (obj, args) =>
            {
                if(OnClick != null)
                    OnClick.Invoke(this, args);
            };
			RenderData = new RenderData
			{
				mdl = Model.Square,
				ResID = dataLoader.getResID("m_menu"),
				ResID2 = dataLoader.getResID("t_epic")
			};
        }

        #endregion

        #endregion
        #region methods




        public void resizeNumbers(bool makeSmaller = false) {

            int[] curMaxSizes = new int[] { 0, 0, 0 };
                foreach(MenuElementBase element in Elements) {

                    Type eletype = element.GetType();
                    Type[] types = eletype.GenericTypeArguments;

                    if(types != null && types.Length == 1 && types[0].IsPrimitive && eletype.Name.StartsWith("Regulator`")) {
                        //Regulator<> regElement;
                        object max = null;
                        object min = null;
                        object val = null;


                        switch(types[0].Name) {
                            case "Int16":
                                max = ((Regulator<short>)element).MaxValue;
                                min = ((Regulator<short>)element).MinValue;
                                val = ((Regulator<short>)element).Value;
                                break;
                            case "Int32":
                                max = ((Regulator<int>)element).MaxValue;
                                min = ((Regulator<int>)element).MinValue;
                                val = ((Regulator<int>)element).Value;
                                break;
                            case "Double":
                                max = ((Regulator<double>)element).MaxValue;
                                min = ((Regulator<double>)element).MinValue;
                                val = ((Regulator<double>)element).Value;
                                break;
                            case "Int64":
                                max = ((Regulator<long>)element).MaxValue;
                                min = ((Regulator<long>)element).MinValue;
                                val = ((Regulator<long>)element).Value;
                                break;
                            case "Single":
                                max = ((Regulator<float>)element).MaxValue;
                                min = ((Regulator<float>)element).MinValue;
                                val = ((Regulator<float>)element).Value;
                                break;
                            case "UInt32":
                                max = ((Regulator<uint>)element).MaxValue;
                                min = ((Regulator<uint>)element).MinValue;
                                val = ((Regulator<uint>)element).Value;
                                break;
                            case "UInt64":
                                max = ((Regulator<ulong>)element).MaxValue;
                                min = ((Regulator<ulong>)element).MinValue;
                                val = ((Regulator<ulong>)element).Value;
                                break;
                            default:
                                throw new NotImplementedException("Cast not implemented yet: " + types[0].Name);
                        }
                        int[] numsize = new int[3];

                    
                            numsize[0] = SpriteFont.DEFAULT.MeasureString(float.Parse(val.ToString()).ToString("0.##")).Width;
                            numsize[1] = SpriteFont.DEFAULT.MeasureString(float.Parse(min.ToString()).ToString("0.##")).Width;
                            numsize[2] = SpriteFont.DEFAULT.MeasureString(float.Parse(max.ToString()).ToString("0.##")).Width;

                        for(int i = 0; i < 3; i++) {
                            if(makeSmaller) {
                                if(numsize[i] > curMaxSizes[i])
                                    curMaxSizes[i] = numsize[i];
                            } else {
                                if(numsize[i] > LrgstRegNumSz[i])
                                    LrgstRegNumSz[i] = numsize[i];
                            }
                        }
                        if(makeSmaller)
                            LrgstRegNumSz = curMaxSizes;
                    }
                }
                foreach(MenuElementBase ele in Elements)
                    ele.invalidateWidths();
        }

        public void resizeStrings() {
                foreach(MenuElementBase element in Elements) {
                    int size = 0;

                    size = SpriteFont.DEFAULT.MeasureString(element.Label).Width;
                

                    if(size + 2 * TextXOffset > LargestStringSize)
                        LargestStringSize = size + 2 * TextXOffset;
                }
                foreach(MenuElementBase ele in Elements)
                    ele.invalidateWidths();
        }

        public string getInputValue(string Label) {
                foreach(InputField input in Elements.FindAll((ele) => ele is InputField)) {
                    if(input.Label == Label)
                        return input.Value;
                }
                foreach(MenuElementListBase container in Elements.FindAll((ele) => ele is MenuElementListBase)) {
                    foreach(InputField input in container.Children.FindAll((ele) => ele is InputField)) {
                        if(input.Label == Label)
                            return input.Value;
                    }
                }

            return null;
        }

        /// <summary>
        /// Looks for a Regulator with given Label and returns it's Value.
        /// </summary>
        /// <typeparam name="T">The type the Regulator maintains</typeparam>
        /// <param name="Label">The Label of the Regulator to look for</param>
        /// <returns>Returns the Value of the Regulator or null, if it was not found</returns>
        public T? getRegulatorValue<T>(string Label) where T : struct, IComparable, IConvertible {
                foreach(Regulator<T> reg in Elements.FindAll((obj) => obj is Regulator<T>)) {
                    if(reg.Label == Label)
                        return reg.Value;
                }

                foreach(MenuElementListBase container in Elements) {
                    foreach(Regulator<T> reg in container.Children.FindAll((obj) => obj is Regulator<T>)) {
                        if(reg.Label == Label)
                            return reg.Value;
                    }
            }
            return null;
        }

        /// <summary>
        /// Checks the arguments if the button was pressed instead of released and if it was the left button.
        /// if the conditions are met, the event is consumed.
        /// </summary>
        /// <param name="args">the event arguments</param>
        /// <returns>true, if the event was valid, false otherwise</returns>
        private static bool checkargs(onClickArgs args) {
            if(args.Down && args.Button == Left) {
                args.Consumed = true;
                return true;
            }
            return false;
        }

        public void open() {
            onOpen?.Invoke(this, EventArgs.Empty);
			this.register();
			Elements.ForEach(e => e.register());
            _isOpen = true;
        }
        public void close() {
			Elements.ForEach(e => e.unregister());
			this.unregister();
			_isOpen = false;
        }

        private void addInput(
            string Label, string defaultValue = "",
            Action onFocus = null, Action onFocusLost = null,
            Action onChange = null) {
            InputField temp = new InputField(this, Label);
            temp.Value = defaultValue;

            if(onChange != null)
                temp.onValueChange += onChange;

            if(onFocus != null)
                temp.onFocus += onFocus;

            if(onFocusLost != null)
                temp.onFocusLost += onFocusLost;
            
                Elements.Add(temp);
        }
        

        #endregion
    }
}