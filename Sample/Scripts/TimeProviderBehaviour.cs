using System;
using System.Collections.Generic;
using UnityServiceLocator;
using UnityEngine;
using Object = UnityEngine.Object;

public class TimeProviderBehaviour : MonoBehaviour, ITimeProvider, IEquatable<TimeProviderBehaviour>, ITimeProvider3
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
