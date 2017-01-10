using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace Game_Java_Port {
    class Host : IDisposable {
        public readonly Random _HostGenRNG = new Random();
        public const float RefreshesPerSecond = 10;
        public bool ClientsChanged { get; set; }
        public bool Listen { get; set; }
        public TcpListener ClientListener { get; }
        public List<TcpClient> ClientList { get; }
        public Dictionary<TcpClient, ulong> ClientPlayer { get; }
        public Timer ListenerThread { get; }
        public Thread CommunicatorThread { get; }
        public Timer RefresherThread { get; }
        public static object ComLock = new object();
        
        public static Func<Host, TimerCallback> RefreshAction = (host) =>
        { return (obj) => {
            if (Game.instance._client != null && GameStatus.GameSubjects.Count > 0)
                Game.instance._client.send(GameClient.CommandType.sync, GameStatus.serializeStates());
        }; };

        private static Func<Host, TimerCallback> ListenerAction = (host) =>
        {
            return (obj) =>
            {
                if(host.Listen) {
                    while(host.ClientListener.Pending()) {
                        TcpClient newClient = host.ClientListener.AcceptTcpClient();
                        lock(host.ClientList)
                            host.ClientList.Add(newClient);
                        lock(host)
                            host.ClientsChanged = true;
                        List<byte> data = new List<byte>();
                        
                        //the only command not sent via the client. initialisation of all values when connecting.

                        data.Add((byte)GameClient.CommandType.init);
                        data.AddRange(GameStatus.Serialize());
                        data = BitConverter.GetBytes(data.Count + CustomMaths.intsize).Concat(data).ToList();
                        newClient.GetStream().Write(data.ToArray(), 0, data.Count);
                    }
                }
            };
        };

        private static Func<Host, ThreadStart> CommunicatorAction = (host) =>
        {
            return async() =>
            {
                //declare variables outside of loops to prevent repetitive memory allocations
                List<TcpClient> tempList = new List<TcpClient>();
                NetworkStream requestStream;
                NetworkStream sendStream;
                byte[] buffer;
                while(host.Listen) {
                    // prevents multiple actions running at the same time.
                    lock(ComLock) {
                        //check if clients have been added or removed and make a new copy of the list if true
                        if(host.ClientsChanged) {
                            lock(host.ClientList) {
                                tempList.Clear();
                                tempList.AddRange(host.ClientList);
                                //changes applied, confirm during lock statement to prevent wrong true's/false's
                                lock (host)
                                    host.ClientsChanged = false;
                            }
                        }

                        //iterate all clients
                        foreach(TcpClient request in tempList) {
                            //check if client disconnected, remove if true.
                            if (!request.Connected) {
                                CharacterBase clientplayer;
                                lock(host.ClientPlayer)
                                    try {
                                        lock(GameStatus.GameSubjects)
                                            clientplayer = GameStatus.GameSubjects.Find((obj) => obj.ID == host.ClientPlayer[request]);
                                        host.ClientPlayer.Remove(request);

                                        Game.instance._client.send(GameClient.CommandType.message, (clientplayer.Name + " is history.").serialize());
                                        Program.DebugLog.Add("Sending Remove Req: " + clientplayer.ID + ". Host.CommunicatorAction.");
                                        Game.instance._client.send(GameClient.CommandType.remove, BitConverter.GetBytes(clientplayer.ID));
                                    } catch (Exception e) {
                                        Game.instance._client.send(GameClient.CommandType.message, "A wild ... uh... nothing to see here, move on. ".serialize());
                                        throw e;
                                    }
                                lock(host.ClientList) {
                                    host.ClientList.Remove(request);
                                    lock(host)
                                        host.ClientsChanged = true;
                                }
                                //otherwise check if client sent data and read it
                            } else if(request.Available > 0) {
                                buffer = new byte[request.Available];
                                List<byte> cmd = new List<byte>();
                                requestStream = request.GetStream();
                                int read = requestStream.Read(buffer, 0, buffer.Length);
                                int protocolSize = buffer.getProtocolSize();
                                if(read < protocolSize) {
                                    Array.Resize(ref buffer, protocolSize);
                                    while (read < protocolSize)
                                        read += requestStream.Read(buffer, read, protocolSize - read);
                                }

                                requestStream.Flush();

                                //iterate all clients again and send data to the connected ones
                                foreach(TcpClient send in tempList) {
                                    if(send.Connected) {

                                        byte[] sendBuffer;

                                        sendBuffer = host.HostSideCheck(request,send,buffer);

                                        if (sendBuffer.Length > 0)
                                            try {
                                                sendStream = send.GetStream();
                                                sendStream.Write(sendBuffer, 0, sendBuffer.Length);
                                                sendStream.Flush();
                                            } catch (Exception) { } // client quit during process

                                    }// end if
                                }// end foreach
                            }// end elseif
                        }// end foreach

                    }
                    // IMMENSLY reduce CPU load
                    await Task.Delay(1);
                }// end while

            };// end return
            
        };// end func

        public Host(int port) {
            Listen = true;
            ClientListener = new TcpListener(IPAddress.Any, port);
            ClientList = new List<TcpClient>();
            ClientPlayer = new Dictionary<TcpClient, ulong>();
            //keep listening until host cancels the process.
            ListenerThread = new Timer(ListenerAction(this));
            RefresherThread = new Timer(RefreshAction(this));
            CommunicatorThread = new Thread(new ThreadStart(CommunicatorAction(this)));

            //start hostprocesses
            ClientListener.Start();
            ListenerThread.Change(0, 1000);
            RefresherThread.Change(0, (int)(1000 / RefreshesPerSecond));
            CommunicatorThread.Start();
        }

        private byte[] HostSideCheck(TcpClient sender, TcpClient reciever, byte[] buffer) {
            List<byte> sendBuffer = new List<byte>();
            
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
                int pos = 0;
                int length = cmd.getInt(ref pos);
                GameClient.CommandType cmdType = cmd.getEnumByte<GameClient.CommandType>(ref pos);

                switch(cmdType) {
                    case GameClient.CommandType.sendPlayer:
                        if(sender == reciever) {
                            NPC newPlayer = Serializers.NPCSerializer.Deserial(buffer, ref pos);

                            lock(ClientPlayer)
                                ClientPlayer.Add(sender, newPlayer.ID);

                            Game.instance._client.send(GameClient.CommandType.message, ("A wild " + newPlayer.Name + " appears!").serialize());
                        } else
                            cmd[CustomMaths.intsize] = (byte)GameClient.CommandType.add;
                        sendBuffer.AddRange(cmd);
                        break;
                    case GameClient.CommandType.sync:
                        if(reciever != sender)
                            sendBuffer.AddRange(cmd);
                        break;
                    default:
                        sendBuffer.AddRange(cmd);
                        break;
                }
            });

            return sendBuffer.ToArray();
        }

        #region IDisposable Support

        private bool disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    lock(ListenerThread)
                        ListenerThread.Dispose();
                    lock(RefresherThread)
                        RefresherThread.Dispose();
                    foreach(TcpClient client in ClientList) lock(client)
                        client.Close();
                    lock(ClientListener)
                        ClientListener.Stop();
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
