using System.Collections.Generic;
using UnityServiceLocator;
using UnityEngine;

public class TimeProvider : ScriptableObject, ITimeProviderLongName
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
