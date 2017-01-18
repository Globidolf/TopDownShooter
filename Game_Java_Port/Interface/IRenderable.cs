using SharpDX.Direct2D1;
using SharpDX;
using System.Linq;

namespace Game_Java_Port.Interface {

    public enum DrawType {
        None,
        Circle,
        Rectangle,
        Polygon,
        Image
    }

    public interface IRenderable {
        
        DrawType drawType { get; set; }

        RectangleF Area { get; set; }

        void draw(RenderTarget rt);

        int Z { get; set; }
    }

    public static class RenderableExtensions {

        public static bool isOutOfRange(this IRenderable me) {

            Vector2 relativePos = me.Area.Location + MatrixExtensions.PVTranslation;
            float distanceToPlayers = float.PositiveInfinity;

            //TODO: Find timer changing collection and create custom tickable class to handle situation instead

            if(GameStatus.GameSubjects.Any((subj) => subj.Team == FactionNames.Players))
                GameStatus.GameSubjects.FindAll((subj) => subj.Team == FactionNames.Players).ForEach((subj) =>
                {
                    float temp;
                    if((temp = Vector2.DistanceSquared(me.Area.Location, subj.Location)) < distanceToPlayers)
                        distanceToPlayers = temp;
                });


            return distanceToPlayers >= (GameStatus.ScreenHeight * GameStatus.ScreenHeight + GameStatus.ScreenWidth * GameStatus.ScreenWidth);
        }
    }
}
