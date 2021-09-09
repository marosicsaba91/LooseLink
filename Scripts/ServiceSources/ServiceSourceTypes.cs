namespace UnityServiceLocator
{
public enum ServiceSourceTypes
{
    Non,
    
    FromSceneGameObject,
    FromPrefabFile,
    FromPrefabPrototype,

    FromScriptableObjectFile,
    FromScriptableObjectPrototype,

    FromMonoBehaviourType,
    FromScriptableObjectType,
}
}