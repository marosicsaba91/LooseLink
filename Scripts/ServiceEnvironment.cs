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

		public void UninstallAllSourceSets()
		{
			_installers.Clear();
			MarkChanged();
		}

		internal bool TryInstallServiceSourceProvider(IServiceSourceProvider serviceSourceProvider)
		{
			if (serviceSourceProvider == null)
				return false;
			if (_installers.Contains(serviceSourceProvider))
				return false;
			_installers.Add(serviceSourceProvider);
			TrySortInstallers();
			MarkChanged();
			return true;
		}

		internal bool TryUninstallServiceSourceProvider(IServiceSourceProvider serviceSourceProvider)
		{
			if (serviceSourceProvider == null)
				return false;
			if (!_installers.Remove(serviceSourceProvider))
				return false;
			TrySortInstallers();
			MarkChanged();
			return true;
		}

		internal void SetAllGlobalInstallers(List<ServiceSourceSet> globalInstallers)
		{
			_installers.Clear();
			foreach (ServiceSourceSet globalInstaller in globalInstallers)
				_installers.Add(globalInstaller);
			TrySortInstallers();
			MarkChanged();
		}

		// SORT INSTALLERS ---------------

		static readonly Comparison<IServiceSourceProvider> _installerSorting = (a, b) => b.PriorityValue.CompareTo(a.PriorityValue);

		internal void SortInstallers()
		{
			TrySortInstallers();
			MarkChanged();
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

		// CHANGES ---------------------

		internal int Revision { get; private set; }

		internal void MarkChanged() => IncrementRevision();

		internal void IncrementRevision()
		{
			unchecked
			{
				Revision++;
			}
		}

		internal void ClearAllState_NoEnvironmentChangeEvent()
		{
			_installers.Clear();
		}

	}
}
