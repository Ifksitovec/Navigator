using UnityEngine;
using VEXNetwork;
using System;

#region PACKET_ENUM
/// <summary>
/// перечисления, в котором содержатся все типы возможных соощений от сервера и клиента
/// </summary>
public enum ServerPackets
{
    SChangeMove = 11,
    SHorn = 12,
}

public enum ClientPackets
{
    CSendPos = 1,
    CProblemInfo = 2,
    CMovingChange = 3,
}
#endregion

public static class NetworkReceiveClient
{
    #region INIT_METHOD
    /// <summary>
    /// инициализация обработчиков для разных типов пакетов
    /// </summary>
    public static void PacketRouter()
    {
        NetworkConfig.ClientSocket.PacketId[(int)ServerPackets.SChangeMove] = new VEXNetwork.Network.Client.DataArgs(Packet_MovingChange);
        NetworkConfig.ClientSocket.PacketId[(int)ServerPackets.SHorn] = new VEXNetwork.Network.Client.DataArgs(Packet_Horn);
    }
    #endregion

    #region PRIVATE_METHODS
    private static void Packet_MovingChange(ref byte[] data)
    {
        ByteBuffer buffer = new ByteBuffer(data);
        TypeMove type = (TypeMove)buffer.ReadInt32();

        RouteChecker.Instance.TMove = type;
        buffer.Dispose();
    }

    private static void Packet_Horn(ref byte[] data)
    {
        ByteBuffer buffer = new ByteBuffer(data);
        bool status = buffer.ReadBoolean();

        RouteChecker.Instance?.Horn_On_Off(status);
        buffer.Dispose();
    }
    #endregion
}

internal static class NetworkReceiveServer
{
    #region EVENTS
    public static Action OnChangeTypeMove;
    #endregion

    #region INIT_METHOD
    public static void PacketRouter()
    {
        NetworkConfig.ServerSocket.PacketId[(int)ClientPackets.CSendPos] = new VEXNetwork.Network.Server.DataArgs(Packet_SetPosition);
        NetworkConfig.ServerSocket.PacketId[(int)ClientPackets.CProblemInfo] = new VEXNetwork.Network.Server.DataArgs(Packet_ProblemInfo);
        NetworkConfig.ServerSocket.PacketId[(int)ClientPackets.CMovingChange] = new VEXNetwork.Network.Server.DataArgs(Packet_MovingChange);
    }
    #endregion

    #region PRIVATE_METHODS
    private static void Packet_SetPosition(int connectionID, ref byte[] data)
    {
        ByteBuffer buffer = new ByteBuffer(data);
        Vector3 pos = buffer.ReadVector3();
        Quaternion rot = buffer.ReadQuaternion();
        Vector3 scale = buffer.ReadVector3();
        CarReceiver.Instance?.SetNewPosition(pos, rot, scale);
        buffer.Dispose();
    }

    private static void Packet_ProblemInfo(int connectionID, ref byte[] data)
    {
        ByteBuffer buffer = new ByteBuffer(data);
        TypeProblem type = (TypeProblem)buffer.ReadInt32();
        bool status = buffer.ReadBoolean();
        CarReceiver.Instance?.ProblemInfo(type, status);
        buffer.Dispose();
    }

    private static void Packet_MovingChange(int connectionID, ref byte[] data)
    {
        ByteBuffer buffer = new ByteBuffer(data);
        TypeMove type = (TypeMove)buffer.ReadInt32();
        RouteChecker.Instance.TMove = type;
        OnChangeTypeMove?.Invoke();
        buffer.Dispose();
    }
    #endregion
}

