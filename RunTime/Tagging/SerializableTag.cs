using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
public class SerializableTag : ITag
{
    public enum TagType
    {
        String,
        TagFile,
        Object
    }

    [SerializeField] TagType tagType;
    
    [SerializeField] string stringTag;
    [SerializeField] TagFile tagFile;
    [SerializeField] Object objectTag;
    
    internal TagType Type
    {
        get => tagType;
        set => tagType = value;
    }

    internal string StringTag
    {
        get => stringTag;
        set => stringTag = value;
    }

    internal TagFile TagFile
    {
        get => tagFile;
        set => tagFile = value;
    }

    internal Object ObjectTag  
    {
        get => objectTag;
        set => objectTag = value;
    }
    

    [SerializeField] bool initialized = false;
    [SerializeField] Color customColor;

    public Color Color
    {
        get
        {
            if(TagObject == null)
                return Color.black;

            if (initialized) 
                return customColor;
            
            customColor = TagHelper.GetNieColorByHash(TagObject);
            initialized = true;
            return customColor;
        }
    }

    
    public string Name => TagObject.ToString();

    public object TagObject
    {
        get
        {
            switch (tagType)
            {
                case TagType.String:
                    return stringTag;
                case TagType.TagFile:
                    return tagFile;
                case TagType.Object:
                    return objectTag;
                default:
                    return null;
            }
        }
    }
}
}