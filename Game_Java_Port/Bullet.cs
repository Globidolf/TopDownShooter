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

namespace Game_Java_Port {
    class Bullet : IRenderable, ITickable, IDisposable {

        AttributeBase _sourceOwner;
        Weapon _source;

        Random _RNG;

        AttributeBase _currentTarget;

        List<AttributeBase> _nonTargets;
        List<AttributeBase> _lockOnTargets;

        public Vector2 pos { get; set; } = new Vector2();
        Vector2 lastPos = new Vector2();

        public RectangleF Area { get; set; }

        //public SizeF Size2d { get { return new SizeF((float)Math.Abs(Math.Sin(_direction)) * _speed, (float)Math.Abs(Math.Cos(_direction) * _speed)); } set { } }
        
        uint _hitsRemain;

        float _speed;
        AngleSingle _direction;

        float _distance;

        bool _disposeMe;
        bool _rendered = false;
        bool _initiated = false;

        float _damage;


        SolidColorBrush _pencil;
        Brush pencil { get {
                if(_pencil == null) {
                    _pencil = new SolidColorBrush(Program._RenderTarget, Color.Red);
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Beam)) {
                        _pencil.Color = Color.DarkBlue;
                    }
                    if(_source.WType == WeaponType.Acid)
                        _pencil.Color = Color.Green;
                }
                return _pencil;
            } }

        public void draw(RenderTarget rt) {
            if(_initiated) {
                lock(pencil) {
                    if(!pencil.IsDisposed) {
                        Vector2 relativePos = pos + MatrixExtensions.PVTranslation;
                        Vector2 relativelastPos = lastPos + MatrixExtensions.PVTranslation;
                        rt.DrawLine(relativelastPos, relativePos, pencil, _source.Behaviour.HasFlag(BulletBehaviour.Beam) ? 1 + 10 - (float)Math.Sqrt(_distance) / _source.Range * 10 : 2);
                        //g.DrawLine(pencil, relativePos, relativelastPos);


                        if(_source.Behaviour.HasFlag(BulletBehaviour.Beam)) {
                            int iterations = 4;
                            float distance = Vector2.Distance(pos, lastPos);
                            float offset = distance * (float)_RNG.NextDouble();
                            float length = 1 + (float)_RNG.NextDouble() * (distance - offset) / 2;
                            AngleSingle dir = lastPos.angleTo(pos);
                            Vector2 split1 = relativelastPos.move(dir, offset);
                            Vector2 split2;
                            while(offset < distance && length > (distance - offset) / 12 && iterations > 0) {

                                offset += distance * (float)_RNG.NextDouble();
                                length /= 2;
                                dir.Radians += (float)(-Math.PI / 2 + _RNG.NextDouble() * Math.PI);
                                split2 = split1.move(dir, length);

                                rt.DrawLine(split1, split2, pencil);

                                split1 = split2;
                                iterations--;
                            }
                        }
                    }
                }
                _rendered = true;
            }
        }

        public AttributeBase getCurrentTarget() {
            return _currentTarget;
        }



        private List<AttributeBase> TargetList {
            get {
                List<AttributeBase> temp = new List<AttributeBase>();
                lock(GameStatus.GameSubjects)
                    temp.AddRange(GameStatus.GameSubjects);
                temp.RemoveAll((targ) =>
                {
                    return _nonTargets.Contains(targ);
                });
                return temp;
            }
        }

        private List<AttributeBase> LockOnTargetList { get {
                List<AttributeBase> temp = TargetList;
                temp.RemoveAll((targ) =>
                {
                    //out of range
                    if( Vector2.Distance(lastPos, targ.Location) - targ.Size / 2 > _source.Range)
                        return true;

                    // not in hit-area
                    if(_sourceOwner.Precision != 0 && lastPos.angleTo(targ.Area.Center).isInBetween(
                            new AngleSingle(_sourceOwner.AimDirection.Revolutions + (1 - _sourceOwner.Precision)/2, AngleType.Revolution),
                            new AngleSingle(_sourceOwner.AimDirection.Revolutions - (1 - _sourceOwner.Precision)/2, AngleType.Revolution)))
                        return true;

                    return false;
                });
                return temp;
            } }

        public Bullet(Weapon source, int? seed = null, List<AttributeBase> reserved = null) {

            //initialize RNG
            if(seed == null)
                _RNG = new Random();
            else
                _RNG = new Random((int)seed);

            //set values
            _source = source;
            _sourceOwner = _source.Owner;

            _damage = _source.Damage * _sourceOwner.RangedMult;

            lastPos = _sourceOwner.Area.Center;
            pos = lastPos;

            //init ignore list (do not hit owner)
            _nonTargets = new List<AttributeBase>();
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

            GameStatus.addRenderable(this);
            GameStatus.addTickable(this);

        }

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
                        
                        _speed = Math.Max(
                            10,
                            _speed * (0.96f + 0.1f * (1 - pos.angleTo(_sourceOwner.Area.Center).offset( _direction, true).Revolutions))
                            );
                        
                        if(_sourceOwner != null) {
                            float trckStr = (float)
                                Math.PI * 6 / GameVars.defaultGTPS *
                                Math.Min(Math.Max(_speed / 1000, 0.1f), 2) *
                                Math.Min(Math.Max(_source.Range / 1000, 0.5f), 8) * 
                                (0.9f + (float)_RNG.NextDouble() * 0.2f);
                            _direction = _direction.track(pos.angleTo(_sourceOwner.Area.Center), trckStr);
                        }
                    }

                    // make direction move towards the current target
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Tracking)) {
                        //get a target if there isn't one yet or it is dead.
                        if(_source.Behaviour.HasFlag(BulletBehaviour.Tracking) &&
                                    TargetList.Count > 0 && (_currentTarget == null || _currentTarget.Health <= 0))
                            nextTarget();
                        if(_currentTarget != null) {
                            float trckStr = (float)
                                Math.PI * 6 / GameVars.defaultGTPS *
                                Math.Min(Math.Max(_speed / 1000, 0.1f), 2) *
                                Math.Min(Math.Max(_source.Range / 1000, 0.5f), 2) *
                                (0.9f + (float)_RNG.NextDouble() * 0.2f);
                            _direction = _direction.track(pos.angleTo(_currentTarget.Area.Center), trckStr);
                        }
                    }
                    #endregion
                }

                Hitscan();
                
                //basic removal check. TODO add all disposals here and remove them elsewhere
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
            if(_distance + (_speed * _speed) / GameVars.defaultGTPS > (_source.Range * _source.Range)) {
                pos = lastPos.move(_direction, _source.Range - (float)Math.Sqrt(_distance));
            } else {
                pos = lastPos.move(_direction, _speed / GameVars.defaultGTPS);
            }

            //check each object in the target list (all objects minus the ignored ones), sorted by their distance to the previous position
            foreach(AttributeBase targ in TargetList.OrderBy((targ) => lastPos.squareDist(targ.Area.Center) - targ.Size / 2f)) {
                //calculate the actual distance of the bullet path to the target
                float dist = targ.Area.Center.distanceToLine(pos, lastPos) - targ.Size / 2;

                //distance is negative if the target is inside the bullet path (0 is still a collision, even if there wouldn't be any damage, it's a game, so...)
                if(dist <= 0 || (dist <= targ.Size && _source.Behaviour.HasFlag(BulletBehaviour.Beam))) {
                    Vector2 col = targ.collisionPoint(lastPos, pos);

                    if(_source.Behaviour.HasFlag(BulletBehaviour.Explosive)) {
                        float size = _source.Range / 10;
                        new Explosion(new RectangleF(col.X - size / 2, col.Y - size/2, size,size));
                        uint potentialtargets = _source.BulletHitCount;
                        foreach(AttributeBase expltarg in TargetList.OrderBy((targ2) => col.squareDist(targ2.Area.Center))) {
                            if(potentialtargets <= 0)
                                break;
                            if(expltarg == targ)
                                continue;
                            float distance = expltarg.Area.Center.squareDist(col) + (expltarg.Area.TopLeft - expltarg.Area.BottomRight).LengthSquared() / 2;
                            if(distance - (_source.Range/10) * (_source.Range / 10) <= 0) {
                                _sourceOwner.Attack(expltarg, _damage / (distance / ((_source.Range / 10) * (_source.Range / 10))));
                                potentialtargets--;
                            }
                            
                        }
                    }
                    //make the target move depending on relative damage, size and direction
                    if(_source.Behaviour.HasFlag(BulletBehaviour.Knockback)) {
                        targ.MovementVector.X += -(float)Math.Cos(_direction.Radians) * Math.Min(80000 / GameVars.defaultGTPS, ((_damage / targ.MaxHealth)) * 10000 / GameVars.defaultGTPS);
                        targ.MovementVector.Y += -(float)Math.Sin(_direction.Radians) * Math.Min(80000 / GameVars.defaultGTPS, ((_damage / targ.MaxHealth)) * 10000 / GameVars.defaultGTPS);
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
                }

                // all hits used up, break out of the loop and make the hit appear clean
                if(_hitsRemain <= 0) {
                    if(lastPos.squareDist(targ.Area.Center) < (targ.Size * targ.Size) / 2 && pos.squareDist(targ.Area.Center) < (targ.Size * targ.Size) / 2) {
                        _speed = 0;
                        pos = lastPos;
                    } else {
                        _speed = Vector2.Distance(lastPos, targ.collisionPoint(lastPos, pos)) * GameVars.defaultGTPS;
                        pos = lastPos.move(_direction, _speed / GameVars.defaultGTPS);
                    }
                    _add = (float)Math.Sqrt(_distance) + (float)Math.Sqrt(pos.squareDist(lastPos));
                    _distance = _add * _add;
                    return;
                }
            }
            // add travelled distance to distance counter
            _add = (float)Math.Sqrt(_distance) + (float)Math.Sqrt(pos.squareDist(lastPos));
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
                    _speed = Vector2.Distance(pos, _currentTarget.Location) * GameVars.defaultGTPS;
                    _direction = pos.angleTo(_currentTarget.Location);


                    //normal bullets will smartly seek a target the user is aiming at. if it is too far away to even hit it'll try to find a closer one
                } else lock(LockOnTargetList) if (LockOnTargetList.Count > 0) {
                    _currentTarget = LockOnTargetList.OrderBy((targ) => _direction.offset(pos.angleTo(targ.Area.Center),true)).First();
                } else {
                    _currentTarget = TargetList.OrderBy((targ) => _direction.offset(pos.angleTo(targ.Area.Center), true)).First(); }

                //no targets are left, dispose
            } else
                _disposeMe = true;
        }

        private void bounceOff(AttributeBase collision) {
            //change the direction and target-location of the bullet to the directon of the point of impact from the target location
            pos = collision.collisionPoint(lastPos,pos);
            _direction = collision.Area.Center.angleTo(pos);
            if (!_source.Behaviour.HasFlag(BulletBehaviour.Beam))
                _speed *= 0.9f;
        }

        public void Dispose() {
            GameStatus.removeRenderable(this);
            GameStatus.removeTickable(this);

            lock(pencil) {
                _pencil.Dispose();
            }
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

            //non bounce or piercing bullets only hit once.
            if(!_source.Behaviour.HasFlag(BulletBehaviour.Bounce) && !_source.Behaviour.HasFlag(BulletBehaviour.Piercing))
                _hitsRemain = 1;
            else
                _hitsRemain = _source.BulletHitCount;

            if(!(_source.Behaviour.HasFlag(BulletBehaviour.Beam | BulletBehaviour.Tracking) && _currentTarget != null)) {

                //beam always hits instantly
                if(_source.Behaviour.HasFlag(BulletBehaviour.Beam))
                    _speed = _source.Range * GameVars.defaultGTPS;
                else
                    _speed = _source.BulletSpeed;

                //randomize bullet direction based on aim and precision
                _direction.Revolutions = _sourceOwner.AimDirection.Revolutions
                    + ((1 - _sourceOwner.Precision)
                    - (1 - _sourceOwner.Precision) * 2 * (float)_RNG.NextDouble()) / 2;
            }

            _initiated = true;
        }
    }
}