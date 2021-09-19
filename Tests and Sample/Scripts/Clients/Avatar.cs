using MUtility;
using UnityEngine;
using UnityServiceLocator;

public class Avatar : MonoBehaviour
{
    [SerializeField] float velocity = 1;
    IAvatarInputProvider _input;

    void Awake()
    {
        UpdateInputProvider();
        ServiceLocator.Environment.SubscribeToEnvironmentChange<IAvatarInputProvider>(UpdateInputProvider);
    }
    
    void UpdateInputProvider()
    {
        _input = ServiceLocator.Get<IAvatarInputProvider>();
    }

    void Update()
    {
        if (_input.TryGetDirection(out Direction2D direction))
            transform.localPosition += (Vector3)direction.ToVector().normalized * (Time.deltaTime * velocity);
    }
}
