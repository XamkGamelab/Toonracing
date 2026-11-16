using System;
using Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListUIManager : MonoBehaviour
{
    [Header("Player List UI prefabs")]
    [SerializeField] private PlayerListItem playerListItemPrefab;
    [Header("Player List Elements")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private TMPro.TextMeshProUGUI titleText;
    [Header("UI Buttons")]
    [SerializeField] private Button startGameButton;
    private bool isMasterClient = false;
    public event Action OnStartButtonPressed;
    private void Start()
    {
        var createAndJoinRooms = FindAnyObjectByType<CreateAndJoinRooms>();
        if (createAndJoinRooms != null)
            createAndJoinRooms.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
        else
            Debug.Log("PlayerListPopulate: CreateAndJoinRooms script not found in the scene.");

        if (startGameButton)
        {
            startGameButton.onClick.AddListener(StartButtonPressed);
        }
    }
    
    private void OnPlayerJoinedRoom(string playerName, string playerId, Color playerColor)
    {
        Debug.Log($"Player joined room: {playerName} with color {playerColor}");
        var newItem = Instantiate(playerListItemPrefab, playerListContainer);
        newItem.SetPlayerName(playerName);
        newItem.SetPlayerColor(playerColor);
        newItem.SetPlayerID(playerId);
        if(playerListContainer.childCount >= 10)
        {
            Debug.Log("Max players reached!");
            if (isMasterClient)
                titleText.text = "Max players reached! Start the game!";
            else
                titleText.text = "Waiting for the game to start...";
        }
    }
    
    void StartButtonPressed()
    {
        Debug.Log("Start button pressed");
        OnStartButtonPressed?.Invoke();
    }

    public void IsMasterClient(bool mode)
    {
        isMasterClient = mode;
        if (isMasterClient) 
            return;
        // Disable the start button for non-master clients
        if(startGameButton)
            startGameButton.gameObject.SetActive(false);
    }
    
    private void OnDisable()
    {
        //
    }
}
