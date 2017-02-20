using Game_Java_Port.Interface;
using Game_Java_Port.Logics;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using static Game_Java_Port.GameStatus;
using static System.Windows.Forms.MouseButtons;

namespace Game_Java_Port
{
	partial class GameMenu
	{

		private abstract class MenuElementListBase : MenuElementBase
		{
			internal List<MenuElementBase> Children { get; set; } = new List<MenuElementBase>();
			
			internal Rectangle childArea;

			internal override void init() {
				base.init();
				area.Height = Math.Max(area.Height, Children.Max(c => c.area.Height)) + 2 * ElementMargin;
				hover = hover && !Children.Any(c => c.hover);
			}
			internal override void postinit() {
				base.postinit();
				childArea = area;
				int maxlabelwidth = Parent.Elements.Max(e => e.labelwidth);
				childArea.X += maxlabelwidth;
				childArea.Width -= maxlabelwidth;
			}

		}

		private abstract class MenuElementBase : IRenderable
		{
			public const int textOffset = 2;
			public int labelwidth;
			public Rectangle area, labelArea;
			public virtual string Label { get; internal set; } = "";
			internal bool hover,
				updateLabel, updateArea,
				doDrawLabel = true;
			internal Color? TextColor = null;

			/// <summary>
			/// Set final values here using the parents size as reference. <para/>
			/// labelArea is set in the base call of this method.
			/// </summary>
			internal virtual void postinit() {

				area.Width = Parent.width - 2 * MenuMargin;
				area.X = Parent.x + MenuMargin;
				int index = Parent.Elements.IndexOf(Container == null ? this : Container);
				area.Y = index > 0 ? Parent.Elements[index - 1].area.Bottom + ElementMargin : Parent.y + MenuMargin;
				labelArea = new Rectangle
				{
					X = area.X + (area.Width - labelwidth)/ 2,
					Y = area.Y + textOffset,
					Height = area.Height - textOffset * 2,
					Width = labelwidth
				};
				hover = area.Contains(MousePos.X, MousePos.Y);
			}

			internal int subObjCount = 1;

			/// <summary>
			/// Override this method to modify the initial attributes of the element.
			/// <para/>
			///	Initializes the <see cref="RenderData"/> object with it's <see cref="RenderData.SubObjs"/> slots matching <see cref="subObjCount"/>
			///	and the basic <see cref="Model.Square"/> <see cref="Model"/>.
			///	<para/>
			///	!!! IMPORTANT !!! The slot 0 is RESERVED for the <see cref="Label"/>! If you want to add additional items, set subObjCount to the amount + 1 and use indices above 0!
			///	<para/>
			///	The area is set to the labelwidth, clamped between <see cref="MaxElementWidth"/> and <see cref="MinElementWidth"/>.
			///	It's height is set to  <see cref="MinElementHeight"/>.
			/// <para/>
			///	When overriding it is recommended to call base.<see cref="init"/> to set these general values to prevent errors.
			/// </summary>
			internal virtual void init() {
				RenderData = new RenderData { SubObjs = new RenderData[subObjCount], mdl = Model.Square };
				labelwidth = SpriteFont.DEFAULT.MeasureString(Label).Width;
				area.Width = Math.Min(MaxElementWidth, Math.Max(MinElementWidth, MinElementWidth + labelwidth));
				area.Height = MinElementHeight;
			}
			/// <summary>
			/// If <see cref="updateArea"/> is true, this method will apply the current area to the <see cref="RenderData"/> and toggle it back to false.
			/// <para/>
			/// If <see cref="updateLabel"/> is true, this method will remove the <see cref="Label"/> from the <see cref="Renderer"/> and,
			/// if the current label has a value, add the new value to the Renderer.
			/// <para/>
			/// </summary>
			public virtual void updateRenderData() {
				if (updateArea) {
					RenderData.Area = area;
					updateArea = false;
				}
				if (updateLabel) {
					Renderer.remove(RenderData.SubObjs[0]);
					if (doDrawLabel && Label != null && Label.Length > 0) {
						RenderData.SubObjs[0] = SpriteFont.DEFAULT.generateText(Label, labelArea, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip, hover ? Color.Orange : (Color?) null);
						Renderer.add(RenderData.SubObjs[0]);
					}
					updateLabel = false;
				}
			}
			public RenderData RenderData { get; set; }
			
			internal GameMenu Parent { get; set; }
			private MenuElementListBase _Container;
			internal MenuElementListBase Container
			{
				get {
					return _Container;
				}
				set {
					if (_Container != null)
						_Container.Children.Remove(this);
					if (value != null) {
						if (value.Children.Contains(this))
							value.Children.Remove(this);
						value.Children.Add(this);
						if (!(this is Button))
							doDrawLabel = false;
					} else if (!(this is Button))
						doDrawLabel = true;
					_Container = value;
				}
			}

		}

		private sealed class RegulatorButtons<T> : MenuElementListBase where T : struct, IComparable, IConvertible
		{
			public override void updateRenderData() {
				bool ulabel = updateLabel, uarea = updateArea;
				base.updateRenderData();
			}

			internal override void init() {
				base.init();
				RenderData.ResID = dataLoader.getResID("m_frame_default");
				RenderData.mdl.VertexBuffer.ApplyColor(Color.Transparent);
			}

			public RegulatorButtons(Regulator<T> regulator, T? stepsize = null) {
				if (stepsize == null)
					stepsize = (T) Convert.ChangeType((Convert.ToSingle(regulator.MaxValue) - Convert.ToSingle(regulator.MinValue)) / 20f, typeof(T));
				Parent = regulator.Parent;
				Label = regulator.Label + "_rb"; //add space to be able to seperate from regulator

				//increase button
				Button inc = new Button(Parent, "+", (args) =>
				{
					if(checkargs(args)) {
						float result =  Convert.ToSingle(regulator.Value) + Convert.ToSingle(stepsize);
						float max =     Convert.ToSingle(CustomMaths.MaxValue<T>());
						float min =     Convert.ToSingle(CustomMaths.MinValue<T>());
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
						float result =	Convert.ToSingle(regulator.Value) - Convert.ToSingle(stepsize);
						float max =		Convert.ToSingle(CustomMaths.MaxValue<T>());
						float min =		Convert.ToSingle(CustomMaths.MinValue<T>());
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

				Parent.Elements.Add(this);
			}
		}

		private sealed class InputField : MenuElementBase
		{
			internal bool textchanged, focused;
			internal Rectangle inputArea, inputTextArea;
			internal string text;

			public event Action onValueChange, onFocus, onFocusLost;

			internal override void postinit() {
				base.postinit();
				inputArea = area;
				int maxlabelwidth = Parent.Elements.Max(e => e.labelwidth);
				inputArea.Left += maxlabelwidth;
				inputArea.Width -= maxlabelwidth;
			}

			public override void updateRenderData() {
				base.updateRenderData();

				if (textchanged) {

					int maxlabelwidth = Parent.Elements.Max(e => e.labelwidth);
					inputArea.Left += maxlabelwidth;
					inputArea.Width -= maxlabelwidth;
					inputTextArea = inputArea;
					inputTextArea.X += textOffset;
					inputTextArea.Y += textOffset;
					inputTextArea.Width -= textOffset;
					inputTextArea.Height -= textOffset;

					Renderer.remove(RenderData.SubObjs[1]);
					if (text != null && text != "") {
						RenderData.SubObjs[1] = SpriteFont.DEFAULT.generateText(text + " ", inputTextArea, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
						Renderer.add(RenderData.SubObjs[1]);
					}
					textchanged = false;
				}
				RenderData.SubObjs[1].SubObjs[RenderData.SubObjs[1].SubObjs.Length - 1].Frameindex = (((CurrentTick * 1000) % 2) == 0 ? ' ' : '_')-32;
			}

			internal override void init() {
				subObjCount = 2;
				base.init();
				inputArea = area;
				inputArea.Left += Parent.LargestStringSize; new Rectangle(
						area.Left + Parent.LargestStringSize,
						area.Top,
						area.Width - Parent.LargestStringSize,
						area.Height);
				RenderData.ResID = dataLoader.getResID("m_frame_default");

				if (Label != null && Label != "")
					RenderData.SubObjs[0] = SpriteFont.DEFAULT.generateText(Label, labelArea, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip );
				if (text != null && text != "")
					RenderData.SubObjs[1] = SpriteFont.DEFAULT.generateText(text + " ", inputTextArea, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip );
			}



			public InputField(GameMenu parent, string label) {
				Label = label;
				Parent = parent;

				EventHandler<onClickArgs> defocus = null;


				EventHandler<onKeyPressArgs> KeyPressAction = (obj, args) =>
				{
					if(!args.Consumed && args.Down) {
                        //won't handle modifiers
                        if(!(args.Data.Control || args.Data.Alt))
							args.Consumed = true;
						else
							return;


						if(text.Length > 0 && args.Data.KeyCode == System.Windows.Forms.Keys.Back) {
                            // Shift + Backspace clears the text, otherwis remove one character
                            if(args.Data.Shift)
								text = "";
							else {
								text = text.Remove(text.Length - 1);
								onValueChange?.Invoke();
							}
						}

						if(args.Data.KeyCode >= System.Windows.Forms.Keys.A && args.Data.KeyCode <= System.Windows.Forms.Keys.Z) {
							int result = (int)args.Data.KeyCode - (int)System.Windows.Forms.Keys.A;
							if(args.Data.Shift)
								result += 'A';
							else
								result += 'a';
							text += (char)result;
						}

					}
				};

				defocus = (obj, args) => {
					if (args.Down && args.Button == Left && !inputArea.Contains(args.Position.X, args.Position.Y)) {
						onFocusLost?.Invoke();
						focused = false;
						onClickEvent -= defocus;
						onKeyEvent -= KeyPressAction;
					}
				};

				onClickEvent += (obj, args) => {
					if (parent.isOpen && args.Down && args.Button == Left && !args.Consumed && inputArea.Contains(args.Position.X, args.Position.Y)) {
						onFocus?.Invoke();
						focused = true;
						onClickEvent += defocus;
						onKeyEvent += KeyPressAction;
					}
				};
			}
		}
		
		private sealed class Regulator<T> : MenuElementBase where T : struct, IComparable, IConvertible
		{
			bool updateMinValue, updateMaxValue, updateValue, drag;
			Rectangle minValText, ValText, maxValText, regulatorArea, regulatorHandle;
			int regulatorStart, regulatorWidth, regPos;

			private T getPos(float percent, bool clamp = true) {
				if (clamp)
					percent = Math.Max(0, Math.Min(1, percent));
				float max = Convert.ToSingle(MaxValue);
				float min = Convert.ToSingle(MinValue);
				return (T)Convert.ChangeType(percent * (max - min) + min, typeof(T));
			}

			public override void updateRenderData() {
				// keep value for the duration of this method
				bool updateArea = this.updateArea;
				base.updateRenderData();

				if (drag) 
					RelativePos = (MousePos.X - regulatorStart) / regulatorWidth;

				if (updateArea) {
					regulatorArea = new Rectangle(
							area.Left + (Container == null ? Parent.LargestStringSize : 0) + Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1] + 4 * textOffset,
							area.Top,
							area.Width - (Container == null ? Parent.LargestStringSize : 0) - Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1] + Parent.LrgstRegNumSz[2] + 6 * textOffset,
							area.Height);
					ValText = new Rectangle(
						regulatorArea.Left - (Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1] + 3 * textOffset),
						regulatorArea.Top + textOffset,
						area.Width,
						area.Height);
					maxValText = new Rectangle(
						regulatorArea.Right + textOffset,
						regulatorArea.Top + textOffset,
						area.Width,
						area.Height);
					minValText = new Rectangle(
						regulatorArea.Left - Parent.LrgstRegNumSz[1],
						regulatorArea.Top + textOffset,
						area.Width,
						area.Height);
					regulatorHandle = new Rectangle(
						regulatorStart + regPos - 1,
						regulatorArea.Top + 2,
						2,
						regulatorArea.Height - 4);
					

					//apply new positions to the strings and disable the update for the rest of the method, as it has been updated here already
					Renderer.remove(RenderData.SubObjs[2]);
					RenderData.SubObjs[2] = SpriteFont.DEFAULT.generateText(MaxValue.ToString(), maxValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
					Renderer.add(RenderData.SubObjs[2]);
					updateMaxValue = false;

					Renderer.remove(RenderData.SubObjs[1]);
					RenderData.SubObjs[1] = SpriteFont.DEFAULT.generateText(MinValue.ToString(), minValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
					Renderer.add(RenderData.SubObjs[1]);
					updateMinValue = false;

					Renderer.remove(RenderData.SubObjs[3]);
					RenderData.SubObjs[3] = SpriteFont.DEFAULT.generateText(Value.ToString(), ValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
					Renderer.add(RenderData.SubObjs[3]);
					updateValue = false;

					RenderData.SubObjs[4].mdl.VertexBuffer.ApplyRectangle(regulatorArea);
					RenderData.SubObjs[5].mdl.VertexBuffer.ApplyRectangle(regulatorHandle);
					updateArea = false;
				}

				if (updateMaxValue) {
					Renderer.remove(RenderData.SubObjs[2]);
					RenderData.SubObjs[2] = SpriteFont.DEFAULT.generateText(MaxValue.ToString(), maxValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
					Renderer.add(RenderData.SubObjs[2]);
					updateMaxValue = false;
				}
				if (updateMinValue) {
					Renderer.remove(RenderData.SubObjs[1]);
					RenderData.SubObjs[1] = SpriteFont.DEFAULT.generateText(MinValue.ToString(), minValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
					Renderer.add(RenderData.SubObjs[1]);
					updateMinValue = false;
				}
				if (updateValue) {
					Renderer.remove(RenderData.SubObjs[3]);
					RenderData.SubObjs[3] = SpriteFont.DEFAULT.generateText(Value.ToString(), ValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
					Renderer.add(RenderData.SubObjs[3]);
					updateValue = false;
				}

			}

			internal override void postinit() {
				base.postinit();

				regulatorArea = new Rectangle(
						area.Left + (Container == null ? Parent.LargestStringSize : 0) + Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1] + 4 * textOffset,
						area.Top,
						area.Width - (Container == null ? Parent.LargestStringSize : 0) - Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1] + Parent.LrgstRegNumSz[2] + 6 * textOffset,
						area.Height);
				ValText = new Rectangle(
					regulatorArea.Left - (Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1] + 3 * textOffset),
					regulatorArea.Top + textOffset,
					area.Width,
					area.Height);
				maxValText = new Rectangle(
					regulatorArea.Right + textOffset,
					regulatorArea.Top + textOffset,
					area.Width,
					area.Height);
				minValText = new Rectangle(
					regulatorArea.Left - Parent.LrgstRegNumSz[1],
					regulatorArea.Top + textOffset,
					area.Width,
					area.Height);
				regulatorHandle = new Rectangle(
					regulatorStart + regPos - 1,
					regulatorArea.Top + 2,
					2,
					regulatorArea.Height - 4);

				//Todo: Determine max characters to init value text and modify ONLY it's texcoords on updates

				RenderData.SubObjs[1] = SpriteFont.DEFAULT.generateText(MinValue.ToString(), minValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
				RenderData.SubObjs[2] = SpriteFont.DEFAULT.generateText(MaxValue.ToString(), maxValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
				RenderData.SubObjs[3] = SpriteFont.DEFAULT.generateText(Value.ToString(), ValText, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip);
				RenderData.SubObjs[4] = new RenderData
				{
					Area = regulatorArea,
					ResID = dataLoader.getResID("m_bar_1_4"),
					AnimationFrameCount = new Point(1,4),
					Frameindex = 0
				};
				RenderData.SubObjs[5] = new RenderData
				{
					Area = regulatorHandle,
					ResID = dataLoader.getResID("m_symbols_8_8"),
					AnimationFrameCount = new Point(8,8),
					Frameindex = 0
				};
			}

			internal override void init() {
				subObjCount += 6;
				base.init();

				RenderData.ResID = dataLoader.getResID("m_frame_default");
			}
			
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
			public T Value
			{
				get {
					return _val;
				}
				set {
					//comparsion only has to be done once...
					int compare = value.CompareTo(Value);
					if (compare == 0)
						return; //no difference, cancel

					if (compare > 0) { //increase
									   // if old value was max there will be no change, cancel
						if (Value.CompareTo(MaxValue) == 0)
							return;

						//at this point the value is guaranteed to be increased
						onIncrease?.Invoke();

						//check if max was reached
						if (value.CompareTo(MaxValue) >= 0) {
							//max was reached. invoke onFull and set value to max
							onFull?.Invoke();
							_val = MaxValue;
						} else //max not reached
							_val = value;
					} else { //decrease

						// same as with max, see above.
						if (Value.CompareTo(MinValue) == 0)
							return;

						onDecrease?.Invoke();

						if (value.CompareTo(MinValue) <= 0) {
							onEmpty?.Invoke();
							_val = MinValue;
						} else
							_val = value;
					}
					//any case that would've made the value NOT change did return. invoke the onchange event.
					onChange?.Invoke();

					//above code has updated the RelativePos value. update the Regulatorposition for the renderloop and inform that it has updated
					regPos = regulatorStart + (int)(RelativePos * regulatorWidth);
					updateValue = true;
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
			private float RelativePos
			{
				get {
					float max = float.Parse(MaxValue.ToString());
					float min = float.Parse(MinValue.ToString());
					float value = float.Parse(Value.ToString());

					if (max == min)
						return 0;

					return (value - min) / (max - min);
				}
				set {
					float max = float.Parse(MaxValue.ToString());
					float min = float.Parse(MinValue.ToString());
					Value = (T) Convert.ChangeType(Math.Max(value, 0) * (max - min) + min, typeof(T));
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
			private EventHandler<onClickArgs> stopregulation { get; }

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
				stopregulation = (obj, args2) => {
					if (args2.Button == Left && !args2.Down) {
						onClickEvent -= stopregulation;
						IsRegulating = false;
						this.onChangeDone?.Invoke();
					}
				};

				// set functionality
				onClickEvent += (obj, args) => {
					if (parent.isOpen && !args.Consumed && args.Button == Left && args.Down && regulatorArea.Contains(args.Position.X, args.Position.Y)) {
						IsRegulating = true;
						onClickEvent += stopregulation;
					}
				};

				// notify parent for changes
				this.onChange += () => parent.resizeNumbers();
				this.onChangeDone += () => parent.resizeNumbers(true);

				if (onChange != null)
					this.onChange += onChange;

				if (onChangeDone != null)
					this.onChangeDone += onChangeDone;

				if (onIncrease != null)
					this.onIncrease += onIncrease;

				if (onDecrease != null)
					this.onDecrease += onDecrease;

				if (onFull != null)
					this.onFull += onFull;

				if (onEmpty != null)
					this.onEmpty += onEmpty;

				if (List != null)
					Container = List;

				parent.Elements.Add(this);
				init();
			}

			/// <summary>
			/// Override of the default draw method.
			/// A lot of changes because of a really complex behaviour.
			/// </summary>
			//public override void draw(DeviceContext rt) {

				/*
				base.draw(rt);

				//horizontal line
				rt.DrawLine(
					new Vector2(RegulatorArea.Left + TextXOffset * 2, RegulatorArea.Top + RegulatorArea.Height / 2),
					new Vector2(RegulatorArea.Right - TextXOffset, RegulatorArea.Top + RegulatorArea.Height / 2),
					MenuPen);


				//vertical line / marker
                g.DrawLine(Pens.Aqua,
                    RegulatorArea.Left + RegulatorArea.Width * RelativePos,
                    RegulatorArea.Top + MenuPadding / 2,
                    RegulatorArea.Left + RegulatorArea.Width * RelativePos,
                    RegulatorArea.Bottom - MenuPadding / 2);
				rt.FillEllipse(_Cursor, MenuPen);
                SpriteFont.DEFAULT.directDrawText(float.Parse(Value.ToString()).ToString(".##"), _ValueArea, rt);
                SpriteFont.DEFAULT.directDrawText(float.Parse(MinValue.ToString()).ToString(".##"), _MinValueArea, rt);
                SpriteFont.DEFAULT.directDrawText(float.Parse(MaxValue.ToString()).ToString(".##"), _MaxValueArea, rt);
                rt.DrawText(float.Parse(Value.ToString()).ToString(".##"), MenuFont, _ValueArea, MenuTextBrush);
                rt.DrawText(float.Parse(MinValue.ToString()).ToString(".##"), MenuFont, _MinValueArea, MenuTextBrush);
                rt.DrawText(float.Parse(MaxValue.ToString()).ToString(".##"), MenuFont, _MaxValueArea, MenuTextBrush);
				rt.DrawLine(_SepTop, _SepBot, MenuBorderPen);
				*/

			//}
			/*
			public override void update() {
				base.update();
				_Cursor = new Ellipse(
					new Vector2(RegulatorArea.Left + TextXOffset * 2 + (RegulatorArea.Width - TextXOffset * 3) * RelativePos,
								RegulatorArea.Top + RegulatorArea.Height / 2),
					MenuPadding / 3, MenuPadding / 3);
				_ValueArea = new RectangleF(
					RegulatorArea.Left - (Parent.LrgstRegNumSz[0] + Parent.LrgstRegNumSz[1] + 3 * TextXOffset),
					RegulatorArea.Top + TextYOffset,
					Area.Width,
					Area.Height);
				_MaxValueArea = new RectangleF(
					RegulatorArea.Right + TextXOffset,
					RegulatorArea.Top + TextYOffset,
					Area.Width,
					Area.Height);
				_MinValueArea = new RectangleF(
					RegulatorArea.Left - Parent.LrgstRegNumSz[1],
					RegulatorArea.Top + TextYOffset,
					Area.Width,
					Area.Height);
				Vector2 textOffset = new Vector2(TextXOffset, TextYOffset);
				_SepTop = _MinValueArea.TopLeft - textOffset;
				_SepBot = _MinValueArea.BottomLeft - textOffset;

				//change position if user is clicking on the bar
				if (IsRegulating) {
					// all values are ints, need to cast to get flating point numbers
					RelativePos = (MousePos.X - RegulatorArea.Left) / RegulatorArea.Width;
				}
			}
			*/
			#endregion
		}


		private sealed class TextElement : MenuElementBase
		{
			private Rectangle _TextArea;

			private bool updateText;

			public override void updateRenderData() {
				base.updateRenderData();
				if (updateText) {
					Renderer.remove(RenderData.SubObjs[1]);
					if (Text != null && Text != "") {
						RenderData.SubObjs[1] = SpriteFont.DEFAULT.generateText(Text, _TextArea, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip + Renderer.LayerOffset_Text);
						Renderer.add(RenderData.SubObjs[1]);
					}
					updateText = false; 
				}
			}

			internal override void init() {
				subObjCount += 1;
				base.init();
				if (Text != null && Text != "")
					RenderData.SubObjs[1] = SpriteFont.DEFAULT.generateText(Text, _TextArea, Renderer.Layer_Menu + Renderer.LayerOffset_Tooltip + Renderer.LayerOffset_Text);
			}

			public override string Label
			{
				get {
					return Text;
				}

				internal set {
					Text = value;
				}
			}

			private string _Text;
			internal string Text
			{
				get {
					return _Text;
				}
				set {
					if (_Text != value) {
						_Text = value;
						updateText = true;
						Parent.update = true;
					}
				}
			}


			public TextElement(GameMenu parent, string Text, Size2 size = default(Size2)) {
				Parent = parent;
				parent.Elements.Add(this);
				_TextArea = new Rectangle(area.X, area.Y, size.Width, size.Height);
				this.Text = Text;
				init();
			}

			internal override Rectangle calcArea() {
				Rectangle temp = base.calcArea();

				temp.Width = temp.Width;
				temp.Height = ElementMargin;

				return temp;
			}

		}

		public static readonly Color DefaultColor = Color.Turquoise;

		/// <summary>
		/// This class is for use within the GameMenu.
		/// Most simple case.
		/// The user can click on this Element, which will in turn raise an event.
		/// Hover effects included.
		/// </summary>
		private sealed class Button : MenuElementBase
		{
			internal override void init() {
				base.init();

				RenderData.mdl.VertexBuffer.ApplyColor(DefaultColor);
			}
			public override void updateRenderData() {
				base.updateRenderData();
			}
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
			private EventHandler<onClickArgs> _remoteOnClick;


			public Button(GameMenu parent, string label, Action<onClickArgs> onClickAction = null, bool logButtonClicks = false) {
				// set data
				Label = label;
				Parent = parent;

				//makes the event raise under the condition that it was properly directed to this button.
				_remoteOnClick = (obj, args) => {
					if (Area.Contains(args.Position.X, args.Position.Y) && parent.isOpen && !args.Consumed)
						onClick?.Invoke(args);
				};

				//register the button on the root clickEvent
				onClickEvent += _remoteOnClick;
				// if the clicks should be logged, add it to the action, same with the functionality
				if (logButtonClicks)
					onClickAction += (args) => { Console.WriteLine("Button '" + label + "' was pressed."); };

				if (onClickAction != null)
					onClick += onClickAction;

				//add the button to the list
				parent.Elements.Add(this);
				init();
			}

		}

		private class IconButton : MenuElementBase
		{
			public override void init() {
				RenderData = new RenderData
				{
					mdl = Model.Square,
					ResID = dataLoader.getResID("m_frame_default"),
					ResID2 = dataLoader.getResID("t_" + Item.Rarity.ToString())
				};
			}
			public override void updateRenderData() {
				base.updateRenderData();
			}
			Tooltip tooltip;

			private EventHandler<onClickArgs> _remoteOnClick;

			//Bitmap iconBG;

			ItemBase Item;
			bool isEquipped
			{
				get {
					if (Game.state != Game.GameState.Menu)
						return Item == Game.instance._player.EquippedWeaponL || Item == Game.instance._player.EquippedWeaponR;
					else
						return false;
				}
			}



			public IconButton(GameMenu parent, ItemBase item, Action<onClickArgs> onClick = null) {

				//makes the event raise under the condition that it was properly directed to this button.
				_remoteOnClick = (obj, args) => {
					if (Area.Contains(args.Position.X, args.Position.Y) && parent.isOpen && !args.Consumed) {
						args.Consumed = true;
						if (args.Button == Right && !args.Down) {
							if (getKeyState(System.Windows.Forms.Keys.ShiftKey)) {
								item.Drop();
							} else {
								if (item is IEquipable && Game.instance._player.getEquipedItem(((IEquipable) item).Slot) != item) {
									((IEquipable) item).Equip(Game.instance._player);
								} else if (item is IUsable) {
									((IUsable) item).Use(Game.instance._player);
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
				if (item is Weapon)
					tooltip = new WeaponTooltip((Weapon) item, Location: () => new Vector2(Area.X + Area.Width / 2, Area.Y + Area.Height / 2), Validation: () => _Hovering);
				else
					tooltip = new Tooltip(item.ItemInfoText, Location: () => new Vector2(Area.X + Area.Width / 2, Area.Y + Area.Height / 2), Validation: () => _Hovering);

				//iconBG = dataLoader.get2D("border_" + item.Rarity.ToString());

				Parent.Elements.Add(this);
				init();
			}



			public override void draw(DeviceContext rt) {
				/*
                Color4 temp = MenuTextBrush.Color;
                MenuTextBrush.Color = Color.Black;
                rt.DrawBitmap(iconBG, Area, 1, BitmapInterpolationMode.NearestNeighbor);
                if(Item.image != null)
                    rt.DrawBitmap(Item.image, Area, 1, BitmapInterpolationMode.NearestNeighbor);
                /*
                if(Item is IEquipable)
                    if(Game.instance._player.getEquipedItem(((IEquipable)Item).Slot) == Item)
                        rt.DrawText("E", MenuFont, Area, MenuTextBrush);
                 
                MenuTextBrush.Color = temp;
                */
			}

			internal override Rectangle calcArea() {
				int index = Container.Children.IndexOf(this);
				int columns = ((InventoryElement)Container).Columns;

				int x = index % columns;
				int y = index / columns;

				int size = ((InventoryElement)Container).ElementSize;

				Rectangle temp = new Rectangle(Container.Area.Left + ElementMargin, Container.Area.Top + ElementMargin, size, size);

				temp.X += x * (size + ElementMargin) + (Container.Area.Width % Container._width) / 2;
				temp.Y += y * (size + ElementMargin);

				return temp.Floor();
			}
			/*
			public override void update() {
				base.update();

				if (_Hovering) {
					if (getKeyState(System.Windows.Forms.Keys.ShiftKey))
						Cursor.CursorType = CursorTypes.Inventory_Remove;
					else if (Item is IEquipable && Game.instance._player.getEquipedItem(((IEquipable) Item).Slot) != Item)
						Cursor.CursorType = CursorTypes.Inventory_Equip;
					else if (Item is IUsable)
						Cursor.CursorType = CursorTypes.Inventory_Use;
				}
				lock (tooltip)
					tooltip.Tick();
			}
			*/
		}

		private class InventoryElement : MenuElementListBase
		{
			public override void updateRenderData() {
				if (update) {
					base.updateRenderData();
				}
			}

			public override void init() {
				RenderData = new RenderData
				{
					mdl = Model.Square,
					ResID = dataLoader.getResID("m_frame_default"),
				};
			}
			private int _ElementSize = 32;

			public int Columns { get; private set; }

			public int ElementSize
			{
				get { return _ElementSize; }
				set {
					_ElementSize = value;
					Columns = (area.Width - 2 * ElementMargin) % (_ElementSize + ElementMargin);
				}
			}

			public InventoryElement(GameMenu Parent, List<ItemBase> Inventory, int size = 32, int Width = 600, Action<onClickArgs> onClick = null) {
				this.Parent = Parent;
				_ElementSize = size;
				Columns = (Width - 2 * ElementMargin) / (_ElementSize + ElementMargin);
				lock (Parent.Elements)
					Parent.Elements.Add(this);
				int i = 0;
				int j;
				for (j = 0 ; j * Columns < Inventory.Count ; j++) { // Rows | Y
					for (i = 0 ; i + j * Columns < Inventory.Count && i < Columns ; i++) { // Columns | X
						IconButton temp = new IconButton(Parent, Inventory[i + j * Columns], onClick);
						temp.Container = this;
					}
				}
				init();
			}

			internal override void invalidateWidths() {
				_width = (ElementSize + ElementMargin) * Columns + ElementMargin;
			}

			internal override Rectangle calcArea() {
				Rectangle temp = base.calcArea();
				temp.Height = ElementMargin + (Children.Count / Columns + 1) * (ElementMargin + ElementSize);
				return temp;
			}

			public override void draw(DeviceContext rt) {
				//drawBorder(rt);
			}
		}
	}
}
