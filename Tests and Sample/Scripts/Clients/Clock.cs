using System.Globalization;
using UnityServiceLocator; 
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField] TextMesh textMesh;
    ITimeProvider _timeProvider;

    void Awake()
    {
        _timeProvider = ServiceLocator.Get<ITimeProvider>(); 
    }

    void Update()
    {
        textMesh.text = "Time: " + _timeProvider.GetTime.ToString(CultureInfo.InvariantCulture);
    }
}
