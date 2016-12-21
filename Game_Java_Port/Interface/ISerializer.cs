using System;
using System.Collections.Generic;

namespace Game_Java_Port.Interface {

    /// <summary>
    /// Universal serialisation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISerializable<T> where T : ISerializable<T> {
        Serializer<T> Serializer { get; }
    }

    /// <summary>
    /// Helper class to implement serialisation on any object type
    /// </summary>
    /// <typeparam name="T">The type of the object to (de)serialize</typeparam>
    public abstract class Serializer<T> where T : ISerializable<T> {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static Serializer<T> _instance;
        /// <summary>
        /// Singleton getter
        /// </summary>
        public static Serializer<T> Instance {
            get {
                return SerializerFactory.get<T>();
            }
        }
        /// <summary>
        /// Serializes the object T into a byte array. Optionally additional parameters may be given via the data parameters.
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <param name="data">Additional data (optional)</param>
        /// <returns>The object as byte array to be used in the Deserialize method.</returns>
        public abstract byte[] Serialize(T obj, params object[] data);
        /// <summary>
        /// Wraps the non-static interface method as static method.
        /// <para/>
        /// see <see cref="Serialize(T, object[])"/>
        /// </summary>
        public static byte[] Serial(T obj, params object[] data) { return Instance.Serialize(obj, data); }
        /// <summary>
        /// Deserializes the object T inside the buffer at the given position back to an instance of the class T.
        /// </summary>
        /// <param name="buffer">the buffer containing the object</param>
        /// <param name="pos">the position at which the object is located</param>
        /// <returns>an instance of the object T</returns>
        public abstract T Deserialize(byte[] buffer, ref int pos);
        /// <summary>
        /// Wraps the non-static interface method as static method.
        /// <para/>
        /// see <see cref="Deserialize(byte[], ref int)"/>
        /// </summary>
        public static T Deserial(byte[] buffer, ref int pos) { return Instance.Deserialize(buffer, ref pos); }
    }

    public static class SerializerFactory{

        private static Dictionary<Type, object> data;

        public static Serializer<T> get<T>() where T : ISerializable<T>{
            if (data == null) {
                data = new Dictionary<Type, object>();
                data.Add(typeof(ItemBase), new Serializers.ItemSerializer());
                data.Add(typeof(Weapon), new Serializers.WeaponSerializer());
                data.Add(typeof(CharacterBase), new Serializers.CharacterSerializer());
                data.Add(typeof(NPC), new Serializers.NPCSerializer());
            }
            return (Serializer<T>)data[typeof(T)];
        }

    }
}
