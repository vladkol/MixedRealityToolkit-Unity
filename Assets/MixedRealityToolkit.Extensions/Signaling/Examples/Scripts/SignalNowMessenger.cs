using Microsoft.MixedReality.Toolkit.Extensions.Signaling;
using Microsoft.SignalNow.Client;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class SignalNowMessenger : MonoBehaviour
{
    public SignalNowManager signalManager;
    public TMPro.TMP_InputField textFieldMessageIn;
    public TMPro.TMP_InputField textFieldMessageOut;

    public GameObject peerElementPrefab;
    public Transform peerListParent;

    private SignalNowClient client;
    private readonly static ConcurrentQueue<Action> RunOnMainThread = new ConcurrentQueue<Action>();

    void Start()
    {
        if(signalManager != null)
        {
            client = signalManager.signalNowClient;
        }

        if(client != null)
        {
            client.NewMessage += Client_NewMessage;
            client.NewPeer += Client_NewPeer;
            client.PeerStatusChanged += Client_PeerStatusChanged;
            client.ConnectionChanged += Client_ConnectionChanged;
        }
    }

    private void Client_ConnectionChanged(SignalNowClient signalNow, bool connected, Exception ifErrorWhy)
    {
        if (!connected)
        {
            RunOnMainThread.Enqueue(() =>
            {
                foreach (var e in peerListParent.GetComponentsInChildren<SignalNowPeerElement>())
                {
                    Destroy(e.gameObject);
                }
            });
        }

    }

    private void Client_NewPeer(SignalNowClient signalNow, SignalNowPeer newPeer)
    {
        if(peerListParent != null && peerElementPrefab != null)
        {
            RunOnMainThread.Enqueue(() =>
            {
                var newObject = Instantiate(peerElementPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                SignalNowPeerElement element = newObject.GetComponent<SignalNowPeerElement>();
                if (element != null)
                {
                    element.peer = newPeer;
                    element.messenger = this;
                }
                newObject.transform.SetParent(peerListParent, false);
            });
        }
    }

    private void Client_PeerStatusChanged(SignalNowClient signalNow, SignalNowPeer peer)
    {
        if (peer.Status == PeerStatus.Offline && peerListParent != null)
        {
            RunOnMainThread.Enqueue(() =>
            {
                foreach (var e in peerListParent.GetComponentsInChildren<SignalNowPeerElement>())
                {
                    if(e.peer.UserId == peer.UserId)
                        Destroy(e.gameObject);
                }
            });
        }
    }

    private void Client_NewMessage(SignalNowClient signalNow, string senderId, string messageType, string messagePayload)
    {
        if(textFieldMessageIn != null && messageType == "MESSAGE")
        {
            RunOnMainThread.Enqueue(() =>
            {
                textFieldMessageIn.text += $"{messagePayload}\n";
            });
        }
    }

    void Update()
    {
        if (!RunOnMainThread.IsEmpty)
        {
            Action action;
            while (RunOnMainThread.TryDequeue(out action))
            {
                action.Invoke();
            }
        }
    }
}
