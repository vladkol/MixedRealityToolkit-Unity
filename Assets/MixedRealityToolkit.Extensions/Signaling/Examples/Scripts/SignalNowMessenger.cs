using Microsoft.MixedReality.Toolkit.Extensions.Signaling;
using Microsoft.SignalNow.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignalNowMessenger : MonoBehaviour
{
    public SignalNowManager signalManager;
    public TMPro.TMP_InputField textFieldMessageIn;
    public TMPro.TMP_InputField textFieldMessageOut;

    private SignalNowClient client;

    void Start()
    {
        if(signalManager != null)
        {
            client = signalManager.signalNowClient;
        }

        if(client != null)
        {
            client.NewMessage += Client_NewMessage;
        }
    }

    private void Client_NewMessage(SignalNowClient signalNow, string senderId, string messageType, string messagePayload)
    {
        if(textFieldMessageIn != null)
        {
            textFieldMessageIn.text += $"\n";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Send()
    {
        if(textFieldMessageOut != null)
        {
            string text = textFieldMessageOut.text;
        }
    }
}
