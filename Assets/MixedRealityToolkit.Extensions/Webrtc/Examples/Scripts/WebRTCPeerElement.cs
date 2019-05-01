using Microsoft.MixedReality.Toolkit.Extensions.Signaling;
using Microsoft.SignalNow.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.Extensions.WebRTC
{
    public class WebRTCPeerElement : MonoBehaviour
    {
        public UnityEngine.UI.Text peerNameText;
        public SignalNowPeerEvent OnCall = new SignalNowPeerEvent();
        public SignalNowPeer peer { get; set; }
        private bool peerAssigned = false;

        public void Call()
        {
            if (peer != null)
            {
                OnCall?.Invoke(peer);
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
