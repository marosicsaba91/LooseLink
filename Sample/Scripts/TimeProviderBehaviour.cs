using System.Collections.Generic;
using LooseServices;
using UnityEngine;

public class TimeProviderBehaviour : MonoBehaviour, ITimeProvider, ITagged
{
    float _time = 0;

    void Update()
    {
        _time += Time.deltaTime;
    }

    public float GetTime => _time;


    [SerializeField] TagFile[] tags;
    public IEnumerable<object> GetTags()
    {
        yield return name;
        yield return 1;
        if(tags == null) yield break;
        foreach (TagFile tagFile in tags)
            yield return tagFile;
    }
    
    public bool EnableCustomTags => true;
}
