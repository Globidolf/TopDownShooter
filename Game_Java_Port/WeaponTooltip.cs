using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using Game_Java_Port.Logics;

namespace Game_Java_Port {
    class WeaponTooltip : Tooltip {

        private Weapon Item;

        public WeaponTooltip(Weapon item, Func<Vector2> Location = null, Func<bool> Validation = null, bool ticksInternal = false) : base(
                    //Giving the base the following text will make the resulting tooltip have a size large enough to contain all information we want to display.
                    //minus 20px width which we add manually (icon size)
                    item.ItemBaseInfo + "\n" +
                    "Lv." + item.Level + " " + item.Rarity.ToString() + " " + item.WType.ToString() + "\n" +
                    item.Behaviour.ToString() + "\n" +
                    item.BulletSpeed + "Px/s" + "\n" +
                    item.Damage.ToString("0.##") + "x" + item.BulletsPerShot + "^" + (item.BulletHitCount > uint.MaxValue / 12 ? "inf" : item.BulletHitCount.ToString()) + "\n" +
                    item.Precision.ToString("0.#%") +
                    Math.Min(1, (item.Precision * (Game.state == Game.GameState.Menu ? 1 : Game.instance._player.PrecisionMult))).ToString("0.#%") + ")\n" +
                    item.Range.ToString("0.##") + "\n" +
                    item.AttackSpeed.ToString("0.##") + "\n" +
                    item.Seed + ":" + item.GenType
            , Location, Validation, ticksInternal) {
            Item = item;
            RectangleF temp = Area;
            temp.Width += 20;
            Area = temp;
            switch(Item.Rarity) {
                case ItemType.Pearlescent:
                    frame = Menu_BG_Tiled.Pearlescent;
                    break;
                case ItemType.Epic:
                    frame = Menu_BG_Tiled.Epic;
                    break;
                case ItemType.Legendary:
                    frame = Menu_BG_Tiled.Legendary;
                    break;
                case ItemType.Rare:
                    frame = Menu_BG_Tiled.Rare;
                    break;
                case ItemType.Common:
                    frame = Menu_BG_Tiled.Common;
                    break;
                default:
                    frame = Menu_BG_Tiled.Default;
                    break;
            }
        }

        public override void draw(DeviceContext rt) {
            frame.draw(rt);
            RectangleF temp = relLabel.Floor();
            rt.DrawBitmap(Item.image, new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            rt.DrawBitmap(dataLoader.get("icon_bg"), new RectangleF(temp.X, temp.Y + temp.Height - 20, 16, 16), 1, BitmapInterpolationMode.Linear);
            rt.DrawBitmap(dataLoader.get("Coin"), new RectangleF(temp.X, temp.Y + temp.Height - 20, 16,16), 1, BitmapInterpolationMode.Linear);
            temp.Offset(0, 20);
            rt.DrawBitmap(dataLoader.get("icon_bg"), new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            rt.DrawBitmap(dataLoader.get("Attack"), new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            temp.Offset(0, 20);
            rt.DrawBitmap(dataLoader.get("icon_bg"), new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            rt.DrawBitmap(dataLoader.get("Precision"), new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            temp.Offset(0, 20);
            rt.DrawBitmap(dataLoader.get("icon_bg"), new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            rt.DrawBitmap(dataLoader.get("Firerate"), new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            temp.Offset(0, 20);
            temp = relLabel;
            temp.Offset(20, 0);
            SpriteFont.DEFAULT.directDrawText(Item.Name, temp, rt);
            temp.Offset(0, 20);
            SpriteFont.DEFAULT.directDrawText(Item.Damage.ToString("0.##"), temp, rt);
            temp.Offset(0, 20);
            SpriteFont.DEFAULT.directDrawText(Item.Precision.ToString("0.##%"), temp, rt);
            temp.Offset(0, 20);
            SpriteFont.DEFAULT.directDrawText(Item.AttackSpeed.ToString("0.## / s"), temp, rt);
            temp.Offset(0, 20);

            temp = new RectangleF(relLabel.X + 20, relLabel.Y + relLabel.Height - 20, relLabel.Width, 20);
            SpriteFont.DEFAULT.directDrawText(Item.SellPrice.ToString("### ### ##0"), temp, rt);
        }

    }
}
