using System.Collections;
using System.Linq;
using Car;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private NetworkManager m_NetworkManager;
    private Transform[] startPositions;
    private int playerCount;
    [Header("Debugging")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private float spawnHeightOffset = .5f; // Offset to spawn the car above the ground
    [SerializeField] private Scriptables.CarSettings carSettings;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        m_NetworkManager = NetworkManager.Singleton.GetComponent<NetworkManager>();
    }

    private void Start()
    {
        var startingPoints = GameObject.FindGameObjectsWithTag("StartPoint");
        // Sort the starting points by hierarchy order to ensure consistent assignment
        var sortedStartingPoints = startingPoints.OrderBy(obj => obj.transform.GetSiblingIndex()).ToArray();
        startPositions = new Transform[sortedStartingPoints.Length];
        for (var i = 0; i < sortedStartingPoints.Length; i++)
        {
            startPositions[i] = sortedStartingPoints[i].transform;
        }
        if(!carPrefab)
            Debug.LogError("Default car prefab not assigned in GameManager");
    }
    public override void OnNetworkSpawn()
    {
        if (m_NetworkManager.IsServer)
        {
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private IEnumerator UpdatePositions()
    {
        while (true)
        {
            if (m_NetworkManager.IsServer && !m_NetworkManager.IsClient)
            {
               // foreach (ulong uid in m_NetworkManager.ConnectedClientsIds)
                //    m_NetworkManager.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<CarMultiplayerController>().Move();
            }
            else
            {
                var playerObject = m_NetworkManager.SpawnManager.GetLocalPlayerObject();
                //var player = playerObject.GetComponent<HelloWorldPlayer>();
                //player.Move();
            }
            yield return null;
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
        // Instantiate the car prefab at the next available starting position for the connected client
        var playerRotation = startPositions[playerCount].rotation;
        var playerPosition = startPositions[playerCount].position + Vector3.up * spawnHeightOffset;
        var spawnedCar = Instantiate(carPrefab, playerPosition, playerRotation);
        if(carSettings)
            spawnedCar.GetComponent<CarController>().SetCarSettings(carSettings);
        spawnedCar.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        spawnedCar.name = $"Car_{clientId}";
        playerCount++;
        Debug.Log($"CarMultiplayerController set to remote controlled for client {clientId}");
    }
}
