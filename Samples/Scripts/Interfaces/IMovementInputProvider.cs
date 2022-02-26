using MUtility;
using LooseLink;

[ServiceType]
public interface IMovementInputProvider
{ 
    bool IsDirectionCommandPressed(GeneralDirection2D direction);
}

public static class AvatarInputHelper
{
    public static bool TryGetDirection(this IMovementInputProvider inputProvider, out Direction2D dir)
    {
        bool up = inputProvider.IsDirectionCommandPressed(GeneralDirection2D.Up);
        bool down = inputProvider.IsDirectionCommandPressed(GeneralDirection2D.Down);
        bool left = inputProvider.IsDirectionCommandPressed(GeneralDirection2D.Left);
        bool right = inputProvider.IsDirectionCommandPressed(GeneralDirection2D.Right);

        dir = default;
        if (!up && !down && !left && !right) return false;

        if (up ^ down)
        {
            if (up)
            {
                if (left ^ right)
                    dir = left ? Direction2D.UpLeft : Direction2D.UpRight;
                else
                    dir = Direction2D.Up;
            }
            else
            {
                if (left ^ right)
                    dir = left ? Direction2D.DownLeft : Direction2D.DownRight;
                else
                    dir = Direction2D.Down;
            }
        }
        else
        {
            if (left ^ right)
                dir = left ? Direction2D.Left : Direction2D.Right;
            else 
                return false;
        }

        return true;
    }
}
