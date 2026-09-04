using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Scripts : MonoBehaviour
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

    public float acceleration = -2000f;
    public float brakingForce = 3000f;
    
    public float currentAcceleration = 0f;
    public float currentBrakingForce = 100f;

    //Turning
    public float maxTurningAngle = 35f;
    public float currentTurningAngle = 0f;
    public float steerSpeed = 5f;
    public float steerReleaseSpeed = 5f;
    public Vector3 FL;
    public Quaternion FLrot;
    void Start()
    {
        FLTransform.GetPositionAndRotation(out FL, out FLrot);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        // Accelerations
        RLCollider.motorTorque = currentAcceleration;
        RRCollider.motorTorque = currentAcceleration;
        FLCollider.motorTorque = currentAcceleration;
        FRCollider.motorTorque = currentAcceleration;
        //Braking
        FLCollider.brakeTorque = currentBrakingForce;
        FRCollider.brakeTorque = currentBrakingForce;
        RLCollider.brakeTorque = currentBrakingForce;
        RRCollider.brakeTorque = currentBrakingForce;

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
    
    public void OnAccelerate(InputAction accelerationInput)
    {
        currentAcceleration = acceleration * accelerationInput.ReadValue<float>();
        if(currentAcceleration < 0.1f)
        {
            currentBrakingForce = brakingForce;
            currentAcceleration = 0f;
        }
        else
            currentBrakingForce = 0f;
        RLCollider.motorTorque = currentAcceleration;
        RRCollider.motorTorque = currentAcceleration;
        FLCollider.motorTorque = currentAcceleration;
        FRCollider.motorTorque = currentAcceleration;
    }
    
    public void OnSteer(InputAction steerInput)
    {
        currentTurningAngle = maxTurningAngle * steerInput.ReadValue<float>();
        FLCollider.steerAngle = currentTurningAngle;
        FRCollider.steerAngle = currentTurningAngle;
    }
    
}

