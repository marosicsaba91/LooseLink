using System;
using System.Collections.Generic; 
using UnityEngine; 
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[DefaultExecutionOrder(order: -1000000)]
public class SceneServiceInstaller : MonoBehaviour, IServiceSourceSet
{
    [SerializeField, HideInInspector] internal bool dontDestroyOnLoad = false;
    [SerializeField, HideInInspector] List<ServiceSource> serviceSources = new List<ServiceSource>();
    [SerializeField, HideInInspector] int priority = 0;
    [SerializeField, HideInInspector] PriorityTypeEnum priorityType;
    
    internal int priorityAtInstallation = 0;
    Dictionary<Type, List<Type>> _nonAbstractToServiceTypeMap;
    
    public List<ServiceSource> ServiceSources => serviceSources;
    public enum PriorityTypeEnum { HighestAtInstallation, ConcreteValue }

    public int Priority
    {
        get => priorityType == PriorityTypeEnum.ConcreteValue ? priority : priorityAtInstallation;
        set
        {
            if(priority == value) return;
            priority = value;
            priorityType = PriorityTypeEnum.ConcreteValue;
            ServiceLocator.Environment.SortInstallers();
        }
    }

    public PriorityTypeEnum PriorityType
    {
        get => priorityType;
        set
        {
            if(value == priorityType) return;
            priorityType = value;
            ServiceLocator.Environment.SortInstallers();
        }
    }

    public string Name => gameObject != null ? name : null;
    public Object Obj => gameObject;
        
    public void ClearDynamicData()
    {
        foreach (ServiceSource source in serviceSources)
            source.ClearDynamicData_NoSourceChange(); 
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

    void GlobalInstall()
    {
        priorityAtInstallation = ServiceLocator.Environment.MaxPriority + 1;
        ServiceLocator.Environment.TryInstallServiceSourceSet(this);
    }

    void GlobalUnInstall() => ServiceLocator.Environment.TryUninstallServiceSourceSet(this);
}
}
