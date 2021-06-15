using System.Collections.Generic;
using LooseServices;
using UnityEngine;

public class SystemTimeProvider : ScriptableObject, ITimeProvider, ITagged
{
    public float GetTime => System.DateTime.Now.Second;
    public void Initialize() { }
    public IEnumerable<object> GetTags()
    {
        yield return "R2D2";
        yield return "Luke Skywalker";
        yield return "Yoda";
    }
}
