using System;
using System.Collections.Generic;
using UnityServiceLocator;
using UnityEngine;
 

public class SystemTimeProvider : ScriptableObject, ITimeProvider, ITagged, IComparer<int>, IFormattable, IInitializable
{

    [SerializeField] bool testBool = false;
    public float GetTime => DateTime.Now.Second;

    public void Initialize()
    {
        testBool = !testBool;
    }

    public IEnumerable<object> GetTags()
    {
        yield return "R2D2";
        yield return "Luke Skywalker";
        yield return "C-3PO";
        yield return "Yoda";
        yield return "Darth Vader";
    }

    public int Compare(int x, int y) => 0;

    public string ToString(string format, IFormatProvider formatProvider)
    {
        return null;
    }
} 