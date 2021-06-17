using System;
using System.Linq;
using UnityEditor; 
using UnityEngine; 

namespace LooseServices
{
[Serializable]
public class SerializableType : ISerializationCallbackReceiver
{
    [SerializeField] string typeName;
    [SerializeField] string fullTypeName;
    [SerializeField] string assemblyQualifiedName;
    [SerializeField] MonoScript monoScript;
    
    Type _type;

    public Type Type
    {
        get
        {
            if (_type != null)
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

        MonoScript[] monoScripts = Resources.FindObjectsOfTypeAll<MonoScript>();
        foreach (MonoScript scriptFile in monoScripts)
        {
            Type type = scriptFile.GetClass();
            if (type == _type)
            {
                monoScript = scriptFile;
                return;
            }
        } 
        monoScript = null;
    }

    public void OnAfterDeserialize()
    {
        _type = FindType();
    }

    Type FindType()
    {
        try
        {
            var type = Type.GetType(assemblyQualifiedName);
            if (type != null) return type;
        }
        catch (Exception)
        {
            /* ignored */
        }

        try
        {
            var type = Type.GetType(fullTypeName);
            if (type != null) return type;
        }
        catch (Exception)
        {
            /* ignored */
        }

        if (monoScript != null && monoScript.GetClass() != null)
            return monoScript.GetClass();

        return ServiceTypeHelper.allTypes.FirstOrDefault(type => type.Name == typeName);
    }
}
}