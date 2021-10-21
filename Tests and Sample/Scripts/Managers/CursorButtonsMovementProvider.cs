using System;
using MUtility;
using UnityEngine;
using UnityServiceLocator;
using Object = UnityEngine.Object;

public class CursorButtonsMovementProvider : MonoBehaviour, IMovementInputProvider, IUnityComponent
{
    public Object UnityObject => gameObject;
    public string NameOfDirectionCommand(GeneralDirection2D direction) => 
        DirectionToKeyCode(direction).ToString();

    public bool IsDirectionCommandPressed(GeneralDirection2D direction) =>
        Input.GetKey(DirectionToKeyCode(direction));

    static KeyCode DirectionToKeyCode(GeneralDirection2D direction)
    {
        switch (direction)
        {
            case GeneralDirection2D.Up: return KeyCode.UpArrow;
            case GeneralDirection2D.Down: return KeyCode.DownArrow;
            case GeneralDirection2D.Right: return KeyCode.RightArrow;
            case GeneralDirection2D.Left: return KeyCode.LeftArrow;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, message: null);
        }
    }

    public GameObject GameObject => gameObject;
}
