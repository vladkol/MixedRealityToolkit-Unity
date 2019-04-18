using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

#if ENABLE_WINMD_SUPPORT
using Windows.ApplicationModel.DataTransfer;
#endif

namespace Microsoft.MixedReality.Toolkit.Extensions.IdentityClient
{
    [RequireComponent(typeof(Authenticator))]
    public class SignInOrchestrator : MonoBehaviour
    {
        public string welcomeMessage = string.Empty;
        public bool autoSignIn = false;
        public Color deviceAuthLinkColor = new Color(0f, 0.42f, 0.714f);
        [Header("Windows 10 UWP and Editor only")]
        public bool deviceCodeInClipboard = true;

        [Header("Internal controls references")]
        public TextMeshPro userNameText;
        public SpriteRenderer userPic;
        public TextMeshPro deviceFlowMessageText;
        public TextMeshPro deviceFlowUserCodeText;

        public Transform[] signInObjects;
        public Transform[] signOutObjects;

        private Authenticator authenticator;
        private string authUrl;
        private string authCode;
        private Sprite originalUserPic;

        void Awake()
        {
            authenticator = GetComponent<Authenticator>();
            authenticator.DeviceCodeReady += Authenticator_DeviceCodeReady;
            authenticator.AuthenticationFailed += Authenticator_AuthenticationFailed;
            authenticator.SignedOut += Authenticator_SignedOut;
            authenticator.SignedIn += Authenticator_SignedIn;

            originalUserPic = userPic?.sprite;
            Authenticator_SignedOut(authenticator);
        }

        public void SignIn()
        {
            if(userNameText != null)
            {
                userNameText.SetText("Signing in...");
            }
            SetUXStates(false, false);
#pragma warning disable 4014
            authenticator.SignIn();
#pragma warning restore
        }

        public void SignOut()
        {
#pragma warning disable 4014
            authenticator.SignOut();
#pragma warning restore
        }

        public void OpenBrowser()
        {
            if (!string.IsNullOrEmpty(authUrl))
            {
                Application.OpenURL(authUrl);
            }
        }

        private void Authenticator_SignedIn(object sender)
        {
            if (deviceFlowMessageText != null)
            {
                deviceFlowMessageText.gameObject.SetActive(false);
            }
            if (deviceFlowUserCodeText != null)
            {
                deviceFlowUserCodeText.gameObject.SetActive(false);
            }

            SetUXStates(false, true);

            if (userNameText != null)
            {
                userNameText.SetText(authenticator.userName);
                userNameText.gameObject.SetActive(true);

                StartCoroutine(GetUserData());
            }
        }

        private void Authenticator_SignedOut(object sender)
        {
            if (userNameText != null)
            {
                userNameText.SetText(welcomeMessage);
                userNameText.gameObject.SetActive(true);
            }
            if (deviceFlowMessageText != null)
            {
                deviceFlowMessageText.gameObject.SetActive(false);
            }
            if (deviceFlowUserCodeText != null)
            {
                deviceFlowUserCodeText.gameObject.SetActive(false);
            }
            if (userPic != null)
            {
                userPic.sprite = originalUserPic;
            }

            SetUXStates(true, false);
        }

        private void Authenticator_AuthenticationFailed(object sender, System.Exception ex)
        {
            Authenticator_SignedOut(sender);
        }

        private void Authenticator_DeviceCodeReady(object sender, string code, string url)
        {
            authUrl = url;
            authCode = code;

            // only works on UWP or in Unity Editor
            if (deviceCodeInClipboard)
            {
                CopyToClipboard(authCode);
            }

            if (userNameText != null)
            {
                userNameText.gameObject.SetActive(false);
            }

            SetUXStates(false, true);

            if (deviceFlowMessageText != null)
            {
                deviceFlowMessageText.SetText($"Open <link><color=#{ColorUtility.ToHtmlStringRGB(deviceAuthLinkColor)}>{authUrl}</color><link>");
                deviceFlowMessageText.gameObject.SetActive(true);
            }
            if (deviceFlowUserCodeText != null)
            {
                deviceFlowUserCodeText.SetText($"Use code <b>{authCode}</b>");
                deviceFlowUserCodeText.gameObject.SetActive(true);
            }
        }

        private IEnumerator GetUserData()
        {
            if (userNameText != null)
            {
                using (var www = UnityWebRequest.Get("https://graph.microsoft.com/beta/me"))
                {
                    www.SetRequestHeader("Authorization", "Bearer " + authenticator.authenticationToken);
                    yield return www.SendWebRequest();

                    if (www.isNetworkError || www.isHttpError)
                    {
                        Debug.Log(www.error);
                    }
                    else
                    {
                        string json = www.downloadHandler.text;
                        var m = System.Text.RegularExpressions.Regex.Match(json, @".*""displayName"":""(.*?)"".*");
                        if (m != null && m.Groups.Count > 0)
                        {
                            var displayName = m.Groups[1].Value;
                            userNameText.SetText(displayName);
                        }
                    }
                }
            }

            if (userPic != null)
            {
                using (var www = UnityWebRequestTexture.GetTexture("https://graph.microsoft.com/beta/me/photos/648x648/$value"))
                {
                    www.SetRequestHeader("Authorization", "Bearer " + authenticator.authenticationToken);
                    yield return www.SendWebRequest();

                    if (www.isNetworkError || www.isHttpError)
                    {
                        Debug.Log(www.error);
                    }
                    else
                    {
                        Texture2D myPic = ((DownloadHandlerTexture)www.downloadHandler).texture;
                        if (myPic != null)
                        {
                            originalUserPic = userPic.sprite;
                            var newSprite = Sprite.Create(myPic, new Rect(0, 0, myPic.width, myPic.height), new Vector2(0.5f, 0.5f));
                            userPic.sprite = newSprite;
                        }
                    }
                }
            }
        }

        private void Start()
        {
            if (autoSignIn)
            {
#pragma warning disable 4014
                authenticator.SignIn(true);
                SetUXStates(false, false);
#pragma warning restore
            }
        }

        private void SetUXStates(bool signInObjectsActive, bool signOutObjectsActive)
        {
            if (signInObjects != null)
            {
                foreach (var t in signInObjects)
                {
                    t.gameObject.SetActive(signInObjectsActive);
                }
            }
            if (signOutObjects != null)
            {
                foreach (var t in signOutObjects)
                {
                    t.gameObject.SetActive(signOutObjectsActive);
                }
            }

        }

        private static void CopyToClipboard(string text)
        {
#if ENABLE_WINMD_SUPPORT
            UnityEngine.WSA.AppCallbackItem copyAction = () =>
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(text);
                Clipboard.SetContent(dataPackage);
            };

            if(!UnityEngine.WSA.Application.RunningOnUIThread())
            {
                UnityEngine.WSA.Application.InvokeOnUIThread(copyAction, false);
            }
            else
            {
                copyAction.Invoke();
            }
#elif UNITY_EDITOR
            GUIUtility.systemCopyBuffer = text;
#endif
        }
    }
}
