using UnityEngine;
using UnityServiceLocator;

[ServiceType]

interface IUnityServiceLocatorTestInterface
{
    string TestValue { get; set; }
    GameObject GameObject { get; }
}

public class UnityServiceLocatorTestComponent3 : MonoBehaviour, IUnityServiceLocatorTestInterface
{
    public string TestValue { get; set; }
    public GameObject GameObject => gameObject;

}
