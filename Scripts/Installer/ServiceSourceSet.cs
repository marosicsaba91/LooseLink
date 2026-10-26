using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{
	[CreateAssetMenu(fileName = "New Service Source Set", menuName = "Service Source Set")]
	public class ServiceSourceSet : ScriptableObject, IServiceSourceProvider
	{
		[SerializeField, HideInInspector] internal bool automaticallyUseAsGlobalInstaller = false;
		[SerializeField, HideInInspector] List<ServiceSource> serviceSources = new();
		[SerializeField, HideInInspector] int priority = 0;

		public InstallerPriority.Type PriorityType => InstallerPriority.Type.ConcreteValue;
		public bool IsSingleSourceProvider => false;

		public ServiceSource GetSourceAt(int index)
		{
			ServiceSource result = serviceSources[index];
			result.serviceSourceProvider = this;
			return result;
		}

		public int PriorityValue
		{
			get => priority;
			set
			{
				if (priority == value)
					return;
				priority = value;
				Services.Environment.SortInstallers();
			}
		}

		public string Name => name;
		public Object ProviderObject => this;

		public bool IsEnabled => true;

		public void ClearDynamicData_NoEnvironmentChangeEvent() =>
			ClearDynamicData_NoEnvironmentChangeEvent(new HashSet<ServiceSourceSet>());

		internal void ClearDynamicData_NoEnvironmentChangeEvent(HashSet<ServiceSourceSet> visitedSets)
		{
			if (!visitedSets.Add(this)) return;
			serviceSources = serviceSources ?? new List<ServiceSource>();
			foreach (ServiceSource source in serviceSources)
				source.ClearCachedTypes_NoEnvironmentChangeEvent(visitedSets);
		}

		public void ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent() =>
			ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent(new HashSet<ServiceSourceSet>());

		internal void ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent(HashSet<ServiceSourceSet> visitedSets)
		{
			if (!visitedSets.Add(this)) return;
			serviceSources = serviceSources ?? new List<ServiceSource>();
			foreach (ServiceSource source in serviceSources)
				source.ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent(visitedSets);
		}

		internal void DestroyOwnedInstances_NoEnvironmentChangeEvent() =>
			DestroyOwnedInstances_NoEnvironmentChangeEvent(new HashSet<ServiceSourceSet>());

		internal void DestroyOwnedInstances_NoEnvironmentChangeEvent(HashSet<ServiceSourceSet> visitedSets)
		{
			if (!visitedSets.Add(this)) return;
			serviceSources ??= new List<ServiceSource>();
			foreach (ServiceSource source in serviceSources)
				source.DestroyOwnedInstances_NoEnvironmentChangeEvent(visitedSets);
		}

		internal bool IsInResources() =>
				Resources.LoadAll<ServiceSourceSet>(string.Empty).Any(so => so == this);

		public static bool IsCircular(ServiceSourceSet set1, ServiceSourceSet set2)
		{
			if (set1.ContainsSet(set2))
				return true;
			if (set2.ContainsSet(set1))
				return true;
			return false;
		}

		bool ContainsSet(ServiceSourceSet set) =>
			ContainsSet(set, new HashSet<ServiceSourceSet>());

		bool ContainsSet(ServiceSourceSet set, HashSet<ServiceSourceSet> visitedSets)
		{
			if (this == set)
				return true;
			if (!visitedSets.Add(this))
				return false;

			foreach (ServiceSource setting in serviceSources)
				if (setting.ServiceSourceObject is ServiceSourceSet child)
					if (child.ContainsSet(set, visitedSets))
						return true;

			return false;
		}

		public int SourceCount => serviceSources.Count;
		public void AddSource(ServiceSource item) => serviceSources.Add(item);

		public void ClearSources() => serviceSources.Clear();

		public bool ContainsSource(ServiceSource item) => serviceSources.Contains(item);

		public bool RemoveSource(ServiceSource item) => serviceSources.Remove(item);

		public int IndexOfSource(ServiceSource item) => serviceSources.IndexOf(item);

		public void InsertSource(int index, ServiceSource item) => serviceSources.Insert(index, item);

		public void RemoveSourceAt(int index) => serviceSources.RemoveAt(index);
		public void SwapSources(int index1, int index2) => serviceSources.Swap(index1, index2);

		public ServiceSource this[int index]
		{
			get => serviceSources[index];
			set => serviceSources[index] = value;
		}

	}
}
