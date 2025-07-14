using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.UI.DefaultControls;

namespace LooseLink
{
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
			return false;
		}

		internal bool TryUninstallServiceSourceProvider(IServiceSourceProvider serviceSourceProvider)
		{
			if (serviceSourceProvider == null)
				return false;
			if (!_installers.Remove(serviceSourceProvider))
				return false;
			TrySortInstallers();
			InvokeEnvironmentChangedOnInstaller(serviceSourceProvider);
			return false;
		}

		internal void SetAllGlobalInstallers(List<ServiceSourceSet> globalInstallers)
		{
			UninstallAllSourceSets();
			foreach (ServiceSourceSet globalInstaller in globalInstallers)
				_installers.Add(globalInstaller);
			TrySortInstallers();
		}

		// SORT INSTALLERS ---------------

		static readonly Comparison<IServiceSourceProvider> installerSorting = (a, b) => b.PriorityValue.CompareTo(a.PriorityValue);

		internal void SortInstallers()
		{
			if (TrySortInstallers())
				InvokeEnvironmentChangedOnWholeEnvironment();
		}

		bool TrySortInstallers()
		{
			if (_installers.Count <= 1)
				return true;
			bool isSorted = true;
			IServiceSourceProvider s1 = _installers[0];
			for (int index = 1; index < _installers.Count; index++)
			{
				IServiceSourceProvider s2 = _installers[index];
				if (installerSorting.Invoke(s1, s2) > 0)
				{
					isSorted = false;
					break;
				}

				s1 = s2;
			}

			if (isSorted)
				return false;

			_installers.Sort(installerSorting);
			return true;
		}

		// INSTALLER & SOURCE GETTERS ---------------

		public List<IServiceSourceProvider> GetAllInstallers() => Application.isPlaying ? _installers : FindInstallers();

		static readonly List<IServiceSourceProvider> sets = new();
		static List<IServiceSourceProvider> FindInstallers()
		{
			sets.Clear();
			sets.AddRange(Services.FindGlobalInstallers);

			List<InstallerComponent> localInstallers = new();
			localInstallers.AddRange(
				FindObjectsOfTypeAll<LocalServiceInstaller>());
			localInstallers.AddRange(
				FindObjectsOfTypeAll<ServerObject>().Where(serverObj => serverObj.InstallAutomatically));


			int maxPriority = sets.Count == 0 ? 0 : sets.Max(set => set.PriorityValue);

			sets.AddRange(localInstallers.Where(
				inst => inst.Priority.type == InstallerPriority.Type.ConcreteValue));

			foreach (InstallerComponent localInstaller in localInstallers.Where(
				inst => inst.Priority.type == InstallerPriority.Type.HighestAtInstallation))
			{
				maxPriority++;
				localInstaller.SetInstallationValue(maxPriority);
				sets.Add(localInstaller);
			}

			sets.Sort(installerSorting);
			return sets;
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

		internal bool TryGetSources(Func<ServiceSource, bool> filter, out IServiceSourceProvider installer, out ServiceSource result)
		{
			foreach (IServiceSourceProvider currentInstaller in GetAllInstallers())
			{
				installer = currentInstaller;
				if (installer.IsEnabled && installer.TryGetFirstSources(filter, out result))
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

		// SUBSCRIPTION ---------------------

		public event Action EnvironmentChanged;
		readonly Dictionary<Type, HashSet<Action>> _subscribers = new();

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

		// INVOKE SUBSCRIPTION ---------------------

		static readonly List<ServiceSource> _sourcesCached = new();

		public void InvokeEnvironmentChangedOnWholeEnvironment()
		{
			// TODO: Save Allocation
			List<Type> types = new();
			TypesOfWholeEnvironment(types);
			InvokeEnvironmentChanged(types);
		}

		internal void InvokeEnvironmentChangedOnInstaller(IServiceSourceProvider provider)
		{
			// TODO: Save Allocation
			List<Type> types = new();
			_sourcesCached.Clear();
			provider.CollectAllEnabled(_sourcesCached);
			foreach (ServiceSource source in _sourcesCached)
				source.CollectServiceTypesRecursively(types);

			InvokeEnvironmentChanged(types);
		}

		internal void InvokeEnvironmentChangedOnSources(ServiceSource source1, ServiceSource source2)
		{
			// TODO: Save Allocation
			List<Type> types = new();
			source1.CollectServiceTypesRecursively(types);
			source2.CollectServiceTypesRecursively(types);
			InvokeEnvironmentChanged(types);
		}

		internal void InvokeEnvironmentChangedOnSource(ServiceSource source)
		{
			// TODO: Save Allocation
			List<Type> types = new();
			source.CollectServiceTypesRecursively(types);
			InvokeEnvironmentChanged(types);
		}

		internal void InvokeEnvironmentChangedOnType(Type type)
		{
			// TODO: Save Allocation
			List<Type> types = new() { type };
			InvokeEnvironmentChanged(types);
		}
		internal void InvokeEnvironmentChangedOnTypes(Type type1, Type type2)
		{
			// TODO: Save Allocation
			List<Type> types = new() { type1, type2 };
			InvokeEnvironmentChanged(types);
		}

		void InvokeEnvironmentChanged(List<Type> types)
		{
			if (!Services.AreServiceLocatorInitialized)
				return;
			if (Services.IsDestroying)
				return;

			if (types.IsEmpty())
				return;

			foreach (Type type in types)
				if (_subscribers.TryGetValue(type, out HashSet<Action> callbacks))
				{
					foreach (Action callback in callbacks)
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