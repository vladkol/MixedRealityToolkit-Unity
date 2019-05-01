using Microsoft.SignalNow.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.Extensions.Signaling
{
    public class SignalNowManager : MonoBehaviour
    {
        public string signalServer;
        public SignalNowClient signalNowClient { get; private set; }
        public ISignalNowAuthenticator authenticator;

        public UnityEvent OnConnected = new UnityEvent();
        public UnityEvent OnConnecting = new UnityEvent();
        public UnityEvent OnDisconnected = new UnityEvent();

        void OnEnable()
        {
            signalNowClient = new SignalNowClient(signalServer, 60);
            signalNowClient.ConnectionChanged += SignalNowClient_ConnectionChanged;
        }

        private void Start()
        {
            OnDisconnected?.Invoke();
        }

        private void SignalNowClient_ConnectionChanged(SignalNowClient signalNow, bool connected, System.Exception ifErrorWhy)
        {
            if(connected)
            {
                OnConnected?.Invoke();
            }
            else
            {
                OnDisconnected?.Invoke();
            }
        }

        private void OnDisable()
        {
            if(signalNowClient != null)
            {
                signalNowClient.Disconnect();
                signalNowClient = null;
            }
        }

        public void SetAuthenticator(ISignalNowAuthenticator authenticator)
        {
            this.authenticator = authenticator;
            this.authenticator.Authenticated += Authenticator_Authenticated;
            this.authenticator.SignedOut += Authenticator_SignedOut;
        }

        private void Authenticator_SignedOut(object obj)
        {
            var t = signalNowClient.Disconnect();
        }

        private void Authenticator_Authenticated(object obj)
        {
            OnConnecting?.Invoke();
            var t = signalNowClient.Connect(authenticator.userName, authenticator.deviceId,
                authenticator.companyName, authenticator.teamName, 
                authenticator.authenticationToken, authenticator.authentcationServiceName);
        }
    }
}