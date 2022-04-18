using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CheckConnect : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    #region MONOEVENTS
    private void Awake()
    {
        _text.text = "Навигатор не найден!";
        NetworkConfig.OnConnect += Connect;
        NetworkConfig.OnDisconnect += Disconnect;
    }

    private void OnDestroy()
    {
        NetworkConfig.OnConnect -= Connect;
        NetworkConfig.OnDisconnect -= Disconnect;
    }
    #endregion

    #region PRIVATE_METHODS
    private void Connect()
    {
        _text.text = "Навигатор подключен!";
    }

    private void Disconnect()
    {
        _text.text = "Навигатор не найден!";
    }
    #endregion
}
