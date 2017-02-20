using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using SharpDX;

namespace Game_Java_Port {
    static public class CustomMaths {
        #region behaviours

        private static List<Tuple<BulletBehaviour, BulletBehaviour>> req = new List<Tuple<BulletBehaviour, BulletBehaviour>>() {
            //multihit depends on piercing behaviour.
                new Tuple<BulletBehaviour, BulletBehaviour>(BulletBehaviour.MultiHit, BulletBehaviour.Piercing),
            };

        private static List<BulletBehaviour> invalid = new List<BulletBehaviour>() {
            //applying a boomerang effect with a tracking effect won't work well
            BulletBehaviour.Tracking | BulletBehaviour.Returning,
            // cannot apply two different collision effects
            BulletBehaviour.Bounce | BulletBehaviour.Piercing,
            // tracking beams cannot bounce, they seek their target
            BulletBehaviour.Bounce | BulletBehaviour.Beam | BulletBehaviour.Tracking,
            // no clue how i should do this...
            BulletBehaviour.Beam | BulletBehaviour.Returning
        };

        private static bool RequirementsMet(this BulletBehaviour flags) {
            foreach(Tuple<BulletBehaviour, BulletBehaviour> pair in req) {
                BulletBehaviour both = pair.Item1 | pair.Item2;
                if(((flags & both) ^ pair.Item2) == both)
                    return false;
            }
            return true;
        }

        //Checks the flags of the behaviour if there are impossibilities.
        public static bool isValid(this BulletBehaviour flags) {

            if(!flags.RequirementsMet())
                return false;
            foreach(BulletBehaviour invFlag in invalid) {
                if(invalid.Contains(flags & invFlag))
                    return false;
            }
            return true;
        }
        public static bool isStackable(this BulletBehaviour flags, BulletBehaviour add) {

            if((flags | add).isValid() && (flags & add) != add)
                return true;
            else
                return false;
        }
        #endregion
        #region angles and points

        public static Vector2 collisionPoint(this CharacterBase inst, Vector2 p1, Vector2 p2) {
            p2 = p2.move(p1.angleTo(p2), inst.Size * Vector2.Distance(inst.Location, p1));
            float sqlen = Vector2.DistanceSquared(p1, p2);
            Vector2 p0 = inst.Location;
            float tan = ((p0.X - p1.X) * (p2.X - p1.X) + (p0.Y - p1.Y) * (p2.Y - p1.Y)) / sqlen;
            Vector2 closestPoint = new Vector2(
                    p1.X + tan * (p2.X - p1.X),
                    p1.Y + tan * (p2.Y - p1.Y));
            return closestPoint.move(closestPoint.angleTo(p1), (float)Math.Cos(Vector2.Distance(closestPoint, inst.Location)  / 2 / inst.Size * Math.PI ) * inst.Size / 2);
        }

        public static float sqDistanceToLine(this Vector2 p0, Vector2 p1, Vector2 p2) {
            float sqlen = Vector2.DistanceSquared(p1, p2);
            float tan;

            if(sqlen == 0)
                return Vector2.DistanceSquared(p0, p1);
            tan = ((p0.X - p1.X) * (p2.X - p1.X) + (p0.Y - p1.Y) * (p2.Y - p1.Y)) / sqlen;
            if(tan < 0)
                return Vector2.DistanceSquared(p0, p1);
            if(tan > 1)
                return Vector2.DistanceSquared(p0, p2);
            return Vector2.DistanceSquared(p0,
                new Vector2(
                    p1.X + tan * (p2.X - p1.X),
                    p1.Y + tan * (p2.Y - p1.Y)));
        }

        public static float distanceToLine(this Vector2 p0, Vector2 p1, Vector2 p2) {
            return (float)Math.Sqrt(p0.sqDistanceToLine(p1, p2));
        }

        public static AngleSingle toAngle(this Vector2 p) {
            return new AngleSingle((float)Math.Atan2(-p.Y, -p.X), AngleType.Radian);
        }

        public static int getREALHashCode<T>(this List<T> me) {
            int hash = 1+me.GetHashCode();
            unchecked {
                foreach(object child in me) {
                    hash *= (1+child.GetHashCode());
                }
            }
            return hash;
        }

        public static AngleSingle difference(this AngleSingle angle, AngleSingle to, bool abs = false) {

            AngleSingle result = angle - to;
            if(abs)
                result.WrapPositive();
            else
                result.Wrap();
            return result;
        }

        public static AngleSingle angleTo(this Vector2 p, Vector2 p2) {
            return new AngleSingle(
                (float)Math.Atan2(p.Y - p2.Y, p.X - p2.X),
                AngleType.Radian);
        }

        public static Vector2 toVector(this AngleSingle angle) {
            return -new Vector2((float)Math.Cos(angle.Radians), (float)Math.Sin(angle.Radians));
        }

        public static Vector2 move(this Vector2 p, AngleSingle direction, float distance) {
            return p + direction.toVector() * distance;
        }

        public static Vector2 absolute(this Vector2 p) {
            return new Vector2(Math.Abs(p.X), Math.Abs(p.Y));
        }

        public static T getEnumByte<T>(this byte[] buffer, ref int pos) where T : struct, IConvertible {
            T temp = (T)Enum.ToObject(typeof(T), buffer[pos]);
            pos += bytesize;
            return temp;
        }

        public static T getEnumShort<T>(this byte[] buffer, ref int pos) where T : struct, IConvertible {
            T temp = (T)Enum.ToObject(typeof(T), buffer.getShort(ref pos));
            return temp;
        }

        public static char getChar(this byte[] buffer, ref int pos) {
            char result = BitConverter.ToChar(buffer, pos);
			Random r;
			
            pos += charsize;
            return result;
        }

        public static string getString(this byte[] buffer, ref int pos) {
            int length = BitConverter.ToInt32(buffer, pos);
            pos += uintsize;

            string result = "";

            for (int i = 0; i < length; i ++) {
                result += buffer.getChar(ref pos);
            }

            return result;
        }

        public static byte[] serialize(this string str) {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(str.Length));
            foreach(char c in str) {
                data.AddRange(BitConverter.GetBytes(c));
            }
            return data.ToArray();
        }

        public static int getProtocolSize(this byte[] buffer) {
            int cmdSize = 0;
            while((cmdSize += BitConverter.ToInt32(buffer, cmdSize)) < buffer.Length);
            return cmdSize;
        }

        public static byte getByte(this byte[] buffer, ref int pos) {
            byte result = buffer[pos];
            pos ++;
            return result;
        }

        public static float getFloat(this byte[] buffer, ref int pos) {
            float result = BitConverter.ToSingle(buffer, pos);
            pos += floatsize;
            return result;
        }

        public static short getShort(this byte[] buffer, ref int pos) {
            short result = BitConverter.ToInt16(buffer, pos);
            pos += shortsize;
            return result;
        }

        public static double getDouble(this byte[] buffer, ref int pos) {
            double result = BitConverter.ToDouble(buffer, pos);
            pos += doublesize;
            return result;
        }

        public static int getInt(this byte[] buffer, ref int pos) {
            int result = BitConverter.ToInt32(buffer, pos);
            pos += intsize;
            return result;
        }

        public static uint getUInt(this byte[] buffer, ref int pos) {
            uint result = BitConverter.ToUInt32(buffer, pos);
            pos += uintsize;
            return result;
        }

        public static long getLong(this byte[] buffer, ref int pos) {
            long result = BitConverter.ToInt64(buffer, pos);
            pos += longsize;
            return result;
        }

        public static ulong getULong(this byte[] buffer, ref int pos) {
            ulong result = BitConverter.ToUInt64(buffer, pos);
            pos += ulongsize;
            return result;
        }

        public static byte[] saveRNG(this Random RNG) {
            byte[] result;
            using(MemoryStream temp = new MemoryStream()) {
                
                    new BinaryFormatter().Serialize(temp, RNG);
                result = temp.ToArray();
            }
            return result;
        }

        public static T getEnum<T>(this Random RNG) where T : struct, IConvertible {
            return (T)Enum.GetValues(typeof(T)).GetValue(RNG.Next(Enum.GetValues(typeof(T)).Length));
        }

        public static FactionNames getValidFaction(this Random RNG) {
            FactionNames temp = RNG.getEnum<FactionNames>();
            while(temp == FactionNames.Environment || temp == FactionNames.Players)
                temp = RNG.getEnum<FactionNames>();
            return temp;
        }

        public static Random loadRNG(this byte[] buffer, ref int pos) {
            Random result;
            try {
                result = (Random)new BinaryFormatter().Deserialize(new MemoryStream(buffer, pos, rngsize));
            } catch(Exception) {
                result = null;
            } finally {
                pos += rngsize;
            }
            return result;
        }

        #endregion

        private static Dictionary<int, Color> ColorCache = new Dictionary<int, Color>();

        public static Color fromArgb(byte A, byte R, byte G, byte B) {
            int index = BitConverter.ToInt32(new byte[] { A, R, G, B }, 0);
            if(!ColorCache.ContainsKey(index))
                ColorCache.Add(index, Color.FromRgba(index));
            return ColorCache[index];
        }

        public const int bytesize = sizeof(byte);
        public const int charsize = sizeof(char);
        public const int floatsize = sizeof(float);
        public const int doublesize = sizeof(double);
        public const int shortsize = sizeof(short);
        public const int intsize = sizeof(int);
        public const int uintsize = sizeof(uint);
        public const int  longsize = sizeof( long);
        public const int ulongsize = sizeof(ulong);
        public const int rngsize = 317;

        public static T MaxValue<T>(this T me) where T : struct, IComparable, IConvertible {
            return MaxValue<T>();
        }

        public static T MaxValue<T>() where T : struct, IComparable, IConvertible {
            object maxValue = default(T);
            TypeCode typeCode = Type.GetTypeCode(typeof(T));
            switch(typeCode) {
                case TypeCode.Byte:
                    maxValue = byte.MaxValue;
                    break;
                case TypeCode.Char:
                    maxValue = char.MaxValue;
                    break;
                case TypeCode.DateTime:
                    maxValue = DateTime.MaxValue;
                    break;
                case TypeCode.Decimal:
                    maxValue = decimal.MaxValue;
                    break;
                case TypeCode.Double:
                    maxValue = decimal.MaxValue;
                    break;
                case TypeCode.Int16:
                    maxValue = short.MaxValue;
                    break;
                case TypeCode.Int32:
                    maxValue = int.MaxValue;
                    break;
                case TypeCode.Int64:
                    maxValue = long.MaxValue;
                    break;
                case TypeCode.SByte:
                    maxValue = sbyte.MaxValue;
                    break;
                case TypeCode.Single:
                    maxValue = float.MaxValue;
                    break;
                case TypeCode.UInt16:
                    maxValue = ushort.MaxValue;
                    break;
                case TypeCode.UInt32:
                    maxValue = uint.MaxValue;
                    break;
                case TypeCode.UInt64:
                    maxValue = ulong.MaxValue;
                    break;
                default:
                    maxValue = default(T);//set default value
                    break;
            }

            return (T)maxValue;
        }

        public static T MinValue<T>(this T me) where T : struct, IComparable, IConvertible {
            return MinValue<T>();
        }

        public static T MinValue<T>() where T : struct, IComparable, IConvertible {
            object minvalue = default(T);
            TypeCode typeCode = Type.GetTypeCode(typeof(T));
            switch(typeCode) {
                case TypeCode.Byte:
                    minvalue = byte.MinValue;
                    break;
                case TypeCode.Char:
                    minvalue = char.MinValue;
                    break;
                case TypeCode.DateTime:
                    minvalue = DateTime.MinValue;
                    break;
                case TypeCode.Decimal:
                    minvalue = decimal.MinValue;
                    break;
                case TypeCode.Double:
                    minvalue = decimal.MinValue;
                    break;
                case TypeCode.Int16:
                    minvalue = short.MinValue;
                    break;
                case TypeCode.Int32:
                    minvalue = int.MinValue;
                    break;
                case TypeCode.Int64:
                    minvalue = long.MinValue;
                    break;
                case TypeCode.SByte:
                    minvalue = sbyte.MinValue;
                    break;
                case TypeCode.Single:
                    minvalue = float.MinValue;
                    break;
                case TypeCode.UInt16:
                    minvalue = ushort.MinValue;
                    break;
                case TypeCode.UInt32:
                    minvalue = uint.MinValue;
                    break;
                case TypeCode.UInt64:
                    minvalue = ulong.MinValue;
                    break;
                default:
                    minvalue = default(T);//set default value
                    break;
            }

            return (T)minvalue;
        }

		public static float mod(float left, float right) {
			float rest = left % right;
			return rest < 0 ? rest + right : rest;
		}
		public static int mod(int left, int right) {
			int rest = left % right;
			return rest < 0 ? rest + right : rest;
		}

		public static bool isInBetween( this AngleSingle a0, AngleSingle a1, AngleSingle a2) {

            //normalize all values
            a0.Wrap();
            a1.Wrap();
            a2.Wrap();
            if(a1 < a2)
                return (a1 <= a0 && a0 <= a2);
            else
                return (a1 <= a0 || a0 <= a2);
        }

        /// <summary>
        /// Calculates an angle by moving the angle a0(this) closer to the target angle by the amount of the strength angle.
        /// <para>
        /// if the 'perfect' parameter is set to true, this will not step over the target angle, making the tracking 'perfect'.
        /// </para>
        /// <para>
        /// Example:
        /// </para>
        /// <para>
        /// track(45°, 50°, 10°, false) returns 55°.
        /// </para>
        /// <para>
        /// track(45°, 50°, 10°, true) returns 50°.
        /// </para>
        /// </summary>
        /// <param name="a0">The base angle.</param>
        /// <param name="target">The target angle.</param>
        /// <param name="strength">The tracking strength.</param>
        /// <param name="perfect">Disallows stepping over the target.</param>
        /// <returns> an Angle </returns>
        public static AngleSingle track(this AngleSingle a0, AngleSingle target, AngleSingle strength, bool perfect = false) {

            //wrap everything to positive values
            a0.WrapPositive();
            target.WrapPositive();
            strength.WrapPositive();
            AngleSingle offset = target - a0;
            offset.WrapPositive();
            

            if (!offset.IsReflex) { // handle positive values(0 to 180)
                if(perfect && strength >= offset)
                    strength = offset;
            } else { // handle negative values (-0 to -180 A.K.A 180 to 360)
                strength = -strength;
                if(perfect && strength <= -offset)
                    strength = -offset;
            }
            // wrap for addition
            strength.Wrap();

            // calculate
            return a0 + strength;

        }
        
        public static AngleSingle track(this AngleSingle a0, AngleSingle target, float strength = 0.1f, AngleType type = AngleType.Revolution, bool perfect = true) {
            //call overload
            return track(a0, target, new AngleSingle(strength, type), perfect);
        }
		public static RectangleF Floor(this RectangleF rect) {
            return new RectangleF((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }
        public static RectangleF Ceil(this RectangleF rect) {
            return new RectangleF((int)(rect.X + 1 - float.Epsilon), (int)(rect.Y + 1 - float.Epsilon), (int)(rect.Width + 1 - float.Epsilon), (int)(rect.Height + 1 - float.Epsilon));
        }

    }

}
