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
        public static bool CollidesWith(this ICollidible me, ICollidible other) {
            
            //not even in detection range
            if(Vector2.DistanceSquared(me.Location, other.Location) > (me.Size / 2 + other.Size / 2) * (me.Size / 2 + other.Size / 2))
                return false;
            switch(me.ColType) {
                case CollisionType.Circle: // check with half size like above
                    switch(other.ColType) {
                        case CollisionType.Circle: // exactly the same as above, as we enter this, we can return true immediately
                            return true;
                        case CollisionType.Rect: // check if the other rect expanded by 'me's size contains 'me's location. as simple as this
                            return new RectangleF(
                                other.Area.X - me.Size / 2,
                                other.Area.Y - me.Size / 2,
                                other.Area.Width + me.Size,
                                other.Area.Height + me.Size).Contains(me.Location);
                        case CollisionType.Poly:
                            // TODO: check for collision
                            return true;
                        default:
                            throw new NotImplementedException("Unknown Collision Type!");
                    }
                case CollisionType.Rect:
                    switch(other.ColType) {
                        case CollisionType.Circle: // same check as above just with inverted assignments
                            return new RectangleF(
                                me.Area.X -      other.Size / 2,
                                me.Area.Y -      other.Size / 2,
                                me.Area.Width +  other.Size,
                                me.Area.Height + other.Size)
                                          .Contains(other.Location);
                        case CollisionType.Rect: // predefined method available
                            return me.Area.Intersects(other.Area);
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
    }
}
