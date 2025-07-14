using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace LooseLink
{
	public interface IServiceSourceProvider
	{
		string Name { get; }
		void ClearDynamicData_NoEnvironmentChangeEvent();
		int PriorityValue { get; }
		InstallerPriority.Type PriorityType { get; }
		bool IsSingleSourceProvider { get; }

		int SourceCount { get; }
		Object ProviderObject { get; }
		bool IsEnabled { get; }

		void AddSource(ServiceSource item);

		void ClearSources();

		bool ContainsSource(ServiceSource item);

		bool RemoveSource(ServiceSource item);

		int IndexOfSource(ServiceSource item);

		void RemoveSourceAt(int index);

		void SwapSources(int index1, int index2);
		ServiceSource GetSourceAt(int index);

	}

	public static class ServiceSourceProviderHelper
	{
		// TODO ALLOC: Use TryGetSources method instead
		public static void CollectAllEnabled(this IServiceSourceProvider provider, List<(IServiceSourceProvider, ServiceSource)> result)
		{
			for (int i = 0; i < provider.SourceCount; i++)
			{
				ServiceSource serviceSource = provider.GetSourceAt(i);
				if (!serviceSource.Enabled)
					continue;
				if (serviceSource.IsServiceSource)
					result.Add((provider, serviceSource));
				else if (serviceSource.IsSourceSet)
				{
					ServiceSourceSet subSet = serviceSource.GetServiceSourceSet();
					if (subSet != null && !subSet.automaticallyUseAsGlobalInstaller)
						subSet.CollectAllEnabled(result);
				}
			}
		}

		public static void CollectAllEnabled(this IServiceSourceProvider provider, List<ServiceSource> result)
		{
			for (int i = 0; i < provider.SourceCount; i++)
			{
				ServiceSource serviceSource = provider.GetSourceAt(i);
				if (!serviceSource.Enabled)
					continue;
				if (serviceSource.IsServiceSource)
					result.Add( serviceSource);
				else if (serviceSource.IsSourceSet)
				{
					ServiceSourceSet subSet = serviceSource.GetServiceSourceSet();
					if (subSet != null && !subSet.automaticallyUseAsGlobalInstaller)
						subSet.CollectAllEnabled(result);
				}
			}
		}


		public static bool TryGetFirstSources(this IServiceSourceProvider provider, Func<ServiceSource, bool> filter, out ServiceSource result)
		{
			for (int i = 0; i < provider.SourceCount; i++)
			{
				ServiceSource serviceSource = provider.GetSourceAt(i);
				if (!serviceSource.Enabled)
					continue;
				if (serviceSource.IsServiceSource)
				{
					if (filter(serviceSource))
					{
						result = serviceSource;
						return true;
					}
				}
				else if (serviceSource.IsSourceSet)
				{
					ServiceSourceSet subSet = serviceSource.GetServiceSourceSet();
					if (subSet != null && !subSet.automaticallyUseAsGlobalInstaller && subSet.TryGetFirstSources(filter, out result))
						return true;
				}
			}

			result = null;
			return false;
		}

		internal static IEnumerable<ServiceSource> GetSources(this IServiceSourceProvider provider)
		{
			for (int i = 0; i < provider.SourceCount; i++)
				yield return provider.GetSourceAt(i);
		}

		public static ServiceSource AddSource(
			this IServiceSourceProvider provider,
			Object sourceObject,
			ServiceSourceTypes preferredType = ServiceSourceTypes.Non)
		{
			ServiceSource newServiceSource = new(sourceObject, provider, preferredType);
			provider.AddSource(newServiceSource);
			return newServiceSource;
		}
	}
}