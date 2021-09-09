using System.Collections.Generic;

namespace UnityServiceLocator
{
public interface ITagged
{
    IEnumerable<object> GetTags(); 
}
}