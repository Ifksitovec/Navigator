using UnityEngine;
using VEXNetwork;

public static class NetworkSendServer
{
    #region PUBLIC_METHODS
    public static void SendCurrentMoveType()
    {
        ByteBuffer buffer = new ByteBuffer(4);
        buffer.WriteInt32((int)ServerPackets.SChangeMove);
        buffer.WriteInt32((int)RouteChecker.Instance.TMove);

        int id = NetworkConfig.CurrentClientID;
        if (NetworkConfig.ServerSocket.ListClientSockets.ContainsKey(id))
        {
            NetworkConfig.ServerSocket.SendDataTo(id, buffer.Data, buffer.Head);
        }

        buffer.Dispose();
    }

    public static void SendHorn(bool status)
    {
        ByteBuffer buffer = new ByteBuffer(4);
        buffer.WriteInt32((int)ServerPackets.SHorn);
        buffer.WriteBoolean(status);

        int id = NetworkConfig.CurrentClientID;
        if (NetworkConfig.ServerSocket.ListClientSockets.ContainsKey(id))
        {
            NetworkConfig.ServerSocket.SendDataTo(id, buffer.Data, buffer.Head);
        }

        buffer.Dispose();
    }
    #endregion
}

public static class NetworkSendClient
{
    #region PUBLIC_METHODS
    public static void SendNewTransform(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        ByteBuffer buffer = new ByteBuffer(4);
        buffer.WriteInt32((int)ClientPackets.CSendPos);
        buffer.WriteVector3(pos);
        buffer.WriteQuternion(rot);
        buffer.WriteVector3(scale);
        NetworkConfig.ClientSocket.SendData(buffer.Data, buffer.Head);

        buffer.Dispose();
    }

    public static void SendProblemInfo(TypeProblem type, bool status)
    {
        ByteBuffer buffer = new ByteBuffer(4);
        buffer.WriteInt32((int)ClientPackets.CProblemInfo);
        buffer.WriteInt32((int)type);
        buffer.WriteBoolean(status);
        NetworkConfig.ClientSocket.SendData(buffer.Data, buffer.Head);

        buffer.Dispose();
    }

    public static void SendMovingInfo(TypeMove type)
    {
        ByteBuffer buffer = new ByteBuffer(4);
        buffer.WriteInt32((int)ClientPackets.CMovingChange);
        buffer.WriteInt32((int)type);

        NetworkConfig.ClientSocket.SendData(buffer.Data, buffer.Head);
        buffer.Dispose();
    }
    #endregion
}
