using SharpDX.Direct2D1;
using SharpDX;
using System.Linq;

namespace Game_Java_Port.Interface {
	/*
    public enum DrawType {
        None,
        Circle,
        Rectangle,
        Polygon,
        Image
    }
    */

	public interface IRenderable {

		RenderData RenderData { get; set; }

	}

	public static class RenderableExtensions {

		public static void register(this IRenderable me) { Renderer.add(me); }
		public static void unregister(this IRenderable me) { Renderer.remove(me); }

		public static Vector2 NearestPoint(this IRenderable me) {

			Vector2 nearest = Vector2.Zero;

			float distanceToPlayers = float.PositiveInfinity;

			if (GameStatus.GameSubjects.Any((subj) => subj.Team == FactionNames.Players))
				GameStatus.GameSubjects.FindAll((subj) => subj.Team == FactionNames.Players).ForEach((subj) =>
				{
					float temp;
					Vector2 temp2 = Vector2.Zero;
					if ((temp = me.RenderData.mdl.VertexBuffer.Min(vb => Vector2.DistanceSquared((temp2 = vb.Pos.XY()), subj.Location))) < distanceToPlayers) {
						distanceToPlayers = temp;
						nearest = temp2;
					}
				});

			return nearest;
		}

		public static bool isOutOfRange(this IRenderable me) {

			//Vector2 relativePos = me.Area.Location + MatrixExtensions.PVTranslation;
			float distanceToPlayers = float.PositiveInfinity;

			//TODO: Find timer changing collection and create custom tickable class to handle situation instead

			if (GameStatus.GameSubjects.Any((subj) => subj.Team == FactionNames.Players))
				GameStatus.GameSubjects.FindAll((subj) => subj.Team == FactionNames.Players).ForEach((subj) =>
				{
					float temp;
					if ((temp = me.RenderData.mdl.VertexBuffer.Min(vb => Vector2.DistanceSquared(vb.Pos.XY(), subj.Location))) < distanceToPlayers)
						distanceToPlayers = temp;
				});


			return distanceToPlayers >= (GameStatus.ScreenHeight * GameStatus.ScreenHeight + GameStatus.ScreenWidth * GameStatus.ScreenWidth);
		}
	}
}