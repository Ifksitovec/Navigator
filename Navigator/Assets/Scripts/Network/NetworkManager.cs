using UnityEngine;

#region ENUMS
public enum TypeSocket
{
    Client = 1,
    Server = 2,
}
#endregion

public class NetworkManager : MonoBehaviour
{
    #region PUBLIC_FIELDS
    public TypeSocket TypeS;
    public string IPAdress = "";
    #endregion

    public static NetworkManager instance;

    #region MONOEVENTS
    void Start()
    {
        NetworkConfig.InitNetwork(TypeS, IPAdress);
    }

    private void OnApplicationQuit()
    {
        NetworkConfig.DisconectFromServer();
    }
    #endregion
}
