using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port.Interface {
    public interface IInteractable : IIndexable {
        Vector2 Location { get; set; }
        Tooltip ActionInfo { get; }
        void interact(CharacterBase who);
    }

    public static class InteractableOverload {
        public static bool drawActionInfo(this IInteractable interactable) {

            //if the game is running, item is within player and mouse pickup range as well as being the closest to the cursor.
            if(Game.state != Game.GameState.Menu &&
                Vector2.DistanceSquared(interactable.Location, Game.instance._player.Location) <= GameVars.pickupRange * GameVars.pickupRange &&
                Vector2.DistanceSquared(interactable.Location + MatrixExtensions.PVTranslation, GameStatus.MousePos) <= GameVars.pickupMouseRange * GameVars.pickupMouseRange &&
                // all usable objects
                GameStatus.GameObjects
                    // all within range
                    .FindAll((obj) => Vector2.DistanceSquared(obj.Location, Game.instance._player.Location) <= GameVars.pickupRange * GameVars.pickupRange)
                    // ordered by distance to mouse
                    .OrderBy((obj) => Vector2.DistanceSquared(obj.Location + MatrixExtensions.PVTranslation, GameStatus.MousePos))
                    // is this the first?
                    .First() == interactable) {
                return true;
            }
            return false;
        }
    }
}
