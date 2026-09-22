using Car;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "CarSettings", menuName = "Scriptable Objects/CarSettings")]
    public class CarSettings : ScriptableObject
    {
        public enum  DriveType
        {
            Fwd,
            Rwd,
            Awd
        }
        public string carName;
        public CarController carPrefab;
        [Tooltip("Base settings for every car. Needed for default values and scaling the car's stats.")]
        public CarBaseSettings baseSettings;
    
        // Car's stats in the game UI. Used then to modify the base settings for the ca
        [Range(1, 5)] public int weightStat;
        [Range(1, 5)] public int accelerationStat;
        [Range(1, 5)] public int topSpeedStat;
        [Range(1, 5)] public int handlingStat;
        public Car.CarController.DriveType driveType;
    
    }
}
