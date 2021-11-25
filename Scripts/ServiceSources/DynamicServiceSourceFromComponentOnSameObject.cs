using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{

abstract class DynamicServiceSourceFromComponentOnTheSameObject : DynamicServiceSource
{
    protected sealed override IEnumerable<Type> GetNonAbstractTypes() { yield break; }

    protected sealed override object GetServiceFromServerObject(Type type, Object serverObject) =>
        ((GameObject) serverObject).GetComponent(type);

    public sealed override object GetServiceOnSourceObject(Type type) => null;
    public sealed override string Name => "Get Component";
    public sealed override Object SourceObject => null;

}
}

public static class AAA
{

    public static void Reeesolve(this Object self)
    {
    }

}