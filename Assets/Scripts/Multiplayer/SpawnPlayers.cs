using Car;
using UnityEngine;
using Photon.Pun;
public class SpawnPlayers : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var spawnPoint = spawnPoints[PhotonNetwork.LocalPlayer.ActorNumber-1 % spawnPoints.Length];
        //var spawnPoint = spawnPoints[spawnPoints.Length - 1];
        var spawnPosition = spawnPoint.position;
        var spawnRotation = spawnPoint.rotation.eulerAngles;
        var spawnedPlayer = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.Euler(0f, spawnRotation.y, 0f));
        spawnedPlayer.tag = "Player";
        // Set the player's userID to the PhotonNetwork.LocalPlayer.UserId. Used when crossing checkpoints and hitting objects
        spawnedPlayer.gameObject.name = PhotonNetwork.LocalPlayer.UserId;
        var playerController = spawnedPlayer.GetComponent<CarController>();
        if (playerController)
        {
            // Disable controls for the local player until the race starts
            InputHandler.Instance.SetLocalPlayer(playerController);
            InputHandler.Instance.SetInputEnabled(false);
            RaceManager.Instance.SetLocalPlayer(playerController);
        }
        else
        {
            Debug.LogError("SpawnPlayers: No CarController found on the spawned player prefab.");
        }
        var cameraController = FindAnyObjectByType<CameraController>();
        if(cameraController)
            cameraController.SetLocalPlayer(spawnedPlayer.transform);
    }
}
