using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{

abstract class DynamicServiceSourceFromGO : DynamicServiceSource
{
    public readonly GameObject gameObject;
    
    internal DynamicServiceSourceFromGO(GameObject gameObject)
    { 
        this.gameObject = gameObject;
    }
    
    protected sealed override IEnumerable<Type> GetNonAbstractTypes() =>
        gameObject?.GetComponents<Component>()
            .Where(component => component!= null)
            .Select(component => component.GetType());

    protected sealed override object GetServiceFromServerObject(Type type, Object serverObject) =>
        ((GameObject) serverObject).GetComponent(type);

    public sealed override object GetServiceOnSourceObject(Type type) => gameObject.GetComponent(type);

    public sealed override string Name => gameObject != null ? gameObject.name : string.Empty;
    public sealed override Object SourceObject => gameObject;
 
}
}