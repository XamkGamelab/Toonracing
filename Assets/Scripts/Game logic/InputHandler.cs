using System.Reflection.Metadata;
using Car;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }
    private CarController localPlayer;
    private bool isInputEnabled;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        if(Instance)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
    }
    public void SetLocalPlayer(CarController player)
    {
        localPlayer = player;
    }
    
    public void SetInputEnabled(bool state)
    {
        isInputEnabled = state;
    }
    
    public void OnAccelerate(InputValue value)
    {
        if(!localPlayer)  
            return;
        if(!isInputEnabled)
            return;
        localPlayer.OnAccelerate(value);
    }
    
    public void OnBrake(InputValue value)
    {
        if(!localPlayer)  
            return;
        if(!isInputEnabled)
            return;
        localPlayer.OnBrake(value);
    }
    
    public void OnSteer(InputValue steerInput)
    {
        if(!localPlayer)  
            return;
        if(!isInputEnabled)
            return;
        localPlayer.OnSteer(steerInput);
    }
    
    public void OnJump(InputValue value)
    {
        if(!localPlayer)  
            return;
        if(!isInputEnabled)
            return;
        localPlayer.OnJump(value);
    }
    
    public void OnHandbrake(InputValue value)
    {
        if(!localPlayer)  
            return;
        if(!isInputEnabled)
            return;
        localPlayer.OnHandbrake(value);
    }

    public void OnUsePickedItem(InputValue value)
    {
        if(!localPlayer)  
            return;
        if(!isInputEnabled)
            return;
        localPlayer.OnUsePickedItem(value);
    }

    public void OnQuit()
    {
        RaceManager.Instance.OnQuit();
    }
}
