using LooseServices;
using UnityEngine;

[CreateAssetMenu(fileName = "Tag", menuName = "Loose Link/Tag File")]
public class TagFile : ScriptableObject, ITag
{
    [SerializeField] Color color;
    public Color Color => color;
    public string Name => name;
}
