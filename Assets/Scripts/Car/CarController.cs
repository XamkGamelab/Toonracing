using System;
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
        
        public enum  DriveType
        {
            Fwd,
            Rwd,
            Awd
        }

        private enum GearSelect
        {
            Drive,
            Reverse,
            Park
        }
        [Header("Car default settings")]
        private DriveType driveType;
        [SerializeField] private float acceleration = 500f;
        [SerializeField] private float brakingForce = 1000f;
        [SerializeField] private float maxTurningAngle = 35f;
        [SerializeField] private float steerSpeed = 5f;
        [SerializeField] private float steerReleaseSpeed = 5f;
        [SerializeField] private float coastingDrag = 1f;
        [SerializeField, Range(0f, 1f)] private float brakeBalance = 0.5f; // 1 = front brakes only, -1 = rear brakes only
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float topSpeed;
    
        [Header("Debug")]
        [SerializeField] private float currentMotorTorque;
        [SerializeField] private float currentBrakingForce;
        [SerializeField] private bool acceleratePressed;
        [SerializeField] private bool brakePressed;
        [SerializeField] private float currentSpeed;
        [SerializeField] private bool speedLimitReached;
        //Turning
        [SerializeField] private float currentTurningRequest = 0f;
        [SerializeField] private float currentTurningAngle = 0f;
        
        
        //General variables
        private Rigidbody rb;
        [SerializeField] private GearSelect gearSelect;
        [SerializeField] private bool handbrake;
        private float prevSteerReqAngle;
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            if(rb == null)
                    Debug.LogError("Rigidbody not found on the car object.");
            Debug.Log("CarController initialized");
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        private void FixedUpdate()
        {
            SpeedHandler();
            BrakeHandler();
            SteerHandler();

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
    
        private void SpeedHandler()
        {
            // This method can be used to handle speed-related logic, such as limiting top speed or applying drag.
            // Accelerations
            var torque = currentMotorTorque;
            // Check speed and limit torque if necessary (e.g., for top speed)
            currentSpeed = rb.linearVelocity.magnitude;
            speedLimitReached = false;
            if (currentSpeed >= topSpeed)
            {
                torque = 0f; // Reduce accelerating if at or above top speed
                speedLimitReached = true;
            }
            
            switch (driveType)
            {
                case DriveType.Awd:
                    RLCollider.motorTorque = torque;
                    RRCollider.motorTorque = torque;
                    FLCollider.motorTorque = torque;
                    FRCollider.motorTorque = torque;
                    break;
                case DriveType.Rwd:
                    RLCollider.motorTorque = torque;
                    RRCollider.motorTorque = torque;
                    FLCollider.motorTorque = 0;
                    FRCollider.motorTorque = 0;
                    break;
                case DriveType.Fwd:
                    RLCollider.motorTorque = 0;
                    RRCollider.motorTorque = 0;
                    FLCollider.motorTorque = torque;
                    FRCollider.motorTorque = torque;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void BrakeHandler()
        {
            // This method can be used to handle braking logic, such as applying brake force based on input and gear selection.
            //Braking
            var frontBrakeForce = currentBrakingForce * brakeBalance;
            var rearBrakeForce = currentBrakingForce * (1f - brakeBalance);
            FLCollider.brakeTorque = frontBrakeForce;
            FRCollider.brakeTorque = frontBrakeForce;
            RLCollider.brakeTorque = rearBrakeForce;
            RRCollider.brakeTorque = rearBrakeForce;
            if (handbrake)
            {
                RLCollider.brakeTorque = brakingForce;
                RRCollider.brakeTorque = brakingForce;
            }
        }
        
        private void SteerHandler()
        {
            // This method can be used to handle steering logic, such as adjusting the steering angle based on input and speed.
            //Steering
            currentTurningAngle = FLCollider.steerAngle;
            // Is the steering centering or turning in the opposite direction of the current request?
            var isFastSteer = !Mathf.Approximately(Mathf.Sign(currentTurningRequest), Mathf.Sign(currentTurningAngle)) || Mathf.Abs(currentTurningRequest) < Mathf.Abs(currentTurningAngle);
            var turningSpeed = isFastSteer ? steerReleaseSpeed : steerSpeed;
            var turningAngle = Mathf.MoveTowards(currentTurningAngle, currentTurningRequest, turningSpeed * Time.fixedDeltaTime);
            FLCollider.steerAngle = turningAngle;
            FRCollider.steerAngle = turningAngle;
            
        }
        
        // Public methods
        public void SetCarSettings(CarSettings carSettings)
        {
            driveType = carSettings.driveType;
            acceleration = carSettings.baseSettings.baseAccelerationForce + carSettings.baseSettings.accelerationMultiplier * (carSettings.accelerationStat - 1);
            brakingForce = carSettings.baseSettings.brakeForce;
            maxTurningAngle = carSettings.baseSettings.maxSteeringAngle;
            // Get how fast the car can steer based on the car's handling stat and the base settings
            var steerTime = carSettings.baseSettings.baseSteerSpeed - carSettings.baseSettings.steerSpeedMultiplier * (carSettings.handlingStat-1);
            var degPerSecond = (maxTurningAngle * 2f) / steerTime;
            steerSpeed = degPerSecond;
            var steerReleaseTime = carSettings.baseSettings.baseSteerReleaseSpeed;
            steerReleaseSpeed = (maxTurningAngle * 2f) / steerReleaseTime;
            jumpForce = carSettings.baseSettings.jumpForce;
            topSpeed = carSettings.baseSettings.baseMaxSpeed + carSettings.baseSettings.accelerationMultiplier * (carSettings.topSpeedStat-1);
        }
        
        // Input system callbacks

        #region Input System Callbacks
        public void OnAccelerate(InputValue value)
        {
            Debug.Log($"Accelerating: {value.isPressed}");
            brakePressed = value.isPressed;
            currentBrakingForce = 0f; // Reset braking force when accelerating
            if(value.isPressed)
            {
                // If the car is stopped, switch to drive gear when accelerate is pressed
                if(RRCollider.rpm > -0.1f)
                    gearSelect = GearSelect.Drive;
                switch (gearSelect)
                {
                    case GearSelect.Drive:
                        currentMotorTorque = acceleration;
                        break;
                    case GearSelect.Reverse:
                        currentBrakingForce = brakingForce;
                        break;
                    case GearSelect.Park:
                        currentMotorTorque = 0f;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                switch (gearSelect)
                {
                    case GearSelect.Drive:
                        currentMotorTorque = 0f;
                        break;
                    case GearSelect.Reverse:
                        currentBrakingForce = brakingForce;
                        break;
                    case GearSelect.Park:
                        currentMotorTorque = 0f;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    
        public void OnBrake(InputValue value)
        {
            Debug.Log($"Braking: {value.isPressed}");
            acceleratePressed = value.isPressed;
            if (value.isPressed)
            {
                // If the car is stopped, switch to reverse gear when brake is pressed
                if(RRCollider.rpm < 0.1f)
                    gearSelect = GearSelect.Reverse;
                switch (gearSelect)
                {
                    case GearSelect.Drive:
                        currentBrakingForce = brakingForce;
                        break;
                    case GearSelect.Reverse:
                        currentMotorTorque = -acceleration;
                        break;
                    case GearSelect.Park:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                currentBrakingForce = 0f;
                switch (gearSelect)
                {
                    case GearSelect.Drive:
                        currentBrakingForce = 0f;
                        break;
                    case GearSelect.Reverse:
                        currentMotorTorque = 0f;
                        currentBrakingForce = 0f;
                        break;
                    case GearSelect.Park:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void OnSteer(InputValue steerInput)
        {
            currentTurningRequest = maxTurningAngle * steerInput.Get<float>();
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

        public void OnHandbrake(InputValue value)
        {
            handbrake = value.isPressed;
        }
        // Private methods
        private bool isOnGround()
        {
            bool isGrounded;
            isGrounded = RLCollider.isGrounded;
            isGrounded = isGrounded || RRCollider.isGrounded;
            isGrounded = isGrounded || FLCollider.isGrounded;
            isGrounded = isGrounded || FRCollider.isGrounded;
            return isGrounded;
        }
        #endregion

    }
}

