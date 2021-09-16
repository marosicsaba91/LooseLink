#if UNITY_EDITOR
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
static class TypeToFileHelper
{
    static readonly Dictionary<Type, Object> _typeObjectDictionary = new Dictionary<Type, Object>();

    public static Object GetObject(Type type)
    {
        if (_typeObjectDictionary.TryGetValue(type, out Object result))
            return result;
        
        Object obj = Find();
        _typeObjectDictionary.Add(type,obj);
        return obj;
        
        Object Find()
        {
            
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:script");
            if (guids.Length == 0) return null;

            string typeName = type.Name;
            int lastPoint = typeName.LastIndexOf('.');
            if (lastPoint >= 0) typeName = typeName.Substring(lastPoint + 1);
            
            string[] paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            string filePath = paths.FirstOrDefault(
                path => path.Substring(path.LastIndexOf('/')+1) == $"{typeName}.cs");
            if (string.IsNullOrEmpty(filePath)) return null;
 
            return AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
        }
    }

    public static Type GetType(Object obj)
    {
        var monoScript = obj as MonoScript;
        if (monoScript != null && monoScript.GetClass() != null)
            return monoScript.GetClass();
        return null;
    }
}
}
#endif