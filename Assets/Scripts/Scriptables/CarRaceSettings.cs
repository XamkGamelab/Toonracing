using UnityEngine;

[CreateAssetMenu(fileName = "CarRaceSettings", menuName = "Scriptable Objects/CarRaceSettings")]
public class CarRaceSettings : ScriptableObject
{
    [Header("Car Color Settings")]
    public Color lightColor; // Color of the car when it is in light
    public Color shadowColor; // Color of the car when it is in shadow
}
