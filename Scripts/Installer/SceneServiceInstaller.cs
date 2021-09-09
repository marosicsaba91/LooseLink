using System;
using System.Collections.Generic; 
using UnityEngine; 
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[DefaultExecutionOrder(order: -1000000)]
public class SceneServiceInstaller : MonoBehaviour, IServiceSourceSet
{
    [SerializeField] bool dontDestroyOnLoad = true;
    [SerializeField, HideInInspector] internal List<ServiceSource> serviceSources = new List<ServiceSource>();
    
    Dictionary<Type, List<Type>> _nonAbstractToServiceTypeMap;
    
    public List<ServiceSource> ServiceSources => serviceSources;
    
    public string Name => gameObject != null ? name : null;
    public Object Obj => gameObject;

    public void ClearDynamicData()
    {
        foreach (ServiceSource source in serviceSources)
            source.ClearDynamicData();
    }

    void OnEnable()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        GlobalInstall();
    }

    void OnDisable()
    {
        GlobalUnInstall();
    }
    
    public void GlobalInstall() => ServiceLocator.AddSceneContextInstaller(this);
    
    public void GlobalUnInstall() => ServiceLocator.RemoveSceneContextInstaller(this);


    public ServiceSource AddServiceSource(Object sourceObject, ServiceSourceTypes preferredType = ServiceSourceTypes.Non)
    {
        if (serviceSources == null) serviceSources = new List<ServiceSource>();
        var newServiceSource = new ServiceSource
        {
            preferredSourceType = preferredType,
            ServiceSourceObject = sourceObject
        };
        serviceSources.Add(newServiceSource);
        return newServiceSource;
    }
}
}
