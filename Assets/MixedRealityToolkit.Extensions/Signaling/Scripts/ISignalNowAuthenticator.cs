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

        string userName { get; set; }
        string teamName { get; set; }
        string companyName { get; set; }
        string deviceId { get; set; }
        string authenticationToken { get; }
    }
}
