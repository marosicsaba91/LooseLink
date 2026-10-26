using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{

	[AutoStaticsCleanup]
	abstract partial class DynamicServiceSource
	{
		readonly struct ConstructionFrame
		{
			public DynamicServiceSource Source { get; }
			public Type RequestedType { get; }

			public ConstructionFrame(DynamicServiceSource source, Type requestedType)
			{
				Source = source;
				RequestedType = requestedType;
			}
		}

		readonly struct ConstructedObject
		{
			public DynamicServiceSource Source { get; }
			public Object Instance { get; }

			public ConstructedObject(DynamicServiceSource source, Object obj)
			{
				Source = source;
				Instance = obj;
			}
		}

		static readonly List<ConstructionFrame> _constructionStack = new();
		static readonly List<ConstructedObject> _constructedObjects = new();
		static bool _constructionFailed;

		readonly Dictionary<Type, object> _typeToServiceOnSource = new();
		readonly List<Type> _dynamicServiceTypes = new();
		readonly List<Type> _possibleAdditionalTypes = new();
		readonly List<Type> _allNonAbstractTypes = new();
		readonly List<IServiceSourceCondition> _resolvingConditions = new();
		ServerObject _serverObject;
		bool _isDynamicTypeDataInitialized = false;
		bool _isInitialized;
		bool _isCreating;
		public event Action ConditionsChanged;

		public virtual Object LoadedObject { get; set; } // GameObject or ScriptableObject

		readonly Dictionary<Type, object> _instantiatedServices = new();


		public abstract Resolvability TypeResolvability { get; }

		public Resolvability Resolvability
		{
			get
			{
				Resolvability result = TypeResolvability;
				if (result.type is Resolvability.Type.Error or Resolvability.Type.BlockedInEditorTime)
					return result;

				if (!IsResolvableByConditions(out string message))
					return new(Resolvability.Type.BlockedByCondition, message);

				return result;
			}
		}


		public ServerObject ServerObject
		{
			get
			{
				InitDynamicTypeDataIfNeeded();
				return _serverObject;
			}
		}

		public bool TryGetService(
			Type type,
			IServiceSourceProvider provider,
			out object service)
		{
			Resolvability resolvability = Resolvability;
			if (resolvability.type is not Resolvability.Type.Resolvable and not Resolvability.Type.AlwaysResolved)
			{
				service = null;
				return false;
			}

			if (LoadedObject == null)
			{
				if (!TryCreateServiceObject(type, provider))
				{
					service = null;
					return false;
				}
			}

			TryInitializeService();

			if (!_instantiatedServices.TryGetValue(type, out service) || !IsServiceAlive(service))
			{
				service = GetServiceFromServerObject(type, LoadedObject);
				if (!IsServiceAlive(service))
				{
					_instantiatedServices.Remove(type);
					service = null;
					return false;
				}

				_instantiatedServices[type] = service;
			}

			return true;
		}

		static bool IsServiceAlive(object service)
		{
			if (service is Object unityObject)
				return unityObject != null;
			return service != null;
		}

		bool TryCreateServiceObject(Type type, IServiceSourceProvider provider)
		{
			if (_isCreating)
			{
				ReportCircularConstruction(type);
				return false;
			}

			bool isRootConstruction = _constructionStack.Count == 0;
			if (isRootConstruction)
				_constructedObjects.Clear();
			_constructionStack.Add(new(this, type));
			_isCreating = true;

			try
			{
				_isInitialized = false;
				Transform parentObject = null;

				if (NeedParentTransformForLoad)
				{
					parentObject = provider != null && provider.GetType().IsSubclassOf(typeof(Component))
						? ((Component)provider).transform
						: Services.ParentObject;
				}

				Object createdObject = Instantiate(parentObject);
				if (_constructionFailed || createdObject == null)
				{
					DestroyOwnedObject(createdObject);
					return false;
				}

				LoadedObject = createdObject;
				_instantiatedServices.Clear();
				_constructedObjects.Add(new(this, createdObject));
				return true;
			}
			finally
			{
				_isCreating = false;
				_constructionStack.RemoveAt(_constructionStack.Count - 1);
				if (isRootConstruction)
				{
					bool constructionFailed = _constructionFailed;
					_constructionFailed = false;
					if (constructionFailed)
						RollbackConstructedObjects();
					else
						_constructedObjects.Clear();
				}
			}
		}

		static void RollbackConstructedObjects()
		{
			List<ConstructedObject> constructedObjects = new(_constructedObjects);
			_constructedObjects.Clear();
			for (int i = constructedObjects.Count - 1; i >= 0; i--)
			{
				ConstructedObject constructedObject = constructedObjects[i];
				constructedObject.Source.RollbackConstructedObject(constructedObject.Instance);
			}
		}

		void RollbackConstructedObject(Object constructedObject)
		{
			if (!ReferenceEquals(LoadedObject, constructedObject))
				return;

			LoadedObject = null;
			_isInitialized = false;
			_instantiatedServices.Clear();
			DestroyOwnedObject(constructedObject);
		}

		void ReportCircularConstruction(Type requestedType)
		{
			if (_constructionFailed)
				return;

			_constructionFailed = true;
			List<string> dependencyChain = new();
			foreach (ConstructionFrame frame in _constructionStack)
				dependencyChain.Add($"{frame.Source.Name} ({frame.RequestedType.Name})");
			dependencyChain.Add($"{Name} ({requestedType.Name})");

			Debug.LogError(
				$"Circular service construction detected: {string.Join(" -> ", dependencyChain)}.\n" +
				"Unity invokes Awake and OnEnable before LooseLink can cache a lazily created object.\n" +
				"Resolve circular service dependencies in IInitializable.Initialize() instead.");
		}

		void TryInitializeService()
		{
			if (_isInitialized)
				return;
			if (LoadedObject == null)
				return;
			_isInitialized = true;
			foreach (IInitializable initializable in GetTypesOf<IInitializable>(LoadedObject))
				initializable.Initialize();
		}

		bool IsResolvableByConditions(out string message)
		{
			foreach (IServiceSourceCondition resolvingCondition in _resolvingConditions)
				if (!resolvingCondition.CanResolve())
				{
					message = Application.isEditor ? resolvingCondition.GetConditionMessage() : null;
					return false;
				}

			message = null;
			return true;
		}

		protected abstract bool NeedParentTransformForLoad { get; }
		protected virtual bool OwnsLoadedObject => false;

		protected abstract Object Instantiate(Transform parent);

		protected abstract object GetServiceFromServerObject(Type type, Object serverObject);

		public abstract string Name { get; }

		public abstract Object SourceObject { get; }

		public List<Type> GetDynamicServiceTypes()
		{
			InitDynamicTypeDataIfNeeded();
			return _dynamicServiceTypes;
		}

		public List<Type> GetPossibleAdditionalTypes()
		{
			InitDynamicTypeDataIfNeeded();
			return _possibleAdditionalTypes;
		}

		public object GetServiceOnSource(Type serviceType)
		{
			InitDynamicTypeDataIfNeeded();
			return _typeToServiceOnSource[serviceType];
		}

		void InitDynamicTypeDataIfNeeded()
		{
			if (_isDynamicTypeDataInitialized)
				return;

			_allNonAbstractTypes.Clear();
			_allNonAbstractTypes.AddRange(GetNonAbstractTypes());
			_dynamicServiceTypes.Clear();
			_possibleAdditionalTypes.Clear();
			_dynamicServiceTypes.Clear();
			_typeToServiceOnSource.Clear();
			if (SourceObject is GameObject go)
				_serverObject = go.GetComponent<ServerObject>();
			ClearConditionSubscriptions();
			_resolvingConditions.AddRange(GetTypesOf<IServiceSourceCondition>(SourceObject));
			foreach (IServiceSourceCondition condition in _resolvingConditions)
				if (condition is IServiceSourceConditionChangeNotifier notifier)
					notifier.ConditionChanged += OnConditionChanged;

			foreach (Type concreteType in _allNonAbstractTypes)
			{
				object serviceInstanceOnSourceObject = GetServiceOnSourceObject(concreteType);

				IEnumerable<Type> abstractTypes = ServiceTypeHelper.GetServicesOfNonAbstractType(concreteType)
					.Where(abstractType => !_dynamicServiceTypes.Contains(abstractType));

				foreach (Type abstractType in abstractTypes)
				{
					_dynamicServiceTypes.Add(abstractType);
					_typeToServiceOnSource.Add(abstractType, serviceInstanceOnSourceObject);
				}

				CollectAllPossibleAdditionalSubclasses(concreteType, includeInterfaces: true, _possibleAdditionalTypes);
			}

			_isDynamicTypeDataInitialized = true;
		}


		static IEnumerable<T> GetTypesOf<T>(Object obj)
		{
			switch (obj)
			{
				case ScriptableObject so:
				{
					if (so is T t)
						yield return t;
					break;
				}
				case GameObject go:
				{
					foreach (T t in go.GetComponents<T>())
						yield return t;
					break;
				}
			}
		}

		void CollectAllPossibleAdditionalSubclasses(Type type, bool includeInterfaces, List<Type> result)
		{
			if (type == null) return;
			if (type == typeof(ServerObject)) return;
			if (type == typeof(LocalServiceInstaller)) return;

			result.Add(type);

			if (includeInterfaces)
				foreach (Type interfaceType in type.GetInterfaces())
				{
					if (interfaceType == typeof(IInitializable)) continue;
					if (interfaceType == typeof(IServiceSourceCondition)) continue;
					if (interfaceType == typeof(IServiceSourceProvider)) continue;
					result.Add(interfaceType);
				}


			Type baseType = type.BaseType;

			if (baseType == null) return;
			if (baseType == typeof(ScriptableObject)) return;
			if (baseType == typeof(Component)) return;
			if (baseType == typeof(Behaviour)) return;
			if (baseType == typeof(MonoBehaviour)) return;

			CollectAllPossibleAdditionalSubclasses(baseType, includeInterfaces: false, result);
		}

		protected abstract IEnumerable<Type> GetNonAbstractTypes();
		public abstract object GetServiceOnSourceObject(Type type);

		public abstract ServiceSourceTypes SourceType { get; }
		public abstract IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get; }
		public List<IServiceSourceCondition> Conditions => _resolvingConditions;

		void OnConditionChanged() => ConditionsChanged?.Invoke();

		void ClearConditionSubscriptions()
		{
			foreach (IServiceSourceCondition condition in _resolvingConditions)
				if (condition is IServiceSourceConditionChangeNotifier notifier)
					notifier.ConditionChanged -= OnConditionChanged;
			_resolvingConditions.Clear();
		}

		public void Disconnect()
		{
			ClearConditionSubscriptions();
			ConditionsChanged = null;
		}

		public void ClearCachedInstancesAndTypes()
		{
			LoadedObject = null;
			_isInitialized = false;
			_instantiatedServices.Clear();
			ClearCachedTypes();
		}

		public void DestroyOwnedInstance()
		{
			if (!OwnsLoadedObject || LoadedObject == null)
				return;

			Object ownedInstance = LoadedObject;
			LoadedObject = null;
			_isInitialized = false;
			_instantiatedServices.Clear();

			DestroyOwnedObject(ownedInstance);
		}

		void DestroyOwnedObject(Object ownedObject)
		{
			if (!OwnsLoadedObject || ownedObject == null)
				return;

			if (Application.isPlaying)
				Object.Destroy(ownedObject);
			else
				Object.DestroyImmediate(ownedObject);
		}

		public void ClearCachedTypes()
		{
			if (Application.isPlaying)
				return;
			_isDynamicTypeDataInitialized = false;
		}
	}
}
