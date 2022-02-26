using System;
using UnityEngine;

public class TimeProviderBehaviour : MonoBehaviour, ITimeProvider, IEquatable<TimeProviderBehaviour>
{
    float _time;

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
