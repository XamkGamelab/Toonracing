using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;
using UnityEngine.UIElements;
using PublicWriteAccessClassOptions = Unity.Services.CloudSave.Models.Data.Player.PublicWriteAccessClassOptions;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace Blocks.PlayerAccount.TestScene
{
    public class PlayerSignInController : MonoBehaviour
    {
        AuthenticationObserver m_AuthenticationObserver;
        const string k_HiddenClass = "hidden";

        void Start()
        {
            // Find UIDocument in scene automatically
            var uiDocument = FindAnyObjectByType<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("No UIDocument found in scene!");
                return;
            }
            var root = uiDocument.rootVisualElement;
            VisualElement signInElement = root.Q<PlayerSignIn>();
            VisualElement playerDataElement = root.Q<VisualElement>(name: "player-data");

            playerDataElement.AddToClassList(k_HiddenClass);

            SetupPostSignIn(signInElement, playerDataElement, root);

            SetupPopulateButton(root);
        }

        void SetupPostSignIn(VisualElement signInElement,
            VisualElement playerDataElement,
            VisualElement root)
        {
            m_AuthenticationObserver = new AuthenticationObserver();
            m_AuthenticationObserver.RegisterSignedInCallback(async () =>
            {
                root.Q<VisualElement>(name: "player-account-modal").AddToClassList("blocks-modal-signed-in");
                signInElement.AddToClassList(k_HiddenClass);
                playerDataElement.RemoveFromClassList(k_HiddenClass);
                var bindings = new GlobalScoreClientBindings(CloudCodeService.Instance);
                int globalScore = 0;
                try
                {
                    globalScore = await bindings.GetGlobalScore();
                }
                catch (CloudCodeException e) when (e.ErrorCode == 9009)
                {
                    Debug.LogError("Could not call Cloud Code. Did you deploy the Cloud Code Module with 'Services' > 'Deployment'?");
                    Debug.LogException(e);
                }

                var label = root.Q<TextField>(name: "global-score");
                label.value = $"{globalScore}";

                var button = root.Q<Button>(name: "global-score-increment-button");
                button.clicked += async () =>
                {
                    try
                    {
                        var newScore = await bindings.UpdateGlobalScore(1);
                        label.value = $"{newScore}";
                    }
                    catch (CloudCodeException e) when (e.ErrorCode == 9009)
                    {
                        Debug.LogError("Could not call Cloud Code. Did you deploy the Cloud Code Module with 'Services' > 'Deployment'?");
                        Debug.LogException(e);
                    }
                };
            });
        }

        static void SetupPopulateButton(VisualElement root)
        {
            Button populatePlayerData = root.Q<Button>(name: "populate-data");
            populatePlayerData.clicked += async () =>
            {
                var cloudSave = CloudSaveService.Instance;
                // Create some `Default` access class data
                // This cannot be read by other players, and can be written by the player or service
                await cloudSave.Data.Player.SaveAsync(
                    new Dictionary<string, SaveItem>()
                    {
                        { "bananas", new SaveItem(5, string.Empty) },
                        { "default-key", new SaveItem("Hello World!", string.Empty) },
                        {
                            "some-object", new SaveItem(new SomeObj()
                            {
                                Name = "obj", Count = 2
                            }, string.Empty)
                        }
                    },
                    new SaveOptions(new DefaultWriteAccessClassOptions()));

                // Create some `Public` access class data
                // This can be read by other players, and can be written by the player or service
                await cloudSave.Data.Player.SaveAsync(
                    new Dictionary<string, SaveItem>()
                    {
                        { "public-number", new SaveItem(5, string.Empty) },
                        { "public-string", new SaveItem("this is public", string.Empty) },
                        {
                            "public-object", new SaveItem(new SomeObj()
                            {
                                Name = "public-obj", Count = 7
                            }, string.Empty)
                        }
                    },
                    new SaveOptions(new PublicWriteAccessClassOptions()));
                var elements = root.Query<PlayerDataLabel>().ToList();
                await Task.WhenAll(elements.Select(e => e.RefreshAsync()));
            };
        }

        struct SomeObj
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }

        void OnDestroy()
        {
            if (m_AuthenticationObserver != null)
                m_AuthenticationObserver.Dispose();
        }
    }
}
