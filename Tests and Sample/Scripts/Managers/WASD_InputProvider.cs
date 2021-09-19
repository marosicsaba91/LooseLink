using System;
using MUtility;
using UnityEngine;
using Object = UnityEngine.Object;

public class WASD_InputProvider : MonoBehaviour, IAvatarInputProvider
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
            case GeneralDirection2D.Up: return KeyCode.W;
            case GeneralDirection2D.Down: return KeyCode.S;
            case GeneralDirection2D.Right: return KeyCode.D;
            case GeneralDirection2D.Left: return KeyCode.A;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}
