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

        void Awake()
        {
            signalNowClient = new SignalNowClient(signalServer);
        }

        // Update is called once per frame
        void Update()
        {
        
        }

    }
}