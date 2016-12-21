using Game_Java_Port.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using static Game_Java_Port.GameStatus;
using static System.Windows.Forms.MouseButtons;

using WinCur = System.Windows.Forms.Cursor;

namespace Game_Java_Port {
    partial class GameMenu {

        private abstract class MenuElementListBase : MenuElementBase {
            internal List<MenuElementBase> Children { get; set; } = new List<MenuElementBase>();


            int check = 0;

            private RectangleF _ContentArea;

            internal RectangleF ContentArea {
                get {
                    return _ContentArea;
                }
            }


            public override void draw(RenderTarget rt) {
                drawBorder(rt);
                drawLabel(rt);
            }

            internal override RectangleF calcArea() {
                RectangleF temp = base.calcArea();

                temp.Height = ElementHeight + ElementMargin * 2;

                lock(Children)
                    Children.ForEach(ch => temp.Height = Math.Max(temp.Height, ch.Height + ElementMargin * 2));
                
                return temp;
            }

            internal override RectangleF calcLabelArea() {
                RectangleF temp = base.calcLabelArea();
                temp.Y += (_Area.Height - ElementHeight) / 2;
                return temp;
            }

            public override void update() {
                base.update();
                lock(Children)
                    _Hovering = _Hovering && !Children.Exists(ch => ch.Area.Contains(MousePos));

                _ContentArea = new RectangleF(
                    Area.Left + (int)Parent.LargestStringSize,
                    Area.Top,
                    Area.Width - (int)Parent.LargestStringSize,
                    Area.Height);

            }
        }

        private abstract class MenuElementBase : IRenderable {

            public bool doDrawLabel = true;
            public virtual string Label { get; internal set; } = "";
            private Size2F _LabelSize;
            public Size2F LabelSize { get {
                    //init
                    if(_LabelSize == default(Size2F)) {
                        //measure
                        using(TextLayout tl = new TextLayout(Program.DW_Factory, Label, MenuFont, ScreenWidth, ScreenHeight))
                            _LabelSize = new Size2F(tl.Metrics.Width, tl.Metrics.Height);
                        //no size -> 1,1
                        if(_LabelSize == default(Size2F))
                            _LabelSize = new Size2F(1, 1);
                    }
                    return _LabelSize;
                } }
            internal GameMenu Parent { get; set; }
            private MenuElementListBase _Container;
            internal MenuElementListBase Container {
                get {
                    return _Container;
                }
                set {
                    if(_Container != null) lock(_Container.Children)
                        _Container.Children.Remove(this);
                    if(value != null) {
                        if(value.Children.Contains(this)) lock(value.Children)
                            value.Children.Remove(this);
                        lock(value.Children)
                            value.Children.Add(this);
                        if(!(this is Button))
                            doDrawLabel = false;
                    } else if(!(this is Button))
                        doDrawLabel = true;
                    _Container = value;
                }
            }
            internal int TextYOffset { get { return 1; } }
            internal int TextXOffset { get { return 5; } }

            internal Color? TextColor = null;

            internal virtual void invalidateWidths() {
                if(Container != null) {
                    float temp;
                    lock (Container.Children)
                        _conpos = Container.Children.FindIndex((ele) => ele.Equals(this));
                    temp = Container.ContentArea.Width - ElementMargin * 2;

                    if(this is Button)
                        temp = Math.Max((int)LabelSize.Width, Container.ContentArea.Height - 2 * ElementMargin);
                    else {
                        int buttoncount = 0;

                        foreach(Button btn in Container.Children.FindAll((ele) => ele is Button)) {
                            buttoncount++;
                            temp -= btn._width;
                        }

                        temp /= Container.Children.Count - buttoncount;

                        temp -= Container.Children.Count * ElementMargin;
                    }
                    _width = temp;

                    temp = Container.ContentArea.X + ElementMargin;
                    for(int i = 0; i < _conpos; i++)
                        temp += Container.Children[i]._width + ElementMargin;
                    _x = temp;
                } else {
                    _width = LabelSize.Width;
                }
            }

            internal virtual RectangleF calcArea() {
                invalidateWidths();

                if(Container == null) {
                    float Y = Parent._Y + ElementMargin;
                    lock(Parent.Elements)
                        Parent.Elements.FindAll((ele) =>
                        {
                            return ele.Container == null && ele.Position < Position;
                        }).ForEach((ele) =>
                        {
                            Y += ElementMargin + ele.Area.Height;
                        });

                    return new RectangleF(
                            Parent._X + ElementMargin,
                            Y - Parent.ScrollOffset,
                            Math.Max(_width + 2 * ElementMargin,
                                     Parent._Width - 2 * ElementMargin),
                            ElementHeight);

                } else {
                    return new RectangleF(
                        _x,
                        Container._Area.Y + ElementMargin,
                        _width,
                        ElementHeight);
                }
            }

            virtual internal RectangleF calcLabelArea() {
                RectangleF temp = _Area;
                temp.X += TextXOffset;
                temp.Width -= TextXOffset;
                temp.Y += TextYOffset;
                temp.Height -= TextYOffset;
                return temp;
            }

            virtual public void update() {
                lock(this) {
                    _Area = calcArea();
                    _LabelArea = calcLabelArea();
                }
                _Hovering = _Area.Contains(MousePos);
            }
            //private int _position = 0;
            protected int _elecount = 0;

            internal int Position {
                get {
                    if(Container == null) {
                        lock(Parent.Elements)
                            return Parent.Elements.IndexOf(this);
                    } else
                        return Container.Position;
                }
            }

            protected int _conelecount = 0;
            protected int _conpos = 0;


            protected float _x = 0;


            internal float _width = 0;

            public float Height {
                get {
                    if(_CustomArea != RectangleF.Empty)
                        return _CustomArea.Height;
                    if(_Area != RectangleF.Empty)
                        return _Area.Height;
                    return ElementHeight;
                }
            }

            public Vector2 Location { get { return Area.Location; } set { } }

            private RectangleF _CustomArea;
            internal RectangleF _Area;
            internal RectangleF _LabelArea;

            internal bool _Hovering = false;

            public virtual RectangleF Area {
                get {

                    if(_CustomArea != RectangleF.Empty)
                        return _CustomArea;
                    
                    return _Area;
                }
                set {
                    lock(this)
                        _CustomArea = value;
                }
            }

            public int Z { get; set; } = 1001;

            public DrawType drawType { get; set; } = DrawType.Rectangle;

            #region Drawing

            /// <summary>
            /// Do not call this method manually. It is handled by the renderer.
            /// overrideable.
            /// </summary>
            public virtual void draw(RenderTarget rt) {
                drawHoverHighlight(rt);
                drawBorder(rt);
                drawLabel(rt);
            }

            /// <summary>
            /// Draws the borders of this Menu Element. You can use this method if you override the Customdraw method.
            /// </summary>
            /// <param name="g">the Graphics object from the Customdraw parameter</param>
            internal void drawBorder(RenderTarget rt) { rt.DrawRectangle(Area, MenuBorderPen); }

            /// <summary>
            /// Writes the Label of this Menu Element. You can use this method if you override the Customdraw method.
            /// </summary>
            /// <param name="g">the Graphics object from the Customdraw parameter</param>
            internal void drawLabel(RenderTarget rt) {
                if(doDrawLabel) {
                    if(TextColor != null) {
                        Color4 tempColor = MenuTextBrush.Color;
                        MenuTextBrush.Color = (Color)TextColor;
                        rt.DrawText(Label, MenuFont, _LabelArea, MenuTextBrush);
                        MenuTextBrush.Color = tempColor;
                    } else
                        rt.DrawText(Label, MenuFont, _LabelArea, MenuTextBrush);
                }
            }

            /// <summary>
            /// Highlights the Menu Element if the mouse hovers over it. You can use this method if you override the Customdraw method.
            /// Note that you should call this method before other drawing methods, as this will otherwise draw above them,
            /// rendering the other calls potentially invisible.
            /// </summary>
            /// <param name="g">the Graphics object from the Customdraw parameter</param>
            internal void drawHoverHighlight(RenderTarget rt) { if(_Hovering) rt.FillRectangle(Area, MenuHoverBrush); }
            
            #endregion

            
        }
        
        private class RegulatorButtons<T> : MenuElementListBase where T : struct, IComparable, IConvertible {



            public RegulatorButtons(Regulator<T> regulator, T? stepsize = null) {
                if(stepsize == null)
                    stepsize = (T)Convert.ChangeType((float.Parse(regulator.MaxValue.ToString()) - float.Parse(regulator.MinValue.ToString())) / 20f, typeof(T));
                Parent = regulator.Parent;
                Label = regulator.Label + " "; //add space to be able to seperate from regulator

                //increase button
                Button inc = new Button(Parent, "+", (args) =>
                {
                    if(checkargs(args)) {


                        double result = double.Parse(regulator.Value.ToString()) + double.Parse(stepsize.ToString());
                        double max = double.Parse(CustomMaths.MaxValue<T>().ToString());
                        double min = double.Parse(CustomMaths.MinValue<T>().ToString());
                        if(result < min)
                            regulator.Value = CustomMaths.MinValue<T>();
                        else if(result > max)
                            regulator.Value = CustomMaths.MaxValue<T>();
                        else
                            regulator.Value = (T)Convert.ChangeType(result, typeof(T));

                    }
                });

                //decrease button
                Button dec = new Button(Parent, "-", (args) =>
                {
                    if(checkargs(args)) {
                        double result = double.Parse(regulator.Value.ToString()) - double.Parse(stepsize.ToString());
                        double max = double.Parse(CustomMaths.MaxValue<T>().ToString());
                        double min = double.Parse(CustomMaths.MinValue<T>().ToString());
                        if(result < min)
                            regulator.Value = CustomMaths.MinValue<T>();
                        else if(result > max)
                            regulator.Value = CustomMaths.MaxValue<T>();
                        else
                            regulator.Value = (T)Convert.ChangeType(result, typeof(T));
                    }
                });

                dec.Container = this;
                regulator.Container = this;
                inc.Container = this;
                lock(Parent.Elements)
                    Parent.Elements.Add(this);
            }
        }
        
        private class InputField : MenuElementBase {

            public string Value { get; set; }

            public bool Editing { get; private set; } = false;

            public event Action onValueChange;

            public event Action onFocus;

            public event Action onFocusLost;


            public InputField(GameMenu parent, string label) {
                Label = label;
                Parent = parent;

                Action<onClickArgs> defocus = null;


                Action<onKeyPressArgs> KeyPressAction = (args) =>
                {
                    if(!args.Consumed && args.Down) {
                        //won't handle modifiers
                        if(!(args.Data.Control || args.Data.Alt))
                            args.Consumed = true;
                        else
                            return;


                        if(Value.Length > 0 && args.Data.KeyCode == System.Windows.Forms.Keys.Back) {
                            // Shift + Backspace clears the text, otherwis remove one character
                            if(args.Data.Shift)
                                Value = "";
                            else {
                                Value = Value.Remove(Value.Length - 1);
                                onValueChange?.Invoke();
                            }
                        }

                        if(args.Data.KeyCode >= System.Windows.Forms.Keys.A && args.Data.KeyCode <= System.Windows.Forms.Keys.Z) {
                            int result = (int)args.Data.KeyCode - (int)System.Windows.Forms.Keys.A;
                            if(args.Data.Shift)
                                result += 'A';
                            else
                                result += 'a';
                            Value += (char)result;
                        }

                    }
                };

                defocus = (args) =>
                {
                    if(args.Down && args.Button == Left && !InputArea.Contains(args.Position)) {
                        onFocusLost?.Invoke();
                        Editing = false;
                        onClickEvent -= defocus;
                        onKeyEvent -= KeyPressAction;
                    }
                };

                onClickEvent += (args) =>
                {
                    if(parent.isOpen && args.Down && args.Button == Left && !args.Consumed && InputArea.Contains(args.Position)) {
                        onFocus?.Invoke();
                        Editing = true;
                        onClickEvent += defocus;
                        onKeyEvent += KeyPressAction;
                    }
                };
            }


            public override void draw(RenderTarget rt) {
                drawBorder(rt);
                drawLabel(rt);

                rt.DrawRectangle(InputArea, MenuBorderPen);

                // if editing, make a '_' blink at the end of the text...
                if(!Editing || (Program.stopwatch.ElapsedMilliseconds * 0.003f) % 3 == 0)
                    rt.DrawText(Value, MenuFont, InputArea, MenuTextBrush);
                else
                    rt.DrawText(Value + "_", MenuFont, InputArea, MenuTextBrush);
            }

            RectangleF _InputArea;

            RectangleF InputArea {
                get {
                    return _InputArea;
                }
            }

            public override void update() {
                base.update();
                _InputArea = new RectangleF(
                        Area.Left + (int)Parent.LargestStringSize,
                        Area.Top,
                        Area.Width - (int)Parent.LargestStringSize,
                        Area.Height);
            }

        }

        /// <summary>
        /// This class is for use within the GameMenu.
        /// It represents a bar consisting of a minimum, a maximum and a value.
        /// The user can change the value between the minimum and maximum by dragging.
        /// Has various events available for additinal functionality.
        /// </summary>
        /// <typeparam name="T">The type of the value. not required, automatically determined.</typeparam>
        private class Regulator<T> : MenuElementBase where T : struct, IComparable, IConvertible {

            #region Layout Data

            private Ellipse _Cursor;

            private RectangleF _ValueArea;
            private RectangleF _MaxValueArea;
            private RectangleF _MinValueArea;
            private Vector2 _SepTop;
            private Vector2 _SepBot;

            #endregion
            
            /// <summary>
            /// Datastorage for the Value property.
            /// </summary>
            private T _val;

            //Properties of this class. From area rectangles to generic values.
            #region Properties

            /// <summary>
            /// Is true as long as the user is dragging the bar of the regulator.
            /// </summary>
            public bool IsRegulating { get; private set; } = false;

            /// <summary>
            /// <para>
            /// Returns the Value which the regulator manages.
            /// </para><para>
            /// When changing the Value, various events will be invoked.
            /// </para><para>
            /// If the new one is bigger then the old one, the onIncrease event invokes,
            /// same with the onDecrease event for a lower value.
            /// </para><para>
            /// Is the new Value larger than MaxValue or lower than MinValue,
            /// it will be set to the corresponding limit and either onFull or onEmpty is invoked.
            /// </para><para>
            /// Events will only raise if the new Value actually differs form the old one.
            /// </para><para>
            /// The onChange event is invoked in all of these cases.
            /// </para>
            /// </summary>
            public T Value {
                get {
                    return _val;
                }
                set {
                    //comparsion only has to be done once...
                    int compare = value.CompareTo(Value);
                    if(compare == 0)
                        return; //no difference, cancel

                    if(compare > 0) { //increase
                        // if old value was max there will be no change, cancel
                        if(Value.CompareTo(MaxValue) == 0)
                            return;

                        //at this point the value is guaranteed to be increased
                        onIncrease?.Invoke();

                        //check if max was reached
                        if(value.CompareTo(MaxValue) >= 0) {
                            //max was reached. invoke onFull and set value to max
                            onFull?.Invoke();
                            _val = MaxValue;
                        } else //max not reached
                            _val = value;
                    } else { //decrease

                        // same as with max, look above. no comments for this part any more...
                        if(Value.CompareTo(MinValue) == 0)
                            return;

                        onDecrease?.Invoke();

                        if(value.CompareTo(MinValue) <= 0) {
                            onEmpty?.Invoke();
                            _val = MinValue;
                        } else
                            _val = value;
                    }
                    //if the value didn't change, it would've returned by now. invoke onChange.
                    onChange?.Invoke();
                }
            }

            /// <summary>
            /// The maximum value of this regulator.
            /// if the Value property is set to this one or lower, the onEmpty event is invoked.
            /// </summary>
            public T MinValue { get; set; }

            /// <summary>
            /// The maximum value of this regulator.
            /// if the Value property is set to this one or higher, the onFull event is invoked.
            /// </summary>
            public T MaxValue { get; set; }

            /// <summary>
            /// <para>
            /// Gets a multiplier between 0.00 and 1.00 of the 
            /// value depending on the maximum and minimum
            /// with which the position within the regulator can be determined.
            /// </para><para>
            /// Set this to a value between 0.00 and 1.00 to change
            /// the actual data to a corresponding value.
            /// </para>
            /// 0.00 being the minimum
            /// <para/>
            /// 1.00 being the maximum
            /// </summary>
            private float RelativePos {
                get {
                    float max = float.Parse(MaxValue.ToString());
                    float min = float.Parse(MinValue.ToString());
                    float value = float.Parse(Value.ToString());

                    if(max == min)
                        return 0;

                    return (value - min) / (max - min);
                }
                set {
                    float max = float.Parse(MaxValue.ToString());
                    float min = float.Parse(MinValue.ToString());
                    Value = (T)Convert.ChangeType(Math.Max(value, 0) * (max - min) + min, typeof(T));
                }
            }

            /// <summary>
            /// The regulator is seperate from the label.
            /// The parent also manages the sizes of the other regulators, so we
            /// can get a neatly aligned design.
            /// </summary>
            RectangleF RegulatorArea {
                get {
                    return new RectangleF(
                        Area.Left + (Container == null ? (int)Parent.LargestStringSize : 0) + (int)(Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1]),
                        Area.Top,
                        Area.Width - (Container == null ? (int)Parent.LargestStringSize : 0) - (int)(Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1] + Parent.LrgstRegNumSz[2]),
                        Area.Height);
                }
            }

            #endregion

            //Various events. A Regulator comes with a lot of possibilities!
            #region Events

            /// <summary>
            /// Invoked if Value hits MinValue or less.
            /// </summary>
            public event Action onEmpty;

            /// <summary>
            /// Invoked if Value hits MaxValue or more.
            /// </summary>
            public event Action onFull;

            /// <summary>
            /// Invoked if Value actually changes.
            /// </summary>
            public event Action onChange;

            /// <summary>
            /// Invoked if Value actually increases.
            /// </summary>
            public event Action onIncrease;

            /// <summary>
            /// Invoked if Value actually decreases.
            /// </summary>
            public event Action onDecrease;

            /// <summary>
            /// Invoked after the user finishes dragging the regulator.
            /// </summary>
            public event Action onChangeDone;

            /// <summary>
            /// private action, not really an event.
            /// Set in the constructor, because it needs access to the regulator instance.
            /// purpose is to un-register event handlers from the event to reduce cpu load
            /// and to raise the onchangedone event.
            /// </summary>
            private Action<onClickArgs> stopregulation { get; }

            #endregion

            // Methods of this class (ctor and draw).
            // most functionality is within the events, set in the constructor.
            #region methods


            public Regulator
                (GameMenu parent,
                    string Label,
                    T minValue, T maxValue, T defaultValue,
                    Action onFull = null, Action onEmpty = null,
                    Action onIncrease = null, Action onDecrease = null,
                    Action onChange = null, Action onChangeDone = null,
                    MenuElementListBase List = null
                ) {
                // set data
                this.Label = Label;
                MinValue = minValue;
                MaxValue = maxValue;
                _val = defaultValue;
                Parent = parent;

                // set remotely called action using local data
                stopregulation = (args2) =>
                {
                    if(args2.Button == Left && !args2.Down) {
                        onClickEvent -= stopregulation;
                        IsRegulating = false;
                        this.onChangeDone?.Invoke();
                    }
                };

                // set functionality
                onClickEvent += (args) =>
                {
                    if(parent.isOpen && !args.Consumed && args.Button == Left && args.Down && RegulatorArea.Contains(args.Position)) {
                        IsRegulating = true;

                        onClickEvent += stopregulation;
                    }
                };

                // notify parent for changes
                this.onChange += () => parent.resizeNumbers();
                this.onChangeDone += () => parent.resizeNumbers(true);

                if(onChange != null)
                    this.onChange += onChange;

                if(onChangeDone != null)
                    this.onChangeDone += onChangeDone;

                if(onIncrease != null)
                    this.onIncrease += onIncrease;

                if(onDecrease != null)
                    this.onDecrease += onDecrease;

                if(onFull != null)
                    this.onFull += onFull;

                if(onEmpty != null)
                    this.onEmpty += onEmpty;

                if(List != null)
                    Container = List;

                lock(Parent.Elements)
                    parent.Elements.Add(this);
            }

            /// <summary>
            /// Override of the default draw method.
            /// A lot of changes because of a really complex behaviour.
            /// </summary>
            public override void draw(RenderTarget rt) {

                base.draw(rt);

                //horizontal line
                rt.DrawLine(
                    new Vector2(RegulatorArea.Left + TextXOffset * 2, RegulatorArea.Top + RegulatorArea.Height / 2),
                    new Vector2(RegulatorArea.Right - TextXOffset, RegulatorArea.Top + RegulatorArea.Height / 2),
                    MenuPen);


                //vertical line / marker
                /*
                g.DrawLine(Pens.Aqua,
                    RegulatorArea.Left + RegulatorArea.Width * RelativePos,
                    RegulatorArea.Top + MenuPadding / 2,
                    RegulatorArea.Left + RegulatorArea.Width * RelativePos,
                    RegulatorArea.Bottom - MenuPadding / 2);
                    */
                rt.FillEllipse(_Cursor, MenuPen);

                rt.DrawText(float.Parse(Value.ToString()).ToString(".##"), MenuFont, _ValueArea, MenuTextBrush);

                rt.DrawText(float.Parse(MinValue.ToString()).ToString(".##"), MenuFont, _MinValueArea, MenuTextBrush);

                rt.DrawLine(_SepTop, _SepBot, MenuBorderPen);

                rt.DrawText(float.Parse(MaxValue.ToString()).ToString(".##"), MenuFont, _MaxValueArea, MenuTextBrush);
            }

            public override void update() {
                base.update();
                _Cursor = new Ellipse(
                    new Vector2(RegulatorArea.Left + TextXOffset * 2 + (RegulatorArea.Width - TextXOffset * 3) * RelativePos,
                                RegulatorArea.Top + RegulatorArea.Height / 2),
                    MenuPadding / 3, MenuPadding / 3);
                _ValueArea = new RectangleF(
                    RegulatorArea.Left - Parent.LrgstRegNumSz[0] - Parent.LrgstRegNumSz[1] + TextXOffset,
                    RegulatorArea.Top + TextYOffset,
                    Area.Width,
                    Area.Height);
                _MaxValueArea = new RectangleF(
                    RegulatorArea.Right + TextXOffset,
                    RegulatorArea.Top + TextYOffset,
                    Area.Width,
                    Area.Height);
                _MinValueArea = new RectangleF(
                    RegulatorArea.Left - Parent.LrgstRegNumSz[1] + TextXOffset,
                    RegulatorArea.Top + TextYOffset,
                    Area.Width,
                    Area.Height);
                Vector2 textOffset = new Vector2(TextXOffset / 2, TextYOffset);
                _SepTop = _MinValueArea.TopLeft - textOffset;
                _SepBot = _MinValueArea.BottomLeft - textOffset;

                //change position if user is clicking on the bar
                if(IsRegulating) {
                    // all values are ints, need to cast to get flating point numbers
                    RelativePos = (MousePos.X - RegulatorArea.Left) / RegulatorArea.Width;
                }
            }

            #endregion
        }


        private class TextElement : MenuElementBase {

            private bool _DownScale = false;

            public bool DownScale { get {
                    return _DownScale;
                }
                set {
                    if (value != _DownScale) {
                        _DownScale = value;
                        resize();
                    }
                }
            }

            private Size2F _CustomSize;
            public Size2F CustomSize { get { return _CustomSize; } set {
                    _CustomSize = value;
                    resize();
                }
            }
            private Size2F Size;

            private string _Text;
            internal string Text { get {
                    return _Text;
                } set {
                    _Text = value;
                    resize();
                }
            }


            private void resize() {
                lock(this) {
                    using(TextLayout tl = new TextLayout(Program.DW_Factory, _Text, MenuFont, _CustomSize.Width, _CustomSize.Height))
                        Size = new Size2F(tl.Metrics.Width, tl.Metrics.Height);
                    update();
                }
            }

            public TextElement(GameMenu parent, string Text, Size2F size = default(Size2F), bool downscale = false) {
                if(size == default(Size2F))
                    size = new Size2F(ScreenWidth, ScreenHeight);
                _CustomSize = size;
                Parent = parent;
                lock(Parent.Elements)
                    parent.Elements.Add(this);
                _Text = Text;

                _DownScale = downscale;

                resize();

            }

            public override void draw(RenderTarget rt) {
                drawBorder(rt);
                rt.DrawText(Text, MenuFont, _LabelArea, MenuTextBrush);
            }

            internal override void invalidateWidths() {
                _width = Size.Width;
            }

            internal override RectangleF calcArea() {
                RectangleF temp = base.calcArea();

                temp.Size = new Size2F((DownScale ?
                    Math.Min(Size.Width + 2 * TextXOffset, temp.Width) :
                    Math.Max(Size.Width + 2 * TextXOffset, temp.Width)),
                    Size.Height + ElementMargin);

                return temp;
            }

            internal override RectangleF calcLabelArea() {
                RectangleF temp = base.calcLabelArea();

                temp.Size = new Size2F((DownScale ?
                    Math.Min(Size.Width, temp.Width) :
                    Math.Max(Size.Width, temp.Width)),
                    Size.Height);

                return temp;
            }

        }

        /// <summary>
        /// This class is for use within the GameMenu.
        /// Most simple case.
        /// The user can click on this Element, which will in turn raise an event.
        /// Hover effects included.
        /// </summary>
        private class Button : MenuElementBase, IDisposable {

            /// <summary>
            /// Will run all registered actions when the button is clicked.
            /// The onClickArgs will give more detailed information of the click and it's 'Consumed' property
            /// should be set to 'true' if the click should no longer be used by underlying elements.
            /// </summary>
            public event Action<onClickArgs> onClick;

            /// <summary>
            /// Set in the Constructor, this action is used to determine if the click is addressed to this button
            /// and only raise the real onClick event if that is true.
            /// </summary>
            private Action<onClickArgs> _remoteOnClick;


            public Button(GameMenu parent, string label, Action<onClickArgs> onClickAction = null, bool logButtonClicks = false) {
                // set data
                Label = label;
                Parent = parent;

                //makes the event raise under the condition that it was properly directed to this button.
                _remoteOnClick = (args) =>
                {
                    if(Area.Contains(args.Position) && parent.isOpen && !args.Consumed)
                        onClick?.Invoke(args);
                };

                //register the button on the root clickEvent
                onClickEvent += _remoteOnClick;
                // if the clicks should be logged, add it to the action, same with the functionality
                if(logButtonClicks)
                    onClickAction += (args) => { Console.WriteLine("Button '" + label + "' was pressed."); };

                if(onClickAction != null)
                    onClick += onClickAction;

                //add the button to the list
                lock(Parent.Elements)
                    parent.Elements.Add(this);
            }

            public void Dispose() {
                //unregister the onclick event.
                if(_remoteOnClick != null)
                    onClickEvent -= _remoteOnClick;
            }


        }

        private class IconButton : MenuElementBase, IDisposable {
            Tooltip tooltip;

            private Action<onClickArgs> _remoteOnClick;

            Color IconColor = Color.Transparent;

            ItemBase Item;
            bool isEquipped {
                get {
                    if(Game.state != Game.GameState.Menu)
                        return Item == Game.instance._player.EquippedWeaponL || Item == Game.instance._player.EquippedWeaponR;
                    else
                        return false;
                }
            }



            public IconButton(GameMenu parent, ItemBase item, Action<onClickArgs> onClick = null) {

                //makes the event raise under the condition that it was properly directed to this button.
                _remoteOnClick = (args) =>
                {
                    if(Area.Contains(args.Position) && parent.isOpen && !args.Consumed) {
                        args.Consumed = true;
                        if(args.Button == Right && !args.Down) {
                            if(getKeyState(System.Windows.Forms.Keys.ShiftKey)) {
                                item.Drop();
                                Dispose();
                            } else {
                                if(item is IEquipable && Game.instance._player.getEquipedItem(((IEquipable)item).Slot) != item) {
                                    ((IEquipable)item).Equip(Game.instance._player);
                                } else if(item is IUsable) {
                                    ((IUsable)item).Use(Game.instance._player);
                                }
                            }
                        }
                        onClick?.Invoke(args);
                    }
                };

                //register the button on the root clickEvent
                onClickEvent += _remoteOnClick;
                
                Parent = parent;
                Item = item;
                tooltip = new Tooltip(item.ItemInfoText, Location: () => Area.Center, Validation: () => _Hovering);
                IconColor = GameVars.RarityColors[item.Rarity];
                
                lock(Parent.Elements)
                    Parent.Elements.Add(this);
            }


            public override void draw(RenderTarget rt) {
                drawBorder(rt);
                Color4 temp = MenuHoverBrush.Color;
                MenuHoverBrush.Color = IconColor;
                rt.FillRectangle(Area, MenuHoverBrush);
                MenuHoverBrush.Color = temp;
                temp = MenuTextBrush.Color;
                MenuTextBrush.Color = Color.Black;
                if(Item.image != null)
                    rt.DrawBitmap(Item.image, Area, 1, BitmapInterpolationMode.Linear);
                if(Item is IEquipable)
                    if(Game.instance._player.getEquipedItem(((IEquipable)Item).Slot) == Item)
                        rt.DrawText("E", MenuFont, Area, MenuTextBrush);

                RectangleF coinArea = Area;
                coinArea.Offset(0, 22);
                coinArea.Size = new Size2F(8, 8);
                
                RectangleF offset = Area;
                offset.Offset(8, 18);
                offset = RectangleF.Intersect(Area, offset);

                rt.DrawBitmap(dataLoader.get("Coin.bmp"), coinArea, 1, BitmapInterpolationMode.Linear);
                
                rt.DrawText(Item.SellPrice.ToString(), MenuFont, offset, MenuTextBrush);

                MenuTextBrush.Color = temp;
            }

            internal override RectangleF calcArea() {
                int index = Container.Children.IndexOf(this);
                int columns = ((InventoryElement)Container).Columns;

                int x = index % columns;
                int y = index / columns;

                float size = ((InventoryElement)Container).ElementSize;

                RectangleF temp = new RectangleF(0, 0, size, size);

                temp.Offset(Container.Area.TopLeft + ElementMargin);

                temp.X += x * (size + ElementMargin) + (Container.Area.Width % Container._width) / 2;
                temp.Y += y * (size + ElementMargin);
                
                return temp;
            }

            public override void update() {
                base.update();

                if(_Hovering) {
                    if(getKeyState(System.Windows.Forms.Keys.ShiftKey))
                        Cursor.CursorType = CursorTypes.Inventory_Remove;
                    else if(Item is IEquipable && Game.instance._player.getEquipedItem(((IEquipable)Item).Slot) != Item)
                        Cursor.CursorType = CursorTypes.Inventory_Equip;
                    else if(Item is IUsable)
                        Cursor.CursorType = CursorTypes.Inventory_Use;
                }
                lock(tooltip)
                tooltip.Tick();
            }

            public void Dispose() {
                onClickEvent -= _remoteOnClick;
                lock(Parent.Elements)
                    Parent.Elements.Remove(this);
                lock(Container.Children)
                    Container.Children.Remove(this);
                lock(tooltip)
                    tooltip.Dispose();
            }
        }

        private class InventoryElement : MenuElementListBase, IDisposable {

            private float _ElementSize = 32;

            public int Columns { get; private set; }

            public float ElementSize { get { return _ElementSize; } set {
                    _ElementSize = value;
                    Columns = (int)((_Area.Width - 2 * ElementMargin) % (_ElementSize + ElementMargin));
                }
            }

            public InventoryElement(GameMenu Parent, List<ItemBase> Inventory, float size = 32, float Width = 600, Action<onClickArgs> onClick = null) {
                this.Parent = Parent;
                _ElementSize = size;
                Columns = (int)((Width - 2 * ElementMargin) / (_ElementSize + ElementMargin));
                lock(Parent.Elements)
                    Parent.Elements.Add(this);
                int i = 0;
                int j;
                for(j = 0; j * Columns < Inventory.Count; j++) { // Rows | Y
                    for(i = 0; i + j * Columns < Inventory.Count && i < Columns; i++) { // Columns | X
                        IconButton temp = new IconButton(Parent, Inventory[i + j * Columns], onClick);
                        temp.Container = this;
                    }
                }
            }

            internal override void invalidateWidths() {
                _width = (ElementSize + ElementMargin) * Columns + ElementMargin;
            }

            internal override RectangleF calcArea() {
                RectangleF temp = base.calcArea();
                temp.Height = ElementMargin + (Children.Count / Columns + 1) * (ElementMargin + ElementSize);
                return temp;
            }

            public override void draw(RenderTarget rt) {
                drawBorder(rt);
            }

            public void Dispose() {
                List<MenuElementBase> temp = Children.FindAll(ch => ch is IDisposable);
                temp.ForEach(ch =>  ((IDisposable)ch).Dispose());
                lock(Parent.Elements)
                    Parent.Elements.Remove(this);
                Parent = null;
            }
        }
    }
}
