using System;
using System.Collections;
using System.Collections.Generic;
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

    public Object UnityObject => this;
    public string NameOfDirectionCommand(GeneralDirection2D direction) => 
        DirectionToKeyCode(direction).ToString();

    public bool IsDirectionCommandPressed(GeneralDirection2D direction) =>
        DirectionToKeyCode(direction).triggered;
 
    InputAction DirectionToKeyCode(GeneralDirection2D direction)
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
