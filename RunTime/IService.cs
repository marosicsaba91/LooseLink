using System;
using System.Collections.Generic; 
using MUtility;

namespace LooseServices
{
public interface IService
{
	// Is called after Service was requested the first time. Could be before Awake.
	// Other Service locations should be called here.
	void Initialize();
}

public static class LooseServiceHelper
{
	public static IEnumerable<object> GetTags(this IService service)
	{
		if (service == null) yield break;
		Type nonAbstractType = service.GetType();
		if (!nonAbstractType.GetInterfaces().Contains(typeof(ITagged))) yield break;

		var tagged = (ITagged) service;
		foreach (object tag in tagged.GetTags())
			if (tag != null)
				yield return tag;
	}
}
}