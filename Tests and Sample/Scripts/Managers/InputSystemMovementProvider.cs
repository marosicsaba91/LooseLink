using System;
using MUtility;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityServiceLocator;
using Object = UnityEngine.Object;

[CreateAssetMenu, ServiceType]
public class InputSystemMovementProvider : ScriptableObject, IMovementInputProvider
{
    [SerializeField] InputAction up;
    [SerializeField] InputAction down;
    [SerializeField] InputAction right;
    [SerializeField] InputAction left;

    void OnEnable()
    {
        up.Enable();
        down.Enable();
        right.Enable();
        left.Enable();
        
        UnSubscribe();
        Subscribe();
    }
    
    void OnValidate()
    {
        UnSubscribe();
        Subscribe();
    }
    
    void OnDisable()
    {
        Subscribe();
    }

    void Subscribe()
    { 
    }
    
    void UnSubscribe(){}

    public Object UnityObject => this;
    public string NameOfDirectionCommand(GeneralDirection2D direction) => 
        DirectionToInputAction(direction).ToString();

    public bool IsDirectionCommandPressed(GeneralDirection2D direction)
    {
        InputAction action = DirectionToInputAction(direction); 
        return action.phase == InputActionPhase.Started;
    }

    InputAction DirectionToInputAction(GeneralDirection2D direction)
    {
        switch (direction)
        {
            case GeneralDirection2D.Up: return up;
            case GeneralDirection2D.Down: return down;
            case GeneralDirection2D.Right: return right;
            case GeneralDirection2D.Left: return left;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    } 
}
