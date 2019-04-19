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

        public string authentcationServiceName => throw new NotImplementedException();

        public string userName { get; private set; }
        public string teamName { get; private set; }
        public string companyName { get; private set; }
        public string deviceId { get; private set; }

        public string authenticationToken => throw new NotImplementedException();

        public event Action<string> Authenticated;
        public event Action<string> AuthenticationFailed;
    }
}