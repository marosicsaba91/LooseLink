using MUtility;
using UnityEngine;
using LooseLink;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

public class Avatar : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] Rigidbody rigidBody;
    [SerializeField] new Collider collider ;
    
    [FormerlySerializedAs("projectilesPrototype")] [FormerlySerializedAs("bulletPrototype")] [SerializeField] Projectile projectilePrototype;
    [Header("Settings")]
    
    [SerializeField] float maxVelocity = 5;
    [SerializeField] float acceleration = 20;
    [SerializeField] float deceleration = 50;
    
    
    IMovementInputProvider _movementInput;
    IShootingInputProvider _shootingInput;
    BallisticsManager _ballisticsManager;
 
    Vector2 _horizontalVelocity;
    public Collider Collider => collider;

    void OnValidate()
    {
        if (rigidBody == null)
            rigidBody = GetComponent<Rigidbody>(); 
    }

    
    void Awake()
    {
        _ballisticsManager = Services.Get<BallisticsManager>();
        UpdateShootingInputProvider();
        _movementInput = Services.Get<IMovementInputProvider>(); 
    }
    
    void UpdateShootingInputProvider()
    {
        _shootingInput = Services.Get<IShootingInputProvider>();
        _shootingInput.ShotMain += ShootMain;
        _shootingInput.ShotSecondary += ShootSecondary;
    }

    void FixedUpdate()
    {  
        
        if (_movementInput.TryGetDirection(out Direction2D direction))
        { 
            Vector2 directionVector = direction.ToVector().normalized;  
            _horizontalVelocity += directionVector * (acceleration * Time.fixedDeltaTime);
            if (_horizontalVelocity.magnitude > maxVelocity)
                _horizontalVelocity = _horizontalVelocity.normalized * maxVelocity;
        }else if(_horizontalVelocity != Vector2.zero) 
        {
            float change = deceleration * Time.fixedDeltaTime;
            if(_horizontalVelocity.magnitude <= change)
                _horizontalVelocity = Vector2.zero;
            else 
                _horizontalVelocity = _horizontalVelocity - (change * _horizontalVelocity.normalized);
        }
 
        rigidBody.velocity = new Vector3(_horizontalVelocity.x, rigidBody.velocity.y, _horizontalVelocity.y);
    }
    
    
    
    void ShootMain(Vector2 position)
    {
        Vector3 start = transform.position;
        _ballisticsManager.Shoot(start, new Vector3(position.x,start.y, position.y), projectilePrototype);
    }
    
    void ShootSecondary(Vector2 position)
    {
        Vector3 start = transform.position;
        _ballisticsManager.Shoot(start, new Vector3(position.x,start.y, position.y), projectilePrototype);
    }


}
