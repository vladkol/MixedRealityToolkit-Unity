using Microsoft.SignalNow.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.Extensions.Signaling
{
    public class SignalNowPeerElement : MonoBehaviour
    {
        public UnityEngine.UI.Text peerNameText;
        public SignalNowPeerEvent OnSendSignal = new SignalNowPeerEvent();

        public SignalNowPeer peer { get; set; }

        private bool peerAssigned = false;

        public void SendSignalMessage()
        {
            if (peer != null)
            {
                OnSendSignal?.Invoke(peer);
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
