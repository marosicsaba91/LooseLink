using System;
using System.Collections.Generic; 
using UnityEngine; 
using Object = UnityEngine.Object;

namespace LooseServices
{
[DefaultExecutionOrder(order: -1000000)]
class SceneServiceSet : MonoBehaviour, IServiceSourceSet
{ 
    [SerializeField] ServiceTypeProvider typeProvider;
    [SerializeField] bool dontDestroyOnLoad = true;
    [SerializeField, HideInInspector] internal List<ServiceSourceSetting> serviceSources = default; 

    Dictionary<Type, List<Type>> _nonAbstractToServiceTypeMap;
    
    public List<ServiceSourceSetting> GetServiceSourceSettings() => serviceSources;

    IEnumerable<ServiceSource> IServiceSourceSet.GetServiceSources()
    {
        if (serviceSources == null) yield break;
        foreach (ServiceSourceSetting serviceSourceSetting in serviceSources)
            if (serviceSourceSetting.enabled)
                foreach (ServiceSource serviceSource in serviceSourceSetting.GetServiceSources(this))
                    yield return serviceSource;
    }
    
    public IServiceTypeProvider ServiceTypeProvider => typeProvider.SelfOrDefault();

    public string Name => gameObject != null ? name : null;
    public Object Obj => gameObject;

    public void Fresh()
    {
        foreach (ServiceSourceSetting sourceSetting in serviceSources)
            sourceSetting.Clear();
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
    
    public void GlobalInstall() => Services.AddSceneContextInstaller(this);
    
    public void GlobalUnInstall() => Services.RemoveSceneContextInstaller(this);
    
    
 
}
}