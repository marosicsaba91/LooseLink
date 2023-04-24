using UnityEngine;

namespace LooseLink
{
	public abstract class InstallerComponent : MonoBehaviour, IServiceSourceProvider
	{
		[SerializeField, HideInInspector] InstallerPriority priority;
		[SerializeField, HideInInspector] bool dontDestroyOnLoad = false;

		public abstract bool InstallAutomatically { get; }

		public bool AutoDontDestroyOnLoad
		{
			get => dontDestroyOnLoad;
			set => dontDestroyOnLoad = value;
		}

		public InstallerPriority Priority
		{
			get => priority;
			set
			{
				int lastValue = priority.Value;
				priority = value;

				if (lastValue != priority.Value)
					Services.Environment.SortInstallers();
			}
		}

		public int PriorityValue => priority.Value;
		public InstallerPriority.Type PriorityType => priority.type;
		public abstract bool IsSingleSourceProvider { get; }
		public abstract void SwapSources(int index1, int index2);

		public abstract ServiceSource GetSourceAt(int index);
		public abstract int SourceCount { get; }
		public bool IsEnabled => isActiveAndEnabled;
		public abstract void AddSource(ServiceSource item);

		public abstract void ClearSources();
		public abstract bool ContainsSource(ServiceSource item);

		public abstract bool RemoveSource(ServiceSource item);

		public abstract int IndexOfSource(ServiceSource item);

		public abstract void InsertSource(int index, ServiceSource item);

		public abstract void RemoveSourceAt(int index);
		public void SetInstallationValue(int value) => priority.SetInstallationValue(value);

		public string Name => gameObject != null ? name : null;
		public Object ProviderObject => gameObject;
		public abstract void ClearDynamicData_NoEnvironmentChangeEvent();

		void Awake()
		{
			if (AutoDontDestroyOnLoad)
				DontDestroyOnLoad(gameObject);

			Install();
		}

		void OnDestroy()
		{
			UnInstall();
		}

		void Install()
		{
			Priority.SetInstallationValue(Services.Environment.MaxPriority + 1);
			Services.Environment.TryInstallServiceSourceProvider(this);
		}

		void UnInstall() => Services.Environment.TryUninstallServiceSourceProvider(this);

	}
}