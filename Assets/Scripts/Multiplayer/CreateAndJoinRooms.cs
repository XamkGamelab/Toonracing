using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace Multiplayer
{
    public class CreateAndJoinRooms : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public TMPro.TMP_InputField clientNameInputField;
        public TMPro.TMP_InputField createInputField;
        public TMPro.TMP_InputField joinInputField;

        [Header("UI Screens")]
        [SerializeField] private GameObject GameStartScreen;
        [SerializeField] private GameObject GameJoinedScreen;
        
        // Privates
        private PlayerListUIManager playerListUIManager;
        
        public event Action<string, string, Color> OnPlayerJoinedRoom;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Debug.Log("CreateAndJoinRooms script started");
            if (createInputField == null)
                Debug.LogError("Create input field is not assigned");
            if (joinInputField == null)
                Debug.LogError("Join input field is not assigned");
            // Read saved username from PlayerPrefs and set it to the input field to allow the user to change it if they want
            var clientName = PlayerPrefs.GetString("UserName", "Player");
            if (clientName != null && clientNameInputField != null)
                clientNameInputField.text = clientName;
            playerListUIManager = FindAnyObjectByType<PlayerListUIManager>();
            ShowJoinScreen();
        }



        public void OnCreateRoom()
        {
            Debug.Log("Create room button pressed");
            if(string.IsNullOrEmpty(createInputField.text))
            {
                Debug.Log("Room name is empty");
                return;
            }
            else if(createInputField.text == "Room ID")
            {  
                Debug.Log("Room name is default text");
                return;
            }
            // Set the player's name and color in the Photon player properties before creating the room
            SetPlayerParameters();
            PhotonNetwork.CreateRoom(createInputField.text);
            DataManager.Instance.SetUserName(clientNameInputField.text);
            PhotonNetwork.AutomaticallySyncScene = true;

            // Subscribe to the OnStartButtonPressed event from PlayerListUIManager to start the game if 
            // the local player is the master client
            if (playerListUIManager)
                playerListUIManager.OnStartButtonPressed += StartGame;
        }
    
        public void OnJoinRoom()
        {
            if(string.IsNullOrEmpty(joinInputField.text))
            {
                Debug.Log("Room name is empty");
                return;
            }
            else if(joinInputField.text == "Room ID")
            {  
                Debug.Log("Room name is default text");
                return;
            }
            // Set the player's name and color in the Photon player properties before joining the room
            //SetPlayerParameters();
            DataManager.Instance.SetUserName(clientNameInputField.text);
            DataManager.Instance.SetPlayerColor(UnityEngine.Random.ColorHSV());
            PhotonNetwork.JoinRoom(joinInputField.text);
            PhotonNetwork.AutomaticallySyncScene = true;
            // Disable the start button for non-master clients
            if(playerListUIManager)
                playerListUIManager.IsMasterClient(false);
        }
        
        public override void OnJoinedRoom()
        {
            Debug.Log("Joined Photon room.");
            
            // Activate the player joined list on the UI and hide the game start screen
            ShowWaitingScreen();
            
            // Get the current list of players in the room
            var players = PhotonNetwork.PlayerList;
            foreach (var player in players)
            {
                var playerName = player.UserId;
                Color playerColor;
                // Get the player's name and color from their custom properties
                playerName = player.NickName;
                if (player.CustomProperties.ContainsKey("PlayerColor"))
                    ColorUtility.TryParseHtmlString($"#{player.CustomProperties["PlayerColor"]}", out playerColor);
                else
                    playerColor = UnityEngine.Random.ColorHSV();
                // Invoke the OnPlayerJoinedRoom event to update the UI with the player's name and color
                // Todo Check if this makes player appear twice 
                OnPlayerJoinedRoom?.Invoke(playerName, player.UserId, playerColor);  
            }
        }
        
        // When local player is the master client, they can start the game by loading the game scene for all players
        void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient) 
                return;
            Debug.Log("Starting the game...");
            PhotonNetwork.LoadLevel("Game");
        }
        
        // Photon callback registering 
        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }
        
        // Photon callback unregistering
        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }
        
        // Implement the IOnEventCallback interface
        // When a player joins the room, this method will be called
        // Also when master client starts the race, this method will be called to notify all players to load the game scene
        public override void OnPlayerEnteredRoom(Player newPlayer)
        { 
            ColorUtility.TryParseHtmlString((string)newPlayer.CustomProperties["PlayerColor"], out var playerColor);
            OnPlayerJoinedRoom?.Invoke(newPlayer.NickName, newPlayer.UserId, playerColor);
        }
        
        private void SetPlayerParameters(string name = null, Color? color = null)
        {
            var playerColor = color ?? UnityEngine.Random.ColorHSV();
            var localName = name ?? clientNameInputField.text;
            var colorHex = ColorUtility.ToHtmlStringRGBA(playerColor);
            // Set the player's name and color in the Photon player properties
            var customProperties = new ExitGames.Client.Photon.Hashtable
            {
                { "PlayerName", localName },
                { "PlayerColor", colorHex }
            };
            // Save the player's name and color to the DataManager and to Photon network
            DataManager.Instance.SetPlayerColor(playerColor);
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
        }
        
        private void ShowJoinScreen()
        {
            GameStartScreen.SetActive(true);
            GameJoinedScreen.SetActive(false);
        }
        
        private void ShowWaitingScreen()
        {
            GameStartScreen.SetActive(false);
            GameJoinedScreen.SetActive(true);
        }

        public void OnEvent(EventData photonEvent)
        {
            return;
        }
    }
}
