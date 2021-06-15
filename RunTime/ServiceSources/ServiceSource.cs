using System;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseServices
{

[Serializable]
abstract class ServiceSource
{
    List<Type> _allNonAbstractTypes;
    List<Type> _allAbstractTypes;
    Dictionary<Type, object> _typeToServiceOnSource;
    Dictionary<Type, ITagged> _typeToTagProviderOnSource; 
 
    public Object InstantiatedObject { get; private set; } // GameObject or ScriptableObject

    public Dictionary<Type, object> InstantiatedServices { get; private set; } =
        new Dictionary<Type, object>();

    public abstract Loadability Loadability { get; }

    public bool TryGetService(Type type, IServiceSourceSet set, object[] conditionTags, out object service, out bool newInstance)
    {
        newInstance = false;
        Loadability loadability = Loadability;
        if (loadability.type != Loadability.Type.Loadable || !GetAllAbstractTypes(set).Contains(type))
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

    public IEnumerable<Type> GetAllNonAbstractTypes(IServiceSourceSet set)
    {
        if (_allNonAbstractTypes == null)
            Init(set);
        return _allNonAbstractTypes;
    }

    public IEnumerable<Type> GetAllAbstractTypes(IServiceSourceSet set)
    {
        if (_allAbstractTypes == null)
            Init(set);
        return _allAbstractTypes;
    }

    public object GetServiceOnSource(IServiceSourceSet set, Type serviceType)
    {
        if (_typeToServiceOnSource == null)
            Init(set);
        return _typeToServiceOnSource[serviceType];
    } 


    void Init(IServiceSourceSet set)
    { 
        _allNonAbstractTypes = GetNonAbstractTypes(set);
        _allAbstractTypes = new List<Type>();
        _typeToServiceOnSource = new Dictionary<Type, object>();
        _typeToTagProviderOnSource = new Dictionary<Type, ITagged>();
        foreach (Type concreteType in _allNonAbstractTypes)
        {
            
            object loosServiceInstanceOnSourceObject = GetServiceOnSourceObject(concreteType);
            ITagged tagProviderInstanceOnSourceObject =
                loosServiceInstanceOnSourceObject is ITagged tagged ? tagged : null;


            IEnumerable<Type> abstractTypes = set.ServiceTypeProvider.GetServicesOfNonAbstractType(concreteType)
                .Where(abstractType => !_allAbstractTypes.Contains(abstractType));

            foreach (Type abstractType in abstractTypes)
            {
                _allAbstractTypes.Add(abstractType);
                _typeToServiceOnSource.Add(abstractType, loosServiceInstanceOnSourceObject);
                _typeToTagProviderOnSource.Add(abstractType, tagProviderInstanceOnSourceObject);
            }
        }
    }
    
    protected abstract List<Type> GetNonAbstractTypes(IServiceSourceSet set);
    public abstract object GetServiceOnSourceObject(Type type);
 
    public abstract ServiceSourceTypes SourceType { get; }
    public abstract IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get; }

    protected abstract void ClearService();

    public ICollection<object> GetAllTags(IServiceSourceSet set)
    {

        if (_typeToTagProviderOnSource == null)
            Init(set);

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

    public IEnumerable<object> GetTagsFor(Type serviceType) => _typeToTagProviderOnSource[serviceType] == null 
            ? new Object[0] 
            :_typeToTagProviderOnSource[serviceType].GetTags();

    public void ClearInstances()
    {
        ClearService();
        InstantiatedObject = null;
        InstantiatedServices.Clear();
    }

    public Texture Icon => FileIconHelper.GetIconOfObject(SourceObject);
}
}