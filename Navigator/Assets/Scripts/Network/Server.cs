using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace VEXNetwork.Network
{
    public sealed class Server : IDisposable
    {
        #region PUBLIC_FIELDS
        public ConcurrentDictionary<int, Socket> ListClientSockets;
        public Server.DataArgs[] PacketId;
        #endregion

        #region PRIVATE_FIELDS
        private List<int> _unsignedIndex;
        private Socket _listener;
        private readonly int _packetCount;
        #endregion

        #region CONSTANTS
        private const short ttl = 255;
        #endregion

        #region PROPERTIES
        public int BufferLimit { get; set; }

        public int ClientLimit { get; }

        public bool IsListening { get; private set; }

        public int HighIndex { get; private set; }

        public int PacketAcceptLimit { get; set; }

        public int PacketDisconnectCount { get; set; }
        #endregion

        #region EVENTS
        public event Server.ConnectionArgs ConnectionReceived;

        public event Server.DisconnectionArgs ConnectionLost;

        public event Server.CrashReportArgs CrashReport;

        public event Server.PacketInfoArgs PacketReceived;

        public event Server.TrafficInfoArgs TrafficReceived;
        #endregion

        #region CONSTRUCTORS
        public Server(int clientLimit = 0)
        {
            if (this._listener != null || this.ListClientSockets != null)
                return;
            this.ListClientSockets = new ConcurrentDictionary<int, Socket>();
            this._unsignedIndex = new List<int>();
            this.ClientLimit = clientLimit;
            this._packetCount = 5000;
            this.PacketId = new Server.DataArgs[_packetCount];
        }
        #endregion

        #region PRIVATE_METHODS
        private void BeginReceiveData(int index)
        {
            Server.ReceiveState receiveStateTcp = new Server.ReceiveState(index, ListClientSockets[index].ReceiveBufferSize);
            this.ListClientSockets[index].BeginReceive(receiveStateTcp.Buffer, 0, ListClientSockets[index].ReceiveBufferSize, SocketFlags.None, new AsyncCallback(DoReceiveTcp), receiveStateTcp);
        }

        private void PacketHandler(ref Server.ReceiveState so)
        {
            int length = so.RingBuffer.Length;
            int num = 0;
            bool flag = false;
            int count;
            while (true)
            {
                count = length - num;
                if (count >= 4)
                {
                    int head = BitConverter.ToInt32(so.RingBuffer, num);
                    if (head >= 4)
                    {
                        if (head <= (count - 4))
                        {
                            int startIndex = num + 4;
                            int packetID = BitConverter.ToInt32(so.RingBuffer, startIndex);
                            byte[] data;
                            if (packetID >= 0 && packetID < this._packetCount)
                            {
                                if (this.PacketId[packetID] != null)
                                {
                                    if ((head - 4) > 0)
                                    {
                                        data = new byte[head - 4];
                                        Buffer.BlockCopy((Array)so.RingBuffer, startIndex + 4, (Array)data, 0, head - 4);
                                        this.PacketReceived?.Invoke(head - 4, packetID, ref data);
                                        int index = so.Index;
                                        UnityThread.ExecuteInUpdate((Action)(() =>
                                        {
                                            this.PacketId[packetID](index, ref data);
                                        }));

                                        num = startIndex + head;
                                        --so.PacketCount;
                                        flag = true;
                                    }
                                    else
                                    {
                                        data = new byte[0];
                                        this.PacketReceived?.Invoke(0, packetID, ref data);
                                        int index = so.Index;
                                        UnityThread.ExecuteInUpdate((Action)(() =>
                                        {
                                            this.PacketId[packetID](index, ref data);
                                        }));

                                        num = startIndex + head;
                                        --so.PacketCount;
                                        flag = true;
                                    }
                                }
                                else
                                    break;
                            }
                            else
                                goto label_20;
                        }
                        else
                            goto label_30;
                    }
                    else
                        goto label_25;
                }
                else
                    goto label_30;
            }
            if (!this.ListClientSockets.ContainsKey(so.Index))
            {
                so.Dispose();
                return;
            }
            this.CrashReport?.Invoke(so.Index, "NullReferenceException");
            this.Disconnect(so.Index);
            so.Dispose();
            return;
        label_20:
            if (!this.ListClientSockets.ContainsKey(so.Index))
            {
                so.Dispose();
                return;
            }
            this.CrashReport?.Invoke(so.Index, "IndexOutOfRangeException");
            this.Disconnect(so.Index);
            so.Dispose();
            return;
        label_25:
            if (!this.ListClientSockets.ContainsKey(so.Index))
            {
                so.Dispose();
                return;
            }
            this.CrashReport?.Invoke(so.Index, "BrokenPacketException");
            this.Disconnect(so.Index);
            so.Dispose();
            return;
        label_30:
            if (count == 0)
            {
                so.RingBuffer = (byte[])null;
                so.PacketCount = 0;
            }
            else
            {
                byte[] numArray = new byte[count];
                Buffer.BlockCopy((Array)so.RingBuffer, num, (Array)numArray, 0, count);
                so.RingBuffer = numArray;
                if (!flag)
                    return;
                so.PacketCount = 1;
            }
        }
        
        //ищет свободный слот
        private int FindEmptySlot(int startIndex)
        {
            for (int index = this._unsignedIndex.Count - 1; index >= 0 && this.HighIndex == this._unsignedIndex[index]; --index)
                --this.HighIndex;
            if (this._unsignedIndex.Count > 0)
            {
                using (List<int>.Enumerator enumerator = this._unsignedIndex.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        int current = enumerator.Current;
                        if (this.HighIndex < current)
                            this.HighIndex = current;
                        this._unsignedIndex.Remove(current);
                        return current;
                    }
                }
                if (this.HighIndex < startIndex)
                    this.HighIndex = startIndex;
                return startIndex;
            }
            if (this.HighIndex < startIndex)
            {
                int key = startIndex;
                while (this.ListClientSockets.ContainsKey(key))
                    ++key;
                this.HighIndex = key;
                return key;
            }
            while (this.ListClientSockets.ContainsKey(this.HighIndex))
                ++this.HighIndex;
            return this.HighIndex;
        }
        #endregion

        #region PUBLIC_METHODS
        //начинаем прослушивать по заданному порту
        public void StartListening(int port, int backlog, int startIndex)
        {
            if (this.ListClientSockets == null || this.IsListening || this._listener != null)
                return;

            this._listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._listener.Bind(new IPEndPoint(IPAddress.Any, port));
            this.IsListening = true;
            _listener.Ttl = ttl;
            this._listener.Listen(backlog);
            this._listener.BeginAccept(new AsyncCallback(this.DoAcceptClient), (object)startIndex);
        }

        //завершение прослушивания
        public void StopListening()
        {
            if (!this.IsListening || this.ListClientSockets == null)
                return;
            this.IsListening = false;
            if (this._listener == null)
                return;
            this._listener.Close();
            this._listener.Dispose();
            this._listener = (Socket)null;
        }

        public void SendDataTo(int index, byte[] data)
        {
            if (!this.ListClientSockets.ContainsKey(index))
                return;
            if (this.ListClientSockets[index] == null && !ListClientSockets[index].Connected)
            {
                this.Disconnect(index);
            }
            else
            {
                try
                {
                    this.ListClientSockets[index].BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(this.DoSend), (object)index);
                }
                catch
                {
                    this.CrashReport?.Invoke(index, "Can't begin send");
                    this.Disconnect(index);
                }
            }
        }

        public void SendDataTo(int index, byte[] data, int head)
        {
            if (!this.ListClientSockets.ContainsKey(index))
                return;
            if (this.ListClientSockets[index] == null && !ListClientSockets[index].Connected)
            {
                this.Disconnect(index);
            }
            else
            {
                byte[] buffer = new byte[head + 4];
                Buffer.BlockCopy((Array)BitConverter.GetBytes(head), 0, (Array)buffer, 0, 4);
                Buffer.BlockCopy((Array)data, 0, (Array)buffer, 4, head);
                try
                {
                    this.ListClientSockets[index].BeginSend(buffer, 0, head + 4, SocketFlags.None, new AsyncCallback(this.DoSend), (object)index);
                }
                catch
                {
                    this.CrashReport?.Invoke(index, "Can't begin send");
                    this.Disconnect(index);
                }
            }
        }

        public bool IsConnected(int index)
        {
            if (!this.ListClientSockets.ContainsKey(index))
                return false;
            if (this.ListClientSockets[index].Connected)
                return true;
            this.Disconnect(index);
            return false;
        }

        public string GetIPv4()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        }

        public string ClientIp(int index)
        {
            if (this.IsConnected(index))
                return ((IPEndPoint)this.ListClientSockets[index].RemoteEndPoint).ToString();
            return "[NULL]";
        }

        public void Disconnect(int index)
        {
            if (!this.ListClientSockets.ContainsKey(index))
            {
                return;
            }
            if (this.ListClientSockets[index] == null && !ListClientSockets[index].Connected)
            {
                this.ListClientSockets.TryRemove(index, out Socket client);
                this._unsignedIndex.Add(index);
            }
            else
            {
                try
                {
                    this.ListClientSockets[index].BeginDisconnect(false, new AsyncCallback(this.DoDisconnect), (object)index);
                }
                catch
                {
                }
            }
        }

        //заканчиваем прослушивание, закрываем все соединения и очищаем все
        public void Dispose()
        {
            this.StopListening();
            foreach (int key in this.ListClientSockets.Keys)
                this.Disconnect(key);
            this.ListClientSockets.Clear();
            this.ListClientSockets = (ConcurrentDictionary<int, Socket>)null;
            this.PacketId = (Server.DataArgs[])null;
            this._unsignedIndex.Clear();
            this._unsignedIndex = (List<int>)null;
            this.ConnectionReceived = (Server.ConnectionArgs)null;
            this.ConnectionLost = (Server.DisconnectionArgs)null;
            this.CrashReport = (Server.CrashReportArgs)null;
            this.PacketReceived = (Server.PacketInfoArgs)null;
            this.TrafficReceived = (Server.TrafficInfoArgs)null;
            this.PacketId = (Server.DataArgs[])null;
        }
        #endregion

        #region CALLBACKS
        private void DoAcceptClient(IAsyncResult ar)
        {
            Socket socket = this._listener.EndAccept(ar);
            socket.Ttl = ttl;

            int asyncState = (int)ar.AsyncState;
            int emptySlot = this.FindEmptySlot(asyncState);
            if (this.ClientLimit > 0 && emptySlot > this.ClientLimit)
            {
                socket.Disconnect(false);
                socket.Dispose();
                socket = null;
            }
            this.ListClientSockets.TryAdd(emptySlot, socket);
            this.ListClientSockets[emptySlot].ReceiveBufferSize = 65536; // 8192; 
            this.ListClientSockets[emptySlot].SendBufferSize = 65536;
            this.BeginReceiveData(emptySlot);
            this.ConnectionReceived?.Invoke(emptySlot);
        }

        private void DoReceiveTcp(IAsyncResult ar)
        {
            Server.ReceiveState asyncState = (Server.ReceiveState)ar.AsyncState;
            int length1;
            try
            {
                length1 = this.ListClientSockets[asyncState.Index].EndReceive(ar);
            }
            catch
            {
                this.CrashReport?.Invoke(asyncState.Index, "ConnectionForciblyClosedException");
                this.Disconnect(asyncState.Index);
                asyncState.Dispose();
                return;
            }
            if (length1 < 1)
            {
                if (!this.ListClientSockets.ContainsKey(asyncState.Index))
                    asyncState.Dispose();
                else if (this.ListClientSockets[asyncState.Index] == null)
                {
                    asyncState.Dispose();
                }
                else
                {
                    this.CrashReport?.Invoke(asyncState.Index, "BufferUnderflowException");
                    this.Disconnect(asyncState.Index);
                    asyncState.Dispose();
                }
            }
            else
            {
                this.TrafficReceived?.Invoke(length1, ref asyncState.Buffer);
                ++asyncState.PacketCount;
                if (this.PacketDisconnectCount > 0 && asyncState.PacketCount >= this.PacketDisconnectCount)
                {
                    this.CrashReport?.Invoke(asyncState.Index, "Packet Spamming/DDOS");
                    this.Disconnect(asyncState.Index);
                    asyncState.Dispose();
                }
                else
                {
                    if (this.PacketAcceptLimit == 0 || this.PacketAcceptLimit > asyncState.PacketCount)
                    {
                        if (asyncState.RingBuffer == null)
                        {
                            asyncState.RingBuffer = new byte[length1];
                            Buffer.BlockCopy((Array)asyncState.Buffer, 0, (Array)asyncState.RingBuffer, 0, length1);
                        }
                        else
                        {
                            int length2 = asyncState.RingBuffer.Length;
                            byte[] numArray = new byte[length2 + length1];
                            Buffer.BlockCopy((Array)asyncState.RingBuffer, 0, (Array)numArray, 0, length2);
                            Buffer.BlockCopy((Array)asyncState.Buffer, 0, (Array)numArray, length2, length1);
                            asyncState.RingBuffer = numArray;
                        }
                        if (this.BufferLimit > 0 && asyncState.RingBuffer.Length > this.BufferLimit)
                        {
                            CrashReport?.Invoke(asyncState.Index, "Длина буфера больше лимита");
                            this.Disconnect(asyncState.Index);
                            asyncState.Dispose();
                            return;
                        }
                    }
                    if (!this.ListClientSockets.ContainsKey(asyncState.Index))
                        asyncState.Dispose();
                    else if (this.ListClientSockets[asyncState.Index] == null)
                    {
                        this.Disconnect(asyncState.Index);
                        asyncState.Dispose();
                    }
                    else
                    {
                        this.PacketHandler(ref asyncState);
                        asyncState.Buffer = new byte[ListClientSockets[asyncState.Index].ReceiveBufferSize];
                        if (!this.ListClientSockets.ContainsKey(asyncState.Index))
                        {
                            asyncState.Dispose();
                        }
                        else
                        {
                            try
                            {
                                this.ListClientSockets[asyncState.Index].BeginReceive(asyncState.Buffer, 0, this.ListClientSockets[asyncState.Index].ReceiveBufferSize,
                                    SocketFlags.None, new AsyncCallback(this.DoReceiveTcp), (object)asyncState);
                            }
                            catch
                            {
                                this.CrashReport?.Invoke(asyncState.Index, "Can't begin receive");
                                this.Disconnect(asyncState.Index);
                                asyncState.Dispose();
                            }
                        }
                    }
                }
            }
        }

        private void DoSend(IAsyncResult ar)
        {
            int asyncState = (int)ar.AsyncState;
            try
            {
                this.ListClientSockets[asyncState].EndSend(ar);
            }
            catch
            {
                this.CrashReport?.Invoke(asyncState, "ConnectionForciblyClosedException");
                this.Disconnect(asyncState);
            }
        }

        private void DoDisconnect(IAsyncResult ar)
        {
            int asyncState = (int)ar.AsyncState;
            try
            {
                this.ListClientSockets[asyncState].EndDisconnect(ar);
            }
            catch
            {
            }
            if (!this.ListClientSockets.ContainsKey(asyncState))
                return;
            this.ListClientSockets[asyncState]?.Dispose();
            this.ListClientSockets[asyncState] = (Socket)null;
            this.ListClientSockets.TryRemove(asyncState, out Socket client);
            lock (_unsignedIndex)
            {
                this._unsignedIndex.Add(asyncState);
            }

            Server.DisconnectionArgs connectionLost = this.ConnectionLost;
            if (connectionLost == null)
                return;
            connectionLost(asyncState);
        }
        #endregion

        #region DELEGATES
        public delegate void ConnectionArgs(int index);

        public delegate void DisconnectionArgs(int index);

        public delegate void DataArgs(int index, ref byte[] data);

        public delegate void DataArgsUdp(int index, ref byte[] data, IPEndPoint point);

        public delegate void CrashReportArgs(int index, string reason);

        public delegate void CrashReportUdpArgs(string reason);

        public delegate void PacketInfoArgs(int size, int header, ref byte[] data);

        public delegate void TrafficInfoArgs(int size, ref byte[] data);

        public delegate void NullArgs();
        #endregion

        #region STRUCTURES
        private struct ReceiveState : IDisposable
        {
            internal int Index;
            internal int PacketCount;
            internal byte[] Buffer;
            internal byte[] RingBuffer;

            internal ReceiveState(int index, int size)
            {
                this.Index = index;
                this.PacketCount = 0;
                this.Buffer = new byte[size];
                this.RingBuffer = (byte[])null;
            }

            public void Dispose()
            {
                this.Buffer = (byte[])null;
                this.RingBuffer = (byte[])null;
            }
        }
        private struct ReceiveStateUdp : IDisposable
        {
            internal int PacketCount;
            internal byte[] Buffer;

            internal ReceiveStateUdp(int size)
            {
                this.PacketCount = 0;
                this.Buffer = new byte[size];
            }

            public void Dispose()
            {
                this.Buffer = (byte[])null;
            }
        }
        #endregion
    }
}
