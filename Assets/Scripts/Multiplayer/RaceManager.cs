using System;
using System.Collections;
using Car;
using ExitGames.Client.Photon;
using Items;
using Photon.Pun;
using Photon.Realtime;
using UI;
using UnityEngine;
using EventType = Items.EventType;

public class RaceManager : MonoBehaviour, IPunObservable, IOnEventCallback
{
    // Make this a singleton so that it can be easily accessed from other scripts
    public static RaceManager Instance { get; private set; }
    [Header("Race Settings")]
    [SerializeField] private float laps = 5f;
    [SerializeField] private double raceStartCountdown = 4f;
    
    [Header("Race Stats")]
    [SerializeField] private bool showStats = false;

    [SerializeField] private float rtt;
    [SerializeField] private float jitter;
    [SerializeField] private float kbytesInSpeed;
    [SerializeField] private float kbytesOutSpeed;
    // For game stats
    private float statsTimer = 0f;
    private float lastBytesIn;
    private float lastBytesOut;
    // Variables for the current track
    int checkpointsCount;
    // Private variables
    private CarController localPlayer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        if(showStats)
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsEnabled = true;
        if(PhotonNetwork.IsMasterClient)
            Invoke(nameof(SendStartRace), 2f);
    }

    // Update is called once per frame
    void Update()
    {
        if(showStats)
            ShowStats();
    }

    
    private void SendStartRace()
    {
        // Start the race countdown and notify other players
        var raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        var eventData = new object[] { PhotonNetwork.Time + raceStartCountdown }; // Send the race start time to other players
        Debug.Log("RaceManager: Sending StartRace event to other players: " + eventData[0]);
        PhotonNetwork.RaiseEvent((byte)EventType.StartRace, eventData, raiseEventOptions, SendOptions.SendReliable);
    }
    private void ShowStats()
    {
        statsTimer += Time.deltaTime;
        if (!(statsTimer >= 1f)) 
            return;
        var peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
        var bytesIn = peer.TrafficStatsIncoming.TotalPacketBytes;
        var bytesOut = peer.TrafficStatsOutgoing.TotalPacketBytes;
            
        kbytesInSpeed = (bytesIn - lastBytesIn) / 1024f / statsTimer;
        kbytesOutSpeed = (bytesOut - lastBytesOut) / 1024f / statsTimer;

        lastBytesIn = bytesIn;
        lastBytesOut = bytesOut;

        statsTimer = 0f;

    }
    
    private IEnumerator RaceStartSequence(double startTime)
    {
        var delta = startTime - PhotonNetwork.Time;
        Debug.Log($"RaceManager: Starting race countdown. Time until race starts: {delta} seconds.");
        var phase = 3;
        var changed = false;
        while (delta > 0)
        { 
            delta = startTime - PhotonNetwork.Time;
            switch (phase)
            {
                case 3:
                    if (delta < 3f)
                    {
                        changed = true;
                    }
                    break;
                case 2:
                    if (delta < 2f)
                    {
                        changed = true;
                    }
                    break;
                case 1:
                    if (delta < 1f)
                    {
                        changed = true;
                    }
                    break;
                case 0:
                    if (delta < 0f)
                    {
                        changed = true;
                    }
                    break;
            }

            if (changed)
            {
                changed = false;
                phase--;
                RaceUIManager.Instance.UpdateStartCountdown(phase);
            }
            yield return null;
        }
        InputHandler.Instance.SetInputEnabled(true);
    }

    #region Photon Events

    public void OnEvent(EventData itemEvent)
    {
        var senderId = itemEvent.Sender;
        // Ignore events sent by the server 
        if(senderId <= 0)
            return;
        var eventCode = itemEvent.Code;
        var data = itemEvent.CustomData as object[];
        switch ((EventType)eventCode)
        {
            case EventType.PlayerHitCheckpoint:
                // Handle checkpoint hit event
                // 1. Get the player name, checkpoint index, and timestamp from the event data
                var checkpointIndex = (int)data[1];
                var timestamp = (double)data[2];
                // 2. Get the player's data from the DataManager
                var playerData = DataManager.Instance.GetPlayerData(senderId);
                if (playerData == null)
                    return;
                // 3. Update the player's checkpoint index and laps elapsed (if applicable) in the DataManager
                // Check that the checkpoint index is 1 greater than the previous checkpoint index to avoid cheating
                if(playerData.previousCheckpointIndex + 1 == checkpointIndex)
                    playerData.previousCheckpointIndex = checkpointIndex;
                // If the checkpoint index is 0, it means the player has crossed the finish line and completed a lap
                // Check if the player has completed the required number of checkpoints to complete a lap 
                if (checkpointIndex == 0)
                {
                    if(playerData.previousCheckpointIndex == checkpointsCount)
                        playerData.lapsElapsed++;
                    //if(playerData)
                }
                // 4. Optional: Notify the UI to update the player's progress in the race
                //PhotonNetwork.RaiseEvent((byte)itemEvent.eventType, )
                break;
            case EventType.ItemCollected:
                // Handle item collection event
                break;
            case EventType.StartRace:
                // Handle race start event
                Debug.Log($"RaceManager: Received StartRace event. Race will start at {data} (Now {PhotonNetwork.Time})");
                if(data is { Length: > 0 })
                    StartCoroutine(nameof(RaceStartSequence), data[0]);
                break;
        }
        //var eventType = itemEvent.eventType;
        //var playerId = itemEvent.playerID;
        //var hitItemID = itemEvent.hitItemID;
        //var eventTime = itemEvent.timestamp;
    }

    public void OnPhotonMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"RaceManager: Master client switched to {newMasterClient.NickName} (ID: {newMasterClient.UserId})");
        // Handle master client switch if needed
        if(!PhotonNetwork.IsMasterClient)
            return;
        // Take over any responsibilities that the previous master client had, such as managing race state, checkpoints, etc.
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            // Send data to other players
            // Send race start time to other
            // Send finish line crossing events to other players
            // Push item pickup effects to the player who picked up the item first
        }
        else
        {
            // Receive data from other players
            // Receive other players' finish line crossing events and update race state accordingly
            // Receive other players' events regarding item pickups and usage, and update race state accordingly
        }
    }
    #endregion

    public void SetLocalPlayer(CarController player)
    {
        localPlayer = player;
    }
    public void SetGamePaused(bool paused)
    {
        if(paused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
    
    public void PlayerHitCheckpoint(string playerName, int checkpointIndex)
    {
        // Only the local player should send the event to avoid duplicate events being sent by all clients
        if(playerName != PhotonNetwork.NickName)
            return;
        // Send an event to the master-client that a player has hit a checkpoint
        var eventTimestamp = PhotonNetwork.Time;
        var raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        var eventData = new object[] {checkpointIndex, eventTimestamp };
        PhotonNetwork.RaiseEvent((byte)EventType.PlayerHitCheckpoint, eventData, raiseEventOptions, SendOptions.SendReliable);
    }
    
    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
        Debug.Log("RaceManager added as callback target for Photon events.");
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        Debug.Log("RaceManager removed as callback target for Photon events.");
    }
    // Input system callbacks
    public void OnQuit()
    {
        var isPaused = Time.timeScale > 0.5f;
        // Pause the game and show the quit confirmation panel
        RaceUIManager.Instance.ShowQuitVerifyPanel(isPaused);
        InputHandler.Instance.SetInputEnabled(!isPaused);
    }
}
