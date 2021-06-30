using System;
using LooseServices;
using MUtility;
using UnityEngine;
using Utility.SerializableCollection;
using Object = UnityEngine.Object;


[Serializable]
public struct SomeStruct
{
    public int someInt;
    public Object someObject;
    public bool someBool;
}
[Serializable]
public struct SomeStructKey
{
    public string someString; 
    public bool someBool;
    public override string ToString() => $"{someString} {someBool}";
}

[Serializable]
public class TestDictionary1 : SerializableDictionary<SomeStructKey, SomeStruct> { } 
[Serializable]
public class TestDictionary2 : SerializableDictionary<ScriptableObject, Vector3> { } 

public class Clock : MonoBehaviour
{
    [SerializeField] TextMesh textMesh;
    ITimeProvider _timeProviderBehaviour;

    [SerializeField] FloatProperty testFloatProperty = new FloatProperty {valueChanged = ValueChanged};
    [SerializeField] TestDictionary1 dictionary;
     
    [SerializeField] Matrix2DInt matrix;
    [SerializeField] TestDictionary2 test2;

    static void ValueChanged(object parent, float oldValue, float newValue)
    {
        Debug.Log( $"Test Vale Changed: {newValue}");
    }


    void Awake()
    {
        _timeProviderBehaviour = Services.Get<ITimeProvider>(); 
    }

    void OnDrawGizmos()
    {
        transform.GetPose().DrawGizmo( Color.blue,0.5f);
    }

    void Start()
    {
        testFloatProperty.Value = 30;
    }

    void Update()
    {
        textMesh.text = $"Time: {_timeProviderBehaviour.GetTime}";
    }  
}
