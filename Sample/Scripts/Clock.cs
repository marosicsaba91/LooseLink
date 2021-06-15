using LooseServices;
using MUtility;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField] TextMesh textMesh;
    ITimeProvider _timeProviderBehaviour;

    [SerializeField] FloatProperty testFloatProperty = new FloatProperty {valueChanged = ValueChanged};

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
