using System;
using System.Collections;
using System.Linq;
using Scriptables;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CarMultiplayerController : NetworkBehaviour
    {
        
        // Wheel physics
        [Header("Wheel Colliders")]
        [SerializeField] WheelCollider RLCollider;
        [SerializeField] WheelCollider RRCollider;
        [SerializeField] WheelCollider FLCollider;
        [SerializeField] WheelCollider FRCollider;
        // Wheel directions
        [Header("Wheel Transforms")]
        [SerializeField] Transform FRTransform;
        [SerializeField] Transform FLTransform;
        [SerializeField] Transform RRTransform;
        [SerializeField] Transform RLTransform;
        
        // Gameobject which holds the car model, used for power slide visualization
        [Header("Car transforms")]
        [SerializeField] Transform carTransform;
        
        
        /*public enum  DriveType
        {
            Fwd,
            Rwd,
            Awd
        }*/

        private enum GearSelect
        {
            Drive,
            Reverse,
            Park
        }
        public enum CarState
        {
            Driving,
            Jumping,
            Sliding,
            FlippedOver
        }
        [Header("Car default settings")]
        private Scriptables.CarSettings.DriveType driveType;
        [SerializeField] private float acceleration = 500f;
        [SerializeField] private float brakingForce = 1000f;
        [SerializeField] private float maxTurningAngle = 35f;
        [SerializeField] private float steerSpeed = 5f;
        [SerializeField] private float steerReleaseSpeed = 5f;
        [SerializeField] private float coastingDrag = 1f;
        [SerializeField, Range(0f, 1f)] private float brakeBalance = 0.5f; // 1 = front brakes only, -1 = rear brakes only
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float topSpeed;
        [SerializeField] private float maxSwayAngle = 30f;
        [SerializeField] private float angleCorrectionForce = 10f;
        [SerializeField] private float carFlipTime = 2f;
        [SerializeField] private float carFlipLimitAngle = 80f;
        [SerializeField] private float jumpTimeout = 2f;
        [SerializeField] private float slideVirtualRotation = 30f;
        [SerializeField] private float slideTransitionTime = 2;
    
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
        [SerializeField] private bool correctingAngle = false;
        [SerializeField] private float angle;
        
        
        //General variables
        private Rigidbody rb;
        [SerializeField] private GearSelect gearSelect;
        [SerializeField] private bool handbrake;
        private float prevSteerReqAngle;
        public CarState carState;
        public float steerDirection = 0; // -1 = left, 1 = right, 0 = straight
        public bool slideServiceRunning = false;
        private bool isFlippingBack = false;
        private bool isFlipFinished = false;
        private bool isJumpPressed = false;
        private float groundCheckDelay;
        private float jumpEndTimeout;
        
        // Multiplayer variables
        private NetworkVariable<FixedString64Bytes> selectedCarType = new NetworkVariable<FixedString64Bytes>();
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            if(rb == null)
                    Debug.LogError("Rigidbody not found on the car object.");
            if(carTransform == null)
                Debug.LogError("Car transform not assigned. Should be the Gameobject that holds the car model.");
            Debug.Log("CarController initialized");
            
        }

        // Update is called once per frame
        private void Update()
        {
            angle = rb.rotation.eulerAngles.z;
            if(groundCheckDelay > 0f)
                groundCheckDelay -= Time.deltaTime;
            switch (carState)
            {
                case CarState.Driving:
                    // Check if the car is flipped over, if it is, switch to the flipped over state
                    if (IsFlippedOver())
                        carState = CarState.FlippedOver;
                    break;
                case CarState.Jumping:
                    if(Mathf.Abs(steerDirection) >= 0.5)    // If steering while in the air, apply torque to the car to rotate it
                    {
                        rb.AddTorque(Vector3.up * (steerDirection * 10f), ForceMode.Force);
                        // Make sure the car is not flipping over while in the air, if it is, apply torque to correct it
                        correctingAngle = false;
                        if (Mathf.Abs(Vector3.Angle(transform.up, Vector3.up)) > maxSwayAngle)
                        {
                            // Count which direction the car is leaning and apply torque in the opposite direction to correct it
                            var rotationAxis = Vector3.Cross(transform.up, Vector3.up);
                            rb.AddTorque(rotationAxis * (angleCorrectionForce * Time.deltaTime), ForceMode.VelocityChange);
                        }
                    }
                    if (groundCheckDelay <= 0f && isOnGround())
                    {
                        carState = CarState.Driving;
                        // Check if user is trying to start a power slide, if they are, switch to the sliding state
                        if(Mathf.Abs(steerDirection) >= 0.5f && currentSpeed > topSpeed * 0.25f && isJumpPressed)
                            carState = CarState.Sliding;
                    }

                    if (jumpEndTimeout > 0f)
                        jumpEndTimeout -= Time.deltaTime;
                    else
                        carState = CarState.Driving;
                    break;
                case CarState.Sliding:
                    // Rotate the car model to simulate a power slide, but keep the car's physics collider aligned with the car's forward direction
                    var rotationOffset = Mathf.Lerp(0f, 30f * steerDirection, Time.deltaTime * slideTransitionTime);
                    carTransform.localEulerAngles = new Vector3(0, rotationOffset, 0);
                    // If user releases the jump button, end the slide and return to driving state
                    if(!isJumpPressed)
                    {
                        carState = CarState.Driving;
                    }
                    if(!slideServiceRunning)
                        StartCoroutine(nameof(SlideVisualsRoutine));
                    // If there is speed boost achieved, add a small amount of extra torque and increase the top speed for a short time to simulate a speed boost from the slide
                    break;
                case CarState.FlippedOver:
                    // If the car has finished flipping, return to the driving state
                    if(isFlipFinished)
                    {
                        carState = CarState.Driving;
                        isFlipFinished = false;
                        break;
                    }
                    // If the car is flipped over, start the flip coroutine to flip it back over
                    if(!isFlippingBack)
                    {
                        isFlippingBack = true;
                        StartCoroutine(nameof(CarFlipRoutine));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void FixedUpdate()
        {
            SpeedHandler();
            BrakeHandler();
            SteerHandler();

            //Wheel rotation
            WheelController(FLCollider, FLTransform, true);
            WheelController(FRCollider, FRTransform, true);
            WheelController(RLCollider, RLTransform);
            WheelController(RRCollider, RRTransform);
        }

        private bool IsFlippedOver()
        {
            return Vector3.Angle(transform.up, Vector3.up) > carFlipLimitAngle;
        }

        private IEnumerator CarFlipRoutine()
        {
            // Stop the car from moving when flipped over
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Make the car kinematic to prevent physics from interfering with the flip
            // Get start position and rotation of the car
            var carStartPosition = transform.position;
            var carStartRotation = transform.rotation;
            // Lift the car up a bit to avoid getting stuck in the ground
            var carFlipEndPosition = transform.position + Vector3.up * 0.1f; 
            // Keep car heading the same, but flip it over
            var carEndRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            var elapsedTime = 0f; // How much time has passed since the flip started
            Debug.Log("Starting to flip the car over");
            while (elapsedTime < carFlipTime)
            {
                elapsedTime += Time.deltaTime;
                var t = elapsedTime / carFlipTime;
                Debug.Log($"Flipping car over: {t * 100f}%");
                transform.position = Vector3.Lerp(carStartPosition, carFlipEndPosition, t);
                transform.rotation = Quaternion.Lerp(carStartRotation, carEndRotation, t);
                yield return null;
            }
            isFlippingBack = false;
            isFlipFinished = true;
            rb.isKinematic = false; // Make the car non-kinematic again to allow physics to take over
        }

        private IEnumerator SlideVisualsRoutine()
        {
            // This coroutine can be used to handle the visual effects of sliding, such as changing the car's model orientation or adding particle effects.
            // For now, it just waits for a short duration to simulate the slide effect.
            var requestedRotation = slideVirtualRotation * steerDirection;
            var startRotation = 0f;
            var startJumpButtonState = isJumpPressed;
            slideServiceRunning = true;
            var elapsedTime = 0f;
            var t = 0f;
            while (true)
            {
                elapsedTime += Time.deltaTime;
                // If the user releases the jump button, reset the requested rotation to 0 to return the car model to its original orientation
                // Latching so it is run only once
                if (isJumpPressed != startJumpButtonState)
                {
                    startRotation = requestedRotation;
                    requestedRotation = 0f;
                    startJumpButtonState = isJumpPressed;
                    elapsedTime = elapsedTime >= slideTransitionTime ? 0f : (1f-t) * slideTransitionTime;
                }
                t = elapsedTime / slideTransitionTime;
                // Rotate the car model to simulate a power slide
                carTransform.localEulerAngles = new Vector3(0, Mathf.Lerp(startRotation, requestedRotation, t), 0);
                // If the user releases the jump button and the car model is back to its original orientation, end the slide and 
                // the coroutine
                if(!isJumpPressed && Mathf.Approximately(carTransform.localEulerAngles.y, 0f))
                    break;
                yield return null;
            }
            carTransform.localEulerAngles = Vector3.zero; // Reset car model orientation after slide
            slideServiceRunning = false;
        }
        
        private void WheelController(WheelCollider wheelCollider, Transform wheelTransform, bool isSteeringWheel = false)
        {
            Vector3 pos;
            Quaternion rot;
            wheelCollider.GetWorldPose(out pos, out rot);
            
            // Adjust only the wheel's world Y position. Updating position directly while the
            // model is rotated changes the child's local X/Z position as a side effect.
            var wheelPosition = wheelTransform.position;
            wheelTransform.localPosition += Vector3.up * (pos.y - wheelPosition.y);
            
            // Wheel rotation
            var localColliderRotation = Quaternion.Inverse(wheelTransform.parent.rotation) * rot;
            // Get wheel pitch angle
            var wheelPitchAngle = localColliderRotation.eulerAngles.x;
            
            var yaw = isSteeringWheel ? wheelCollider.steerAngle : 0f;
            
            wheelTransform.localRotation = Quaternion.Euler(wheelPitchAngle, yaw, 0f);
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
                case Scriptables.CarSettings.DriveType.Awd:
                    RLCollider.motorTorque = torque;
                    RRCollider.motorTorque = torque;
                    FLCollider.motorTorque = torque;
                    FRCollider.motorTorque = torque;
                    break;
                case Scriptables.CarSettings.DriveType.Rwd:
                    RLCollider.motorTorque = torque;
                    RRCollider.motorTorque = torque;
                    FLCollider.motorTorque = 0;
                    FRCollider.motorTorque = 0;
                    break;
                case Scriptables.CarSettings.DriveType.Fwd:
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
        public void SetCarSettings(Scriptables.CarSettings carSettings)
        {
            driveType = driveType;
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
        
        // Multiplayer specific methods
        [ServerRpc]
        private void SetCarServerRpc(string carType)
        {
            selectedCarType.Value = carType;
        }

        private void OnCarDataChanged(string carType)
        {
            // Initialize the cars array
            var cars = Resources.LoadAll<CarSettings>("Cars");
            var newCarSettings = cars.FirstOrDefault(car => car.carName == carType);
            // Get the correct scriptable object based on the car type and set the car settings
            if(newCarSettings == null)
            {
                Debug.LogError($"Car with name {carType} not found in Resources/Cars");
                return;
            }
            SetCarSettings(newCarSettings);
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
            steerDirection = steerInput.Get<float>();
            currentTurningRequest = maxTurningAngle * steerDirection;
        }

        public void OnJump(InputValue value)
        {
            isJumpPressed = false;
            if (!value.isPressed)
                return;
            if (!isOnGround()) 
                return;
            isJumpPressed = true;
            Debug.Log("Jumping");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            groundCheckDelay = 0.1f; // Delay ground check for a short time to avoid immediately detecting the ground after jumping
            carState = CarState.Jumping;
            jumpEndTimeout = jumpTimeout; // Allow the car to be in the jumping state for a short time even if it lands immediately
        }

        public void OnHandbrake(InputValue value)
        {
            handbrake = value.isPressed;
        }
        // Private methods
        private bool isOnGround()
        {
            var isGrounded = RLCollider.isGrounded;
            isGrounded = isGrounded || RRCollider.isGrounded;
            isGrounded = isGrounded || FLCollider.isGrounded;
            isGrounded = isGrounded || FRCollider.isGrounded;
            return isGrounded;
        }
        
        private void OnDrawGizmos()
        {
            // Draw a line from the car to the ground to visualize the ground check
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1f);
        }
        #endregion

    }
}
