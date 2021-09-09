using System;
using UnityEngine;

namespace UnityServiceLocator
{
public class DefaultTag : ITag
{
    public readonly object obj; 
    public string Name => 
        obj == null ? "null" :
        obj is UnityEngine.Object o ? o.name
        : obj.ToString();
    public object TagObject => obj;
    public Type ObjectType =>  obj?.GetType();

    public DefaultTag(object obj)
    {
        this.obj = obj;
    }
}
}