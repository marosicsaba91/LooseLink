using PlayLogging;
using MUtility;
using UnityEngine;
using UnityServiceLocator;

public class Avatar : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] Rigidbody rgbdBody;
    [SerializeField] Bullet bulletPrototype;
    [Header("Settings")]
    
    [SerializeField] float maxVelocity = 5;
    [SerializeField] float acceleration = 20;
    [SerializeField] float deceleration = 50;
    
    IMovementInputProvider _movementInput;
    IShootingInputProvider _shootingInput; 
    BallisticsManager _ballisticsManager; 
    LogStore _logStore; 

    Vector2 _horizontalVelocity;
    
    void OnValidate()
    {
        if (rgbdBody == null)
            rgbdBody = GetComponent<Rigidbody>();
    }

    void Awake()
    {
        UpdateShootingInputProvider();
        UpdateMovementInputProvider();
        _ballisticsManager = ServiceLocator.Resolve<BallisticsManager>(); 
        _logStore = ServiceLocator.Resolve<LogStore>(); 
        // ServiceLocator.Environment.SubscribeToEnvironmentChange<IMovementInputProvider>(UpdateMovementInputProvider);
        // ServiceLocator.Environment.SubscribeToEnvironmentChange<IShootingInputProvider>(UpdateShootingInputProvider);
    }

    void UpdateMovementInputProvider() => _movementInput = ServiceLocator.Resolve<IMovementInputProvider>(); 
    void UpdateShootingInputProvider()
    {
        _shootingInput = ServiceLocator.Resolve<IShootingInputProvider>();
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
 
        rgbdBody.velocity = new Vector3(_horizontalVelocity.x, rgbdBody.velocity.y, _horizontalVelocity.y);
    }
    
    
    
    void ShootMain(Vector2 position)
    {
        var log = new PlayLog("Main Shot");
        _logStore.Log(log );
        _logStore.Log("Main Shot", this, (LogTag)null, 1, "béka", Time.time);
        Vector3 start = transform.position;
        _ballisticsManager.Shoot(start, new Vector3(position.x,start.y, position.y), bulletPrototype);
    }
    
    void ShootSecondary(Vector2 position)
    {
        _logStore.Log("Secondary Shot", this);
        Vector3 start = transform.position;
        _ballisticsManager.Shoot(start, new Vector3(position.x,start.y, position.y), bulletPrototype);
    }


}
