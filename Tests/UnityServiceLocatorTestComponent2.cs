using UnityEngine;
using UnityServiceLocator;

[ServiceType]
public class UnityServiceLocatorTestComponent2 : MonoBehaviour , IInitializable
{ 
    public int initializationCount = 0;
    public void Initialize()
    {
        initializationCount++;
    }
}
