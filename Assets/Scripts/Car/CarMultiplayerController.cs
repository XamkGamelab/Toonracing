using System;
using Unity.Netcode;
using UnityEngine;

namespace Car
{
    public class CarMultiplayerController : NetworkBehaviour
    {
        // General adjustments
        [SerializeField] private float interpolationSpeed = 10f;
        //General variables
        private Rigidbody rb;
        private CarController carController;

        // Network parameters
        public NetworkVariable<Vector3> netPosition = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);
        public NetworkVariable<Quaternion> netRotation = new NetworkVariable<Quaternion>(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);
        public NetworkVariable<Vector3> netVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);
        public NetworkVariable<Vector3> netAngularVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if(rb == null)
                    Debug.LogError("Rigidbody not found on the car object.");
            carController = GetComponent<CarController>();
            if(carController == null)
                    Debug.LogError("CarController not found on the car object.");
        }

        public override void OnNetworkSpawn()
        {
            gameObject.name = $"Car_{OwnerClientId}";
            if (!IsOwner)
            {
                carController.SetRemoteControlled(true);
                Debug.Log("CarMultiplayerController spawned as remote car");
            }
            else
            {
                carController.SetRemoteControlled(false);
                Debug.Log("CarMultiplayerController spawned on client");
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                SyncNetworkState();
            }
            else
            {
                InterpolateMovement();
            }
        }

        // Network synchronization
        private void SyncNetworkState()
        {
            netPosition.Value = transform.position;
            netRotation.Value = transform.rotation;
            netVelocity.Value = rb.linearVelocity;
            netAngularVelocity.Value = rb.angularVelocity;
        }

        private void InterpolateMovement()
        {
            var targetPosition = netPosition.Value + (netVelocity.Value * Time.deltaTime);
            
            // Interpolate position and rotation for smooth movement
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, netRotation.Value, Time.deltaTime * interpolationSpeed);

        }
    }
}
