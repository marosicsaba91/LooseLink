using System;
using System.Linq;
using MarosiUtility;
using UnityEngine;
using Object = UnityEngine.Object;

public class FallbackMovementProvider : ScriptableObject, IMovementInputProvider
{
    public Object UnityObject => this;
    public bool IsDirectionCommandPressed(GeneralDirection2D direction) => 
        DirectionToKeyCode(direction).Any(Input.GetKey);

    static readonly KeyCode[] right = { KeyCode. D, KeyCode.RightArrow};
    static readonly KeyCode[] left = { KeyCode. A, KeyCode.LeftArrow};
    static readonly KeyCode[] up = { KeyCode. W, KeyCode.UpArrow};
    static readonly KeyCode[] down = { KeyCode. S, KeyCode.DownArrow};
    
    static KeyCode[] DirectionToKeyCode(GeneralDirection2D direction)
    {
        switch (direction)
        {
            case GeneralDirection2D.Right: return right;
            case GeneralDirection2D.Left: return left;
            case GeneralDirection2D.Up: return up;
            case GeneralDirection2D.Down: return down;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }
}
