using System;
using Items;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using EventType = Items.EventType;

public class RaceManager : MonoBehaviour, IPunObservable
{
    // Make this a singleton so that it can be easily accessed from other scripts
    public static RaceManager Instance { get; private set; }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnEvent(ItemEvent itemEvent)
    {
        switch (itemEvent.eventType)
        {
            case EventType.FinishLineCrossed:
                
                // Handle finish line crossing event
                var raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                //PhotonNetwork.RaiseEvent((byte)itemEvent.eventType, )
                break;
            case EventType.ItemCollected:
                // Handle item collection event
                break;
        }
        var eventType = itemEvent.eventType;
        var playerId = itemEvent.playerID;
        var hitItemID = itemEvent.hitItemID;
        var eventTime = itemEvent.timestamp;
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
}
