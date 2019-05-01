using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Microsoft.SignalNow.Client
{
    [Serializable]
    public class SignalNowPeerEvent : UnityEvent<SignalNowPeer>
    {
    }
}
