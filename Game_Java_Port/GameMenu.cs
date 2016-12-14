using System;
using System.Collections.Generic;
using Game_Java_Port.Interface;
using System.Linq;

using static Game_Java_Port.GameStatus;
using static System.Windows.Forms.MouseButtons;
using SharpDX;
using SharpDX.Direct2D1;

namespace Game_Java_Port {
    public partial class GameMenu : IRenderable, ITickable {

        public const int ScrollBarWidth = 8;

        #region fields

        public event Action onContinue;

        public event Action onOpen;

        public event Action<onClickArgs> OnClick;
        private bool _isOpen;
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        #endregion
        #region properties

        public static System.Drawing.Font _menufont = new System.Drawing.Font(System.Drawing.FontFamily.Families[0], 12);
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

        private float _ScrollOffset = 0;
        private float _trueHeight = 0;
        private float _height = 0;
        private float _Y = MenuPadding;
        private float _X = MenuPadding;

        public float ScrollOffset { get { return _ScrollOffset; } }

        float Height {
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
            set { _Area = value; }
        }

        public float LargestStringSize { get; set; } = 0;

        public float[] LrgstRegNumSz { get; set; } = new float[] { 0, 0, 0 };

        #endregion
        #region constants

        public const int ElementHeight = 20;
        public const int ElementMargin = 5;
        public const int GroupMargin = 3;

        private const bool _log = false;
        #endregion
        #region Generation
        #region constructor

        /// <summary>
        /// This class will use presets. no need for a public constructor.
        /// </summary>
        private GameMenu() {
            onScrollEvent += (args) =>
            {
                _ScrollOffset -= args.Delta / 10;
            };
            onClickEvent += (args) =>
            {
                if(OnClick != null)
                    OnClick.Invoke(args);
            };
        }

        #endregion

        #endregion
        #region methods


        public void resizeNumbers(bool makeSmaller = false) {

            float[] curMaxSizes = new float[] { 0, 0, 0 };
            lock(Elements)
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
                        float[] numsize;


                        lock(TextRenderer) {
                            numsize = new float[] {
                                TextRenderer.MeasureString(float.Parse(val.ToString()).ToString("0.##"),_menufont).Width,
                                TextRenderer.MeasureString(float.Parse(min.ToString()).ToString("0.##"),_menufont).Width,
                                TextRenderer.MeasureString(float.Parse(max.ToString()).ToString("0.##"),_menufont).Width
                            };
                        }

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
            lock(Elements)
                foreach(MenuElementBase ele in Elements)
                    ele.invalidateWidths();
        }

        public void resizeStrings() {
            lock(Elements) {
                foreach(MenuElementBase element in Elements) {

                    float size;
                    lock(TextRenderer)
                        size = TextRenderer.MeasureString(element.Label, _menufont).Width;
                    if(size > LargestStringSize)
                        LargestStringSize = size;
                }
                foreach(MenuElementBase ele in Elements)
                    ele.invalidateWidths();
            }
        }

        public string getInputValue(string Label) {
            lock(Elements)
                foreach(InputField input in Elements.FindAll((ele) => ele is InputField)) {
                    if(input.Label == Label)
                        return input.Value;
                }
            lock(Elements)
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
            lock(Elements) {
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
            onOpen?.Invoke();
            //code is soooo great, after three ticks all is in constant order.
            //... so do three ticks before opening the menu to make it appear cleanly
            Tick();
            Tick();
            Tick();
            addRenderable(this);
            _isOpen = true;
        }
        public void close() {
            removeRenderable(this);
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

            lock(Elements)
                Elements.Add(temp);
        }


        Brush black = new SolidColorBrush(Program._RenderTarget, Color.Black);

        public void draw(RenderTarget rt) {
            //draws the border of the menu and each element afterwards

            rt.FillRectangle(Area, black);
            rt.DrawRectangle(Area, MenuBorderPen);
            lock(Elements)
                Elements.ForEach((ele) => ele.draw(rt));
        }

        public void Tick() {
            update();
            lock(Elements)
                Elements.ForEach((ele) => ele.update());
        }

        public void update() {

            #region height
            _height = ElementMargin;
            lock(Elements)
                Elements.FindAll((ele) => ele.Container == null).ForEach((ele) =>
                {
                    _height += ElementMargin + ele.Height;
                });
            _trueHeight = _height;
            _height = Math.Min(
                ScreenHeight - 2 * MenuPadding,
                _height);
            if(_ScrollOffset < 0)
                _ScrollOffset = 0;
            if(_ScrollOffset > _trueHeight - _height)
                _ScrollOffset = _trueHeight - _height;

            #endregion

            #region width

            //default width
            resizeStrings();
            _Width = (int)(LargestStringSize + 4 * ElementMargin) - (tooLarge ? ScrollBarWidth : 0);

            // max width
            lock(Elements)
                if(Elements.Any //any start
                    ((ele) =>
                    {
                        bool result = ele.GetType().Name.StartsWith("Regulator");
                        if(ele is MenuElementListBase) {
                            result = result || ((MenuElementListBase)ele).Children.Any((ele2) => ele2.GetType().Name.StartsWith("Regulator"));
                        }
                        return result;
                    })          //any end
                    ) {                 // if block start
                    _Width = ScreenWidth - 2 * MenuPadding - (tooLarge ? ScrollBarWidth : 0);
                }                       // if block end
                else if(Elements.Any((ele) => ele is MenuElementListBase)) {
                    Elements.FindAll((ele) => ele is MenuElementListBase).ForEach((ele) =>
                    {
                    // min width
                    int tempWidth = 3 * ElementMargin + (int)LargestStringSize;
                        ((MenuElementListBase)ele).Children.ForEach((chl) =>
                        {
                            tempWidth += (int)chl.Area.Width + ElementMargin;
                        });
                        if(tempWidth > _Width)
                            _Width = tempWidth;
                    });
                }

            #endregion

            _X = ScreenWidth / 2 - _Width / 2;
            _Y = ScreenHeight / 2 - _height / 2;
            _Area = new RectangleF(_X, _Y, _Width + (tooLarge ? ScrollBarWidth : 0), Height);
        }

        #endregion
    }
}