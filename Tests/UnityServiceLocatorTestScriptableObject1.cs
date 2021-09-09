using System.Collections.Generic;
using UnityEngine;
using UnityServiceLocator;

[ServiceType]
public class UnityServiceLocatorTestScriptableObject1 : ScriptableObject, ITagged
{
    internal const int testTag1 = 789;
    internal const string testTag2 = "I hate snakes!";
    public IEnumerable<object> GetTags()
    {
        yield return testTag1;
        yield return testTag2;
    }
}
