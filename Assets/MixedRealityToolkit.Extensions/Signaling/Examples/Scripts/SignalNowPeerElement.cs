using Microsoft.SignalNow.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.Signaling
{
    public class SignalNowPeerElement : MonoBehaviour
    {
        public UnityEngine.UI.Text peerNameText;
        public SignalNowMessenger messenger;
        public SignalNowPeer peer;

        private bool peerAssigned = false;

        public void SendSignalMessage()
        {
            if (messenger != null && messenger.signalManager != null && peer != null)
            {
                var t = messenger.signalManager.signalNowClient.SendMessage(peer.UserId, false, "MESSAGE",
                    messenger.textFieldMessageOut.text, true);
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
