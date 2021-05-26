using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LooseServices
{
public class DefaultTag : ITag
{
    public readonly object obj;
    public Color Color => GetNieColorByHash(obj);
    public string Name => obj == null? "null" : obj.ToString();
    public Type ObjectType =>  obj?.GetType();

    public DefaultTag(object obj)
    {
        this.obj = obj;
    }

    public static Color GetNieColorByHash(object tagObject)
    {
        if(tagObject == null) return new Color(0.75f, 0.75f, 0.75f);
        int hash = tagObject.GetHashCode();
        Random.InitState(hash);
        float  randomNum = Random.Range(0,1f);
        return TagHelper.GetRandomNiceColorByRandomFloat(randomNum);
    }  
}
}