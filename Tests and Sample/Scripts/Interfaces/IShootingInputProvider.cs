using System; 
using UnityEngine;
using UnityServiceLocator;

[ServiceType]
public interface IShootingInputProvider
{
    event Action<Vector2> ShotMain;
    event Action<Vector2> ShotSecondary;
    Vector2 AimingPosition { get; }
} 
