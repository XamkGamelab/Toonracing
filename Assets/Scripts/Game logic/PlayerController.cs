using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game_logic
{
    public class PlayerController : NetworkBehaviour
    {
        //In editor
        [SerializeField] private float maxSpeed;
        [SerializeField] private float maxAcceleration;
        [SerializeField] private float maxRotationSpeed;
        
        [Header("Debug")]
        [SerializeField] private Vector3 acceleration;
        [SerializeField] private Vector3 velocity;
        [SerializeField] private Vector2 moveInput;
        
        Rigidbody rb;
        float speed;
        
       

        void Awake()
        {
            rb = GetComponent<Rigidbody>();

        }

        
        // Update is called once per frame
        void FixedUpdate()
        {
            if(!IsOwner)
                return;
            Move();
        }

        void Move()
        {
            float moveHorizontal = moveInput.x;
            float moveVertical = moveInput.y;
            // Speed adjust
            Vector3 targetVelocity = transform.forward * (maxSpeed * moveVertical);
            Vector3 deltaV =  targetVelocity - rb.linearVelocity;
            acceleration = deltaV / Time.deltaTime;
            acceleration = Vector3.ClampMagnitude(acceleration, maxAcceleration);
            velocity = rb.linearVelocity;
            rb.AddForce(acceleration, ForceMode.Acceleration);
            
            //Rotation
            rb.MoveRotation(Quaternion.Euler(0, moveHorizontal * maxRotationSpeed * Time.deltaTime + rb.rotation.eulerAngles.y, 0));
        }
        
        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
    }
}
