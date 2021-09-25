
using System;
using MUtility;
using UnityEngine;
using UnityServiceLocator;

public class MouseShootingInput : MonoBehaviour, IShootingInputProvider 
{
    public event Action<Vector2> ShotMain; 
    public event Action<Vector2> ShotSecondary;

    AvatarCamera _avatarCamera;
    
    void Awake()
    {
        _avatarCamera = ServiceLocator.Get<AvatarCamera>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            ShotMain?.Invoke(AimingPosition);
        if (Input.GetMouseButtonDown(1))
            ShotSecondary?.Invoke(AimingPosition);
    }
    
    public Vector2 AimingPosition {
        get
        {
            Ray ray = _avatarCamera.Camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plain( new Vector3(0, 1, 0), Vector3.up);
            Vector3 intersection = plane.Intersect(new Line(ray));
            return new Vector2(intersection.x, intersection.z);
        }
    }
}