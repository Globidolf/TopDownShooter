using SharpDX;
using System;
using System.Collections.Generic;

using static System.BitConverter;

namespace Game_Java_Port.Serializers {

    public class NPCSerializer : Interface.Serializer<NPC> {
        public override NPC Deserialize(byte[] buffer, ref int pos) {
            NPC temp;
            if(buffer.getEnumByte<CharacterBase.GenerationType>(ref pos) != CharacterBase.GenerationType.NPC)
                throw new InvalidOperationException("Attempted to Serialize a non-NPC object as NPC");
            int Seed = buffer.getInt(ref pos);
            if(Seed != -1) {
                temp = new NPC(buffer.getUInt(ref pos), Seed, add: false);
            } else {
                temp = new NPC();
                temp.Rank = buffer.getEnumByte<Rank>(ref pos);
                if(temp.Rank == Rank.Player)
                    temp.AI = AI_Library.PlayerSim;
                else if(temp.Rank != Rank.Furniture)
                    temp.AI = AI_Library.NPC_AI;
                temp.Seed = Seed;
                temp.Level = buffer.getUInt(ref pos);
                temp.Size = buffer.getFloat(ref pos);
                foreach(Attribute attr in Enum.GetValues(typeof(Attribute))) {
                    temp.Attributes[attr] = buffer.getUInt(ref pos);
                }
            }
            temp.ID = buffer.getULong(ref pos);
            temp.Team = buffer.getEnumByte<FactionNames>(ref pos);
            temp.Pencil.Color = Color.FromRgba(buffer.getInt(ref pos));
            temp.Location = new Vector2(buffer.getFloat(ref pos), buffer.getFloat(ref pos));
            temp.MovementVector.X = buffer.getFloat(ref pos);
            temp.MovementVector.Y = buffer.getFloat(ref pos);
            temp.DirectionVector.X = buffer.getFloat(ref pos);
            temp.DirectionVector.Y = buffer.getFloat(ref pos);
            temp.AimDirection = new AngleSingle(buffer.getFloat(ref pos), AngleType.Radian);
            temp.Health = buffer.getFloat(ref pos);
            temp.Exp = buffer.getUInt(ref pos);
            temp.Name = buffer.getString(ref pos);
            int inventoryItems = buffer.getInt(ref pos);
            int equippedWeaponL = buffer.getInt(ref pos);
            int equippedWeaponR = buffer.getInt(ref pos);

            for(int i = 0; i < inventoryItems; i++) {
                ItemSerializer.Instance.Deserialize(buffer, ref pos).PickUp(temp);
            }

            if(equippedWeaponL >= 0) {
                temp.EquippedWeaponL = (Weapon)temp.Inventory[equippedWeaponL];
            }

            if(equippedWeaponR >= 0) {
                temp.EquippedWeaponR = (Weapon)temp.Inventory[equippedWeaponR];
            }

            return temp;
        }

        public override byte[] Serialize(NPC obj, params object[] data) {
            if(obj.Pencil.IsDisposed)
                throw new InvalidOperationException("Attempted to serialize disposed object!");

            List<byte> cmd = new List<byte>();

            cmd.Add((byte)obj.GenType);

            cmd.AddRange(GetBytes(obj.Seed));
            if(obj.Seed != -1) {
                cmd.AddRange(GetBytes(obj.Level));
            } else {
                cmd.Add((byte)obj.Rank);
                cmd.AddRange(GetBytes(obj.Level));
                cmd.AddRange(GetBytes(obj.Size));
                foreach(Attribute attr in Enum.GetValues(typeof(Attribute))) {
                    cmd.AddRange(GetBytes(obj.Attributes[attr]));
                }
            }
            cmd.AddRange(GetBytes(obj.ID));
            cmd.Add((byte)(FactionNames)obj.Team);
            cmd.AddRange(GetBytes(((Color4)obj.Pencil.Color).ToRgba()));
            cmd.AddRange(GetBytes(obj.Location.X));
            cmd.AddRange(GetBytes(obj.Location.Y));
            cmd.AddRange(GetBytes(obj.MovementVector.X));
            cmd.AddRange(GetBytes(obj.MovementVector.Y));
            cmd.AddRange(GetBytes(obj.DirectionVector.X));
            cmd.AddRange(GetBytes(obj.DirectionVector.Y));
            cmd.AddRange(GetBytes(obj.AimDirection.Radians));
            cmd.AddRange(GetBytes(obj.Health));
            cmd.AddRange(GetBytes(obj.Exp));
            cmd.AddRange(obj.Name == null ? GetBytes(0) : obj.Name.serialize());

            cmd.AddRange(GetBytes(obj.Inventory.Count));
            if(obj.EquippedWeaponL == null)
                cmd.AddRange(GetBytes(-1));
            else
                cmd.AddRange(GetBytes(obj.Inventory.IndexOf(obj.EquippedWeaponL)));
            if(obj.EquippedWeaponR == null)
                cmd.AddRange(GetBytes(-1));
            else
                cmd.AddRange(GetBytes(obj.Inventory.IndexOf(obj.EquippedWeaponR)));

            foreach(ItemBase item in obj.Inventory) {
                cmd.AddRange(ItemSerializer.Serial(item));
            }

            return cmd.ToArray();
        }
    }

    public class CharacterSerializer : Interface.Serializer<CharacterBase> {
        public override CharacterBase Deserialize(byte[] buffer, ref int pos) {

            CharacterBase.GenerationType GenType = buffer.getEnumByte<CharacterBase.GenerationType>(ref pos);
            pos--; // peek byte

            switch(GenType) {
                case CharacterBase.GenerationType.NPC: return NPCSerializer.Deserial(buffer, ref pos);
                case CharacterBase.GenerationType.INVALID:
                default: throw new InvalidOperationException("Attempted to deserialize an invalid (Game-)Character object: " + GenType.ToString());
            }
        }

        public override byte[] Serialize(CharacterBase obj, params object[] data) {
            switch(obj.GenType) {
                case CharacterBase.GenerationType.NPC: return NPCSerializer.Serial((NPC)obj, data);
                default: throw new NotImplementedException("A new Type of Character was being serialized which has not been implemented serialisation yet.");
            }
        }
    }
}
