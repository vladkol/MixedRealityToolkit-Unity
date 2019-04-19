using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.SignalNow.Client
{
    public interface ISignalNowAuthenticator
    {
        event Action<string> Authenticated;
        event Action<string> AuthenticationFailed;

        string authentcationServiceName { get; }

        string userName { get; }
        string teamName { get; }
        string companyName { get; }
        string deviceId { get; }
        string authenticationToken { get; }
    }
}
