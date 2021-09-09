using UnityEngine;
using UnityServiceLocator;
using Object = UnityEngine.Object;

public class UnityServiceLocatorTestScriptableObject2 : ScriptableObject, IUnityServiceLocatorTestInterface2
{
    public Object testMonoBehaviourScriptFile;
    public Object testScriptableObjectScriptFile;
}

[ServiceType]
interface IUnityServiceLocatorTestInterface2{}