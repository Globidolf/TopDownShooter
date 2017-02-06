using Game_Java_Port.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using SharpDX.Mathematics.Interop;
using SharpDX.Direct2D1;
using SharpDX;
using Game_Java_Port.Logics;
using System.Threading;
using System.Diagnostics;
using SharpDX.Direct2D1.Effects;

namespace Game_Java_Port {
    class Bullet : IRenderable, ITickable, IDisposable {
		

		public void updateRenderData() {
            RenderData.Area = new RectangleF(pos.X, pos.Y, _hitrange, _hitrange);
		}
        public RenderData RenderData { get; set; }
        
        float creationtime;

        CharacterBase _sourceOwner;
        Weapon _source;

        Random _RNG;

        CharacterBase _currentTarget;

        List<CharacterBase> _nonTargets;
        List<CharacterBase> _lockOnTargets;

        public Vector2 pos { get; set; } = new Vector2();
        Vector2 lastPos = new Vector2();

        public RectangleF Area { get; set; }

        uint _hitsRemain;

        float _speed;
        AngleSingle _direction;

        float _distance;

        bool _disposeMe;
        bool _rendered = false;
        bool _initiated = false;

        float _damage;

        float _hitrange = 10;

        public CharacterBase getCurrentTarget() {
            return _currentTarget;
        }
        
        private List<CharacterBase> TargetList {
            get {
                List<CharacterBase> temp = new List<CharacterBase>();
                    temp.AddRange(GameStatus.GameSubjects);
                temp.RemoveAll((targ) =>
                {
                    return _nonTargets.Contains(targ);
                });
                return temp;
            }
        }

        private List<CharacterBase> LockOnTargetList { get {
                List<CharacterBase> temp = TargetList;
                temp.RemoveAll((targ) =>
                {
                    //out of range
                    if( Vector2.Distance(lastPos, targ.Location) - targ.Size / 2 > _source.Range)
                        return true;

                    // not in hit-area
                    if(_sourceOwner.PrecisionR != 0 && lastPos.angleTo(targ.Area.Center).isInBetween(
                            new AngleSingle(_sourceOwner.AimDirection.Revolutions + (1 - _sourceOwner.PrecisionR)/2, AngleType.Revolution),
                            new AngleSingle(_sourceOwner.AimDirection.Revolutions - (1 - _sourceOwner.PrecisionR)/2, AngleType.Revolution)))
                        return true;

                    return false;
                });
                return temp;
            } }

        public int Z { get; set; } = 9;

        //public DrawType drawType { get; set; } = DrawType.Polygon;

        public Bullet(Weapon source, int? seed = null, List<CharacterBase> reserved = null) {
            lastPos = _sourceOwner.Location;
            pos = lastPos;
            creationtime = (float)Program.stopwatch.Elapsed.TotalSeconds;
			RenderData = new RenderData
			{
				AnimationOffset = (((creationtime * Stopwatch.Frequency) % 10000) / 1000) % 1f
			};
            RenderData.Area = new RectangleF(pos.X, pos.Y, _hitrange, _hitrange);

            
            //initialize RNG
            if(seed == null)
                _RNG = new Random();
            else
                _RNG = new Random((int)seed);

            //set values
            _source = source;
            _sourceOwner = _source.Owner;

            switch(_source.WType) {
                case WeaponType.Throwable:
                    if(_source.Name.Contains("Throwing Knives"))

                        RenderData.ResID = dataLoader.getResID("bullet_knife");//bullettexture = dataLoader.get2D("bullet_knife");

                    else if(_source.Name.Contains("Boomerang"))
                        RenderData.ResID = dataLoader.getResID("bullet_boomerang");//bullettexture = dataLoader.get2D("bullet_boomerang");
                    else if(_source.Name.Contains("Rock")) {
                        //animation = Animated_Tileset.Bullet_Rock;
                        RenderData.ResID = dataLoader.getResID("bullet_rock_16_32");//animation.Off_Set_Frame(pseudorandom % 1f);
                        RenderData.AnimationFrameCount = new Point(2, 2);
                        RenderData.AnimationSpeed = 5.3543f;
                    } else
                        RenderData.ResID = dataLoader.getResID("bullet_normal");
                    break;
                case WeaponType.Acid:
                    //animation = Animated_Tileset.Bullet_Acid;
                    RenderData.ResID = dataLoader.getResID("bullet_acid_16_32");// animation.Off_Set_Frame(pseudorandom % 1f);
                    RenderData.AnimationFrameCount = new Point(2, 2);
                    RenderData.AnimationSpeed = 5.472743f;
                    break;

                default:
                    RenderData.ResID = dataLoader.getResID("bullet_normal");
                    break;
            }


            if(_sourceOwner == Game.instance?._player) {
                RenderData.mdl.VertexBuffer.ApplyColor(Color.Blue);
            } else if(
                  (_sourceOwner.Team.Dispositions.ContainsKey(Game.instance._player.Team) &&
                   _sourceOwner.Team.Dispositions[Game.instance._player.Team] == Faction.Disposition.Allied) ||

                 (!_sourceOwner.Team.Dispositions.ContainsKey(Game.instance._player.Team) &&
                   _sourceOwner.Team.DefaultDisposition == Faction.Disposition.Allied)) {
                RenderData.mdl.VertexBuffer.ApplyColor(Color.Green);
            } else {
                RenderData.mdl.VertexBuffer.ApplyColor(Color.Red);
            }
            
            _damage = _source.Damage * _sourceOwner.RangedMult;


            //TODO: Bullet Size
            
            //InitialPerspective = MatrixExtensions.CreateScaleMatrix(new Vector2(_hitrange / dataLoader.D3DResources[RenderData.ResID].Description.Width)).Translate(new Vector2(-_hitrange / 2));

            //init ignore list (do not hit owner)
            _nonTargets = new List<CharacterBase>();
            _nonTargets.Add(_sourceOwner);

            //tracking lists initialisation
            if(_source.Behaviour.HasFlag(BulletBehaviour.Tracking)) {
                // get all targets inside the aim-radius
                _lockOnTargets = LockOnTargetList;

                //if we have a multi-laser shot, remove the previously reserved targets from the list
                if(_source.Behaviour.HasFlag(BulletBehaviour.Beam)) {
                    _lockOnTargets.RemoveAll((targ) => reserved != null && reserved.Contains(targ));
                }

                // even with the reservations, we still have targets left, 
                if(_source.Behaviour.HasFlag(BulletBehaviour.Beam) && _lockOnTargets.Count > 0) {
                    //reserve the next selected target
                    nextTarget();
                    if(reserved != null && !reserved.Contains(_currentTarget))
                        reserved.Add(_currentTarget);

                    // all targets are selected, reset the reservation and start over with the first target
                } else if(_source.Behaviour.HasFlag(BulletBehaviour.Beam) && TargetList.Count > 0) {
                    reserved.Clear();
                    _lockOnTargets = LockOnTargetList;
                    nextTarget();
                    reserved.Add(_currentTarget);
                }
            } // non-beam tracking bullets will find their target on their own.

            _disposeMe = false;

			this.register();// GameStatus.addRenderable(this);
            GameStatus.addTickable(this);
        }
        

        //TODO: Add bullet sizes

        public void Tick() {

            if(!_initiated)
                init();
            //bullet is still alive
            if(!_disposeMe) {

                //non beam bullet can have their direction modified without hitting a thing
                if(!_source.Behaviour.HasFlag(BulletBehaviour.Beam)) {

                    #region bulletpath

                    // increase speed and direction towards source
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Returning)) {
                        
                        if(_sourceOwner != null) {
                            float trckStr = GameStatus.TimeMultiplier * (float)Math.Max(
                                // min tracking = 0° per second (makes time delay easier)
                                0,
                                //max tracking = 90° per second
                                Math.Min(0.25,
                                //            seconds passed - 1 * 0.25 = after one second tracking goes from 0°/s to 90°/s over 4 seconds (1 second / 0.25 = 4 seconds)
                                (Program.stopwatch.Elapsed.TotalSeconds - creationtime - 1) * 0.25));
                            _direction = _direction.track(pos.angleTo(_sourceOwner.Location), trckStr * _RNG.NextFloat(0.90f, 1.10f), perfect: true);
                        }
                    }

                    // make direction move towards the current target
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Tracking)) {
                        //get a target if there isn't one yet or it is dead.
                        if(TargetList.Count > 0 && (_currentTarget == null || _currentTarget.Health <= 0))
                            nextTarget();
                        if(_currentTarget != null) {
                            float trckStr =
                                (float)(Program.stopwatch.Elapsed.TotalSeconds - creationtime) *
                                GameStatus.TimeMultiplier * 0.1f *
                                Math.Min(Math.Max(_speed / 1000, 0.1f), 2) *
                                Math.Min(Math.Max(_source.Range / 1000, 0.5f), 2) *
                                (_source.Precision == 0 ? 1 : _source.Precision) *
                                (0.9f + (float)_RNG.NextDouble() * 0.2f);
                            _direction = _direction.track(pos.angleTo(_currentTarget.Location), trckStr);
                        }
                    }
                    #endregion
                }

                Hitscan();

                //relativePos = pos + MatrixExtensions.PVTranslation;

                switch(_source.WType) {
                    // rotate
                    case WeaponType.Throwable:
                    case WeaponType.Acid:
                        RenderData.mdl.VertexBuffer.ApplyRotation(Vector3.UnitZ, GameStatus.TimeMultiplier * (float)Math.PI * 2f);
                        //Transform.TransformMatrix = InitialPerspective.Rotate2D(new AngleSingle(pseudorandom % 1f + (float)Program.stopwatch.Elapsed.TotalSeconds % 1f, AngleType.Revolution)).Translate(relativePos);
                        break;
                    // apply direction
                    default:
                        //Transform.TransformMatrix = InitialPerspective.Rotate2D(_direction).Translate(relativePos);
                        break;
                }
                /*
                if(Effect != null)
                    Effect.Dispose();

                ColorFilter.SetInput(0, bullettexture, false);

                Effect = Transform.Output;
                */
                // add beam effect
                if (_source.Behaviour.HasFlag(BulletBehaviour.Beam))
                    new Beam(lastPos, pos, 3, 2, true, 12, _RNG.Next(), Color.Aqua); 

                //basic removal check. 
                if(_distance >= (_source.Range * _source.Range) - 1 || _hitsRemain == 0) {
                    _disposeMe = true;
                }


                //bullet won't be needed anymore. if it wasn't rendered yet, wait with removal...
            } else if(_rendered) {
                Dispose();
            }
        }

        private void Hitscan() {
            //current position is now the previous position
            lastPos = pos;
            float _add;
            //current position is moving according to it's speed, or if the range isn't sufficient, according to that.
            if(_distance + (_speed * _speed) * GameStatus.TimeMultiplier > (_source.Range * _source.Range)) {
                pos = lastPos.move(_direction, _source.Range - (float)Math.Sqrt(_distance));
            } else {
                pos = lastPos.move(_direction, _speed * GameStatus.TimeMultiplier);
            }

            //check each object in the target list (all objects minus the ignored ones), sorted by their distance to the previous position
            foreach(CharacterBase targ in TargetList.OrderBy((targ) => Vector2.DistanceSquared(lastPos, targ.Area.Center) - targ.Size / 2f)) {
                //calculate the actual distance of the bullet path to the target
                float dist = targ.Area.Center.distanceToLine(pos, lastPos) - targ.Size / 2;

                //distance is negative if the target is inside the bullet path (0 is still a collision, even if there wouldn't be any damage, it's a game, so...)
                if(dist <= 0 || (dist <= targ.Size && _source.Behaviour.HasFlag(BulletBehaviour.Beam))) {
                    Vector2 col = targ.collisionPoint(lastPos, pos);

                    if(_source.Behaviour.HasFlag(BulletBehaviour.Explosive)) {
                        float size = _source.Range / 10;
                        new Explosion(new RectangleF(col.X - size / 2, col.Y - size/2, size,size));
                        uint potentialtargets = _source.BulletHitCount;
                        foreach(CharacterBase expltarg in TargetList.OrderBy((targ2) => Vector2.DistanceSquared(col, targ2.Area.Center))) {
                            if(potentialtargets <= 0)
                                break;
                            if(expltarg == targ)
                                continue;
                            float distance = Vector2.DistanceSquared(expltarg.Area.Center, col) + (expltarg.Area.TopLeft - expltarg.Area.BottomRight).LengthSquared() / 2;
                            if(distance - (_source.Range/10) * (_source.Range / 10) <= 0) {
                                _sourceOwner.Attack(expltarg, _damage / (distance / ((_source.Range / 10) * (_source.Range / 10))));
                                potentialtargets--;
                            }
                            
                        }
                    }
                    //make the target move depending on relative damage, size and direction
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Knockback)) {
                        targ.MovementVector += _direction.toVector() * _damage;
                    }

                    // change target if not multihit and target matches
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Tracking) &&
                        !_source.Behaviour.HasFlag(BulletBehaviour.MultiHit) &&
                        _currentTarget != null && targ == _currentTarget &&
                        _hitsRemain > 1) {
                        nextTarget();
                    }

                    // damage the target
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Beam)) {
                        // if beam, damage downscales with distance
                        _sourceOwner.Attack(targ, _damage - _damage * ((float)Math.Sqrt(_distance) + Vector2.Distance(targ.Location,lastPos)) / _source.Range );
                    } else {
                        _sourceOwner.Attack(targ, _damage);
                    }

                    //register hit on the counter
                    _hitsRemain--;

                    //change bullet direction relative to the hit position of the target
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Bounce)) {
                        bounceOff(targ);
                    }

                    //new SharpDX.Direct2D1.SpriteBatch(Program._RenderTarget.QueryInterface<DeviceContext3>())
                    
                }

                // all hits used up, break out of the loop and make the hit appear clean
                if(_hitsRemain <= 0) {
                    if(Vector2.DistanceSquared(lastPos, targ.Area.Center) < (targ.Size * targ.Size) / 2 && Vector2.DistanceSquared(pos, targ.Area.Center) < (targ.Size * targ.Size) / 2) {
                        _speed = 0;
                        pos = lastPos;
                    } else {
                        _speed = Vector2.Distance(lastPos, targ.collisionPoint(lastPos, pos)) / GameStatus.TimeMultiplier;
                        pos = lastPos.move(_direction, _speed * GameStatus.TimeMultiplier);
                    }
                    break;
                }
            }
            // add travelled distance to distance counter
            _add = (float)Math.Sqrt(_distance) + Vector2.Distance(pos, lastPos);
            _distance = _add * _add;
        }

        private void nextTarget() {

            // ignore current target for the rest of the bullet lifetime
            if(_currentTarget != null) {
                _nonTargets.Add(_currentTarget);
            }

            // create some reusable conditions
            List<bool> conditions = new List<bool>();
            conditions.Add(TargetList.Count > 0 && !_source.Behaviour.HasFlag(BulletBehaviour.Beam));
            conditions.Add(_lockOnTargets != null && _lockOnTargets.Count > 0 && _source.Behaviour.HasFlag(BulletBehaviour.Beam) && _nonTargets.Count == 1);
            conditions.Add(TargetList.Count > 0 && _source.Behaviour.HasFlag(BulletBehaviour.Beam) && _nonTargets.Count > 1);

            // if any of these is true, it means we can switch to the next target
            if(conditions.Any((val) => val)) {

                // beams also have to change bullet path if there's a new target
                if(_source.Behaviour.HasFlag(BulletBehaviour.Beam)) {
                    // first beam target is being selected, only use those in range.
                    if (conditions[1])
                        _currentTarget = _lockOnTargets.OrderBy((targ) => Vector2.Distance(pos, targ.Location)).First();

                    // not first target, so this means the beam is jumping, here we can ignore the range and just select the closest one
                    else if (conditions[2])
                        _currentTarget = TargetList.OrderBy((targ) => Vector2.Distance(pos, targ.Location)).First();

                    //update speed and direction to instantly hit the next target
                    _speed = Vector2.Distance(pos, _currentTarget.Location) / GameStatus.TimeMultiplier;
                    _direction = pos.angleTo(_currentTarget.Location);


                    //normal bullets will smartly seek a target the user is aiming at. if it is too far away to even hit it'll try to find a closer one
                } else if (LockOnTargetList.Count > 0) {
                    _currentTarget = LockOnTargetList.OrderBy((targ) => _direction.difference(pos.angleTo(targ.Location),true)).First();
                } else {
                    _currentTarget = TargetList.OrderBy((targ) => _direction.difference(pos.angleTo(targ.Location), true)).First(); }

                //no targets are left, dispose
            } else
                _disposeMe = true;
        }

        private void bounceOff(CharacterBase collision) {
            //change the direction and target-location of the bullet to the directon of the point of impact from the target location
            pos = collision.collisionPoint(lastPos,pos);
            _direction = collision.Location.angleTo(pos);
            if (!_source.Behaviour.HasFlag(BulletBehaviour.Beam))
                _speed *= 0.9f;
        }

        private bool disposed = false;

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            if(disposed)
                return;
            if(disposing) {
				this.unregister();
				//GameStatus.removeRenderable(this);
                GameStatus.removeTickable(this);
                //Transform.Dispose();
            }

            disposed = true;
        }

        private void disposeList<T>(ref List<T> list) {
            if(list != null) {
                list.Clear();
                list = null;
            }
        }

        private void init() {

            lastPos = _sourceOwner.Area.Center;
            pos = lastPos;
            //relativePos = pos + MatrixExtensions.PVTranslation;

            //non bounce or piercing bullets only hit once.
            if(!_source.Behaviour.HasFlag(BulletBehaviour.Bounce) && !_source.Behaviour.HasFlag(BulletBehaviour.Piercing))
                _hitsRemain = 1;
            else
                _hitsRemain = _source.BulletHitCount;

            if(!(_source.Behaviour.HasFlag(BulletBehaviour.Beam | BulletBehaviour.Tracking) && _currentTarget != null)) {

                //beam always hits instantly
                if(_source.Behaviour.HasFlag(BulletBehaviour.Beam))
                    _speed = _source.Range / GameStatus.TimeMultiplier;
                else
                    _speed = _source.BulletSpeed;

                //randomize bullet direction based on aim and precision
                _direction.Revolutions = _sourceOwner.AimDirection.Revolutions
                    + ((1 - _sourceOwner.PrecisionR)
                    - (1 - _sourceOwner.PrecisionR) * 2 * (float)_RNG.NextDouble()) / 2;
            }

            _initiated = true;
        }
    }
}