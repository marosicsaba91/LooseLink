using System.Collections.Generic;
using LooseServices;
using UnityEngine;

[IgnoreService]
public class TimeProvider : ScriptableObject, ITimeProvider
{
    public float GetTime => Time.time;
    public void Initialize() { } 
    
    public IEnumerable<object> GetTags()
    { 
        yield return "Gandalf the Grey";
        yield return "Frodo Baggins";
        yield return "Samwise Gamgee";
    }
}
