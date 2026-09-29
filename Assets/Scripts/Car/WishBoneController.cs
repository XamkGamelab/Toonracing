using UnityEngine;

namespace Car
{
    [ExecuteInEditMode] // So that the script runs in edit mode as well
    public class WishBoneController : MonoBehaviour
    {
        [SerializeField] private Transform innerJoint; // Wheel end
        [SerializeField] private Transform outerJoint; // Chassis end

        [Header("Car's Up Reference")]
        [SerializeField] private Transform carUpReference; // Esim. Auton runko/Chassis
        
        [SerializeField] private Vector3 meshRotationOffset = new Vector3(0, 90, 0);

        private void Start()
        {
            if(innerJoint == null || outerJoint == null)
                Debug.LogError("WishBoneController: One or more required transforms are not assigned.");
            transform.position = innerJoint.position; // Align the controller's position with the wishbone pivot
        }

        private void LateUpdate()
        {
            if(!innerJoint || !outerJoint)
                return;
            transform.position = outerJoint.position; // Align the controller's position with the wishbone pivot
            // Turn the wishbone pivot to face the ball joint target, using the car's up reference for orientation
            var direction = transform.position - innerJoint.position;
            if (direction == Vector3.zero) 
                return;
            var upVector = carUpReference ? carUpReference.up : transform.up;
            transform.rotation = Quaternion.LookRotation(direction, upVector) * Quaternion.Euler(meshRotationOffset);
        }
    }
}
