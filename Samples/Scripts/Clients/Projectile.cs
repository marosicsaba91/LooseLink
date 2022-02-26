using System;
using LooseLink;
using UnityEngine; 
using Vector3 = UnityEngine.Vector3;

public class Projectile : MonoBehaviour
{
    [SerializeField] float startVelocity;
    [SerializeField] float timeOut; 

    [SerializeField] new Rigidbody rigidbody;
    [SerializeField] new Collider collider;
     
 
    float _age;
    BallisticsManager _ballisticsManager;

    void Awake()
    {
        var avatar = Services.Get<Avatar>();
        Physics.IgnoreCollision(collider, avatar.Collider);
    }

    public void Shoot(Vector3 start, Vector3 destination)
    { 
        rigidbody.velocity = BallisticsManager.CalculateShootDirection( start, destination, startVelocity);
        transform.position = start;
        _age = 0;
    }

    public Projectile Prototype { get; private set; }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age > timeOut)
            BackToPool();
    }

    void BackToPool()
    { 
        _ballisticsManager.PutPrototypeBackInPool(this);
    }

    public void Setup(Projectile prototype, BallisticsManager ballisticsManager)
    {
        Prototype = prototype;
        _ballisticsManager = ballisticsManager;
    }
}
