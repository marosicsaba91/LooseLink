using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{
 
abstract class DynamicServiceSource
{
    List<Type> _allNonAbstractTypes;
    List<Type> _allAbstractTypes;
    List<Type> _possibleAdditionalTypes;
    Dictionary<Type, object> _typeToServiceOnSource;
    Dictionary<Type, ITagged> _typeToTagProviderOnSource;
    ServiceSource _setting; 
 
    public Object InstantiatedObject { get; private set; } // GameObject or ScriptableObject

    public Dictionary<Type, object> InstantiatedServices { get; private set; } =
        new Dictionary<Type, object>();

    public abstract Loadability Loadability { get; }

    public bool TryGetService(
        Type type,
        IServiceSourceSet set,
        object[] conditionTags, 
        out object service,
        out bool newInstance)
    {
        newInstance = false;
        Loadability loadability = Loadability;
        if (loadability.type != Loadability.Type.Loadable)
        {
            service = default;
            return false;
        }

        if (conditionTags!= null && conditionTags.Length>0) 
        {
            ITagged tagged = _typeToTagProviderOnSource[type];
            bool success = tagged != null;
            if (success)
            {
                if (conditionTags.Any(tag => tag != null && !tagged.GetTags().Contains(tag)))
                    success = false;
            }

            if (!success)
            {
                service = default;
                return false;
            }
        }

        if (InstantiatedObject == null)
        {
            Transform parentObject = null;

            if (NeedParentTransform)
            {
                parentObject = set != null && set.GetType().IsSubclassOf(typeof(Component))
                    ? ((Component) set).transform
                    : Services.ParentObject;
            }
            
            InstantiatedObject = Instantiate(parentObject);
            newInstance = true;
        }

        if (!InstantiatedServices.ContainsKey(type))
            InstantiatedServices.Add(type,  GetService(type, InstantiatedObject));

        service = InstantiatedServices[type];
        return true;
    }

    protected abstract bool NeedParentTransform { get; }

    protected abstract Object Instantiate(Transform parent);

    protected abstract object GetService( Type type, Object instantiatedObject); 
    
    public abstract string Name { get; }

    public abstract Object SourceObject { get; }

    public IReadOnlyList<Type> GetAllNonAbstractTypes()
    {
        if (_allNonAbstractTypes == null)
            Init();
        return _allNonAbstractTypes;
    }

    public IReadOnlyList<Type> GetAllAbstractTypes()
    {
        if (_allAbstractTypes == null)
            Init();
        return _allAbstractTypes;
    }

    public IReadOnlyList<Type> GetPossibleAdditionalTypes()
    {        
        if (_possibleAdditionalTypes == null)
            Init();
        return _possibleAdditionalTypes;
    }

    public object GetServiceOnSource(Type serviceType)
    {
        if (_typeToServiceOnSource == null)
            Init();
        return _typeToServiceOnSource[serviceType];
    } 


    void Init()
    { 
        _allNonAbstractTypes = GetNonAbstractTypes();
        _allAbstractTypes = new List<Type>();
        _typeToServiceOnSource = new Dictionary<Type, object>();
        _typeToTagProviderOnSource = new Dictionary<Type, ITagged>();
        _possibleAdditionalTypes = new List<Type>(); 
        foreach (Type concreteType in _allNonAbstractTypes)
        { 
            object loosServiceInstanceOnSourceObject = GetServiceOnSourceObject(concreteType);
            ITagged tagProviderInstanceOnSourceObject =
                loosServiceInstanceOnSourceObject is ITagged tagged ? tagged : null;


            IEnumerable<Type> abstractTypes = ServiceTypeHelper.GetServicesOfNonAbstractType(concreteType)
                .Where(abstractType => !_allAbstractTypes.Contains(abstractType));

            foreach (Type abstractType in abstractTypes)
            {
                _allAbstractTypes.Add(abstractType);
                _typeToServiceOnSource.Add(abstractType, loosServiceInstanceOnSourceObject);
                _typeToTagProviderOnSource.Add(abstractType, tagProviderInstanceOnSourceObject);
            }
            
            
            foreach (Type subclass in AllPossibleAdditionalSubclassesOf(concreteType))
                _possibleAdditionalTypes.Add(subclass);
        }
    }

    IEnumerable<Type> AllPossibleAdditionalSubclassesOf(Type type, bool includeInterfaces = true )
    {        
        if(type == null)
            yield break;
        
        yield return type;
        if (includeInterfaces)
        {
            foreach (Type interfaceType in type.GetInterfaces())
                if (interfaceType != typeof(ITagged) && interfaceType != typeof(IInitializable))
                    yield return interfaceType;
        }

        Type baseType = type.BaseType;
        if(baseType == null ||
           baseType == typeof(ScriptableObject) ||
           baseType == typeof(Component) ||
           baseType == typeof(Behaviour) ||
           baseType == typeof(MonoBehaviour))
            yield break;
        
        foreach (Type b in AllPossibleAdditionalSubclassesOf(baseType, includeInterfaces: false ))
            yield return b;
    }

    protected abstract List<Type> GetNonAbstractTypes();
    public abstract object GetServiceOnSourceObject(Type type);
 
    public abstract ServiceSourceTypes SourceType { get; }
    public abstract IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get; }

    protected abstract void ClearService();

    public ICollection<object> GetDynamicTags()
    {

        if (_typeToTagProviderOnSource == null)
            Init();

        var tags = new List<object>();
        foreach (KeyValuePair<Type, ITagged> pair in _typeToTagProviderOnSource)
            if (pair.Value != null)
            {
                foreach (object tag in pair.Value.GetTags())
                    if (!tags.Contains(tag))
                        tags.Add(tag);
            }

        return tags;
    }

    public void ClearInstances()
    {
        ClearService();
        InstantiatedObject = null;
        InstantiatedServices.Clear();
    }

    public Texture Icon => FileIconHelper.GetIconOfObject(SourceObject);
}
}