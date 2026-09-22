using Unity.Netcode;
using UnityEngine;
namespace Game_logic
{
    public class NetworkCarSpawner : NetworkBehaviour
    {
        // List of available cars to spawn in the game. This list is populated from the ScriptableObject CarDataSO
        private Scriptables.CarSettings[] availableCars;
        
        private NetworkManager m_NetworkManager;
        
        private void Awake()
        {
            m_NetworkManager = GetComponent<NetworkManager>();
            LoadAvailableCars();
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void LoadAvailableCars()
        {
            availableCars = Resources.LoadAll<Scriptables.CarSettings>("Cars");
        }

    }
}
