using Items;
using UnityEngine;
using Photon.Pun;
using EventType = Items.EventType;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Photon.Pun.PhotonView))]
public class FinishLine : MonoBehaviour
{
    private PhotonView photonView;
    private int id;
    private void Start()
    {
        photonView = GetComponent<PhotonView>();
        id = photonView.ViewID;
    }
    // Only job for the finish line is to detect when a player crosses it and 
    // send a event
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) 
            return;
        // Get the PhotonView component of the player
        var otherPhotonView = other.GetComponent<PhotonView>();
        // React only for the local player to avoid duplicate events being sent by all clients
        if (otherPhotonView == null || !otherPhotonView.IsMine)
            return;
        // Get the current synced time from the Photon Network
        var currentTime = PhotonNetwork.Time;
        // Create a new ItemEvent with the finish line crossing information
        //var finishLineEvent = new ItemEvent(EventType.FinishLineCrossed, otherPhotonView.ViewID, currentTime);
        // Send the event
        //RaceManager.Instance.OnEvent(finishLineEvent);
    }
}
