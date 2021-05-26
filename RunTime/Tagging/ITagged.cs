using System.Collections.Generic;

namespace LooseServices
{
public interface ITagged
{
    IEnumerable<object> GetTags();
}
}