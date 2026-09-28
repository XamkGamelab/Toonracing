using Car;
using UnityEngine;

public class SpawnCar : MonoBehaviour
{
    [SerializeField] private string carName;
    private CarSettings[] cars;
    [SerializeField, Range(0f, 1f)] private float dropHeight;
    [SerializeField] private bool spawnOnStart = false;
    CarController carInstance;
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
    public void SpawnCarToScene()
    {
        if(string.IsNullOrEmpty(carName))
        {
            Debug.LogError("Car name not set");
            return;
        }
        // Allow only one car to be spawned at a time
        if(carInstance != null)
            return; 
        foreach (var car in cars)
        {
            if (car.carName != carName) 
                continue;
            carInstance = Instantiate(car.carPrefab, transform.position, transform.rotation);
            carInstance.SetCarSettings(car);
            return;
        }
        Debug.LogError($"Car with name {carName} not found in Resources/Cars");
    }
    
    public void DeleteCarFromScene()
    {
        if(carInstance == null)
            return;
        Destroy(carInstance.gameObject);
        carInstance = null;
    }
    
    public void DropCar()
    {
        if(carInstance == null)
            return;
        var newPosition = carInstance.transform.position + Vector3.up * dropHeight;
        carInstance.transform.position = newPosition;
    }
    
}
