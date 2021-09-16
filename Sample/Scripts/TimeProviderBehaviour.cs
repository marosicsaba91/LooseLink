using System; 
using UnityEngine; 

public class TimeProviderBehaviour : MonoBehaviour, ITimeProvider, IEquatable<TimeProviderBehaviour>, ITimeProvider2
{
    float _time = 0;

    void Update()
    {
        _time += Time.deltaTime;
    }

    public float GetTime => _time;

    public bool Equals(TimeProviderBehaviour other)
    {
        return other == this;
    }
}
