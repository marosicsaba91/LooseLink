﻿using System;
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
    Dictionary<Type, IService> _typeToServiceOnSource;
    Dictionary<Type, ITagged> _typeToTagProviderOnSource;
 
    public Object InstantiatedObject { get; private set; } // GameObject or ScriptableObject 
    public ServiceSourceSetting setting;

    public Dictionary<Type, IService> InstantiatedServices { get; private set; } =
        new Dictionary<Type, IService>();

    public abstract Loadability GetLoadability { get; }

    public bool TryGetService(Type type, IServiceInstaller installer, object[] conditionTags, out object service, out bool newInstance)
    {
        newInstance = false;
        if (GetLoadability != Loadability.Loadable || !AllAbstractTypes.Contains(type))
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
                parentObject = installer != null && installer.GetType().IsSubclassOf(typeof(Component))
                    ? ((Component) installer).transform
                    : Services.ParentObject;
            }
            
            InstantiatedObject = Instantiate(parentObject);
            newInstance = true;
        }

        if (!InstantiatedServices.ContainsKey(type))
            InstantiatedServices.Add(type, (IService) GetService(type, InstantiatedObject));

        service = InstantiatedServices[type];
        return true;
    }

    protected abstract bool NeedParentTransform { get; }

    protected abstract Object Instantiate(Transform parent);

    protected abstract object GetService(Type type, Object instantiatedObject); 
    
    public abstract string Name { get; }

    public abstract Object SourceObject { get; }

    public IEnumerable<Type> AllNonAbstractTypes
    {
        get {
            if (_allNonAbstractTypes == null)
                Init();
            return _allNonAbstractTypes;
        }
    }

    public IEnumerable<Type> AllAbstractTypes
    {
        get
        {
            if (_allAbstractTypes == null)
                Init();
            return _allAbstractTypes;
        }
    }
    
    public IService GetServiceOnSource(Type serviceType)
    {
        if (_typeToServiceOnSource == null)
            Init();
        return _typeToServiceOnSource[serviceType];
    } 


    void Init()
    {
        Services.InitServiceTypeMap();
        _allNonAbstractTypes = GetNonAbstractTypes();
        _allAbstractTypes = new List<Type>();
        _typeToServiceOnSource = new Dictionary<Type, IService>();
        _typeToTagProviderOnSource = new Dictionary<Type, ITagged>();
        foreach (Type concreteType in _allNonAbstractTypes)
        {
            
            IService loosServiceInstanceOnSourceObject = GetServiceOnSourceObject(concreteType);
            ITagged tagProviderInstanceOnSourceObject =
                loosServiceInstanceOnSourceObject is ITagged tagged ? tagged : null;


            IEnumerable<Type> abstractTypes = Services.nonAbstractToILooseServiceTypeMap[concreteType]
                .Where(abstractType => !_allAbstractTypes.Contains(abstractType));

            foreach (Type abstractType in abstractTypes)
            {
                _allAbstractTypes.Add(abstractType);
                _typeToServiceOnSource.Add(abstractType, loosServiceInstanceOnSourceObject);
                _typeToTagProviderOnSource.Add(abstractType, tagProviderInstanceOnSourceObject);
            }
        }
    }
    
    protected abstract List<Type> GetNonAbstractTypes();
    public abstract IService GetServiceOnSourceObject(Type type);

    public abstract string NotInstantiatableReason { get; }
    public abstract bool HasProtoTypeVersion { get; }

    protected abstract void ClearService();

    public ICollection<object> GetAllTags()
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

    public IEnumerable<object> GetTagsFor(Type serviceType) => _typeToTagProviderOnSource[serviceType] == null 
            ? new Object[0] 
            :_typeToTagProviderOnSource[serviceType].GetTags();

    public void ClearInstances()
    {
        ClearService();
        InstantiatedObject = null;
        InstantiatedServices.Clear();
    }

    public abstract Texture Icon { get; }
}
}