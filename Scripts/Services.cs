using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using Object = UnityEngine.Object;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LooseLink
{

	[AutoStaticsCleanup]
	public static partial class Services
	{
		static readonly bool _debugLogs = false;
		public static TimeSpan SetupTime { get; private set; }

		static ServiceEnvironment _environment = new();
		static Dictionary<Type, IPermanentProxy> _permanentProxies = new();
		public static ServiceEnvironment Environment
		{
			get
			{
				TryInit();
				return _environment;
			}
		}
		public static void NotifyEnvironmentChanged() => Environment.IncrementRevisionCounter();
		internal static bool IsDestroying { get; private set; }
		internal static bool AreServiceLocatorInitialized { get; private set; }

		static Transform _parentObject;

#if UNITY_EDITOR
		[OnCodeUnloading]
		static void UnsubscribeFromEditorEvents() =>
			EditorApplication.playModeStateChanged -= OnUnityPlayModeChanged;

		static void OnUnityPlayModeChanged(PlayModeStateChange change)
		{
			if (change == PlayModeStateChange.ExitingPlayMode)
			{
				IsDestroying = true;
				ResetPermanentProxies();
				ClearAllCachedData();
				_environment.ClearAllState_NoEnvironmentChangeEvent();
				AreServiceLocatorInitialized = false;
			}
			else if (change == PlayModeStateChange.EnteredEditMode)
				TryInit();
		}
#endif

		internal static void TryInit()
		{
			if (AreServiceLocatorInitialized)
				return;
			DateTime start = DateTime.Now;

#if UNITY_EDITOR
			EditorApplication.playModeStateChanged -= OnUnityPlayModeChanged;
			EditorApplication.playModeStateChanged += OnUnityPlayModeChanged;
#endif
			_environment.SetAllGlobalInstallers(FindGlobalInstallers);
			_environment.InitServiceSources();
			ServiceTypeHelper.Init();
			IsDestroying = false;
			AreServiceLocatorInitialized = true;

			SetupTime = DateTime.Now - start;
			if (_debugLogs)
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
				TryInit();
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

		public static PermanentProxy<TService> GetProxy<TService>()
		{
			TryInit();
			Type serviceType = typeof(TService);
			if (_permanentProxies.TryGetValue(serviceType, out IPermanentProxy existingProxy))
				return (PermanentProxy<TService>)existingProxy;

			PermanentProxy<TService> proxy = new();
			_permanentProxies.Add(serviceType, proxy);
			return proxy;
		}

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
			TryInit();
			if (_debugLogs)
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


		internal static object CantFindService(Type looseServiceType)
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

		static void ResetPermanentProxies()
		{
			foreach (IPermanentProxy proxy in _permanentProxies.Values.ToArray())
				proxy.Reset();
		}

	}
}
