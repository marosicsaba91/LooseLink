using System;
using System.Collections.Generic;
using MUtility;
using UnityEngine;
using UnityEngine.Serialization;

namespace LooseServices
{
[CreateAssetMenu(fileName = "Service Source Set", menuName = "Loose Link/Service Source Set", order = 1)]
class ServiceSourceSet : ScriptableObject
{
	[SerializeField] bool isGlobalInstaller;
	[SerializeField] FreshButton fresh;
	[FormerlySerializedAs("systemSources")] [SerializeField] List<ServiceSourceSetting> serviceSources = default; 

	internal IEnumerable<ServiceSource> GetServiceSources()
	{
		foreach (ServiceSourceSetting serviceSourceSetting in serviceSources)
		foreach (ServiceSource serviceSource in serviceSourceSetting.GetServiceSources())
			yield return serviceSource;
	}

	
	internal void Fresh()
	{
		foreach (ServiceSourceSetting sourceSetting in serviceSources)
			sourceSetting.Clear();
	}
    
	public static bool IsCircular(ServiceSourceSet set1, ServiceSourceSet set2)
	{
		if (set1.Contains(set2)) return true;
		if (set2.Contains(set1)) return true;
		return false;
	}

	bool Contains(ServiceSourceSet set)
	{
		if (this == set) return true;
		
		foreach (ServiceSourceSetting setting in serviceSources)
			if (setting.serviceSourceObject is ServiceSourceSet child)
				if (child.Contains(set))
					return true;
		
		return false;
	}
	
	
	[Serializable] class FreshButton :InspectorButton<ServiceSourceSet>
	{
		protected override void OnClick(ServiceSourceSet obj) => obj.Fresh();
		protected override string Text(ServiceSourceSet obj, string original) => "Clear Loaded Instances & Fresh";
	}

}
}