using System;
using MUtility;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu]
public class KeyboardInputProvider : ScriptableObject, IAvatarInputProvider
{
    [SerializeField] KeyCode up = KeyCode.UpArrow;
    [SerializeField] KeyCode down = KeyCode.DownArrow;
    [SerializeField] KeyCode left = KeyCode.LeftArrow;
    [SerializeField] KeyCode right = KeyCode.RightArrow;
    
    public Object UnityObject => this;
    public string NameOfDirectionCommand(GeneralDirection2D direction) => 
        DirectionToKeyCode(direction).ToString();

    public bool IsDirectionCommandPressed(GeneralDirection2D direction) =>
        Input.GetKey(DirectionToKeyCode(direction));

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
