using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LooseServices
{
public static class TagHelper
{
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

    public static string ShortText(this ITag tag, float width)
    {
        const int maxCharacterWidth = 10;
        string text = (tag == null ? "null" : tag.Name) ?? "null";
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
    
    
    public static ITag ToITag(this object tagObject)
    { 
        if (tagObject is ITag t)
            return t;
        return new DefaultTag(tagObject); 
    }

    public static Type ObjectType(this ITag tag) => tag is DefaultTag dt ? dt.ObjectType : tag.GetType();

    public static string TextWithType(this ITag tag) => $"{tag.Name} ({tag.ObjectType()})";
}
}