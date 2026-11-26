using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

namespace Dev
{
    public class MultiplayerConnectionStats : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI rttTextField;
        [SerializeField] private TMPro.TextMeshProUGUI jitterTextField;
        [SerializeField] private TMPro.TextMeshProUGUI bandwidthTextField;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
        
        }

        // Update is called once per frame
        private void Update()
        {
            rttTextField?.SetText($"RTT:     {PhotonNetwork.GetPing()} ms");
        }
    }
}
