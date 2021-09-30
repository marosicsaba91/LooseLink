using UnityEngine;

namespace UnityServiceLocator
{
public abstract class InstallerComponent : MonoBehaviour, IServiceSourceProvider
{
    [SerializeField, HideInInspector] LocalInstallerPriority priority;
    [SerializeField, HideInInspector] bool dontDestroyOnLoad = false;
   
    public bool AutoDontDestroyOnLoad { 
        get => dontDestroyOnLoad;
        set => dontDestroyOnLoad = value;
    } 

    public LocalInstallerPriority Priority
    {
        get => priority;
        set
        {
            int lastValue = priority.Value;
            priority = value; 
            
            if(lastValue != priority.Value)
                ServiceLocator.Environment.SortInstallers();
        }
    }

    public int PriorityValue => priority.Value;
    public abstract bool IsSingleSourceProvider{ get; }
    public abstract void SwapSources(int index1, int index2);

    public abstract ServiceSource GetSourceAt(int index);
    public abstract int SourceCount { get; }
    public bool IsEnabled => isActiveAndEnabled;
    public abstract void AddSource(ServiceSource item);

    public abstract void ClearSources();
    public abstract bool ContainsSource(ServiceSource item);

    public abstract bool RemoveSource(ServiceSource item);

    public abstract int IndexOfSource(ServiceSource item);

    public abstract void InsertSource(int index, ServiceSource item);

    public abstract void RemoveSourceAt(int index);
    public void SetInstallationValue(int value) => priority.SetInstallationValue(value);
    
    public string Name => gameObject != null ? name : null;
    public Object ProviderObject => gameObject;
    public abstract void ClearDynamicData_NoEnvironmentChangeEvent();
    
    void Awake()
    {
        if (AutoDontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        Install();
    }

    void OnDestroy()
    {
        UnInstall();
    }

    void Install()
    {
        Priority.SetInstallationValue(ServiceLocator.Environment.MaxPriority + 1);
        ServiceLocator.Environment.TryInstallServiceSourceProvider(this);
    }

    void UnInstall() => ServiceLocator.Environment.TryUninstallServiceSourceProvider(this);

}
}