using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LooseLink
{
	[NoAutoStaticsCleanup]
	public class ServiceEnvironment
	{
		readonly List<IServiceSourceProvider> _installers = new();

		// INSTALL & UNINSTALL SETS ---------------

		public bool TryInstallServiceSourceSet(ServiceSourceSet serviceSourceSet) =>
			TryInstallServiceSourceProvider(serviceSourceSet);

		public bool TryUninstallServiceSourceSet(ServiceSourceSet serviceSourceSet) =>
			TryUninstallServiceSourceProvider(serviceSourceSet);

		static readonly List<Type> _typeCache = new();
		public void UninstallAllSourceSets()
		{
			_typeCache.Clear();
			TypesOfWholeEnvironment(_typeCache);
			DestroyAllOwnedInstances();
			_installers.Clear();
			InvokeEnvironmentChanged(_typeCache);
		}

		internal bool TryInstallServiceSourceProvider(IServiceSourceProvider serviceSourceProvider)
		{
			if (serviceSourceProvider == null)
				return false;
			if (_installers.Contains(serviceSourceProvider))
				return false;
			_installers.Add(serviceSourceProvider);
			TrySortInstallers();
			InvokeEnvironmentChangedOnInstaller(serviceSourceProvider);
			return true;
		}

		internal bool TryUninstallServiceSourceProvider(IServiceSourceProvider serviceSourceProvider)
		{
			if (serviceSourceProvider == null)
				return false;
			if (!_installers.Remove(serviceSourceProvider))
				return false;
			serviceSourceProvider.DestroyOwnedInstances();
			TrySortInstallers();
			InvokeEnvironmentChangedOnInstaller(serviceSourceProvider);
			return true;
		}

		internal void SetAllGlobalInstallers(List<ServiceSourceSet> globalInstallers)
		{
			UninstallAllSourceSets();
			foreach (ServiceSourceSet globalInstaller in globalInstallers)
				_installers.Add(globalInstaller);
			TrySortInstallers();
		}

		// SORT INSTALLERS ---------------

		static readonly Comparison<IServiceSourceProvider> _installerSorting = (a, b) => b.PriorityValue.CompareTo(a.PriorityValue);

		internal void SortInstallers()
		{
			if (TrySortInstallers())
				InvokeEnvironmentChangedOnWholeEnvironment();
			else
				ReportAmbiguousBindings();
		}

		bool TrySortInstallers()
		{
			if (_installers.Count <= 1)
				return false;
			bool isSorted = true;
			IServiceSourceProvider s1 = _installers[0];
			for (int index = 1; index < _installers.Count; index++)
			{
				IServiceSourceProvider s2 = _installers[index];
				if (_installerSorting.Invoke(s1, s2) > 0)
				{
					isSorted = false;
					break;
				}

				s1 = s2;
			}

			if (isSorted)
				return false;

			_installers.Sort(_installerSorting);
			return true;
		}

		// INSTALLER & SOURCE GETTERS ---------------

		public IReadOnlyList<IServiceSourceProvider> GetAllInstallers() => Application.isPlaying ? _installers : FindInstallers();

		internal bool ContainsInstaller(IServiceSourceProvider installer) => GetAllInstallers().Contains(installer);

		static readonly List<IServiceSourceProvider> _sets = new();
		static List<IServiceSourceProvider> FindInstallers()
		{
			_sets.Clear();
			_sets.AddRange(Services.FindGlobalInstallers);

			List<InstallerComponent> localInstallers = new();
			localInstallers.AddRange(
				FindObjectsOfTypeAll<LocalServiceInstaller>());
			localInstallers.AddRange(
				FindObjectsOfTypeAll<ServerObject>().Where(serverObj => serverObj.InstallAutomatically));


			_sets.AddRange(localInstallers.Where(
				inst => inst.Priority.type == InstallerPriority.Type.ConcreteValue));

			int maxPriority = _sets.Count == 0 ? 0 : _sets.Max(set => set.PriorityValue);

			foreach (InstallerComponent localInstaller in localInstallers.Where(
				inst => inst.Priority.type == InstallerPriority.Type.HighestAtInstallation))
			{
				maxPriority++;
				localInstaller.SetInstallationValue(maxPriority);
				_sets.Add(localInstaller);
			}

			_sets.Sort(_installerSorting);
			return _sets;
		}

		static IEnumerable<T> FindObjectsOfTypeAll<T>() where T : Component
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded || !scene.IsValid())
					continue;
				foreach (GameObject go in scene.GetRootGameObjects())
					foreach (T t in go.GetComponentsInChildren<T>(includeInactive: true))
						yield return t;
			}
		}

		internal void GetAllServiceSources(List<ServiceSource> result)
		{
			foreach (IServiceSourceProvider installer in GetAllInstallers())
			{
				if (installer.IsEnabled)
					installer.CollectAllEnabled(result);
			}
		}

		internal bool TryGetSourceWithType(Type type, out IServiceSourceProvider installer, out ServiceSource result)
		{
			foreach (IServiceSourceProvider currentInstaller in GetAllInstallers())
			{
				installer = currentInstaller;
				if (installer.IsEnabled && installer.TryGetSourceWithType(type, out result))
					return true;
			}
			installer = null;
			result = null;
			return false;
		}




		public int MaxPriority => GetAllInstallers().FirstOrDefault()?.PriorityValue ?? 0;

		static readonly List<ServiceSource> _sourceCache = new();

		internal void InitServiceSources()
		{
			_sourceCache.Clear();
			GetAllServiceSources(_sourceCache);
			foreach (ServiceSource source in _sourceCache)
				source.InitDynamicIfNeeded();
		}

		internal void DestroyAllOwnedInstances()
		{
			foreach (IServiceSourceProvider installer in _installers)
				installer.DestroyOwnedInstances();
		}

		// SUBSCRIPTION ---------------------

		public event Action EnvironmentChanged;
		readonly Dictionary<Type, HashSet<Action>> _subscribers = new();
		readonly HashSet<string> _reportedAmbiguousBindingWarnings = new();
		bool _isReportingAmbiguousBindings;

		public void SubscribeToEnvironmentChange<T>(Action callback) =>
			SubscribeToEnvironmentChange(typeof(T), callback);

		public void UnSubscribeToEnvironmentChange<T>(Action callback) =>
			UnSubscribeToEnvironmentChange(typeof(T), callback);


		public void SubscribeToEnvironmentChange(Type type, Action callback)
		{
			if (!_subscribers.TryGetValue(type, out HashSet<Action> callbacks))
			{
				callbacks = new HashSet<Action>();
				_subscribers.Add(type, callbacks);
			}

			callbacks.Add(callback);
		}

		public void UnSubscribeToEnvironmentChange(Type type, Action callback)
		{
			if (!_subscribers.TryGetValue(type, out HashSet<Action> callbacks))
				return;
			callbacks.Remove(callback);
		}

		internal void ClearAllState_NoEnvironmentChangeEvent()
		{
			_installers.Clear();
			_subscribers.Clear();
			_reportedAmbiguousBindingWarnings.Clear();
			EnvironmentChanged = null;
		}

		internal void ReportAmbiguousBindings()
		{
			if (!Services.AreServiceLocatorInitialized || Services.IsDestroying || _isReportingAmbiguousBindings)
				return;

			_isReportingAmbiguousBindings = true;
			try
			{
				Dictionary<(Type type, int priority), List<(IServiceSourceProvider provider, ServiceSource source)>> bindingsByTypeAndPriority = new();

				foreach (IServiceSourceProvider provider in GetAllInstallers())
				{
					if (!provider.IsEnabled) continue;

					List<ServiceSource> sources = new();
					provider.CollectAllEnabled(sources);
					foreach (ServiceSource source in sources)
					{
						if (!source.Resolvability.IsResolvable) continue;

						List<Type> types = new();
						source.CollectServiceTypesRecursively(types);
						foreach (Type type in types)
						{
							if (type == null) continue;

							(Type type, int priority) key = (type, provider.PriorityValue);
							if (!bindingsByTypeAndPriority.TryGetValue(key, out List<(IServiceSourceProvider provider, ServiceSource source)> bindings))
							{
								bindings = new List<(IServiceSourceProvider provider, ServiceSource source)>();
								bindingsByTypeAndPriority.Add(key, bindings);
							}

							if (!bindings.Any(binding =>
									ReferenceEquals(binding.provider, provider) && ReferenceEquals(binding.source, source)))
								bindings.Add((provider, source));
						}
					}
				}

				HashSet<string> activeWarnings = new();
				foreach (KeyValuePair<(Type type, int priority), List<(IServiceSourceProvider provider, ServiceSource source)>> entry in bindingsByTypeAndPriority)
				{
					if (entry.Value.Count <= 1) continue;

					string[] bindingNames = entry.Value
						.Select(binding => $"'{binding.provider.Name} / {binding.source.Name}'")
						.OrderBy(name => name, StringComparer.Ordinal)
						.ToArray();
					string typeName = entry.Key.type.FullName ?? entry.Key.type.Name;
					string warning =
						$"Ambiguous LooseLink binding for '{typeName}' at priority {entry.Key.priority}: " +
						$"{string.Join(", ", bindingNames)}. Resolution follows the current installer/source order, " +
						"which is not guaranteed for equal priorities. Assign different priorities to define precedence.";

					activeWarnings.Add(warning);
					if (_reportedAmbiguousBindingWarnings.Add(warning))
						Debug.LogWarning(warning, entry.Value[0].provider.ProviderObject);
				}

				_reportedAmbiguousBindingWarnings.IntersectWith(activeWarnings);
			}
			finally
			{
				_isReportingAmbiguousBindings = false;
			}
		}

		// INVOKE SUBSCRIPTION ---------------------

		static readonly List<ServiceSource> _sourcesCached = new();
		static readonly List<Type> _typesCached = new();

		public void InvokeEnvironmentChangedOnWholeEnvironment()
		{
			_typesCached.Clear();
			TypesOfWholeEnvironment(_typesCached);
			InvokeEnvironmentChanged(_typesCached);
		}

		internal void InvokeEnvironmentChangedOnInstaller(IServiceSourceProvider provider)
		{
			_typesCached.Clear();
			_sourcesCached.Clear();
			provider.CollectAllEnabled(_sourcesCached);
			foreach (ServiceSource source in _sourcesCached)
				source.CollectServiceTypesRecursively(_typesCached);

			InvokeEnvironmentChanged(_typesCached);
		}

		internal void InvokeEnvironmentChangedOnSources(ServiceSource source1, ServiceSource source2)
		{
			_typesCached.Clear();
			source1.CollectServiceTypesRecursively(_typesCached);
			source2.CollectServiceTypesRecursively(_typesCached);
			InvokeEnvironmentChanged(_typesCached);
		}

		internal void InvokeEnvironmentChangedOnSource(ServiceSource source)
		{
			_typesCached.Clear();
			source.CollectServiceTypesRecursively(_typesCached);
			InvokeEnvironmentChanged(_typesCached);
		}

		internal void InvokeEnvironmentChangedOnType(Type type)
		{
			_typesCached.Clear();
			_typesCached.Add(type);
			InvokeEnvironmentChanged(_typesCached);
		}

		internal void InvokeEnvironmentChangedOnTypes(Type type1, Type type2)
		{
			_typesCached.Clear();
			_typesCached.Add(type1);
			_typesCached.Add(type2);
			InvokeEnvironmentChanged(_typesCached);
		}

		internal void InvokeEnvironmentChangedOnTypes(List<Type> types) => InvokeEnvironmentChanged(types);

		void InvokeEnvironmentChanged(List<Type> types)
		{
			if (!Services.AreServiceLocatorInitialized)
				return;
			if (Services.IsDestroying)
				return;
			ReportAmbiguousBindings();

			Type[] changedTypes = types
				.Where(type => type != null)
				.Distinct()
				.ToArray();
			if (changedTypes.Length == 0)
				return;

			foreach (Type type in changedTypes)
				if (_subscribers.TryGetValue(type, out HashSet<Action> callbacks))
				{
					foreach (Action callback in callbacks.ToArray())
						callback?.Invoke();
				}
			EnvironmentChanged?.Invoke();
		}

		// TYPES OF METHODS ---------------------
		void TypesOfWholeEnvironment(List<Type> result)
		{
			foreach (IServiceSourceProvider provider in GetAllInstallers())
			{
				if (!provider.IsEnabled) continue;
				_sourcesCached.Clear();
				provider.CollectAllEnabled(_sourcesCached);
				foreach (ServiceSource source in _sourcesCached)
					source.CollectServiceTypesRecursively(result);
			}
		}
	}
}
