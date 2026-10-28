using System.Collections.Generic;
using MUtility;
using UnityEngine;

namespace LooseLink
{
	[DefaultExecutionOrder(order: -1000001)]
	public class LocalServiceInstaller : InstallerComponent
	{
		[SerializeField, HideInInspector] List<ServiceSource> serviceSources = new();

		public override bool InstallAutomatically => true;

		public override bool IsSingleSourceProvider => false;
		public override ServiceSource GetSourceAt(int index)
		{
			ServiceSource result = serviceSources[index];
			result.serviceSourceProvider = this;
			return result;
		}

		public override int SourceCount => serviceSources.Count;

		public override void ClearDynamicData_NoEnvironmentChangeEvent()
		{
			HashSet<ServiceSourceSet> visitedSets = new();
			foreach (ServiceSource source in serviceSources)
				source.ClearCachedTypes_NoEnvironmentChangeEvent(visitedSets);
		}

		public int ServiceSourceCount => serviceSources.Count;
		public override void AddSource(ServiceSource item)
		{
			serviceSources.Add(item);
			Services.Environment.MarkChanged();
		}

		public override void ClearSources()
		{
			if (serviceSources.Count == 0) return;
			serviceSources.Clear();
			Services.Environment.MarkChanged();
		}

		public override bool ContainsSource(ServiceSource item) => serviceSources.Contains(item);

		public override bool RemoveSource(ServiceSource item)
		{
			if (!serviceSources.Remove(item)) return false;
			Services.Environment.MarkChanged();
			return true;
		}

		public override int IndexOfSource(ServiceSource item) => serviceSources.IndexOf(item);

		public override void InsertSource(int index, ServiceSource item)
		{
			serviceSources.Insert(index, item);
			Services.Environment.MarkChanged();
		}

		public override void RemoveSourceAt(int index)
		{
			serviceSources.RemoveAt(index);
			Services.Environment.MarkChanged();
		}

		public override void SwapSources(int index1, int index2)
		{
			if (index1 == index2) return;
			serviceSources.Swap(index1, index2);
			Services.Environment.MarkChanged();
		}

	}
}
