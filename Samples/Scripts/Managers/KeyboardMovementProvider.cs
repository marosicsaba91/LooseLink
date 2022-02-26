using System;
using MUtility;
using UnityEngine; 
using Object = UnityEngine.Object;

[CreateAssetMenu]
public class KeyboardMovementProvider : ScriptableObject, IMovementInputProvider
{
    [SerializeField, SearchEnum] KeyCode up = KeyCode.UpArrow;
    [SerializeField, SearchEnum] KeyCode down = KeyCode.DownArrow;
    [SerializeField, SearchEnum] KeyCode left = KeyCode.LeftArrow;
    [SerializeField, SearchEnum] KeyCode right = KeyCode.RightArrow;
    
    public Object UnityObject => this; 
    public bool IsDirectionCommandPressed(GeneralDirection2D direction)
    {
        return Input.GetKey(DirectionToKeyCode(direction));
    }

    KeyCode DirectionToKeyCode(GeneralDirection2D direction)
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
