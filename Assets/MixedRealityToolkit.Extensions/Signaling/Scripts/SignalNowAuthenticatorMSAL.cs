using Microsoft.SignalNow.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Signaling
{
    public class SignalNowAuthenticatorMSAL : MonoBehaviour, ISignalNowAuthenticator
    {
        public Microsoft.MixedReality.Toolkit.Extensions.IdentityClient.Authenticator authenticator;
        public SignalNowManager signalNowManager;
        public string teamId = "*";

        public string authentcationServiceName => "graph.microsoft.com";

        public string userName { get; private set; }
        public string teamName { get; private set; }
        public string companyName { get; private set; }
        public string deviceId { get; private set; }

        public string authenticationToken { get; private set; }

        public event Action<object> Authenticated;
        public event Action<object> AuthenticationFailed;
        public event Action<object> SignedOut;

        private void Awake()
        {
            deviceId = Guid.NewGuid().ToString();

            authenticator.SignedIn += Authenticator_SignedIn;
            authenticator.AuthenticationFailed += Authenticator_AuthenticationFailed;
            authenticator.SignedOut += Authenticator_SignedOut;

            if (signalNowManager != null)
            {
                signalNowManager.SetAuthenticator(this);
            }
        }

        private void Authenticator_SignedOut(object sender)
        {
            SignedOut?.Invoke(this);
        }

        private void Authenticator_AuthenticationFailed(object sender, Exception ex)
        {
            AuthenticationFailed?.Invoke(this);
        }

        private void Authenticator_SignedIn(object sender)
        {
            teamName = teamId;
            userName = authenticator.userName;
            companyName = authenticator.TenantId;
            authenticationToken = authenticator.authenticationToken;

            Authenticated?.Invoke(this);
        }
    }
}