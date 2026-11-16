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
        var cameraController = FindAnyObjectByType<CameraController>();
        if(cameraController)
            cameraController.SetLocalPlayer(spawnedPlayer.transform);
    }
}
