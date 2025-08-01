﻿using System.Linq;
using EasyEditor;
using MUtility;
using UnityEngine;

namespace LooseLink
{
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

		static ServiceLocationSetupData[] allInstances = null;
		static ServiceLocationSetupData instance = null;

		public static ServiceLocationSetupData Instance
		{
			get
			{
				if (Application.isEditor)
				{
					allInstances = Resources.LoadAll<ServiceLocationSetupData>(string.Empty);
					if (!allInstances.IsNullOrEmpty())
						return allInstances.First();
				}
				else if (allInstances == null)
				{
					allInstances = Resources.FindObjectsOfTypeAll<ServiceLocationSetupData>();
					instance = allInstances.FirstOrDefault();
				}

				if (instance == null)
				{
					instance = CreateInstance<ServiceLocationSetupData>();
					instance.IsDefault = true;
				}

				return instance;
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
				allInstances ??= Resources.FindObjectsOfTypeAll<ServiceLocationSetupData>();
				return allInstances.Length > 1;
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