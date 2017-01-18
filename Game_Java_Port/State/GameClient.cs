using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpDX;
using Game_Java_Port.Interface;

namespace Game_Java_Port {

    class GameClient : IDisposable {
        private bool _initiated = false;
        public readonly TcpClient Client;
        public Thread ClientThread { get; }
        public bool Listen { get; set; }
        public List<string> commandStack;
        public GameClient(string IP, int port) {
            byte[] buffer;
            Client = new TcpClient(IP, port);
            Listen = true;
            ClientThread = new Thread(new ThreadStart(async () =>
           {
               NetworkStream clientStream = Client.GetStream();
               while(Listen) {
                   byte[] errorcmd = null;
                   try {
                       if(!Client.Connected) {
                           Game.instance.addMessage("Connection lost...");
                           Client.Close();
                           GameStatus.reset();
                       } else if(Client.Available > 0) {
                           buffer = new byte[Client.Available];
                               clientStream.Read(buffer, 0, buffer.Length);
                               clientStream.Flush();
                               commandStack = new List<string>();

                               List<byte[]> commands = new List<byte[]>();

                               int i = 0;
                               int size = 0;


                               do {
                                   i += size;
                                   size = buffer.getInt(ref i);
                                   i -= CustomMaths.intsize;
                                   byte[] cmd = new byte[size];
                                   Array.ConstrainedCopy(buffer, i, cmd, 0, size);
                                   commands.Add(cmd);
                               } while(i + size < buffer.Length);

                               commands.ForEach((cmd) =>
                               {
                                   errorcmd = cmd;
                                   parseCommand(cmd);
                               });
                       }
                   } catch(Exception e) {
                       e.Data.Add("command", errorcmd);
                       parseCommand(errorcmd);
                       Game.instance.addMessage("Connection lost. Reason:");
                       Game.instance.addMessage(e.Message);
                       Client.Close();
                       GameStatus.reset();
                   }
                    //make those commands stack
                    await Task.Delay(20);
               }
           }));
            ClientThread.Start();
        }

        public enum CommandType {
            disconnect,
            init,
            sync,
            updatePos,
            updateAim,
            updateState,
            updateSpeed,
            remove,
            add,
            sendPlayer,
            message,
            updateWpnRngState,
            interaction,
            invalid
        }

        private void parseCommand(byte[] buffer) {

            int pos = 0;

            int length = buffer.getInt(ref pos);

            CommandType cmdType = buffer.getEnumByte<CommandType>(ref pos);

            Game.instance.addMessage(cmdType.ToString());

            commandStack.Add(cmdType.ToString());

            switch(cmdType) {
                case CommandType.disconnect:
                    disconnect(buffer, ref pos);
                    break;
                case CommandType.init:
                    init(buffer, ref pos);
                    break;
                case CommandType.sync:
                    sync(buffer, ref pos);
                    break;
                case CommandType.updatePos:
                    updatePos(buffer, ref pos);
                    break;
                case CommandType.updateAim:
                    updateAim(buffer, ref pos);
                    break;
                case CommandType.updateState:
                    updateState(buffer, ref pos);
                    break;
                case CommandType.updateSpeed:
                    updateSpeed(buffer, ref pos);
                    break;
                case CommandType.remove:
                    remove(buffer, ref pos);
                    break;
                case CommandType.updateWpnRngState:
                    updateWpnRngState(buffer, ref pos);
                    break;
                case CommandType.add:
                    add(buffer, ref pos);
                    break;
                case CommandType.message:
                    message(buffer, ref pos);
                    break;
                case CommandType.sendPlayer:
                    sendPlayer(buffer, ref pos);
                    break;
                case CommandType.interaction:
                    interaction(buffer, ref pos);
                    break;
                case CommandType.invalid:
                default:
                    throw new NotImplementedException("unknown command recieved. are you using the same version as the other players?");
            }
        }

        private void interaction(byte[] buffer, ref int pos) {
            ulong ID_act_src = buffer.getULong(ref pos);
            ulong ID_act_on = buffer.getULong(ref pos);

            CharacterBase interactor;
            IInteractable interact;
            
                interactor = GameStatus.GameSubjects.First((subj) => subj.ID == ID_act_src);
                interact = GameStatus.GameObjects.First((obj) => obj.ID == ID_act_on);

            interact.interact((NPC)interactor);

        }

        private void disconnect(byte[] buffer, ref int pos) {
            Listen = false;
        }

        private void init(byte[] buffer, ref int pos) {
            GameStatus.RNG = buffer.loadRNG(ref pos);
            NameGen.NameRandomizer = buffer.loadRNG(ref pos);

            int count = buffer.getInt(ref pos);
            for(int i = 0; i < count; i++) {
                CharacterBase temp = Serializers.CharacterSerializer.Deserial(buffer, ref pos);
                Program.DebugLog.Add("Adding Subject " + temp.ID + ". GameClient.init(byte[], ref int).");
                temp.addToGame();
            }
            _initiated = true;
        }

        private void sync(byte[] buffer, ref int pos) {

            if(!Game.state.HasFlag(Game.GameState.Menu) && Game.instance.statechanged) {
                Game.instance._client.send(CommandType.updatePos, Game.instance.SerializePos());
                Game.instance._client.send(CommandType.updateSpeed, Game.instance.SerializeSpeed());
                Game.instance._client.send(CommandType.updateAim, Game.instance.SerializeAimDir());
                    Game.instance.statechanged = false;
            }

            int sizeindex = pos - CustomMaths.intsize;

            int count = buffer.getInt(ref pos);
            int i = 0;
            while(i < count && pos < buffer.Length) {
                ulong ID = buffer.getULong(ref pos);
                CharacterBase target;

                    target = GameStatus.GameSubjects.First((obj) => obj.ID == ID);

                target.setState(buffer, ref pos);
                i++;
            }
        }

        private void updatePos(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            if(target != Game.instance._player) {
                target.Location = new Vector2(buffer.getFloat(ref pos), buffer.getFloat(ref pos));
            } else {
                pos += CustomMaths.floatsize * 2;
            }
        }


        private void updateSpeed(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            if(target != Game.instance._player) {
                    target.MovementVector.X = buffer.getFloat(ref pos);
                    target.MovementVector.Y = buffer.getFloat(ref pos);
            } else {
                pos += CustomMaths.floatsize * 2;
            }
        }

        private void updateAim(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            if (target != Game.instance._player) {
                target.AimDirection = new AngleSingle(buffer.getFloat(ref pos),AngleType.Radian);
            } else {
                pos += CustomMaths.floatsize;
            }
        }

        private void updateState(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            target.setInputState(buffer, ref pos);
        }

        private void remove(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            CharacterBase remove;
                remove = GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            Program.DebugLog.Add("Removing Subject " + remove.ID + ". GameClient.remove(byte[],ref int).");
            remove.removeFromGame();
        }

        private void updateWpnRngState(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            target.setWeaponRandomState(buffer, ref pos);
        }

        private void add(byte[] buffer, ref int pos) {
            CharacterBase temp = Serializers.CharacterSerializer.Deserial(buffer, ref pos);
            Program.DebugLog.Add("Adding Subject " + temp.ID + ". GameClient.add(byte[], ref int).");
            temp.addToGame();
        }

        private void message(byte[] buffer, ref int pos) {
            Game.instance.addMessage(buffer.getString(ref pos));
        }

        private void sendPlayer(byte[] buffer, ref int pos) {
            Game.instance._player = Serializers.NPCSerializer.Deserial(buffer, ref pos);
            Game.instance._player.AI = AI_Library.RealPlayer;
            Program.DebugLog.Add("Adding Subject " + Game.instance._player.ID + ". GameClient.sendPlayer(byte[], ref int).");
            Game.instance._player.addToGame();
            Game.instance.addMessage("Connection successful!");
        }

        public void send(CommandType cmdtype, byte[] data) {
            byte[] concat = new byte[(data == null ? 0 : data.Length) + CustomMaths.bytesize + CustomMaths.intsize];
            BitConverter.GetBytes(concat.Length).CopyTo(concat, 0);
            concat[CustomMaths.intsize] = (byte)cmdtype;
            if (data != null)
                data.CopyTo(concat, CustomMaths.intsize + CustomMaths.bytesize);
            try {
                Client.GetStream().Write(concat, 0, concat.Length);
            } catch(Exception e) {
                Game.instance.addMessage("Connection lost: " + e.Message);
                Client.Close();
                GameStatus.reset();
            }
        }

        public async Task awaitInit() {
            await Task.Run(async () =>
            {
                while(!_initiated) {
                    await Task.Delay(100);
                }
            });
            return;
        }

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {

                if(disposing) {
                    Client.Close();
                }

                disposed = true;
            }
        }
        public void Dispose() {
            Dispose(true);
        }
        #endregion

    }
}
