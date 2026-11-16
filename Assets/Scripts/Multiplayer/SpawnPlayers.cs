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
        var spawnPosition = spawnPoint.position;
        var spawnRotation = spawnPoint.rotation.eulerAngles;
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.Euler(0f, spawnRotation.y, 0f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
