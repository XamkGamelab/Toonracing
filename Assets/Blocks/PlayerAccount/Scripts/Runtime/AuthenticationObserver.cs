using System;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Blocks.PlayerAccount
{
    /// <summary>
    /// Manage authentication in your scenes.
    /// Should be in authentication service.
    /// </summary>
    public class AuthenticationObserver : ServiceObserver<IAuthenticationService>
    {
        /// <summary>
        /// Registers a callbacks to trigger when services are initialized, which will happen only once.
        /// If services are already initialized, this will trigger right away.
        /// </summary>
        public void RegisterInitializedCallback(Action action)
        {
            if (UnityServices.Instance != null && UnityServices.Instance.State == ServicesInitializationState.Initialized)
            {
                action();
                return;
            }

            Action<IAuthenticationService> autoDeregister = null;
            autoDeregister = (serv) =>
            {
                action();
                Initialized -= autoDeregister;
            };
            Initialized += autoDeregister;
        }

        /// <summary>
        /// Registers a callbacks to trigger when the player is signed in, which will happen only once.
        /// If services are already initialized, this will trigger right away.
        /// </summary>
        public void RegisterSignedInCallback(Action action)
        {
            if (UnityServices.Instance == null || UnityServices.Instance.State != ServicesInitializationState.Initialized)
            {
                RegisterInitializedCallback(() => RegisterSignedInCallback(action));
                return;
            }

            if (!Service.IsSignedIn)
            {
                Action autoDeregister = null;
                autoDeregister =  () =>
                {
                    action();
                    Service.SignedIn -= autoDeregister;
                };
                Service.SignedIn += autoDeregister;
                return;
            }
            action();
        }
    }
}
