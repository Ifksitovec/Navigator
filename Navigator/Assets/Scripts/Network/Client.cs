using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace VEXNetwork.Network
{
    #region ENUMS
    public enum IpOrDns
    {
        IpAddress = 1,
        Dns = 2,
    }
    #endregion

    public sealed class Client : IDisposable
    {
        #region PUBLIC_FIELDS
        public Socket Socket;
        public Client.DataArgs[] PacketId;
        #endregion

        #region PRIVATE_FIELDS
        private byte[] _receiveBuffer;
        private byte[] _packetRing;
        private int _packetCount;
        private bool _connecting;
        #endregion

        #region CONSTANTS
        private const short _ttl = 255;
        #endregion

        #region EVENTS
        /// <summary>
        /// Вызывается, если подключение успешно
        /// </summary>
        public event Client.ConnectionArgs ConnectionSuccess;

        /// <summary>
        /// Вызывается, если подключение провалено
        /// </summary>
        public event Client.ConnectionArgs ConnectionFailed;

        /// <summary>
        /// Вызывается при отключение от сервера
        /// </summary>
        public event Client.ConnectionArgs ConnectionLost;

        /// <summary>
        /// Вызывается при возникновении ошибок
        /// </summary>
        public event Client.CrashReportArgs CrashReport;

        /// <summary>
        /// Вызывается, когда принят пакет
        /// </summary>
        public event Client.PacketInfoArgs PacketReceived;
        /// <summary>
        /// Вызывается, когда принят пакет
        /// </summary>
        public event Client.PacketInfoArgs PacketReceivedUdp;

        /// <summary>
        /// Вызывается, когда приняты какие-либо данные
        /// </summary>
        public event Client.TrafficInfoArgs TrafficReceived;
        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// 
        /// </summary>
        /// <param name="packetCount"> Количество типов возможных пакетов</param>
        public Client(int packetCount)
        {
            UnityThread.InitUnityThread(false);
            if (this.Socket != null)
                return;
            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Socket.ReceiveTimeout = 1000;
            this._packetCount = packetCount;
            this.PacketId = new Client.DataArgs[packetCount];

            //UnityThread.initUnityThread();
        }
        #endregion

        #region PRIVATE_METHODS
        /// <summary>
        /// вызывается для начала прослушки данных на наличие данных
        /// </summary>
        private void BeginReceiveData()
        {
            this._receiveBuffer = new byte[Socket.ReceiveBufferSize];
            this.Socket?.BeginReceive(this._receiveBuffer, 0, this.Socket.ReceiveBufferSize, SocketFlags.None, new AsyncCallback(this.DoReceive), null);
        }

        /// <summary>
        /// обработка считанных с потока данных
        /// </summary>
        private void PacketHandler()
        {
            int length = this._packetRing.Length;                       // длина готовых к обработке байтов
            int num = 0;                                                // сколько прочитано
            int count;                                                  //еще не прочитанное кол-во байт в сообщении
            while (true)
            {
                count = length - num;
                if (count >= 4)
                {
                    int head = BitConverter.ToInt32(this._packetRing, num);  // длина данных в сообщении без head
                    if (head >= 4)                                           // Если размер тела сообщения не меньше 4 байт (int, packetID)
                    {
                        if (head <= (count - 4))                             // Count - 4 потому что head сам себя не считает а весит 4 байта
                        {
                            int startIndex = num + 4;
                            int packetID = BitConverter.ToInt32(this._packetRing, startIndex);  //считывает тип принятого пакета
                            byte[] data;
                            if (packetID >= 0 && packetID < this._packetCount)      //Если укладываемся в возможный диапазон пакетID
                            {
                                if (this.PacketId[packetID] != null)                //если существует обработчик с таким ключом
                                {
                                    if (head - 4 > 0)                               //есть ли данные после пакетID
                                    {
                                        data = new byte[head - 4];                  //под данные после типа пакета (PacketID)
                                        Buffer.BlockCopy((Array)this._packetRing, startIndex + 4, (Array)data, 0, head - 4);

                                        UnityThread.ExecuteInUpdate((Action)(() =>
                                        {
                                            Client.PacketInfoArgs packetReceived = this.PacketReceived;
                                            if (packetReceived == null)
                                                return;
                                            packetReceived(head - 4, packetID, ref data);
                                        }));

                                        UnityThread.ExecuteInUpdate((Action)(() =>
                                        {
                                            this.PacketId[packetID](ref data);
                                        }));

                                        num = startIndex + head;
                                    }
                                    else
                                    {
                                        data = new byte[0];
                                        UnityThread.ExecuteInUpdate((Action)(() =>
                                        {
                                            Client.PacketInfoArgs packetReceived = this.PacketReceived;
                                            if (packetReceived == null)
                                                return;
                                            packetReceived(0, packetID, ref data);
                                        }));

                                        UnityThread.ExecuteInUpdate((Action)(() =>
                                        {
                                            this.PacketId[packetID](ref data);
                                        }));

                                        num = startIndex + head;
                                    }

                                }
                                else
                                    break;
                            }
                            else
                                goto label_13;
                        }
                        else
                            goto label_19;
                    }
                    else
                        goto label_16;
                }
                else
                    goto label_19;
            }
            this.CrashReport?.Invoke("NullReferenceException");
            this.Disconnect();
            return;
            label_13:
            Client.CrashReportArgs crashReport2 = this.CrashReport;
            crashReport2?.Invoke("IndexOutOfRangeException");
            this.Disconnect();
            return;
            label_16:
            Client.CrashReportArgs crashReport3 = this.CrashReport;
            crashReport3?.Invoke($"BrokenPacketException BufferSize: {_packetRing.Length}");
            this.Disconnect();
            return;
            label_19:
            if (count == 0)
            {
                this._packetRing = (byte[])null;
            }
            else
            {
                byte[] numArray = new byte[count];
                Buffer.BlockCopy((Array)this._packetRing, num, (Array)numArray, 0, count);
                this._packetRing = numArray;
            }
        }
        #endregion

        #region PUBLIC_METHODS
        /// <summary>
        /// подключение к серверу по указанному IP и порту
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Connect(string ip, int port, IpOrDns type = IpOrDns.IpAddress)
        {
            if (this.Socket == null || this.Socket.Connected || this._connecting)
                return;
            switch (type)
            {
                case IpOrDns.IpAddress:
                    {
                        if (ip.ToLower() == "localhost")
                            this.Socket.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), new AsyncCallback(this.DoConnect), (object)null);
                        else
                            this.Socket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), port), new AsyncCallback(this.DoConnect), (object)null);
                        break;
                    }
                case IpOrDns.Dns:
                    {
                        if (ip.ToLower() == "localhost")
                            this.Socket.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), new AsyncCallback(this.DoConnect), (object)null);
                        else
                        {
                            IPHostEntry ipAddresses = Dns.GetHostEntry(ip);

                            this.Socket.BeginConnect(new IPEndPoint(ipAddresses.AddressList[0], port), new AsyncCallback(this.DoConnect), (object)null);
                        }
                        break;
                    }

            }

        }

        /// <summary>
        /// Отправка данных без хедера
        /// </summary>
        /// <param name="data"> Данные </param>
        public void SendData(byte[] data)
        {
            if (!this.Socket.Connected)
                return;
            try
            {
                this.Socket?.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(this.DoSend), (object)null);
            }
            catch
            {
                this.CrashReport?.Invoke("Send Failed!");
            }
        }

        /// <summary>
        /// Отправка данных
        /// </summary>
        /// <param name="data"> Данные </param>
        /// <param name="head"> Показывает сколько данных в байтах </param>
        public void SendData(byte[] data, int head)
        {
            if (!this.Socket.Connected)
                return;
            byte[] buffer = new byte[head + 4];
            Buffer.BlockCopy((Array)BitConverter.GetBytes(head), 0, (Array)buffer, 0, 4);
            Buffer.BlockCopy((Array)data, 0, (Array)buffer, 4, head);
       
            try
            {
                this.Socket?.BeginSend(buffer, 0, head + 4, SocketFlags.None, new AsyncCallback(this.DoSend), (object)null);
            }
            catch
            {
                this.CrashReport?.Invoke("Send Failed!");
            }
        }

        /// <summary>
        /// определяет, есть ли подключение к серверу
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (this.Socket != null)
                    return this.Socket.Connected;
                return false;
            }
        }

        /// <summary>
        /// вызывается, когда нужно отключиться от сервера
        /// </summary>
        public void Disconnect()
        {
            if (this.Socket == null)
                return;
            this.Socket?.BeginDisconnect(false, new AsyncCallback(this.DoDisconnect), (object)null);
        }

        /// <summary>
        /// очистка памяти
        /// </summary>
        public void Dispose()
        {
            this.Disconnect();
            this.Socket.Close();
            this.Socket.Dispose();
            this.Socket = (Socket)null;
            this.PacketId = (Client.DataArgs[])null;
            this.ConnectionSuccess = (Client.ConnectionArgs)null;
            this.ConnectionFailed = (Client.ConnectionArgs)null;
            this.ConnectionLost = (Client.ConnectionArgs)null;
            this.CrashReport = (Client.CrashReportArgs)null;
            this.PacketReceived = (Client.PacketInfoArgs)null;
            this.TrafficReceived = (Client.TrafficInfoArgs)null;
            this.PacketId = (Client.DataArgs[])null;
        }
        #endregion

        #region CALLBACKS
        /// <summary>
        /// Callback, который вызывается, когда происходит подключение к серверу
        /// </summary>
        /// <param name="ar"></param>
        private void DoConnect(IAsyncResult ar)
        {
            try
            {
                this.Socket.EndConnect(ar);
                Socket.Ttl = _ttl;
                Debug.Log($"ip adress подключения: {Socket.LocalEndPoint}");
            }
            catch
            {
                this.ConnectionFailed?.Invoke();
                this._connecting = false;
                return;
            }
            if (!this.Socket.Connected)
            {
                this.ConnectionFailed?.Invoke();
                this._connecting = false;
            }
            else
            {
                this._connecting = false;
                this.ConnectionSuccess?.Invoke();
                this.Socket.ReceiveBufferSize = 8192;
                this.Socket.SendBufferSize = 8192;
                this.BeginReceiveData();
            }
        }

        /// <summary>
        /// Callback, который вызывается, когда есть данные для чтения в потоке
        /// </summary>
        /// <param name="ar"></param>
        private void DoReceive(IAsyncResult ar)
        {
            int length1;
            ;
            try
            {
                length1 = this.Socket.EndReceive(ar);
            }
            catch
            {
                this.CrashReport?.Invoke("ConnectionForciblyClosedException");
                this.Disconnect();
                return;
            }
            if (length1 < 1)
            {
                if (this.Socket == null)
                    return;
                this.CrashReport?.Invoke("BufferUnderFlowException");
                this.Disconnect();
            }
            else
            {
                this.TrafficReceived?.Invoke(length1, ref this._receiveBuffer);
                if (this._packetRing == null)
                {
                    this._packetRing = new byte[length1];
                    Buffer.BlockCopy((Array)this._receiveBuffer, 0, (Array)this._packetRing, 0, length1);
                }
                else
                {
                    int length2 = this._packetRing.Length;
                    byte[] numArray = new byte[length1 + length2];
                    Buffer.BlockCopy((Array)this._packetRing, 0, (Array)numArray, 0, length2);
                    Buffer.BlockCopy((Array)this._receiveBuffer, 0, (Array)numArray, length2, length1);
                    this._packetRing = numArray;
                }
                this.PacketHandler();

                this._receiveBuffer = new byte[Socket.ReceiveBufferSize];
                this.Socket?.BeginReceive(this._receiveBuffer, 0, this.Socket.ReceiveBufferSize, SocketFlags.None, new AsyncCallback(this.DoReceive), null);
            }
        }

        /// <summary>
        /// Callback, который вызывается, когда отправлены данные
        /// </summary>
        /// <param name="ar"></param>
        private void DoSend(IAsyncResult ar)
        {
            try
            {
                this.Socket.EndSend(ar);
            }
            catch
            {
                this.CrashReport?.Invoke("ConnectionForciblyClosedException");
                this.Disconnect();
            }
        }

        /// <summary>
        /// Callback, который вызывается, когда происходит отключение от сервера
        /// </summary>
        /// <param name="ar"></param>
        private void DoDisconnect(IAsyncResult ar)
        {
            try
            {
                this.Socket.EndDisconnect(ar);
            }
            catch
            {
            }
            Client.ConnectionArgs connectionLost = this.ConnectionLost;
            if (connectionLost == null)
                return;
            connectionLost();
        }
        #endregion

        #region DELEGATES
        public delegate void ConnectionArgs();

        public delegate void DataArgs(ref byte[] data);

        public delegate void CrashReportArgs(string reason);

        public delegate void PacketInfoArgs(int size, int header, ref byte[] data);

        public delegate void TrafficInfoArgs(int size, ref byte[] data);
        #endregion
    }
}
