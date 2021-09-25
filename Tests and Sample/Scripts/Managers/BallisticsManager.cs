using UnityEngine;
 
public class BallisticsManager : MonoBehaviour
{
    public void Shoot(Vector3 start, Vector3 destination)
    {
        Debug.Log($"Shot Here: {destination}");
    }
} 