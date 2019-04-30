using Microsoft.MixedReality.Toolkit.Extensions.Signaling;
using Microsoft.SignalNow.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.WebRTC
{
    public class WebRTCPeerElement : MonoBehaviour
    {
        public UnityEngine.UI.Text peerNameText;
        public SignalNowWebRTCCaller caller;
        public SignalNowPeer peer;

        private bool peerAssigned = false;

        public void Call()
        {
            if (caller != null && peer != null)
            {
                caller.MakeCall(peer);
            }
        }

        private void Update()
        {
            if(!peerAssigned && peer != null && peerNameText != null)
            {
                peerAssigned = true;
                peerNameText.text = peer.UserName;
            }
        }
    }
}
