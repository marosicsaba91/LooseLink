using UnityEngine;
using UnityServiceLocator;

[ServiceType]
public class UAvatarCamera : MonoBehaviour
{
    [SerializeField] Transform distanceTransform;
    [SerializeField] Camera cam;
    [SerializeField, Min(0)] float distance = 30;
    [SerializeField, Range(0, 90)] float angle = 40;
    [SerializeField, Range(0, 360)] float direction = 0;
    [SerializeField, Range(0, 360)] float fieldOfView = 30;

    Transform _followable;
    public Camera Camera => cam;

    void OnValidate()
    {
        Update();
    }

    void Awake()
    {
        _followable = ServiceLocator.Resolve<Transform>("Avatar"); 
    } 

    void Update()
    {
        if (_followable != null)
            transform.position = _followable.position;
        distanceTransform.localPosition = new Vector3(0, distance, 0);
        transform.rotation = Quaternion.Euler(-angle, direction, 0);
        cam.fieldOfView = fieldOfView;
    }
}
