using System;
using System.Collections.Generic;
using LooseServices;
using UnityEngine;
 

public class SystemTimeProvider : ScriptableObject, ITimeProvider, ITagged, IComparer<int>, IFormattable
{
    public float GetTime => DateTime.Now.Second;

    public void Initialize()
    {
    }

    public IEnumerable<object> GetTags()
    {
        yield return "R2D2";
        yield return "Luke Skywalker";
        yield return "Yoda";
    }

    public bool EnableCustomTags => true;
    public int Compare(int x, int y)
    {
        return 0;
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return null;
    }
} 