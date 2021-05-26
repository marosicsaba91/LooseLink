using UnityEngine;

namespace LooseServices
{
[DefaultExecutionOrder(order: -1000000)]
class SceneInstaller : LooseServiceInstallerComponent
{
    [SerializeField] bool dontDestroyOnLoad = true;

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