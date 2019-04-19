using Microsoft.Identity.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Extensions.IdentityClient
{
    public class Authenticator : MonoBehaviour
    {
        private const string _msGraphCacheFileName = "MSGraphIdentityCache.dat";
        private const int _silentTokenAquisitionTimeout = 5000;

        public delegate void SignedInHandler(object sender);
        public delegate void SignedOutHandler(object sender);
        public delegate void AuthenticationFailedHandler(object sender, Exception ex);
        public delegate void DeviceCodeReadyHandler(object sender, string code, string url);

        public string applicationClientId;
        public string[] scopes = new string[] { "User.Read" };
        public bool cacheAuthenticationTokens = true;

        public string authenticationToken { get; private set; }
        public string userName { get { return account != null ? account.Username : string.Empty; } }
        public string userId { get; private set; }
        public IPublicClientApplication identityClient
        {
            get
            {
                return client;
            }
        }
        public IAccount account { get; private set; }
        public string TenantId { get { return account != null ? account.HomeAccountId.TenantId : string.Empty; } }

        public event SignedInHandler SignedIn;
        public event SignedOutHandler SignedOut;
        public event DeviceCodeReadyHandler DeviceCodeReady;
        public event AuthenticationFailedHandler AuthenticationFailed;

        private IPublicClientApplication client;
        private MSGraphCache cache;
        private AuthenticationResult authResult;
        private Exception exception;
        private CancellationTokenSource deviceFlowCancellation;
        private List<string> requestedScopes;

        private string userIdInProcess;
        private string deviceFlowUrlInProcess;
        private string deviceCodeInProcess;
        private bool justSignedIn = false;
        private bool justSignedOut = false;

        void Awake()
        {
            if (!string.IsNullOrEmpty(applicationClientId))
            {
                InitializeClient();
            }
            else
            {
                Debug.LogError($"Application CLient Id should not be empty");
            }
        }

        public async Task SignOut()
        {
            if(deviceFlowCancellation != null)
            {
                deviceFlowCancellation.Cancel();
                deviceFlowCancellation = null;
            }
            if (cache != null)
            {
                cache.lastUserId = string.Empty;
                cache.tokenCache = null;
                cache.Save(_msGraphCacheFileName);
            }

            if (client != null && account != null)
            {
                userId = string.Empty;
                authenticationToken = string.Empty;

                await client.RemoveAsync(account);
                account = null;
            }
            justSignedOut = true;
        }

        public async Task SignIn(bool onlySilent = false)
        {
            if (client == null)
            {
                InitializeClient();
                if (client == null)
                {
                    return;
                }
            }

            userIdInProcess = cache.lastUserId;

            if (!string.IsNullOrEmpty(userIdInProcess))
            {
                account = await client.GetAccountAsync(userIdInProcess);
            }

            bool needAuth = true;
            if (account != null)
            {
                try
                {
                    var tokenAqusitionCancellation = new System.Threading.CancellationTokenSource();

                    var silentAquisitionTask = client.AcquireTokenSilent(requestedScopes, account).
                                                        ExecuteAsync(tokenAqusitionCancellation.Token);
                    tokenAqusitionCancellation.CancelAfter(_silentTokenAquisitionTimeout);

                    authResult = await silentAquisitionTask;
                    if (authResult != null)
                    {
                        needAuth = false;
                    }
                    else
                    {
                        account = null;
                    }
                }
                catch (MsalUiRequiredException)
                {
                    Debug.LogWarning($"Cannot aquire token for saved credentials ({account.Username}). Authentication is required.");
                    account = null;
                }
                catch (TaskCanceledException)
                {
                    Debug.LogWarning($"Cannot aquire token for saved credentials ({account.Username}) within timeout allowed. Authentication is required.");
                    account = null;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Cannot aquire authentication token. {ex.Message}");
                    account = null;
                    exception = ex;
                }
            }

            if (!needAuth)
            {
                userId = userIdInProcess;
                authenticationToken = authResult.AccessToken;
                justSignedIn = true;
            }
            else 
            {
                userId = string.Empty;
                userIdInProcess = string.Empty;
                account = null;

                if (cache != null)
                {
                    cache.lastUserId = string.Empty;
                    cache.tokenCache = null;
                    cache.Save(_msGraphCacheFileName);
                }
                if (!onlySilent)
                {
                    await SignInWithDeviceFlow();
                }
            }

            if (account != null && !string.IsNullOrEmpty(authenticationToken) && cache != null)
            {
                cache.lastUserId = userId;
                if (cacheAuthenticationTokens)
                {
                    cache.tokenCache = client.UserTokenCache.SerializeMsalV3();
                }
                cache.Save(_msGraphCacheFileName);
            }
        }

        private async Task SignInWithDeviceFlow()
        {
            try
            {
                deviceFlowCancellation = new CancellationTokenSource();
                authResult = await client.AcquireTokenWithDeviceCode(requestedScopes, (DeviceCodeResult codeResult) =>
                {
                    deviceCodeInProcess = codeResult.UserCode;
                    deviceFlowUrlInProcess = codeResult.VerificationUrl;
                    Debug.Log($"Sign in at {codeResult.VerificationUrl} with code {codeResult.UserCode}");

                    return Task.FromResult(0);
                }).ExecuteAsync(deviceFlowCancellation.Token);

                if (authResult != null)
                {
                    account = authResult.Account;
                    userId = account.HomeAccountId.Identifier;
                    authenticationToken = authResult.AccessToken;

                    if (cache != null)
                    {
                        cache.lastUserId = userId;
                    }

                    justSignedIn = true;
                }
            }
            catch (MsalServiceException ex)
            {
                // Kind of errors you could have (in ex.Message)

                // AADSTS50059: No tenant-identifying information found in either the request or implied by any provided credentials.
                // Mitigation: as explained in the message from Azure AD, the authoriy needs to be tenanted. you have probably created
                // your public client application with the following authorities:
                // https://login.microsoftonline.com/common or https://login.microsoftonline.com/organizations

                // AADSTS90133: Device Code flow is not supported under /common or /consumers endpoint.
                // Mitigation: as explained in the message from Azure AD, the authority needs to be tenanted

                // AADSTS90002: Tenant <tenantId or domain you used in the authority> not found. This may happen if there are 
                // no active subscriptions for the tenant. Check with your subscription administrator.
                // Mitigation: if you have an active subscription for the tenant this might be that you have a typo in the 
                // tenantId (GUID) or tenant domain name.
                string err = $"Error Acquiring Token For Device Code:{Environment.NewLine}{ex}";
                Debug.LogError(err);
                exception = ex;
            }
            catch (OperationCanceledException ex)
            {
                // If you use a CancellationToken, and call the Cancel() method on it, then this may be triggered
                // to indicate that the operation was cancelled. 
                // See https://docs.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads 
                // for more detailed information on how C# supports cancellation in managed threads.
                string err = $"Error Acquiring Token For Device Code:{Environment.NewLine} Operation Cancelled";
                Debug.LogWarning(err);
                exception = ex;
            }
            catch (MsalClientException ex)
            {
                // Verification code expired before contacting the server
                // This exception will occur if the user does not manage to sign-in before a time out (15 mins) and the
                // call to `AcquireTokenWithDeviceCodeAsync` is not cancelled in between
                string err = $"Error Acquiring Token For Device Code - Token Expired:{Environment.NewLine}{ex}";
                Debug.LogError(err);
                exception = ex;
            }
            catch (Exception ex)
            {
                string err = $"Error: Please check your connection and retry {Environment.NewLine}{ex.Message}";
                Debug.LogError(err);
                exception = ex;
            }
        }

        private void InitializeClient()
        {
            if (client != null)
                return;

            if (!string.IsNullOrEmpty(applicationClientId))
            {
                client = PublicClientApplicationBuilder.Create(applicationClientId).Build();
                if (client != null)
                {
                    cache = MSGraphCache.LoadFromFile(_msGraphCacheFileName);
                    if(cache == null)
                    {
                        cache = new MSGraphCache();
                    }
                    else if (cacheAuthenticationTokens && cache.tokenCache != null)
                    {
                        client.UserTokenCache.DeserializeMsalV3(cache.tokenCache);
                    }
                }
            }
        }

        private void Start()
        {
            // MSAL always sends the scopes 'openid profile offline_access', remove them from requested scopes
            var alwaysRequested = new string[] { "openid", "profile", "offline_access" };
            requestedScopes = scopes.Where(i => !alwaysRequested.Contains(i, StringComparer.InvariantCultureIgnoreCase)).ToList();
        }

        void Update()
        {
            if (!string.IsNullOrEmpty(deviceFlowUrlInProcess))
            {
                string url = deviceFlowUrlInProcess;
                deviceFlowUrlInProcess = null;

                DeviceCodeReady?.Invoke(this, deviceCodeInProcess, url);
            }
            else if (exception != null)
            {
                var ex = exception;
                exception = null;

                AuthenticationFailed?.Invoke(this, ex);
            }

            if (justSignedIn)
            {
                justSignedIn = false;
                SignedIn?.Invoke(this);
            }
            if (justSignedOut)
            {
                justSignedOut = false;
                SignedOut?.Invoke(this);
            }
        }

    }

    [Serializable]
    internal class MSGraphCache
    {
        public string lastUserId = string.Empty;
        public byte[] tokenCache = null;

        private object _saveLock = new object();

        public static MSGraphCache LoadFromFile(string fileName)
        {
            string fullPath = GetFullPathName(fileName);

            if (System.IO.File.Exists(fullPath))
            {
                var cache = JsonUtility.FromJson<MSGraphCache>(System.IO.File.ReadAllText(fullPath));
                if(cache.lastUserId == null)
                {
                    cache.lastUserId = string.Empty;
                }
                return cache;
            }
            else
            {
                return null;
            }
        }

        public void Remove(string fileName)
        {
            string fullPath = GetFullPathName(fileName);
            lock (_saveLock)
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }

        public void Save(string fileName)
        {
            string fullPath = GetFullPathName(fileName);

            lock (_saveLock)
            {
                string json = JsonUtility.ToJson(this, false);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
                System.IO.File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);
            }
        }

        private static string GetFullPathName(string fileName)
        {
            string fullPath = fileName;
            if (!System.IO.Path.IsPathRooted(fileName))
            {
                fullPath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            }
            return fullPath;
        }
    }
}