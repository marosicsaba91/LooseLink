using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using Object = UnityEngine.Object;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LooseLink
{

	public static class Services
	{
		static readonly bool debugLogs = false;
		public static TimeSpan SetupTime { get; private set; }

		static readonly ServiceEnvironment environment = new();
		public static ServiceEnvironment Environment => environment;
		internal static bool IsDestroying { get; private set; }
		internal static bool AreServiceLocatorInitialized { get; private set; }

		static Services() => Init();

		static Transform _parentObject;

#if UNITY_EDITOR
		static void OnUnityPlayModeChanged(PlayModeStateChange change)
		{
			if (change == PlayModeStateChange.ExitingPlayMode)
			{
				IsDestroying = true;
				ClearAllCachedData();
			}
		}
#endif

		internal static void Init()
		{
			if (AreServiceLocatorInitialized)
				return;
			DateTime start = DateTime.Now;

#if UNITY_EDITOR
			EditorApplication.playModeStateChanged += OnUnityPlayModeChanged;
#endif
			environment.SetAllGlobalInstallers(FindGlobalInstallers);
			environment.InitServiceSources();
			ServiceTypeHelper.Init();
			IsDestroying = false;
			AreServiceLocatorInitialized = true;

			SetupTime = DateTime.Now - start;
			if (debugLogs)
				Debug.Log($"Init {SetupTime.TotalMilliseconds} ms");
		}

		internal static List<ServiceSourceSet> FindGlobalInstallers => Resources
			.LoadAll<ServiceSourceSet>(string.Empty)
			.Where(contextInstaller => contextInstaller.automaticallyUseAsGlobalInstaller)
			.ToList();

		public static Transform ParentObject
		{
			get
			{
				if (_parentObject == null)
				{
					_parentObject = new GameObject("Services").transform;
					Object.DontDestroyOnLoad(_parentObject.gameObject);
				}

				return _parentObject;
			}
		}

		static readonly List<ServiceSource> _sourceCache = new();

		internal static void ClearAllCachedData()
		{
			_sourceCache.Clear();
			Environment.GetAllServiceSources(_sourceCache);
			foreach (ServiceSource source in _sourceCache)
				source?.ClearCachedInstancesAndTypes_NoEnvironmentChangeEvent();
		}

		public static TService Get<TService>() =>
			(TService)Get(typeof(TService));

		public static object Get(Type looseServiceType)
		{
			if (TryGet(looseServiceType, out object service))
				return service;

			return CantFindService(looseServiceType);
		}

		public static bool TryGet<TService>(out TService service)
		{
			if (TryGet(typeof(TService), out object service1))
			{
				service = (TService)service1;
				return true;
			}

			service = default;
			return false;
		}

		public static bool TryGet(Type looseServiceType, out object service)
		{
			if (debugLogs)
				Debug.Log("Resolve");

			if (Environment.TryGetSourceWithType(looseServiceType, out IServiceSourceProvider installer, out ServiceSource source))
			{
				if (TryGetServiceInSource(looseServiceType, installer, source, out object serv))
				{
					service = serv;
					return true;
				}
			}

			service = null;
			return false;
		}

		static bool TryGetServiceInSource(
			Type looseServiceType,
			IServiceSourceProvider provider,
			ServiceSource source,
			out object service)
		{
			if (!source.TryGetService(looseServiceType, provider, out service))
				return false;

			return true;
		}


		static object CantFindService(Type looseServiceType)
		{
			ServiceLocationSetupData.CantResolveAction action = ServiceLocationSetupData.Instance.whenServiceCantBeResolved;

			if (action == ServiceLocationSetupData.CantResolveAction.ReturnNull)
				return null;

			string text = $"Can't find Services of this Type: {looseServiceType}";

			if (action == ServiceLocationSetupData.CantResolveAction.ThrowException)
				throw new ArgumentException(text);

			Debug.LogWarning(text);
			return null;
		}

		internal static IEnumerable<IServiceSourceProvider> GetAllInstallers() =>
			Environment.GetAllInstallers();

	}
}