using System;
using System.Linq; 
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
public class SerializableType : ISerializationCallbackReceiver
{
    [SerializeField] string typeName;
    [SerializeField] string fullTypeName;
    [SerializeField] string assemblyQualifiedName;
    [SerializeField] Object monoScript;

    bool _typeIsSet = false;
    Type _type;

    public Type Type
    {
        get
        {
            if (_type != null || _typeIsSet)
                return _type;

            _type = FindType();
            
            return _type;
        }
        set
        {
            _type = value; 
            UpdateSerializedValues();
        }
    }

    public string Name => typeName;

    public void OnBeforeSerialize()
    {
        if(_type == null) return;
        UpdateSerializedValues();
    }


    void UpdateSerializedValues()
    {
        assemblyQualifiedName = _type?.AssemblyQualifiedName;
        fullTypeName = _type?.FullName;
        typeName = _type?.Name;
        monoScript = TypeToFileHelper.GetObject(_type);
    }
    

    public void OnAfterDeserialize()
    {
        _type = FindType();
    }

    Type FindType()
    {
        Type type;
        _typeIsSet = true;
        try
        {
            type = Type.GetType(assemblyQualifiedName);
            if (type != null) return type;
        }
        catch (Exception)
        {
            /* ignored */
        }

        type = TypeToFileHelper.GetType(monoScript);
        if (type != null) return type;
        
        try
        {
            type = Type.GetType(fullTypeName);
            if (type != null) return type;
        }
        catch (Exception)
        {
            /* ignored */
        }
      
        type = ServiceTypeHelper.allTypes.FirstOrDefault(t => t.Name == typeName);
        return type;
    }
}
}