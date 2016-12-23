using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;

namespace Game_Java_Port {
    class WeaponTooltip : Tooltip {

        private Weapon Item;

        public WeaponTooltip(Weapon item, Func<Vector2> Location = null, Func<bool> Validation = null, bool ticksInternal = false) : base(
            item.ItemBaseInfo + "\n" +
                    "Lv." + item.Level + " " + item.Rarity.ToString() + " " + item.WType.ToString() + "\n" +
                    "Bullets: " + item.Behaviour.ToString() + "\n" +
                    "Speed: " + item.BulletSpeed + "Px/s" + "\n" +
                    "Damage: " + item.Damage.ToString("0.##") + "x" + item.BulletsPerShot + "^" + (item.BulletHitCount > uint.MaxValue / 12 ? "inf" : item.BulletHitCount.ToString()) + "\n" +
                    "Precision: " + item.Precision.ToString("0.#%") +
                    " (" + Math.Min(1, (item.Precision * (Game.state == Game.GameState.Menu ? 1 : Game.instance._player.PrecisionMult))).ToString("0.#%") + ")\n" +
                    "Range: " + item.Range.ToString("0.##") + "\n" +
                    "APS: " + item.AttackSpeed.ToString("0.##") + "\n" +
                    "Seed: " + item.Seed + ":" + item.GenType
            , Location, Validation, ticksInternal) {
            Item = item;
        }

        public override void draw(RenderTarget rt) {
            drawBG(rt);
            RectangleF temp = relLabel;
            rt.DrawBitmap(Item.image, new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            rt.DrawBitmap(dataLoader.get("Coin"), new RectangleF(temp.X, temp.Y + temp.Height - 20, 16,16), 1, BitmapInterpolationMode.Linear);
            temp.Offset(0, 20);
            rt.DrawBitmap(dataLoader.get("Attack"), new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            temp.Offset(0, 20);
            rt.DrawBitmap(dataLoader.get("Precision"), new RectangleF(temp.X, temp.Y, 16, 16), 1, BitmapInterpolationMode.Linear);
            temp.Offset(0, 20);
            temp = relLabel;
            temp.Offset(20, 0);
            rt.DrawText(Item.Name,GameStatus.MenuFont, temp, GameStatus.MenuTextBrush);
            temp.Offset(0, 20);
            rt.DrawText(Item.Damage.ToString("0.##"), GameStatus.MenuFont, temp, GameStatus.MenuTextBrush);
            temp.Offset(0, 20);
            rt.DrawText(Item.Precision.ToString("0.##%"), GameStatus.MenuFont, temp, GameStatus.MenuTextBrush);
            temp.Offset(0, 20);

            temp = new RectangleF(relLabel.X + 20, relLabel.Y + relLabel.Height - 20, relLabel.Width, 20);
            rt.DrawText(Item.SellPrice.ToString("#,##0"), GameStatus.MenuFont, temp, GameStatus.MenuTextBrush);
        }
    }
}
