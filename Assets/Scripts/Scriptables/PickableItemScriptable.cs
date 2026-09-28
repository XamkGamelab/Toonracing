using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "PickableItemScriptable", menuName = "Scriptable Objects/PickableItemScriptable")]
    public class PickableItemScriptable : ScriptableObject
    {
        public Sprite sprite;
        public enum ItemType
        {
            SpeedBoost,
            Money,
            Health,
            Ammo
        }
        public ItemType itemType;
        public float value;
        public float AccelerationBoost;
        public float BoostTime;
    }
}
