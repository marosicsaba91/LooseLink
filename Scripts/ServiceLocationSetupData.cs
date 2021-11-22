using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine; 

namespace UnityServiceLocator
{
public class ServiceLocationSetupData : ScriptableObject
{ 
    [SerializeField] ErrorMessage errorMessage; 
    [SerializeField] SetupTimeMessage lastTypeMapSetupTime;
    public bool setupTypeMapAtPlayInEditor;
    public bool setupTypeMapAtStartInBuild = true;
    // [Space]
    // public bool enableTags = false;

    public bool IsDefault { get; private set; } = false;

    static ServiceLocationSetupData[] _allInstances = null; 
    static ServiceLocationSetupData _instance = null;
    
    public static ServiceLocationSetupData Instance
    {
        get
        {
            if (Application.isEditor)
            {
                _allInstances = Resources.LoadAll<ServiceLocationSetupData>(string.Empty);
                if (!_allInstances.IsNullOrEmpty())
                    return _allInstances.First();
            }
            else if (_allInstances == null)
            {
                _allInstances = Resources.FindObjectsOfTypeAll<ServiceLocationSetupData>();
                _instance = _allInstances.FirstOrDefault();
            }

            if (_instance == null)
            {
                _instance = CreateInstance<ServiceLocationSetupData>();
                _instance.IsDefault = true;
            }

            return _instance;
        }
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    internal static void UpdateGlobalInstallers()
    {
        ServiceLocationSetupData instance = Instance;
        if (Application.isEditor && !instance.setupTypeMapAtPlayInEditor) return;
        if (!Application.isEditor && !instance.setupTypeMapAtStartInBuild) return;
        ServiceLocator.Init();
    }

    public static bool AreMultipleSetupInstances
    {
        get
        {
            if (_allInstances == null)
                _allInstances = Resources.FindObjectsOfTypeAll<ServiceLocationSetupData>();
            return _allInstances.Length > 1;
        }
    }

    internal bool IsInResources() => 
        Resources.LoadAll<ServiceLocationSetupData>(string.Empty).Any(so => so == this);
    
    [Serializable]
    class ErrorMessage : InspectorMessage<ServiceLocationSetupData>
    {
        protected override string Text(ServiceLocationSetupData parentObject, string originalLabel) =>
            !parentObject.IsInResources() 
                ? "ServiceLocationSetupData only works in a Resources folder!" 
                : "There are more than one ServiceLocationSetupData instance!";
        
        protected override InspectorMessageType MessageType(ServiceLocationSetupData parentObject) => 
            InspectorMessageType.Error;

        protected override bool IsVisible(ServiceLocationSetupData parentObject) =>
            !parentObject.IsDefault && (!parentObject.IsInResources() || AreMultipleSetupInstances);
    }
    
    [Serializable]
    class SetupTimeMessage : InspectorMessage<ServiceLocationSetupData>
    {
        protected override IEnumerable<string> GetLines(ServiceLocationSetupData parentObject)
        {
            yield return "Service Locator needs to setup a Type-Map one to operate fast after.";
            yield return "This process takes relatively long time, so it can cause a noticeable hiccup.";
            yield return "Last Type-Map setup was:  " + ServiceLocator.SetupTime.Milliseconds + " ms.";
            yield return "You can choose to do this at the start of the software or at the first Resolve call.";
        }

        protected override InspectorMessageType MessageType(ServiceLocationSetupData parentObject) => 
            InspectorMessageType.Info;
    }
}
}