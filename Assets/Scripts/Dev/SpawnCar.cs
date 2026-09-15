using UnityEngine;

public class SpawnCar : MonoBehaviour
{
    [SerializeField] private string carName;
    private CarSettings[] cars;
    [SerializeField] private bool spawnOnStart = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        cars = Resources.LoadAll<CarSettings>("Cars");
        if(spawnOnStart)
        {
            SpawnCarToScene();
        }
    }
    
    // Button pressed in Unity ui to spawn the car
    [ContextMenu("Spawn Car Into Scene")]
    public void SpawnCarToScene()
    {
        if(string.IsNullOrEmpty(carName))
        {
            Debug.LogError("Car name not set");
            return;
        }
        foreach (var car in cars)
        {
            if (car.carName != carName) 
                continue;
            var carInstance = Instantiate(car.carPrefab, transform.position, transform.rotation);
            carInstance.SetCarSettings(car);
            return;
        }
        Debug.LogError($"Car with name {carName} not found in Resources/Cars");
    }
    
    
}
