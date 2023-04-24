using System.Collections.Generic;

namespace LooseLink
{
	public interface ITagged
	{
		IEnumerable<object> GetTags();
	}
}