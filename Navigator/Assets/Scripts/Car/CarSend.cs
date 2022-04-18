using System.Collections;
using UnityEngine;
using VEXNetwork;

#region ENUMS
public enum TypeProblem
{
    PressT = 1,
}
#endregion

public class CarSend : MonoBehaviour
{
    private bool _isProblemT = false;

    #region MONOEVENTS
    // Start is called before the first frame update
    void Start()
    {
        NetworkConfig.OnConnect += StartSend;
        NetworkConfig.OnDisconnect += EndSend;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        NetworkConfig.OnConnect -= StartSend;
        NetworkConfig.OnDisconnect -= EndSend;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            _isProblemT = !_isProblemT;
            SendProblem(TypeProblem.PressT, _isProblemT);
        }
    }
    #endregion

    #region CALLBACS
    private void StartSend()
    {
        UnityThread.ExecuteInUpdate(() => 
        {
            StartCoroutine(SendNewTransform());
        });
    }

    private void EndSend()
    {
        UnityThread.ExecuteInUpdate(() =>
        {
            StopAllCoroutines();
        });
    }
    #endregion

    #region PRIVATE_METHODS
    private void SendProblem(TypeProblem typeProblem, bool status)
    {
        NetworkSendClient.SendProblemInfo(typeProblem, status);
    }
    #endregion

    #region COROUTINES
    private IEnumerator SendNewTransform()
    {
        if (NetworkConfig.ClientSocket.IsConnected)
        {
            NetworkSendClient.SendNewTransform(transform.position, transform.rotation, transform.localScale);
        }
        else 
        {
            Debug.Log("NotConnect");
        }
        yield return new WaitForSeconds(1f / 20f);
        StartCoroutine(SendNewTransform());
    }
    #endregion

    #region PUBLIC_METHODS
    public void SendInfoMoving(TypeMove typeMove)
    {
        NetworkSendClient.SendMovingInfo(typeMove);
    }
    #endregion
}
