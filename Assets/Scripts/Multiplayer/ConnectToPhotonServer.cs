using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
public class ConnectToPhotonServer : MonoBehaviourPunCallbacks
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Connecting to Photon server...");
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon server.");
        PhotonNetwork.JoinLobby();
    }
    
    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Photon lobby.");
        // Open Lobby scene
        SceneManager.LoadScene("PhotonLobby");
        
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Photon room.");
        // Load the multiplayer scene
        SceneManager.LoadScene("Game");
    }
}
