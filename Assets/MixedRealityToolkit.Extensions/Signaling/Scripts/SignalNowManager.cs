using Microsoft.SignalNow.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Signaling
{
    public class SignalNowManager : MonoBehaviour
    {
        public string signalServer;
        public SignalNowClient signalNowClient { get; private set; }
        public ISignalNowAuthenticator authenticator;

        void OnEnable()
        {
            signalNowClient = new SignalNowClient(signalServer);
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
            var t = signalNowClient.Connect(authenticator.userName, authenticator.deviceId,
                authenticator.companyName, authenticator.teamName, 
                authenticator.authenticationToken, authenticator.authentcationServiceName);
        }
    }
}