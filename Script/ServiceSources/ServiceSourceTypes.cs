namespace UnityServiceLocator
{
enum ServiceSourceTypes
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