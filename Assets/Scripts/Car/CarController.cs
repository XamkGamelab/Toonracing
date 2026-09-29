using System;
using System.Collections;
using Scriptables;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CarController : MonoBehaviour, Interfaces.IPicker
    {
        
        // Wheel physics
        [Header("Wheel Colliders")]
        [SerializeField]
        private WheelCollider rlCollider;
        [SerializeField] private WheelCollider rrCollider;
        [SerializeField] private WheelCollider flCollider;
        [SerializeField] private WheelCollider frCollider;
        // Wheel directions
        [Header("Wheel Transforms")]
        [SerializeField] private Transform frWheelHub;
        [SerializeField] private Transform flWheelHub;
        [SerializeField] private Transform rrWheelHub;
        [SerializeField] private Transform rlWheelHub;

        private Transform frWheel;
        private Transform flWheel;
        private Transform rrWheel;
        private Transform rlWheel;
        // Game object which holds the car model, used for power slide visualization
        [Header("Car transforms")]
        [SerializeField]
        private Transform carTransform;

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
        private CarSettings.DriveType driveType;
        [SerializeField] private float acceleration = 500f;
        [SerializeField] private float boostAccelerationDelta = 1000f;
        [SerializeField] private float brakingForce = 1000f;
        [SerializeField] private float maxTurningAngle = 35f;
        [SerializeField] private float steerSpeed = 5f;
        [SerializeField] private float steerReleaseSpeed = 5f;
        [SerializeField] private float coastingDrag = 1f;
        [SerializeField, Range(0f, 1f)] private float brakeBalance = 0.5f; // 1 = front brakes only, -1 = rear brakes only
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float topSpeed = 10f;
        [SerializeField] private float maxSwayAngle = 30f;
        [SerializeField] private float angleCorrectionForce = 10f;
        [SerializeField] private float carFlipTime = 2f;
        [SerializeField] private float carFlipLimitAngle = 80f;
        [SerializeField] private float jumpTimeout = 2f;
        [SerializeField] private float slideVirtualRotation = 10f;
        [SerializeField] private float slideTransitionTime = 2;
    
        [Header("Debug")]
        [SerializeField] private float currentMotorTorque;
        [SerializeField] private float currentBrakingForce;
        [SerializeField] private float currentSpeed;
        //Turning
        [SerializeField] private float currentTurningRequest;
        [SerializeField] private float currentTurningAngle;
        
        
        //General variables
        private Rigidbody rb;
        private PlayerInput playerInput;
        [SerializeField] private GearSelect gearSelect;
        [SerializeField] private bool handbrake;
        private float prevSteerReqAngle;
        public CarState carState;
        public float steerDirection; // -1 = left, 1 = right, 0 = straight
        public bool slideServiceRunning;
        private bool isFlippingBack;
        private bool isFlipFinished;
        private bool isJumpPressed;
        private float groundCheckDelay;
        private float jumpEndTimeout;
        private bool isRemoteControlled; // If the car is controlled by a remote player, disable input handling and physics updates
        private NetworkObject networkObject;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if(rb == null)
            {
                Debug.LogError("Rigidbody not found on the car object.");
                return;
            }
            rb.isKinematic = false; // Ensure the car is not kinematic at the start
            if(carTransform == null)
                Debug.LogError("Car transform not assigned. Should be the Game object that holds the car model.");
            playerInput = GetComponent<PlayerInput>();
            if(playerInput == null)
                Debug.LogError("PlayerInput not found on the car object.");
            frWheel = frWheelHub.Find("Tire");
            flWheel = flWheelHub.Find("Tire");
            rrWheel = rrWheelHub.Find("Tire");
            rlWheel = rlWheelHub.Find("Tire");
            networkObject = GetComponent<NetworkObject>();
        }

        private void Start()
        {
            Debug.Log("CarController initialized");
        }

        // Update is called once per frame
        private void Update()
        {
            // If the car is controlled by a remote player, disable input handling and physics updates
            if(isRemoteControlled)
                return;
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
            Debug.Log(
                $"{name} | owner={networkObject.IsOwner} | " +
                $"remote={isRemoteControlled} | " +
                $"kinematic={rb.isKinematic} | " +
                $"torque={currentMotorTorque} | " +
                $"speed={topSpeed} | " +
                $"driveType={driveType}"
            );
            // If the car is controlled by a remote player, disable input handling and physics updates
            if(isRemoteControlled)
                return;
            SpeedHandler();
            BrakeHandler();
            SteerHandler();

            //Wheel rotation
            if(!frWheel || !flWheel || !rrWheel || !rlWheel)
            {
                Debug.Log("One or more wheel transforms are not assigned.");
                return;
            }
            WheelController(flCollider, flWheel, true);
            WheelController(frCollider, frWheel, true);
            WheelController(rlCollider, rlWheel);
            WheelController(rrCollider, rrWheel);
        }

        private void LateUpdate()
        {
            if(!isRemoteControlled)
                return;
            // If the car is controlled by a remote player, update wheel positions based on raycasts to the ground to simulate wheel rotation and suspension movement
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
        
        private static void WheelController(WheelCollider wheelCollider, Transform wheelTransform, bool isSteeringWheel = false)
        {
            wheelCollider.GetWorldPose(out var pos, out var rot);
            
            // Adjust only the wheel's world Y position. Updating position directly while the
            // model is rotated changes the child's local X/Z position as a side effect.
            var wheelPosition = wheelTransform.position;
            wheelTransform.parent.localPosition += Vector3.up * (pos.y - wheelPosition.y);
            
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
            if (currentSpeed >= topSpeed)
            {
                torque = 0f; // Reduce accelerating if at or above top speed
            }
            
            switch (driveType)
            {
                case CarSettings.DriveType.Awd:
                    rlCollider.motorTorque = torque;
                    rrCollider.motorTorque = torque;
                    flCollider.motorTorque = torque;
                    frCollider.motorTorque = torque;
                    break;
                case CarSettings.DriveType.Rwd:
                    rlCollider.motorTorque = torque;
                    rrCollider.motorTorque = torque;
                    flCollider.motorTorque = 0;
                    frCollider.motorTorque = 0;
                    break;
                case CarSettings.DriveType.Fwd:
                    rlCollider.motorTorque = 0;
                    rrCollider.motorTorque = 0;
                    flCollider.motorTorque = torque;
                    frCollider.motorTorque = torque;
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
            flCollider.brakeTorque = frontBrakeForce;
            frCollider.brakeTorque = frontBrakeForce;
            rlCollider.brakeTorque = rearBrakeForce;
            rrCollider.brakeTorque = rearBrakeForce;
            if (handbrake)
            {
                rlCollider.brakeTorque = brakingForce;
                rrCollider.brakeTorque = brakingForce;
            }
        }
        
        private void SteerHandler()
        {
            // This method can be used to handle steering logic, such as adjusting the steering angle based on input and speed.
            //Steering
            currentTurningAngle = flCollider.steerAngle;
            // Is the steering centering or turning in the opposite direction of the current request?
            var isFastSteer = !Mathf.Approximately(Mathf.Sign(currentTurningRequest), Mathf.Sign(currentTurningAngle)) || Mathf.Abs(currentTurningRequest) < Mathf.Abs(currentTurningAngle);
            var turningSpeed = isFastSteer ? steerReleaseSpeed : steerSpeed;
            var turningAngle = Mathf.MoveTowards(currentTurningAngle, currentTurningRequest, turningSpeed * Time.fixedDeltaTime);
            flCollider.steerAngle = turningAngle;
            frCollider.steerAngle = turningAngle;
            
        }
        
        // Private methods
        private bool isOnGround()
        {
            var isGrounded = rlCollider.isGrounded;
            isGrounded = isGrounded || rrCollider.isGrounded;
            isGrounded = isGrounded || flCollider.isGrounded;
            isGrounded = isGrounded || frCollider.isGrounded;
            return isGrounded;
        }
        
        
        private IEnumerator SpeedBoostRoutine(float speedBoost, float accelerationBoost, float boostDuration)
        {
            var originalTopSpeed = topSpeed;
            var newTopSpeed = topSpeed * speedBoost;
            var timeout = boostDuration;
            var newAcceleration = acceleration * accelerationBoost;
            const float loopTime = .2f;
            while(true)
            {
                // Just in case keep setting the top speed and acceleration to the boosted values, in case player gets a new boost while already boosted
                topSpeed = newTopSpeed;
                currentMotorTorque = newAcceleration;
                if(timeout > loopTime)
                    timeout -= loopTime;
                else
                    break;
                yield return new WaitForSeconds(loopTime);
            }
            topSpeed = originalTopSpeed;
            currentMotorTorque = acceleration;
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
        
        // This method is used to set whether the car is controlled by a remote player or not.
        // If it is, disable input handling and physics updates.
        public void SetRemoteControlled(bool isRemote)
        {
            isRemoteControlled = isRemote;
            if (playerInput != null)
            {
                playerInput.enabled = !isRemote;
                if (isRemote)
                    playerInput.DeactivateInput();
                else
                    playerInput.ActivateInput();
            }

            if (rb != null)
                rb.isKinematic = isRemote;
        }
        // Input system callbacks

        #region Input System Callbacks
        public void OnAccelerate(InputValue value)
        {
            
            Debug.Log($"Accelerating: {value.isPressed}");
            currentBrakingForce = 0f; // Reset braking force when accelerating
            if(value.isPressed)
            {
                // If the car is stopped, switch to drive gear when accelerate is pressed
                if(rrCollider.rpm > -0.1f)
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
            if (value.isPressed)
            {
                // If the car is stopped, switch to reverse gear when brake is pressed
                if(rrCollider.rpm < 0.1f)
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

        public void OnUsePickedItem(InputValue value)
        {
            if (!value.isPressed)
                return;
            Debug.Log("Using picked item");
            
        }

        #endregion

        // Interface implementation
        #region IPicker implementation
        public void OnHitPickableItem(PickableItemScriptable item)
        {
            switch (item.itemType)
            {
                case PickableItemScriptable.ItemType.Health:
                    // Handle health item
                    break;
                case PickableItemScriptable.ItemType.Ammo:
                    // Handle ammo item
                    break;
                case PickableItemScriptable.ItemType.SpeedBoost:
                    StartCoroutine(SpeedBoostRoutine(item.value, item.AccelerationBoost, item.BoostTime));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion
        
        
    }
}
