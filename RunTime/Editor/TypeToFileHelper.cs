#if UNITY_EDITOR
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace LooseServices
{
static class TypeToFileHelper
{
    static readonly Dictionary<Type, Object> typeObjectDictionary = new Dictionary<Type, Object>();

    public static Object GetObject(Type type)
    {
        if (typeObjectDictionary.TryGetValue(type, out Object result))
            return result;
        
        Object obj = Find();
        typeObjectDictionary.Add(type,obj);
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
}
}
#endif