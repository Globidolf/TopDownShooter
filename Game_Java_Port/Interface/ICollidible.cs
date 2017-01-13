using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.Interface {
    public enum CollisionType {
        Poly,
        Circle,
        Rect
    }
    public interface ICollidible {
        /// <summary>
        /// only used if ColType is Poly.
        /// </summary>
        List<Vector2> Poly { get; set; }
        /// <summary>
        /// only used if ColType is Rect
        /// </summary>
        RectangleF Area { get; set; }
        CollisionType ColType { get; set; }
        Vector2 Location { get; set; }
        float Size { get; set; }
    }

    public static class CollidibleExtensions {

        public static bool CollidesWith(this ICollidible me, CollisionType pseudotype, Vector2 pseudolocation, float pseudosize, RectangleF? pseudoarea = null, List<Vector2> pseudopoly = null) {

            //not even in detection range
            if(Vector2.DistanceSquared(me.Location, pseudolocation) > (me.Size / 2 + pseudosize / 2) * (me.Size / 2 + pseudosize / 2))
                return false;
            switch(me.ColType) {
                case CollisionType.Circle: // check with half size like above
                    switch(pseudotype) {
                        case CollisionType.Circle: // exactly the same as above, as we enter this, we can return true immediately
                            return true;
                        case CollisionType.Rect: // check if the other rect expanded by 'me's size contains 'me's location. as simple as this
                            return new RectangleF(
                                pseudoarea.Value.X - me.Size / 2,
                                pseudoarea.Value.Y - me.Size / 2,
                                pseudoarea.Value.Width + me.Size,
                                pseudoarea.Value.Height + me.Size).Contains(me.Location);
                        case CollisionType.Poly:
                            // TODO: check for collision
                            return true;
                        default:
                            throw new NotImplementedException("Unknown Collision Type!");
                    }
                case CollisionType.Rect:
                    switch(pseudotype) {
                        case CollisionType.Circle: // same check as above just with inverted assignments
                            return new RectangleF(
                                me.Area.X - pseudosize / 2,
                                me.Area.Y - pseudosize / 2,
                                me.Area.Width +  pseudosize,
                                me.Area.Height + pseudosize)
                                          .Contains(pseudolocation);
                        case CollisionType.Rect: // predefined method available
                            return me.Area.Intersects(pseudoarea.Value);
                        case CollisionType.Poly:
                            // TODO: check for collision
                            return true;
                        default:
                            throw new NotImplementedException("Unknown Collision Type!");
                    }
                case CollisionType.Poly:
                    // TODO: check for collision
                    return true;
                default:
                    throw new NotImplementedException("Unknown Collision Type!");
            }
        }

        public static bool CollidesWith(this ICollidible me, ICollidible other) {
            return (me.CollidesWith(other.ColType, other.Location, other.Size, other.Area, other.Poly));
        }
    }
}
