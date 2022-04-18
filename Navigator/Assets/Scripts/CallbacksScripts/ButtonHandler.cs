using System.Collections;
using UnityEngine;
using TMPro;

public class ButtonHandler : MonoBehaviour
{
    #region SERIALIZE_FIELDS
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private TextMeshProUGUI _networkText;
    [SerializeField] private GameObject _routeText;
    #endregion

    #region MONOEVENTS
    // Start is called before the first frame update
    void Start()
    {
        NetworkReceiveServer.OnChangeTypeMove += ChangeText;
        NetworkConfig.OnConnectClient += ClientConnect;
        NetworkConfig.OnDisconnectClient += ClientDisconnect;
        switch (RouteChecker.Instance.TMove)
        {
            case TypeMove.Arrows:
                _text.text = "Автопилот выключен!";
                break;

            case TypeMove.Navigator:
                _text.text = "Автопилот включен!";
                break;
        }
    }

    private void OnDestroy()
    {
        NetworkReceiveServer.OnChangeTypeMove -= ChangeText;
        NetworkConfig.OnConnectClient -= ClientConnect;
        NetworkConfig.OnDisconnectClient -= ClientDisconnect;
        StopAllCoroutines();
    }
    #endregion

    #region PUBLIC_METHODS
    public void ClickHorn_Down()
    {
        NetworkSendServer.SendHorn(true);
    }

    public void ClickHorn_Up()
    {
        NetworkSendServer.SendHorn(false);
    }

    public void ClickAutopilot()
    {
        switch (RouteChecker.Instance.TMove)
        {
            case TypeMove.Arrows:
                RouteChecker.Instance.TMove = TypeMove.Navigator;
                _text.text = "Автопилот включен!";
                break;

            case TypeMove.Navigator:
                RouteChecker.Instance.TMove = TypeMove.Arrows;
                _text.text = "Автопилот выключен!";
                break;
        }

        NetworkSendServer.SendCurrentMoveType();
    }
    #endregion

    #region PRIVATE_METHODS
    private void ChangeText()
    {
        switch (RouteChecker.Instance.TMove)
        {
            case TypeMove.Arrows:
                _text.text = "Автопилот выключен!";
                StartCoroutine(ShowRouteText());
                break;

            case TypeMove.Navigator:
                _text.text = "Автопилот включен!";
                break;
        }
    }

    private void ClientConnect()
    {
        _networkText.text = "Подключено!";
    }

    private void ClientDisconnect()
    {
        _networkText.text = "Не подключено!";
    }
    #endregion

    #region COROUTINES
    private IEnumerator ShowRouteText()
    {
        _routeText.SetActive(true);
        yield return new WaitForSeconds(3f);
        _routeText.SetActive(false);
    }
    #endregion
}
