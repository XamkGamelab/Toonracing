using System.Collections;
using Interfaces;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    [SerializeField] private float verticalMovementAmplitude = 0.5f; // Amplitude of the vertical movement
    [SerializeField] private float verticalMovementFrequency = 1f; // Frequency of the
    [SerializeField] private float rotationSpeed = 50f; // Speed of rotation in degrees per second
    [SerializeField] private float reappearTime = 10f; // Time in seconds before the item reappears after being picked up
    private GameObject itemVisual; // The visual representation of the item in 3D world
    private Vector3 initialScale;
    
    // Private variables to store the initial position and rotation of the item
    private Transform itemTransform;
    private Scriptables.PickableItemScriptable[] items;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        itemTransform = transform.GetChild(0); // Assuming the visual representation is the first child of the item
        if(!itemTransform)
        {
            Debug.LogError("ItemController: No child found for itemVisual. Please assign a child GameObject as the visual representation of the item.");
            return;
        }
        ReadItems();
        initialScale = itemTransform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if(!itemTransform)
            return;
        itemTransform.localPosition = Vector3.up * (Mathf.Sin(Time.time * verticalMovementFrequency) * verticalMovementAmplitude);
        itemTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    
    private Scriptables.PickableItemScriptable GetRandomItem()
    {
        // Get a random item from the Resources/Items folder
         return items[Random.Range(0, items.Length)];
    }

    private void ReadItems()
    {
        // Get all items from the Resources/Items folder
        items = Resources.LoadAll<Scriptables.PickableItemScriptable>("Pickables");
        foreach (var item in items)
        {
            Debug.Log($"Item: {item.name}, Type: {item.itemType}, Value: {item.value}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        var picker = other.GetComponent<Interfaces.IPicker>();
        if (picker == null) // Check if the other collider has a component that implements the IPicker interface
            return;
        var currentPickableItem = GetRandomItem();
        picker.OnHitPickableItem(currentPickableItem);  // Give information about the item to the player through the IPicker interface
        // Here you can add logic to give the item to the player, e.g., increase health, ammo, etc.
        Debug.Log($"Player picked up: {currentPickableItem.itemType} with value: {currentPickableItem.value}");
        // Run the hit animation on the item visual representation
        if(itemTransform)
        {
            itemTransform.localScale = Vector3.zero; // Scale down the item to zero to simulate a hit animation
            transform.GetComponent<BoxCollider>().enabled = false;
            StartCoroutine(HitTimeout());
        }
    }
    
    private IEnumerator HitTimeout()
    {
        yield return new WaitForSeconds(reappearTime);
        transform.GetComponent<BoxCollider>().enabled = true;
        if(itemTransform)
            itemTransform.localScale = initialScale; // Scale back the item to its original size to simulate reappearing
    }
    
}
