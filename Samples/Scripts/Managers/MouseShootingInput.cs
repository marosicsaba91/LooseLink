using System;
using LooseLink;
using MUtility;
using UnityEngine; 
using Vector3 = UnityEngine.Vector3;

[CreateAssetMenu]
public class MouseShootingInput : ScriptableObject, IShootingInputProvider , IInitializable
{
    public event Action<Vector2> ShotMain; 
    public event Action<Vector2> ShotSecondary;

    UAvatarCamera _uAvatarCamera;
     

    public void Initialize()
    {       
        Debug.Log("Try Init Soot Input");
        _uAvatarCamera = Services.Get<UAvatarCamera>();
        if(Services.TryGet(out UpdateProvider updateProvider))
            updateProvider.OnUpdate += Update;
        else
            Debug.Log("Shit");
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
            Ray ray = _uAvatarCamera.Camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plain( new Vector3(0, 1, 0), Vector3.up);
            Vector3 intersection = plane.Intersect(new Line(ray));
            return new Vector2(intersection.x, intersection.z);
        }
    }
}