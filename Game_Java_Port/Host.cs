using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace Game_Java_Port {
    class Host {
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

        public ulong ID_Offset = 0;

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
                } else {
                    host.ListenerThread.Change(Timeout.Infinite, Timeout.Infinite);
                    host.ClientListener.Stop();
                }
            };
        };

        private static Func<Host, ThreadStart> CommunicatorAction = (host) =>
        {
            return async() => {
                //declare variables outside of loops to prevent repetitive memory allocations
                List<TcpClient> tempList = new List<TcpClient>();
                NetworkStream requestStream;
                NetworkStream sendStream;
                byte[] buffer;
                while(host.Listen) {
                    //check if clients have been added or removed and make a new copy of the list if true
                    if(host.ClientsChanged) {
                        lock(host.ClientList) {
                            tempList.Clear();
                            tempList.AddRange(host.ClientList);
                            //changes applied, confirm during lock statement to prevent wrong true's/false's
                            lock(host)
                                host.ClientsChanged = false;
                        }
                    }

                    //iterate all clients
                    foreach(TcpClient request in tempList) {
                        //check if client disconnected, remove if true.
                        if (!request.Connected) {
                            AttributeBase clientplayer;
                            lock(host.ClientPlayer)
                                try {
                                    lock(GameStatus.GameSubjects)
                                        clientplayer = GameStatus.GameSubjects.Find((obj) => obj.ID == host.ClientPlayer[request]);
                                    host.ClientPlayer.Remove(request);

                                    Game.instance._client.send(GameClient.CommandType.message, (clientplayer.Name + " is history.").serialize());
                                    Game.instance._client.send(GameClient.CommandType.remove, BitConverter.GetBytes(clientplayer.ID));
                                } catch (Exception) {
                                    Game.instance._client.send(GameClient.CommandType.message, "A wild ... uh... nothing to see here, move on. ".serialize());
                                }
                            lock(host.ClientList) {
                                host.ClientList.Remove(request);
                                lock(host)
                                    host.ClientsChanged = true;
                            }
                            //otherwise check if client sent data and read it
                        } else if(request.Available > 0) {
                            buffer = new byte[request.Available];
                            requestStream = request.GetStream();
                            requestStream.Read(buffer, 0, buffer.Length);
                            requestStream.Flush();

                            //iterate all clients again and send data to the connected ones
                            foreach(TcpClient send in tempList) {
                                if(send.Connected) {

                                    byte[] sendBuffer = host.HostSideCheck(request,send,buffer);

                                    if (sendBuffer.Length > 0)
                                        try {
                                            sendStream = send.GetStream();
                                            sendStream.Write(sendBuffer, 0, sendBuffer.Length);
                                            sendStream.Flush();
                                        } catch (Exception) { } // client quit during process

                                }// endif
                            }// end foreach
                        }// end elseif
                    }// end foreach

                    // IMMENSLY reduce CPU load
                    await Task.Delay(20);

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

            int pos = 0;

            while(pos < buffer.Length) {
                int cmdpos = pos;
                int length = buffer.getInt(ref pos);
                GameClient.CommandType cmdType = (GameClient.CommandType)buffer.getByte(ref pos);

                //seperates current command from the rest of the buffer.
                byte[] cmd = buffer.Skip(cmdpos).Take(length).ToArray();

                //client sent own player, reference it's id in the dictionary and message all players of new companion
                if(cmdType == GameClient.CommandType.sendPlayer && sender == reciever) {
                    NPC newPlayer = NPC.Deserialize(buffer, ref pos);

                    lock(ClientPlayer)
                        ClientPlayer.Add(sender, newPlayer.ID);

                    Game.instance._client.send(GameClient.CommandType.message, ("A wild " + newPlayer.Name + " appears!").serialize());
                }

                //only sender needs to set the control to the player, rest simply adds.
                if(cmdType == GameClient.CommandType.sendPlayer && sender != reciever)
                    cmd[CustomMaths.intsize] = (byte)GameClient.CommandType.add;

                //all commands but syncs to host itself will be sent.
                if(!(reciever == sender && cmdType == GameClient.CommandType.sync))
                    sendBuffer.AddRange(cmd);

                pos = cmdpos + length;
            }

            return sendBuffer.ToArray();
        }
    }
}
