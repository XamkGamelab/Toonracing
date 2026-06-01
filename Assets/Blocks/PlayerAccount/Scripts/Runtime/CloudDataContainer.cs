using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Properties;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;
using UnityEngine.UIElements;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace Blocks.PlayerAccount
{
    /// <summary>
    /// Provies observable access to player data and events to notify
    /// when the stauts of the data has changed
    /// </summary>
    public class CloudDataContainer : IDataSourceViewHashProvider, INotifyBindablePropertyChanged, IDisposable
    {
        const string k_DefaultDataValue = "<Value>";

        [SerializeField, DontCreateProperty]
        string m_PlayerName = "<Player Name>";
        [SerializeField, DontCreateProperty]
        string m_PlayerId = "<Player Id>";
        [SerializeField, DontCreateProperty]
        List<string> m_DataKeyChoices = new List<string>();
        [SerializeField, DontCreateProperty]
        string m_DataValue = k_DefaultDataValue;

        bool m_HasDataKeys;
        bool m_IsRefreshing;
        bool m_IsLoadingValue;
        bool m_IsUpdateQueued;

        int m_Version;
        string m_DataKey;
        AccessClass m_DataAccessType = AccessClass.Default;

        AuthenticationObserver m_AuthenticationObserver;

        /// <summary> This observable property tracks the name of the player.
        /// It will be updated in case of a remote change </summary>
        [CreateProperty]
        public string PlayerName
        {
            get => m_PlayerName;
            set => SetField(ref m_PlayerName, value);
        }

        /// <summary> This observable property tracks the ID of the player.
        /// It will only be updated once on initialization or sign in</summary>
        [CreateProperty]
        public string PlayerId
        {
            get => m_PlayerId;
            set => SetField(ref m_PlayerId, value);
        }

        /// <summary> Provides the keys available to the specified Access Class </summary>
        [CreateProperty]
        public List<string> DataKeyChoices
        {
            get => m_DataKeyChoices;
            set => SetField(ref m_DataKeyChoices, value);
        }

        /// <summary> The value with the selected key for the specified Access Class </summary>
        [CreateProperty]
        public string DataValue
        {
            get => m_DataValue;
            set => SetField(ref m_DataValue, value);
        }

        /// <summary> Whether Keys have been loaded </summary>
        [CreateProperty]
        public bool HasDataKeys
        {
            get => m_HasDataKeys;
            set => SetField(ref m_HasDataKeys, value);

        }

        /// <summary> Whether the entire control is refreshing </summary>
        [CreateProperty]
        public bool IsRefreshing
        {
            get => m_IsRefreshing;
            set => SetField(ref m_IsRefreshing, value);
        }

        /// <summary> Whether the Data Value is being loaded </summary>
        [CreateProperty]
        public bool IsLoadingValue
        {
            get => m_IsLoadingValue;
            set => SetField(ref m_IsLoadingValue, value);
        }

        /// <summary>Will be raised if any observable property of the object changes</summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        public CloudDataContainer()
        {
            if (Application.isPlaying)
            {
                m_AuthenticationObserver = new AuthenticationObserver();
                m_AuthenticationObserver.RegisterInitializedCallback(() =>
                {
                    UpdatePlayerInformation();
                    SetupEvents();
                });
            }
        }

        void SetupEvents()
        {
            m_AuthenticationObserver.Service.PlayerNameChanged += ServiceOnPlayerNameChanged;
        }

        void UpdatePlayerInformation()
        {
            PlayerName = m_AuthenticationObserver.Service.PlayerName;
            PlayerId = m_AuthenticationObserver.Service.PlayerId;
        }

        void ServiceOnPlayerNameChanged(string name)
        {
            PlayerName = name;
        }

        /// <summary> Update the container's information based on cloud-save data, the keys and the value </summary>
        public void UpdateCloudSaveInformation()
        {
            if (!Application.isPlaying)
                return;

            IsRefreshing = true;
            m_AuthenticationObserver.RegisterSignedInCallback(UpdateCloudSaveInformationInternal);
        }

        async void UpdateCloudSaveInformationInternal()
        {
            try
            {
                var cloudSave = CloudSaveService.Instance;
                var itemKeys = await cloudSave.Data.Player.LoadAllAsync(new LoadAllOptions(GetReadAccessClassOptions()));
                DataKeyChoices = itemKeys.Select(i => i.Key).ToList();
                HasDataKeys = DataKeyChoices.Count > 0;
                await UpdateLocalDataValue();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>Selects a key to fetch from cloud-save for the specified Access Class </summary>
        /// <param name="key">The key to fetch</param>
        public void SelectDataKey(string key)
        {
            m_DataKey = key;
            if (string.IsNullOrEmpty(key))
            {
                DataValue = k_DefaultDataValue;
                return;
            }

            if (m_IsUpdateQueued)
                return;

            m_IsUpdateQueued = true;
            m_AuthenticationObserver.RegisterSignedInCallback(async () => await UpdateLocalDataValue());
        }

        /// <summary>Selects the Access Class (Default, Public, Protected) to fetch for the control</summary>
        /// <param name="accessClass">The Access Class value</param>
        public void SelectDataAccessClass(AccessClass accessClass)
        {
            if (accessClass == m_DataAccessType)
                return;

            m_DataAccessType = accessClass;
            if (accessClass == AccessClass.Private)
            {
                DataValue = k_DefaultDataValue;
                Debug.LogError("Cannot access private data from a user client");
                return;
            }

            if (m_IsUpdateQueued)
                return;

            m_IsUpdateQueued = true;
            m_AuthenticationObserver.RegisterSignedInCallback(async () => await UpdateLocalDataValue());
        }

        /// <summary>Updates the player name </summary>
        public Task UpdateRemotePlayerName(string newName)
        {
            return m_AuthenticationObserver.Service.UpdatePlayerNameAsync(newName);
        }

        /// <summary> This updates the remote value associate with the key-access class pair </summary>
        public async Task UpdateRemoteDataValue(string value)
        {
            if (string.IsNullOrEmpty(m_DataKey))
                return;

            try
            {
                var keySet = new Dictionary<string, SaveItem>() { {m_DataKey, new SaveItem(value, null)} };
                var accessClass = GetWriteAccessClassOptions();
                await CloudSaveService.Instance.Data.Player.SaveAsync(keySet, new SaveOptions(accessClass));
                DataValue = value;
            }
            catch (Exception e)
            {
                Debug.LogError($"ERROR - Could not save player data for key '{m_DataKey}': {e.Message}");
                DataValue = $"Failed to Save '{m_DataKey}'";
            }
        }

        /// <summary> This updates the local value with changes made the remote value if any</summary>
        public async Task UpdateLocalDataValue()
        {
            if (string.IsNullOrEmpty(m_DataKey))
                return;

            try
            {
                DataValue = "loading...";
                IsLoadingValue = true;
                var keySet = new HashSet<string>() { m_DataKey };
                var accessClass = GetReadAccessClassOptions();
                var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(keySet, new LoadOptions(accessClass));
                DataValue = playerData[m_DataKey].Value.GetAsString();
            }
            catch (Exception e)
            {
                Debug.LogError($"ERROR - Could not load player data for key '{m_DataKey}': {e.Message}.\n" +
                               "Did you generate player data with the 'Populate Player' button?");

                DataValue = $"Failed to Load '{m_DataKey}'";
            }
            finally
            {
                IsLoadingValue = false;
                m_IsUpdateQueued = false;
            }
        }

        ReadAccessClassOptions GetReadAccessClassOptions()
        {
            ReadAccessClassOptions accessClass = m_DataAccessType switch
            {
                AccessClass.Default => new DefaultReadAccessClassOptions(),
                AccessClass.Protected => new ProtectedReadAccessClassOptions(),
                AccessClass.Public => new PublicReadAccessClassOptions(),
                _ => throw new ArgumentOutOfRangeException(nameof(m_DataAccessType), $"{m_DataAccessType} read is not supported")
            };

            return accessClass;
        }

        WriteAccessClassOptions GetWriteAccessClassOptions()
        {
            WriteAccessClassOptions accessClass = m_DataAccessType switch
            {
                AccessClass.Default => new DefaultWriteAccessClassOptions(),
                AccessClass.Public => new PublicWriteAccessClassOptions(),
                _ => throw new ArgumentOutOfRangeException(nameof(m_DataAccessType), $"{m_DataAccessType} write is not supported")
            };

            return accessClass;
        }

        /// <summary> Sign in anonymously. </summary>
        public Task SignInAnonymously()
        {
            return m_AuthenticationObserver.Service.SignInAnonymouslyAsync();
        }

        /// <summary> Sign in with Unity. Will open a browser tab, which allows you to use Google or other third-party providers.
        /// When the sign in completes, the game will resume as usual.
        /// Wait for the signed-in event to do anything else.
        /// Requires Identity Provider to be set up.</summary>
        public void StartSignInWithUnity()
        {
            // This task begins the sign in, but completed state
            // will not be achieved until the signed in event is called
            PlayerAccountService.Instance.SignedIn -= SignedInHandler;
            PlayerAccountService.Instance.SignedIn += SignedInHandler;
            PlayerAccountService.Instance.StartSignInAsync();
        }

        async void SignedInHandler()
        {
            PlayerAccountService.Instance.SignedIn -= SignedInHandler;
            Debug.LogWarning($"Sign in with Unity '{PlayerAccountService.Instance.AccessToken}'");
            try
            {
                await m_AuthenticationObserver.Service.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error signing in. Was the Unity player Account identity provider set up under Project Settings > Authentication > Identity Providers? Details: {e}");
                throw;
            }
        }

        /// <summary>
        /// Sign-in with username and password.
        /// Requires Identity Provider to be set up.</summary>
        public async Task SignInWithPassword(string username, string password)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"Error signing in. Was the Unity username/password identity provider set up under Project Settings > Authentication > Identity Providers? Details: " + ex.Message);
            }
        }

        /// <summary>
        /// Sign-Up with username and password. Must be done once between any sign in can be done.
        /// Requires Identity Provider to be set up.</summary>
        public async Task SignUpWithPassword(string username, string password)
        {
            try
            {
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            }
            catch (AuthenticationException ex) when (ex.ErrorCode != 10003)
            {
                // 10003 means user exists, so we bubble it up to the UI
                Debug.LogError($"Error signing in. Was the Unity username/password identity provider set up under Project Settings > Authentication > Identity Providers? Details: " + ex.Message);
            }
        }

        long IDataSourceViewHashProvider.GetViewHashCode()
        {
            return m_Version;
        }

        protected bool SetField<T>(
            ref T field,
            T value,
            Action<T> onFieldChanged = null,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
            onFieldChanged?.Invoke(field);
            IncrementVersion();
            return true;
        }

        void IncrementVersion()
        {
            m_Version++;
        }

        /// <summary> Disposes underlying classes, and deregisters from events if any </summary>
        public void Dispose()
        {
            if (m_AuthenticationObserver != null)
            {
                if (m_AuthenticationObserver.Service != null)
                    m_AuthenticationObserver.Service.PlayerNameChanged -= ServiceOnPlayerNameChanged;
                m_AuthenticationObserver.Dispose();
                m_AuthenticationObserver = null;
            }
        }
    }
}
