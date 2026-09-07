using UnityEngine;

[CreateAssetMenu(fileName = "CarBaseSettings", menuName = "Scriptable Objects/CarBaseSettings")]
public class CarBaseSettings : ScriptableObject
{
    // Global car settings
    [Header("Global car settings that every car will use")]
    public float brakeForce;
    public float maxSteeringAngle;
    public float jumpForce;
    
    // Multipliers for car stats, used to scale the base settings for different cars
    [Header("Multipliers for car stats")]
    public float weightMultiplier;
    public float accelerationMultiplier;
    public float steerSpeedMultiplier;
    
    // Base settings for the worst car in the game, used for balancing and scaling other cars
    [Header("Base settings for the worst car in the game")]
    public float baseWeight;
    public float baseAccelerationForce;
    public float baseSteerSpeed;
    public float baseSteerReleaseSpeed;
    public float baseMaxSpeed;
}
