using System;
using System.Text;
using UnityEngine;
using MUtility; 
using Object = UnityEngine.Object;

namespace UnityServiceLocator
{
[Serializable]
public class Tag
{   
    public Tag( )  => Init("New Tag", TagType.String); 
    public Tag(string objectTag) => Init(objectTag, TagType.String); 
    public Tag(Object objectTag) => Init(objectTag, TagType.Object);
    public Tag(object objectTag) => Init(objectTag, TagType.Other);
    
    
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

    public string Name    
    {
        get
        {
            switch (tagType)
            {
                case TagType.String:
                    return stringTag ?? "null";
                case TagType.Object:
                    return unityObjectTag == null ? "null" : unityObjectTag.name;
                case TagType.Other:
                    return _otherTypeTag == null ? "null" : _otherTypeTag.ToString();
                default:
                    return null;
            }
        }
    }

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

    public Type GetObjectType()
    {
        switch (tagType)
        {
            case TagType.String:
                return typeof(string); 
            case TagType.Object:
                return typeof(Object); 
            case TagType.Other:
                return _otherTypeTag?.GetType();
            default:
                return null;
        }
    }
    
    public string TextWithType() => $"{Name} ({GetObjectType()})";
    
    
    public string ShortText(float width)
    {
        const int maxCharacterWidth = 9;
        string text = (TagObject == null ? "null" : Name) ?? "null";
        var maxCharacterCount = (int) (width / maxCharacterWidth);
        if (maxCharacterCount >= text.Length) return text;

        string firstOrUpperLetters = FirstOrUpperLetters(text);

        return firstOrUpperLetters.Substring(0, Mathf.Min(maxCharacterCount, firstOrUpperLetters.Length));
    }

    static string FirstOrUpperLetters(string input)
    { 
        var separators = new[] { ' ','-', ',', '.' };
        var result = new StringBuilder(input.Length);
        var makeUpperNext = true;
        foreach (char c in input)
        {
            if (makeUpperNext)
            {
                if (char.IsNumber(c) || char.IsLetter(c))
                {
                    result.Append(char.ToUpper(c));
                    makeUpperNext = false;
                }
            }
            else if(char.IsNumber(c) || char.IsUpper(c))
                result.Append(c);
            

            if (separators.Contains(c))
                makeUpperNext = true;
        }

        return result.ToString();
    }
    
    /*
    public static Color GetNieColorByHash(object tagObject)
    {
        if(tagObject == null) return new Color(0.75f, 0.75f, 0.75f);
        int hash = tagObject.GetHashCode();
        Random.InitState(hash);
        float  randomNum = Random.Range(0,1f);
        return GetRandomNiceColorByRandomFloat(randomNum);
    } 

    static Color GetRandomNiceColorByRandomFloat(float randomNum)
    {
        var reddish = new Color(1f, 0.45f, 0.39f);
        var yellowish = new Color(0.98f, 0.79f, 0.2f);
        var greenish = new Color(0.71f, 0.89f, 0.36f);
        var blueish = new Color(0.36f, 0.76f, 0.89f);
        var purplish = new Color(0.72f, 0.56f, 0.98f);
        Color[] colors = {reddish, yellowish, greenish, blueish, purplish};
        
        
        var index = (int)(randomNum / (1f/colors.Length));
        float insideRandomNum = randomNum % (1f/colors.Length) * colors.Length;

        
        Color colorA = colors[index];
        Color colorB = colors[(index + 1) % colors.Length]; 

        return Color.LerpUnclamped(colorA, colorB, insideRandomNum);
    }
    */
}
}
