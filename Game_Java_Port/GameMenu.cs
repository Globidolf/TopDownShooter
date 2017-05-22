using Game_Java_Port.Interface;
using Game_Java_Port.Logics;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
	public interface IMenuContainer<T> {
		List<T> Content { get; }
		RenderData RenderData { get; set; }
		Rectangle Area { get; set; }
		Rectangle SubArea { get; set; }
		int Margin { get; set; }
		int Padding { get; set; }
	}
	public static class ContainerExtensions {
		public static void Add<T>(this IMenuContainer<T> me, T obj) { me.Content.Add(obj); }
		public static void Remove<T>(this IMenuContainer<T> me, T obj) { me.Content.Remove(obj); }
		public static void Sort<T>(this IMenuContainer<T> me, Func<T, float> sort) { me.Content.OrderBy(sort); }
	}
	public enum MenuName : byte {
		MainMenu,
		PauseMenu,
		InventoryMenu
	}
	public class GameMenu : IMenuContainer<Element>, IRenderable {
		private bool _isOpen;
		public bool isOpen { get { return _isOpen; } set {
				if (value != _isOpen) {
					_isOpen = value;
					if (value)
						Renderer.add(this);
					else
						Renderer.remove(this);
				}
			} }
		public readonly MenuName Name;
		public int Margin { get; set; }
		public int Padding { get; set; }
		public Rectangle Area { get { return _Area; } set { _Area = value; } }
		protected Rectangle _Area;

		public GameMenu(MenuName Name, params Element[] Children) {
			this.Name = Name;
			Content = new List<Element>();
			Content.AddRange(Children);

			Content.ForEach(ele => ele.Parent = this);

			_Area = new Rectangle(0, 0, Program.width, Program.height);

			//      [ - - - ( a ) - - - ]  
			//   ( a[cdefghijklmnopqrstu]w )
			Content.ForEach(ele => ele.Minimize());

			//      [ { - - ( a ) - - } ]
			//      [ ( abcdefghij... ) ]
			Content.ForEach(ele => ele.Maximize());

			//      [ ( - - - a - - - ) ]
			//      [ ( abcdefghij... ) ]
			Content.ForEach(ele => ele.Autosize());
			SubArea = new Rectangle(SubArea.X, SubArea.Y, Content.Max(ele => ele.Area.Width), Content.Sum(ele => ele.FullArea.Height));
		}

		public Rectangle FullArea {
			get { return new Rectangle(Area.X - Margin, Area.Y - Margin, Area.Width + 2 * Margin, Area.Height + 2 * Margin); }
			set { Area = new Rectangle(value.X + Margin, value.Y + Margin, value.Width - 2 * Margin, value.Height - 2 * Margin); }
		}
		public Rectangle SubArea {
			get { return new Rectangle(Area.X + Padding, Area.Y + Padding, Area.Width - 2 * Padding, Area.Height - 2 * Padding); }
			set { Area = new Rectangle(value.X - Padding, value.Y - Padding, value.Width + 2 * Padding, value.Height + 2 * Padding); }
		}

		public List<Element> Content { get; }
		public RenderData RenderData { get; set; }


		public static _MenuList MenuList { get { return _MenuList.Instance; } }

		public class _MenuList {
			private static _MenuList instance;
			public static _MenuList Instance { get { if (instance == null) instance = new _MenuList(); return instance; } }
			public GameMenu get(MenuName Name) { return MenuList[Name]; }
			private static GameMenu _MainMenu;
			public static GameMenu MainMenu {
				get {
					if (_MainMenu == null) {
						_MainMenu = new GameMenu(MenuName.MainMenu,
							new Button("Start"),
							new Button("Exit"));
						_MainMenu.RenderData = new RenderData {
							Area = _MainMenu.Area,
							ResID = dataLoader.getResID("m_menu"),
							SubObjs = _MainMenu.Content.Select(cnt => cnt.RenderData).ToArray()
						};
					}
					return _MainMenu;
				}
			}
			public GameMenu this[MenuName Menu] {
				get {
					switch (Menu) {
						case MenuName.MainMenu: return MainMenu;
						default: return null;
					}
				}
			}
		}
	}

	public abstract class Element {
		/// <summary>
		/// Size to fit into parent if too large or special case.
		/// </summary>
		public abstract void Maximize();
		/// <summary>
		/// Size to make content fit.
		/// </summary>
		public abstract void Minimize();
		/// <summary>
		/// Size to match largest sibling.
		/// </summary>
		public abstract void Autosize();
		/// <summary>
		/// Initialize Renderdata and other data.
		/// </summary>
		public abstract void init();

		public IMenuContainer<Element> Parent;
		public int Margin { get; set; }
		public Rectangle Area { get { return _Area; } set { _Area = value; } }
		protected Rectangle _Area;

		public abstract RenderData RenderData { get; set; }

		public Rectangle FullArea {
			get { return new Rectangle(Area.X - Margin, Area.Y - Margin, Area.Width + 2 * Margin, Area.Height + 2 * Margin); }
			set { Area = new Rectangle(value.X + Margin, value.Y + Margin, value.Width - 2 * Margin, value.Height - 2 * Margin); }
		}

	}

	public abstract class ContainerElement : Element, IMenuContainer<Element> {
		public int Padding { get; set; }

		public Rectangle SubArea {
			get { return new Rectangle(Area.X + Padding, Area.Y + Padding, Area.Width - 2 * Padding, Area.Height - 2 * Padding); }
			set { Area = new Rectangle(value.X - Padding, value.Y - Padding, value.Width + 2 * Padding, value.Height + 2 * Padding); }
		}

		private Element[] _Content;
		public List<Element> Content { get; }
	}
	public sealed class Button : Element {

		string Label;
		public Button(string Label) : base() { this.Label = Label; }
		public Button() : base() { Label = "Button"; }
		public override RenderData RenderData { get; set; }

		public override void Autosize() {
			_Area.Width = Parent.Content.Max(ele => ele.Area.Width);
			int index = Parent.Content.IndexOf(this);
			if (index > 0)
				_Area.Y = Parent.Content[index - 1].FullArea.Bottom + Margin;
		}
		public override void Maximize() {
			_Area.Width = Math.Min(Parent.SubArea.Width - Margin, _Area.Width);
			_Area.Height = Math.Min(Parent.SubArea.Height - Margin, _Area.Height);
		}
		public override void Minimize() {
			Size2 size = SpriteFont.DEFAULT.MeasureString(Label);
			_Area.Width = size.Width;
			_Area.Height = size.Height;
			_Area.X = Parent.SubArea.X + Margin;
			_Area.Y = Parent.SubArea.Y + Margin;
		}
		public override void init() {
			RenderData = new RenderData {
				Area = _Area,
				ResID = dataLoader.getResID("m_bar_1_4"),
				ResID2 = dataLoader.getResID("t_special"),
				TextureRepetition = new Vector2(1, 2),
				Frameindex = 0,
				Z = Renderer.Layer_Menu + Renderer.LayerOffset_Text
			};
		}
	}
}