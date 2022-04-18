using System;
using UnityEngine;
using VEXNetwork;
using VEXNetwork.Network;

public static class NetworkConfig
{
    #region PUBLIC_FIELDS
    public static int CurrentClientID = 0;
    public static Client ClientSocket;
    public static Server ServerSocket;
    #endregion

    #region PRIVATE_FIELDS
    private static TypeSocket _typeS;
    private static string _ip;
    private static int _port = 15555;
    private static bool _isDisconnect = false;
    #endregion

    #region EVENTS
    public static Action OnConnect;
    public static Action OnDisconnect;
    public static Action OnConnectClient;
    public static Action OnDisconnectClient;
    #endregion

    #region PUBLIC_METHODS
    public static void InitNetwork(TypeSocket typeSocket, string ip)
    {
        _typeS = typeSocket;
        _ip = ip;
        switch (_typeS)
        {
            case TypeSocket.Client:
                if (!ReferenceEquals(ClientSocket, null)) return;
                ClientSocket = new Client(1000);
                NetworkReceiveClient.PacketRouter();
                ClientSocket.ConnectionLost += ClientSocket_ConnectionLost;
                ClientSocket.ConnectionSuccess += ClientSocket_ConnectionSuccess;
                ClientSocket.ConnectionFailed += ClientSocket_ConnectionFailed;
                ConnectToServer(_ip, _port, IpOrDns.IpAddress);
                break;

            case TypeSocket.Server:
                if (!ReferenceEquals(ServerSocket, null)) return;
                ServerSocket = new Server(1);
                NetworkReceiveServer.PacketRouter();

                ServerSocket.ConnectionLost += ServerSocket_DeleteClient;
                ServerSocket.ConnectionReceived += ServerSocket_NewClientConnect;
                ServerSocket.StartListening(_port, 5, 0);
                break;
            default:
                Debug.LogError("Unknown type!");
                break;
        }
        _isDisconnect = false;
    }
    
    public static void ConnectToServer(string address, int port, IpOrDns ipOrDns)
    {
        ClientSocket.Connect(address, port, ipOrDns);
    }

    public static void DisconectFromServer()
    {
        _isDisconnect = true;
        switch (_typeS)
        {
            case TypeSocket.Client:
                ClientSocket.Disconnect();
                break;
            case TypeSocket.Server:
                ServerSocket.StopListening();
                ServerSocket.Dispose();
                break;
        }
    }
    #endregion

    #region PRIVATE_METHODS
    private static void ClientSocket_ConnectionSuccess()
    {
        Debug.Log("Success connection!");
        UnityThread.ExecuteInUpdate(() => 
        {
            OnConnect?.Invoke();
        });
    }

    private static void ServerSocket_NewClientConnect(int index)
    {
        CurrentClientID = index;
        NetworkSendServer.SendCurrentMoveType();
        Debug.Log($"New Connection {index}");
        ServerSocket.StopListening();
        UnityThread.ExecuteInUpdate(() =>
        {
            OnConnectClient?.Invoke();
        });
    }

    private static void ClientSocket_ConnectionLost()
    {
        Debug.Log("Disconnect - Reconnect");
        ClientSocket = null;

        if (_isDisconnect)
        {
            return;
        }

        InitNetwork(_typeS, _ip);
        //ConnectToServer(_ip, _port, IpOrDns.IpAddress);
        UnityThread.ExecuteInUpdate(() =>
        {
            OnDisconnect?.Invoke();
        });
    }

    private static void ServerSocket_DeleteClient(int index)
    {
        Debug.LogError($"Disconnect {index}");
        if (_isDisconnect)
        {
            return;
        }
        ServerSocket.StartListening(_port, 5, 0);
        UnityThread.ExecuteInUpdate(() =>
        {
            OnDisconnectClient?.Invoke();
        });
    }

    private static void ClientSocket_ConnectionFailed()
    {
        if (_isDisconnect)
        {
            return;
        }
        ConnectToServer(_ip, _port, IpOrDns.IpAddress);
    }
    #endregion
}
