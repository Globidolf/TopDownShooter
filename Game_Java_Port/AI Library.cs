using Game_Java_Port.Interface;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Game_Java_Port.GameStatus;

namespace Game_Java_Port {
    public static class AI_Library {
        private static Vector2 _LastMousePos;
        private static Controls _lastState;
        private static AngleSingle LastAimDirection;


        private static Func<AttributeBase, Faction.Disposition, Func<AttributeBase, bool>> Seek_By_Disposition =
            (me, disp) =>
            (targ) =>
            targ != me && (me.Team.Dispositions.ContainsKey(targ.Team) ? me.Team.Dispositions[targ.Team] : me.Team.DefaultDisposition) == disp;

        private static AttributeBase ResultOrNull(IOrderedEnumerable<AttributeBase> orderedList, Func<AttributeBase, bool> criteria) {
            return orderedList.Any(criteria) ? orderedList.First(criteria) : null;
        }

        private static Action<NPC> NonConduction_AI = (me) =>
                {
                    IOrderedEnumerable<AttributeBase> orderedList = GameSubjects.OrderBy((subj) => Vector2.Distance(subj.Location, me.Location));

                    AttributeBase closestEnemy = ResultOrNull(orderedList, Seek_By_Disposition(me, Faction.Disposition.Enemy));

                    AttributeBase closestAlly = ResultOrNull(orderedList, Seek_By_Disposition(me, Faction.Disposition.Allied));

                    AttributeBase closestFearSource = ResultOrNull(orderedList, Seek_By_Disposition(me, Faction.Disposition.Fear));

                    if(closestFearSource != null && Vector2.Distance(closestFearSource.Location, me.Location) - closestFearSource.Size / 2 <= me.ViewRadius) {
                        Run(me, closestFearSource);
                    } else if(closestEnemy != null) {
                        if(Vector2.Distance(closestEnemy.Location, me.Location) - closestEnemy.Size / 2 <= me.ViewRadius) {
                            Attack?.Invoke(me, closestEnemy);
                        } else if(closestAlly != null && Vector2.Distance(closestAlly.Location, me.Location) - closestAlly.Size / 2 <= me.ViewRadius) {
                            Follow?.Invoke(me, closestAlly, me.ViewRadius / 2);
                        } else {
                            Search?.Invoke(me, closestEnemy);
                        }
                    } else if(closestAlly != null && Vector2.Distance(closestAlly.Location, me.Location) - closestAlly.Size / 2 <= me.ViewRadius) {
                        Follow?.Invoke(me, closestAlly, me.ViewRadius / 2);
                    } else {
                        Wander?.Invoke(me);
                    }
                };

        private static bool Conduction_AI(NPC me, AttributeBase activator, Faction.Conduct conduct) {

            if(activator != null) {
                if(activator.Health <= 0) {
                    //target lost
                    return true;
                } else
                    switch(conduct) {
                        case Faction.Conduct.Attack:

                            if((me.Team.Dispositions.ContainsKey(activator.Team) ? me.Team.Dispositions[activator.Team] : me.Team.DefaultDisposition) != Faction.Disposition.Allied) {
                                if(Vector2.Distance(me.Location, activator.Location) < me.ViewRadius)
                                    Attack.Invoke(me, activator);
                                else
                                    Search.Invoke(me, activator);
                            }
                            break;
                        case Faction.Conduct.Follow:
                            if(Vector2.Distance(me.Location, activator.Location) < me.ViewRadius)
                                Follow.Invoke(me, activator, me.Size + activator.Size);
                            else
                                Search.Invoke(me, activator);
                            break;
                        case Faction.Conduct.Run:
                            if(Vector2.Distance(me.Location, activator.Location) < me.ViewRadius)
                                Run(me, activator);
                            break;
                        case Faction.Conduct.Ignore:
                        default:
                            break;
                    }
            } else
                return true;
            return false;
        }

        public static Action<NPC> NPC_AI = (me) =>
                {
                    if (Conduction_AI(me, me.Agressor, me.Team.AgressionConduct)) {
                        me.Agressor = null;
                        if (Conduction_AI(me, me.Interactor, me.Team.InteractionConduct)) {
                            me.Interactor = null;
                            NonConduction_AI(me);
                        }
                    }
                };

        public static Action<NPC> Wander = (me) =>
                {
                    if (me.RNG.Next(5 * (int)GameVars.defaultGTPS) == 0) {
                        me.IsMoving ^= true;
                    }
                    if(me.RNG.Next(10 * (int)GameVars.defaultGTPS) == 0) {
                        me.AimDirection = new AngleSingle(me.RNG.Next(360), AngleType.Degree);
                    }

                    AngleSingle movementAngle = me.DirectionVector.toAngle();
                    movementAngle = movementAngle.track(me.AimDirection, (float)Math.PI * 6 / GameVars.defaultGTPS * 0.8f);

                    me.DirectionVector = movementAngle.toVector();
                };

        public static Action<NPC, AttributeBase> Search = (me, target) =>
                {
                    me.AimDirection = me.AimDirection.track(me.Location.angleTo(target.Location) + new AngleSingle(me.RNG.NextFloat(-0.5f, 0.5f), AngleType.Revolution), 0.01f);

                    AngleSingle movementAngle = me.DirectionVector.toAngle();

                    movementAngle = movementAngle.track(me.AimDirection, (float)Math.PI * 6 / GameVars.defaultGTPS * 0.8f);

                    me.DirectionVector = movementAngle.toVector();

                    if(me.AimDirection.offset(me.Location.angleTo(target.Location), true).Revolutions < 0.1)
                        me.IsMoving = true;
                    else
                        me.IsMoving = false;
                };

        public static Action<NPC, AttributeBase, float> Follow = (me, target, dist) =>
                {
                    float distance = Vector2.Distance(target.Location, me.Location);

                    me.AimDirection = me.Location.angleTo(target.Location);

                    AngleSingle movementAngle = me.DirectionVector.toAngle();

                    movementAngle = movementAngle.track(me.AimDirection, (float)Math.PI * 6 / GameVars.defaultGTPS * 0.8f);

                    me.DirectionVector = movementAngle.toVector();

                    if(distance > dist)
                        me.IsMoving = true;
                    else
                        me.IsMoving = false;
                };

        public static void Run(NPC me, AttributeBase target, bool lookback = false)
                {
                    me.AimDirection = me.Location.angleTo(target.Location) + (lookback ? AngleSingle.ZeroAngle : AngleSingle.StraightAngle);

                    AngleSingle movementAngle = me.DirectionVector.toAngle();

                    movementAngle = movementAngle.track(
                        me.AimDirection + (lookback ? AngleSingle.StraightAngle : AngleSingle.ZeroAngle),
                        (float)Math.PI * 6 / GameVars.defaultGTPS * 0.8f);

                    me.DirectionVector = movementAngle.toVector();
                    
                    me.IsMoving = true;
                }

        public static Action<NPC, AttributeBase> Attack = (me, target) =>
                {
                    float distance = Vector2.Distance(target.Location, me.Location);
                    float range = me.EquippedWeaponR == null ? me.RMeleeRange : me.EquippedWeaponR.Range;
                    float targetRange = target.EquippedWeaponR == null ? target.RMeleeRange : target.EquippedWeaponR.Range;
                    bool canRun = range > targetRange;
                    me.AimDirection = me.Location.angleTo(target.Location);

                    if(me.Rank >= Rank.Elite) {
                        if(canRun && distance < targetRange)
                            Run(me, target, true);
                        else
                            Circle.Invoke(me, target, range / 2);
                    } else
                        Follow.Invoke(me, target, range / 2);

                    if(distance < range)
                        me.Fire();

                };

        public static Action<NPC> RealPlayer = (me) =>
        {

            me.inputstate = getInputState();

            float range;

            //reduce cpu load
            if(MousePos != _LastMousePos) {
                range = Math.Max(me.RWeaponRange, me.RMeleeRange);
                _LastMousePos = MousePos;

                me.AimDirection = Game.instance.Area.Center.angleTo(new Vector2(MousePos.X / Program._RenderTarget.Size.Width * Program.width, MousePos.Y / Program._RenderTarget.Size.Height * Program.height));

            } else if(me.MovementVector.LengthSquared() != 0) {
                range = Math.Max(me.RWeaponRange, me.RMeleeRange);
                me.AimDirection = Game.instance.Area.Center.angleTo(MousePos);
            }

            if(Game.instance._client != null && _lastState != getInputState()) {

                Game.instance._client.send(GameClient.CommandType.updateState, Game.instance.SerializeInputState());
                if(!_lastState.HasFlag(Controls.fire) && getInputState().HasFlag(Controls.fire))
                    Game.instance._client.send(GameClient.CommandType.updateWpnRngState, me.getWeaponRandomState());
                
            }

            if(LastAimDirection != me.AimDirection) {
                lock(Game.instance)
                    Game.instance.statechanged = true;
                LastAimDirection = me.AimDirection;
            }

            _lastState = getInputState();
            
            PlayerSim.Invoke(me);
        };

        public static Action<NPC> PlayerSim = (me) =>
                {
                    float running = me.inputstate.HasFlag(Controls.run) ? 1.5f : 1;
                    Vector2 moveDir = new Vector2();

                    if(me.inputstate.HasFlag(Controls.move_left)) {
                        moveDir.X--;
                    }
                    if(me.inputstate.HasFlag(Controls.move_right)) {
                        moveDir.X++;
                    }
                    if(me.inputstate.HasFlag(Controls.move_up)) {
                        moveDir.Y--;
                    }
                    if(me.inputstate.HasFlag(Controls.move_down)) {
                        moveDir.Y++;
                    }

                    if(moveDir.X != 0 || moveDir.Y != 0) {
                        me.IsMoving = true;
                        me.DirectionVector = moveDir;

                        if(me == Game.instance._player)
                            Game.instance.statechanged = true;
                    } else
                        me.IsMoving = false;



                    if(me.justPressed(Controls.open_pausemenu))
                        GameMenu.PauseMenu.open();

                    if(me.EquippedWeaponR != null && me.EquippedWeaponR.WType == WeaponType.Revolver) {
                        if(me.justPressed(Controls.fire)) {
                            me.Fire();
                        }
                    } else if(me.inputstate.HasFlag(Controls.fire)) {
                        me.Fire();
                    }

                    if(me.EquippedWeaponL != null && me.EquippedWeaponL.WType == WeaponType.Revolver) {
                        if(me.justPressed(Controls.fire)) {
                            me.Fire(true);
                        }
                    } else if(me.inputstate.HasFlag(Controls.fire)) {
                        me.Fire(true);
                    }

                    if(me.justPressed(Controls.interact)) {
                        lock(GameObjects) {
                            IOrderedEnumerable<IInteractable> list = GameObjects.OrderBy((obj) => { return Vector2.Distance(obj.Location + MatrixExtensions.PVTranslation, MousePos); });
                            if (list.Any((obj) => {
                                return obj != me &&
                                Vector2.Distance(obj.Location, me.Location) < 200 &&
                                Vector2.Distance(obj.Location + MatrixExtensions.PVTranslation, MousePos) < 20; })){
                                list.FirstOrDefault(( obj ) => { return obj != me && Vector2.Distance(obj.Location, me.Location) < 200; })
                                    .interact(me);
                            }
                        }
                    }

                    if (me.justPressed(Controls.reload))
                    {
                        if (me.EquippedWeaponL != null)
                            me.EquippedWeaponL.Reload();
                        if (me.EquippedWeaponR != null)
                            me.EquippedWeaponR.Reload();
                    }

                    if (me == Game.instance._player) {
                        if (me.justPressed(Controls.open_inventory))
                        {
                            GameMenu.InventoryMenu.open();
                        }
                    }


                    //reduce cpu load
                    if(me != Game.instance._player && me.AimDirection != me.LastAimDirection) {
                        if (me == Game.instance._player)
                            Game.instance.statechanged = true;

                        me.LastAimDirection = me.AimDirection;
                    }



                    /*
                    if(me == Game.instance._player && me.justPressed(Controls.walk) && (Game.instance._host != null || Game.instance._client == null)) {

                        NPC temp = new NPC(
                            seed: Game.instance._host == null ? (int?)null : Game.instance._host._HostGenRNG.Next(),
                            add: Game.instance._host == null);
                        temp.Location = me.Target;
                        if(Game.instance._host != null)
                            Game.instance._client.send(GameClient.CommandType.add, temp.serialize());
                    }
                    */

                    me._lastState = me.inputstate;
                };

        public static Action<NPC, AttributeBase, float> Circle = (me, target, dist) =>
                {
                    float distance = Vector2.Distance(target.Location, me.Location);

                    me.AimDirection = me.Location.angleTo(target.Location);

                    AngleSingle movementAngle = me.DirectionVector.toAngle();
                    if (distance > dist) {
                        movementAngle = movementAngle.track(me.AimDirection, (float)Math.PI * 6 / GameVars.defaultGTPS * 0.8f);
                    } else {
                        //determine by seed if clock- or counter-clockwise (RNG won't work for this)
                        int sign = (me.Seed % 2 == 0) ? 1 : -1;

                        AngleSingle angle = me.AimDirection;

                        //turn point at half viewradius (bit more than 92 to ensure some distance)
                        angle += new AngleSingle((distance / (me.ViewRadius * 2) - 1) * sign * 92, AngleType.Degree);

                        movementAngle = movementAngle.track(angle, (float)Math.PI * 6 / GameVars.defaultGTPS * 0.8f);
                    }

                    me.DirectionVector = movementAngle.toVector();

                    if(distance < me.ViewRadius)
                        me.IsMoving = true;
                    else
                        me.IsMoving = false;
                };
    }
}
