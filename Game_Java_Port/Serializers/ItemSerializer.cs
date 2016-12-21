using Game_Java_Port.Interface;
using System;
using System.Collections.Generic;

using static System.BitConverter;

namespace Game_Java_Port.Serializers {

    public class WeaponSerializer : Serializer<Weapon> {

        public override byte[] Serialize(Weapon obj, params object[] data) {

            List<byte> cmd = new List<byte>();
            cmd.Add(obj.GenType);
            cmd.AddRange(GetBytes(obj.Level));
            cmd.AddRange(GetBytes(obj.Seed));
            switch(obj.GenType) {
                case 0:                                 break;
                case 1:     cmd.Add((byte)obj.WType);   break;
                case 2:     cmd.Add((byte)obj.Rarity);  break;
                case 3:     cmd.Add((byte)obj.WType);
              /*case 3:*/   cmd.Add((byte)obj.Rarity);  break;
                default: throw new InvalidOperationException("Generation Type of Object is not valid for a Weapon: " + obj.GenType);
            }
            return cmd.ToArray();
        }
        public override Weapon Deserialize(byte[] buffer, ref int pos) {
            
            byte GenType = buffer.getByte(ref pos);
            switch(GenType) {
                case 0: //weapon
                    return new Weapon(buffer.getUInt(ref pos), buffer.getInt(ref pos));
                case 1: //weapon with predefined type
                    return new Weapon(buffer.getUInt(ref pos), buffer.getInt(ref pos), buffer.getEnumByte<WeapPreset>(ref pos));
                case 2: //weapon with predefined rarity
                    return new Weapon(buffer.getUInt(ref pos), buffer.getInt(ref pos), rarity: buffer.getEnumByte<ItemType>(ref pos));
                case 3: //both
                    return new Weapon(buffer.getUInt(ref pos), buffer.getInt(ref pos), buffer.getEnumByte<WeapPreset>(ref pos), buffer.getEnumByte<ItemType>(ref pos));
                default: //unknown / invalid
                    throw new InvalidOperationException("Unknown Item Generation Type: " + GenType);
            }
        }
    }

    public class ItemSerializer : Serializer<ItemBase> {
        override public byte[] Serialize(ItemBase obj, params object[] data) {

            List<byte> cmd = new List<byte>();
            if(obj is Weapon)
                cmd.AddRange(WeaponSerializer.Serial((Weapon)obj, data));
            return cmd.ToArray();
        }
        override public ItemBase Deserialize(byte[] buffer, ref int pos) {

            byte GenType = buffer.getByte(ref pos);
            pos--; //Peek byte
            switch(GenType) {
                case 0:
                case 1:
                case 2:
                case 3: // 0 - 3 are weapon generation types
                    return WeaponSerializer.Deserial(buffer, ref pos);
                default: //unknown
                    throw new InvalidOperationException("Unknown Item Generation Type: " + GenType);
            }
        }
    }
}
