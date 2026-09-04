using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CarController : MonoBehaviour
    {
        
        // Wheel physics
        [SerializeField] WheelCollider RLCollider;
        [SerializeField] WheelCollider RRCollider;
        [SerializeField] WheelCollider FLCollider;
        [SerializeField] WheelCollider FRCollider;
        // Wheel directions
        [SerializeField] Transform FRTransform;
        [SerializeField] Transform FLTransform;
        [SerializeField] Transform RRTransform;
        [SerializeField] Transform RLTransform;
        
        private enum  DriveType
        {
            Fwd,
            Rwd,
            Awd
        }
        [Header("Car settings")]
        [SerializeField] DriveType driveType;
        [SerializeField] private float acceleration = 500f;
        [SerializeField] private float brakingForce = 1000f;
        [SerializeField] private float maxTurningAngle = 35f;
        [SerializeField] private float steerSpeed = 5f;
        [SerializeField] private float steerReleaseSpeed = 5f;
        [SerializeField] private float coastingDrag = 1f;
        [SerializeField, Range(0f, 1f)] private float brakeBalance = 0.5f; // 1 = front brakes only, -1 = rear brakes only
        [SerializeField] private float jumpForce = 5f;
    
        [Header("Debug")]
        [SerializeField] private float currentAcceleration = 0f;
        [SerializeField] private float currentBrakingForce = 0f;
        //Turning
        public float currentTurningAngle = 0f;
        
        //General variables
        private Rigidbody rb;
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            if(rb == null)
                    Debug.LogError("Rigidbody not found on the car object.");
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        private void FixedUpdate()
        {
            // Accelerations
            if (driveType is DriveType.Awd)
            {
                RLCollider.motorTorque = currentAcceleration;
                RRCollider.motorTorque = currentAcceleration;
                FLCollider.motorTorque = currentAcceleration;
                FRCollider.motorTorque = currentAcceleration;
            }
            else if (driveType is DriveType.Rwd)
            {
                RLCollider.motorTorque = currentAcceleration;
                RRCollider.motorTorque = currentAcceleration;
                FLCollider.motorTorque = 0;
                FRCollider.motorTorque = 0;
            }
            else if (driveType is DriveType.Fwd)
            {          
                RLCollider.motorTorque = 0;
                RRCollider.motorTorque = 0;
                FLCollider.motorTorque = currentAcceleration;
                FRCollider.motorTorque = currentAcceleration;
            }

            //Braking
            var frontBrakeForce = currentBrakingForce * brakeBalance;
            var rearBrakeForce = currentBrakingForce * (1f - brakeBalance);
            FLCollider.brakeTorque = frontBrakeForce;
            FRCollider.brakeTorque = frontBrakeForce;
            RLCollider.brakeTorque = rearBrakeForce;
            RRCollider.brakeTorque = rearBrakeForce;

            //Steering
            FLCollider.steerAngle = currentTurningAngle;
            FRCollider.steerAngle = currentTurningAngle;

            //Wheel rotation
            wheelController(FLCollider, FLTransform);
            wheelController(FRCollider, FRTransform);
            wheelController(RLCollider, RLTransform);
            wheelController(RRCollider, RRTransform);
        }

        void wheelController(WheelCollider wheelCollider, Transform transform)
        {
            Vector3 pos;
            Quaternion rot;
            wheelCollider.GetWorldPose(out pos, out rot);
            transform.position = pos;
            transform.rotation = rot;
        }
    
        public void OnAccelerate(InputValue value)
        {
            Debug.Log($"Accelerating: {value.isPressed}");
            currentBrakingForce = 0f; // Reset braking force when accelerating
            if(value.isPressed && RRCollider.rpm < -0.1f)
            {
                // If the car is moving backward and the accelerate button is pressed, apply brakes instead of acceleration
                currentBrakingForce = brakingForce;
                currentAcceleration = 0f;
            }
            else
                currentAcceleration = value.isPressed ? acceleration : 0f;
        }
    
        public void OnBrake(InputValue value)
        {
            Debug.Log($"Braking: {value.isPressed}");
            if (value.isPressed && RRCollider.rpm < 0.1f)
            {
                // Initiate reverse if the car is stopped and the brake is pressed
                currentAcceleration = -acceleration;
                currentBrakingForce = 0f;
            }
            else
                currentBrakingForce = value.isPressed ? brakingForce : 0f;
        }

        public void OnSteer(InputValue steerInput)
        {
            currentTurningAngle = maxTurningAngle * steerInput.Get<float>();
            FLCollider.steerAngle = currentTurningAngle;
            FRCollider.steerAngle = currentTurningAngle;
        }

        public void OnJump(InputValue value)
        {
            if (!value.isPressed)
                return;
            if (isOnGround())
            {
                Debug.Log("Jumping");
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        private bool isOnGround()
        {
            bool isGrounded;
            isGrounded = RLCollider.isGrounded;
            isGrounded = isGrounded || RRCollider.isGrounded;
            isGrounded = isGrounded || FLCollider.isGrounded;
            isGrounded = isGrounded || FRCollider.isGrounded;
            return isGrounded;
        }
    }
}

