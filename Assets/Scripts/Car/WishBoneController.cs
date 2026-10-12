using UnityEngine;

namespace Car
{
    [ExecuteInEditMode] // So that the script runs in edit mode as well
    public class WishBoneController : MonoBehaviour
    {
        [SerializeField] private Transform innerJoint; // Wheel end
        [SerializeField] private bool invertLookAt = false; // If true, the controller will look away from the inner joint instead of towards it.

        [Header("Car's Up Reference")]
        [SerializeField] private Transform carUpReference; // Esim. Auton runko/Chassis

        private void Start()
        {
            if (innerJoint != null) 
                return;
            Debug.LogError("WishBoneController: One or more required transforms are not assigned.");
        }

        private void LateUpdate()
        {
            if (!innerJoint)
                return;

            // Point from the chassis pivot toward the wheel-side ball joint.
            var direction = innerJoint.position - transform.position;
            if (direction == Vector3.zero) 
                return;

            var upVector = carUpReference ? carUpReference.up : transform.up;
            transform.LookAt(innerJoint, Vector3.right); // Orient the controller to face the wheel-side ball joint
            // If invertLookAt is true, rotate the controller 180 degrees around its up axis to look away from the inner joint.
            if (invertLookAt)
            {
                transform.Rotate(0, 180, 0, Space.Self);
            }
        }
    }
}
