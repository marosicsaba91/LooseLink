using System.Linq;
using EasyEditor;
using MUtility;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace LooseLink
{
	[NoAutoStaticsCleanup]
	class ServiceLocationSetupData : ScriptableObject
	{
		internal enum CantResolveAction
		{
			ReturnNull,
			ReturnNullWithWarning,
			ThrowException,
		}

		[ShowIf(nameof(ShowError))] public EasyMessage errorMessage = new(nameof(GetErrorMessage))
		{ messageType = MessageType.Error };

		public EasyMessage lastTypeMapSetupTime = new(nameof(InfoMessage)) { messageType = MessageType.Info };

		string InfoMessage =>
			"Service Locator needs to setup itself once to operate fast after that." +
			"This process takes relatively long time, so it can cause a noticeable hiccup." +
			"Last setup was:  " + Services.SetupTime.Milliseconds + " ms." +
			"You can choose to do this at the start of the software or at the first use of the Service Locator.";

		public bool setupAtPlayInEditor;
		public bool setupAtStartInBuild = true;

		[Space] public CantResolveAction whenServiceCantBeResolved = CantResolveAction.ReturnNullWithWarning;

		public bool IsDefault { get; private set; } = false;

		static ServiceLocationSetupData[] _allInstances = null;
		static ServiceLocationSetupData _instance = null;

		public static ServiceLocationSetupData Instance
		{
			get
			{
				if (Application.isEditor)
				{
					_allInstances = Resources.LoadAll<ServiceLocationSetupData>(string.Empty);
					if (!_allInstances.IsNullOrEmpty())
						return _allInstances.First();
				}
				else if (_allInstances == null)
				{
					_allInstances = Resources.FindObjectsOfTypeAll<ServiceLocationSetupData>();
					_instance = _allInstances.FirstOrDefault();
				}

				if (_instance == null)
				{
					_instance = CreateInstance<ServiceLocationSetupData>();
					_instance.IsDefault = true;
				}

				return _instance;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		internal static void UpdateGlobalInstallers()
		{
			ServiceLocationSetupData instance = Instance;
			if (Application.isEditor && !instance.setupAtPlayInEditor)
				return;
			if (!Application.isEditor && !instance.setupAtStartInBuild)
				return;
			Services.Init();
		}

		public static bool AreMultipleSetupInstances
		{
			get
			{
				_allInstances ??= Resources.FindObjectsOfTypeAll<ServiceLocationSetupData>();
				return _allInstances.Length > 1;
			}
		}

		internal bool IsInResources() =>
			Resources.LoadAll<ServiceLocationSetupData>(string.Empty).Any(so => so == this);

		bool ShowError() => !IsDefault && (!IsInResources() || AreMultipleSetupInstances);
		string GetErrorMessage() => !IsInResources()
			? "ServiceLocationSetupData only works in a Resources folder!"
			: "There are more than one ServiceLocationSetupData instance!";
	}
}