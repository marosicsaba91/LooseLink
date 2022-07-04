using System;
using MarosiUtility;
using UnityEngine;
using LooseLink;
using Object = UnityEngine.Object;

[ServiceType]
public class CursorButtonsMovementProvider : MonoBehaviour, IMovementInputProvider, IUnityComponent
{
    public Object UnityObject => gameObject; 
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
