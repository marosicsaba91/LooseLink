using System.Linq;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

#if UNITY_EDITOR 
using UnityEditor;
#endif

namespace UnityServiceLocator
{
static class TypeToFileHelper
{
    static readonly Dictionary<Type, Object> typeObjectDictionary = new Dictionary<Type, Object>();

    public static Object GetObject(Type type)
    {
        if (type == null) return null;
        
        if (typeObjectDictionary.TryGetValue(type, out Object result))
            return result;
        
        Object obj = Find();
        typeObjectDictionary.Add(type,obj);
        return obj;

        Object Find()
        {

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:script");
            if (guids.Length == 0) return null;

            string typeName = type.Name;
            int lastPoint = typeName.LastIndexOf('.');
            if (lastPoint >= 0) typeName = typeName.Substring(lastPoint + 1);

            string[] paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            string filePath = paths.FirstOrDefault(
                path => path.Substring(path.LastIndexOf('/') + 1) == $"{typeName}.cs");
            if (string.IsNullOrEmpty(filePath)) return null;

            return AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
#else
            return null;
#endif
        }
    }

    public static Type GetType(Object obj)
    {
#if UNITY_EDITOR
        var monoScript = obj as MonoScript;
        if (monoScript != null && monoScript.GetClass() != null)
            return monoScript.GetClass();
#endif
        return null;
    }
}
} 