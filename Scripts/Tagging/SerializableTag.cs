using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
public class SerializableTag : ITag
{   
    public SerializableTag( ) {}
    public SerializableTag(string objectTag) => Init(objectTag, TagType.String); 
    public SerializableTag(Object objectTag) => Init(objectTag, TagType.Object);
    public SerializableTag(object objectTag) => Init(objectTag, TagType.Other);
    
    
    void Init(object tag, TagType type)
    {
        tagType = type;
        switch (tagType)
        {
            case TagType.String:
                stringTag = (string) tag;
                break;  
            case TagType.Object:
                unityObjectTag = (Object) tag;
                break;
            case TagType.Other:
                _otherTypeTag = tag;
                break;
        }
    }

    
    public enum TagType
    {
        String, 
        Object,
        Other
    }

    [SerializeField] TagType tagType;
    
    [SerializeField] string stringTag; 
    [SerializeField] Object unityObjectTag;
    object _otherTypeTag = null;
    
    internal TagType Type
    {
        get => tagType;
        set => tagType = value;
    }

    internal string StringTag
    {
        get => stringTag;
        set
        {
            stringTag = value;
            tagType = TagType.String;
        }
    }

    internal Object UnityObjectTag  
    {
        get => unityObjectTag;
        set
        {
            unityObjectTag = value;
            tagType = TagType.Object;
        }
    }
    
    internal object OtherTypeTag
    {
        get => _otherTypeTag;
        set
        {
            _otherTypeTag = value;
            tagType = TagType.Other;
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
                case TagType.Object:
                    return unityObjectTag;
                case TagType.Other:
                    return _otherTypeTag;
                default:
                    return null;
            }
        }
    }
}
}