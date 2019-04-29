using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SignalNow.Client
{
    public interface ISignalNowAuthenticator
    {
        event Action<object> Authenticated;
        event Action<object> AuthenticationFailed;
        event Action<object> SignedOut;

        string authentcationServiceName { get; }

        string userName { get; }
        string teamName { get; }
        string companyName { get; }
        string deviceId { get; }
        string authenticationToken { get; }
    }
}
