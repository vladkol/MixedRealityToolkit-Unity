using Microsoft.MixedReality.Toolkit.Extensions.Signaling;
using Microsoft.MixedReality.Toolkit.Extensions.WebRTC.Signaling;
using Microsoft.SignalNow.Client;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.WebRTC
{
    public class SignalNowWebRTCCaller : MonoBehaviour
    {
        private const string kWebRTCMessageType = "MRTKWEBRTC";

        public SignalNowManager signalManager;
        public WebrtcPeerEvents peerEventsInstance;

        public GameObject peerElementPrefab;
        public Transform peerListParent;

        private SignalNowClient client;
        private string peerToCall = string.Empty;
        private readonly static ConcurrentQueue<Action> RunOnMainThread = new ConcurrentQueue<Action>();

        void Start()
        {
            if(signalManager != null)
            {
                client = signalManager.signalNowClient;
            }

            if(client != null)
            {
                client.NewPeer += Client_NewPeer;
                client.PeerStatusChanged += Client_PeerStatusChanged;
                client.ConnectionChanged += Client_ConnectionChanged;
                client.NewMessage += Client_NewMessage;
                client.RequestFailed += Client_RequestFailed;
            }

            peerEventsInstance.OnPeerReady.AddListener(() =>
            {
                peerEventsInstance.AddStream(audioOnly: false);
            });

            // bind our handler so when an offer is ready we can write it to signalling
            peerEventsInstance.OnSdpOfferReadyToSend.AddListener((string offer) =>
            {
                if (peerToCall != null)
                {
                    SendMessage(new SignalerMessage()
                    {
                        MessageType = SignalerMessage.WireMessageType.Offer,
                        Data = offer,
                        TargetId = peerToCall
                    });
                }
            });

            // bind our handler so when an answer is ready we can write it to signalling
            peerEventsInstance.OnSdpAnswerReadyToSend.AddListener((string answer) =>
            {
                if (peerToCall != null)
                {
                    SendMessage(new SignalerMessage()
                    {
                        MessageType = SignalerMessage.WireMessageType.Answer,
                        Data = answer,
                        TargetId = peerToCall
                    });
                }
            });

            // bind our handler so when an ice message is ready we can to signalling
            peerEventsInstance.OnIceCandiateReadyToSend.AddListener((string candidate, int sdpMlineindex, string sdpMid) =>
            {
                if (peerToCall != null)
                {
                    SendMessage(new SignalerMessage()
                    {
                        MessageType = SignalerMessage.WireMessageType.Ice,
                        Data = candidate + "|" + sdpMlineindex + "|" + sdpMid,
                        IceDataSeparator = "|",
                        TargetId = peerToCall
                    });
                }
            });
        }

        public void MakeCall(SignalNowPeer peer)
        {
            peerToCall = peer.UserId;
            peerEventsInstance.CreateOffer();
        }

        private void SendMessage(SignalerMessage message)
        {
            var json = JsonUtility.ToJson(message);
            if(client != null)
            {
                var t = client.SendMessage(message.TargetId, false, kWebRTCMessageType, json, true);
            }
        }

        private void HandleMessage(SignalerMessage msg)
        {
            // depending on what type of message we get, we'll handle it differently
            // this is the "glue" that allows two peers to establish a connection.
            switch (msg.MessageType)
            {
                case SignalerMessage.WireMessageType.Offer:
                    peerEventsInstance.SetRemoteDescription("offer", msg.Data);
                    // if we get an offer, we immediately send an answer
                    peerEventsInstance.CreateAnswer();
                    break;
                case SignalerMessage.WireMessageType.Answer:
                    peerEventsInstance.SetRemoteDescription("answer", msg.Data);
                    break;
                case SignalerMessage.WireMessageType.Ice:
                    // this "parts" protocol is defined above, in PeerEventsInstance.OnIceCandiateReadyToSend listener
                    var parts = msg.Data.Split(new string[] { msg.IceDataSeparator }, StringSplitOptions.RemoveEmptyEntries);
                    peerEventsInstance.AddIceCandidate(parts[0], int.Parse(parts[1]), parts[2]);
                    break;
                case SignalerMessage.WireMessageType.SetPeer:
                    // we can ignore it here
                    break;
                default:
                    Debug.Log("Unknown message: " + msg.MessageType + ": " + msg.Data);
                    break;
            }
        }

        private void Client_NewMessage(SignalNowClient signalNow, string senderId, string messageType, string messagePayload)
        {
            if(messageType == kWebRTCMessageType)
            {
                try
                {
                    SignalerMessage msg = JsonUtility.FromJson<SignalerMessage>(messagePayload);
                    peerToCall = senderId;
                    HandleMessage(msg);
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private void Client_ConnectionChanged(SignalNowClient signalNow, bool connected, Exception ifErrorWhy)
        {
            if (!connected)
            {
                peerToCall = string.Empty;
                RunOnMainThread.Enqueue(() =>
                {
                    foreach (var e in peerListParent.GetComponentsInChildren<WebRTCPeerElement>())
                    {
                        Destroy(e.gameObject);
                    }
                });
            }
            else
            {
                peerToCall = string.Empty;
            }
        }

        private void Client_NewPeer(SignalNowClient signalNow, SignalNowPeer newPeer)
        {
            if(peerListParent != null && peerElementPrefab != null)
            {
                RunOnMainThread.Enqueue(() =>
                {
                    var newObject = Instantiate(peerElementPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    WebRTCPeerElement element = newObject.GetComponent<WebRTCPeerElement>();
                    if (element != null)
                    {
                        element.peer = newPeer;
                        element.caller = this;
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
                    foreach (var e in peerListParent.GetComponentsInChildren<WebRTCPeerElement>())
                    {
                        if(e.peer.UserId == peer.UserId)
                            Destroy(e.gameObject);
                    }
                });
            }
        }

        private void Client_RequestFailed(SignalNowClient signalNow, string errorMessage)
        {
            Debug.LogError($"WebRTC Signaling Error: {errorMessage}");
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
}
