﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpDX;

namespace Game_Java_Port {

    class GameClient {
        private bool _initiated = false;
        public TcpClient Client { get; }
        public Thread ClientThread { get; }
        public bool Listen { get; set; }
        public GameClient(string IP, int port) {
            byte[] buffer;
            int pos;
            Client = new TcpClient(IP, port);
            Listen = true;
            ClientThread = new Thread(new ThreadStart( async() =>
            {
                NetworkStream clientStream = Client.GetStream();
                while(Listen) {
                    try {
                        if (!Client.Connected) {
                            Game.instance.addMessage("Connection lost...");
                            Client.Close();
                            GameStatus.reset();
                        }else if(Client.Available > 0) {
                            buffer = new byte[Client.Available];
                            lock(clientStream) {
                                clientStream.Read(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            lock(this) {
                                pos = 0;
                                while(pos < buffer.Length) {
                                    parseCommand(buffer, ref pos);
                                }
                            }
                        }
                    } catch(Exception e) {
                        Game.instance.addMessage("Connection lost. Reason:");
                        Game.instance.addMessage(e.Message);
                        Client.Close();
                        GameStatus.reset();
                    }
                    //make those commands stack
                    await Task.Delay(20);
                }
                Client.Close();
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
            invalid
        }

        private void parseCommand(byte[] buffer, ref int pos) {

            int length = buffer.getInt(ref pos);

            CommandType cmdType = buffer.getEnumByte<CommandType>(ref pos);

            //Game.instance.addMessage("CMD" + cmdType);



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
                case CommandType.invalid:
                default:
                    throw new NotImplementedException("unknown command recieved. are you using the same version as the other players?");
            }
        }

        private void disconnect(byte[] buffer, ref int pos) {
            Listen = false;
        }

        private void init(byte[] buffer, ref int pos) {
            GameStatus.RNG = buffer.loadRNG(ref pos);
            NameGen.NameRandomizer = buffer.loadRNG(ref pos);

            int count = buffer.getInt(ref pos);
            for (int i = 0; i < count; i++)
                NPC.Deserialize(buffer, ref pos).addToGame();
            _initiated = true;
        }

        private void sync(byte[] buffer, ref int pos) {

            if(Game.instance._player != null && Game.instance.statechanged) {
                Game.instance._client.send(CommandType.updatePos, Game.instance.SerializePos());
                Game.instance._client.send(CommandType.updateSpeed, Game.instance.SerializeSpeed());
                Game.instance._client.send(CommandType.updateAim, Game.instance.SerializeAimDir());
                lock(Game.instance)
                    Game.instance.statechanged = false;
            }

            int sizeindex = pos - CustomMaths.intsize;
            
                lock(GameStatus.GameSubjects) {
                    int count = buffer.getInt(ref pos);
                    int i = 0;
                    while(i < count && pos < buffer.Length) {
                        ulong ID = buffer.getULong(ref pos);
                        AttributeBase target;

                        target = GameStatus.GameSubjects.First((obj) => obj.ID == ID);

                        target.setState(buffer, ref pos);
                        i++;
                    }
                }
        }

        private void updatePos(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
            lock(GameStatus.GameSubjects) {
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            }
            if(target != Game.instance._player) {
                target.Location = new Vector2(buffer.getFloat(ref pos), buffer.getFloat(ref pos));
            } else {
                pos += CustomMaths.floatsize * 2;
            }
        }


        private void updateSpeed(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
            lock(GameStatus.GameSubjects) {
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            }
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
            lock(GameStatus.GameSubjects) {
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            }
            if (target != Game.instance._player) {
                target.AimDirection = new AngleSingle(buffer.getFloat(ref pos),AngleType.Radian);
            } else {
                pos += CustomMaths.floatsize;
            }
        }

        private void updateState(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
            lock(GameStatus.GameSubjects) {
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            }
            target.setInputState(buffer, ref pos);
        }

        private void remove(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            lock(GameStatus.GameSubjects) {
                GameStatus.GameSubjects.First((obj) => obj.ID == ID).removeFromGame();
            }
        }

        private void updateWpnRngState(byte[] buffer, ref int pos) {
            ulong ID = buffer.getULong(ref pos);
            NPC target = null;
            lock(GameStatus.GameSubjects) {
                target = (NPC)GameStatus.GameSubjects.First((obj) => obj.ID == ID);
            }
            target.setWeaponRandomState(buffer, ref pos);
        }

        private void add(byte[] buffer, ref int pos) {
            lock(GameStatus.GameSubjects)
                NPC.Deserialize(buffer, ref pos).addToGame();
        }

        private void message(byte[] buffer, ref int pos) {
            Game.instance.addMessage(buffer.getString(ref pos));
        }

        private void sendPlayer(byte[] buffer, ref int pos) {
            Game.instance._player = NPC.Deserialize(buffer, ref pos);
            Game.instance._player.AI = AI_Library.RealPlayer;
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

    }
}