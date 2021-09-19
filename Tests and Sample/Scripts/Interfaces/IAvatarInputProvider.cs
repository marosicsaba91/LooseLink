using MUtility;
using UnityEngine;
using UnityServiceLocator;

[ServiceType]
public interface IAvatarInputProvider
{
    Object UnityObject { get; }
    string NameOfDirectionCommand(GeneralDirection2D direction);
    bool IsDirectionCommandPressed(GeneralDirection2D direction);
}
