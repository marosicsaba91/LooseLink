using System;
using System.Collections.Generic;
using UnityEngine;
 

public class SystemTimeProvider : ScriptableObject, ITimeProvider, IComparer<int>, IFormattable
{
    public float GetTime => DateTime.Now.Second;

    public int Compare(int x, int y) => 0;

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return null;
    }
} 