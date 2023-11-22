using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{

	abstract class DynamicServiceSource
	{
		readonly Dictionary<Type, object> _typeToServiceOnSource = new();
		readonly List<Type> _dynamicServiceTypes = new();
		readonly List<Type> _possibleAdditionalTypes = new();
		readonly List<Type> _allNonAbstractTypes = new();
		readonly List<IServiceSourceCondition> _resolvingConditions = new();
		readonly List<ITagged> _dynamicTaggers = new();
		ServerObject _serverObject;
		bool _isDynamicTypeDataInitialized = false;
		bool _isInitialized;

		public virtual Object LoadedObject { get; set; } // GameObject or ScriptableObject

		readonly Dictionary<Type, object> _instantiatedServices = new();


		public abstract Resolvability TypeResolvability { get; }

		public Resolvability Resolvability
		{
			get
			{
				Resolvability result = TypeResolvability;
				if (result.type == Resolvability.Type.Error || result.type == Resolvability.Type.BlockedInEditorTime)
					return result;

				if (!IsResolvableByConditions(out string message))
					return new Resolvability(Resolvability.Type.BlockedByCondition, message);

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
			if (resolvability.type != Resolvability.Type.Resolvable && resolvability.type != Resolvability.Type.AlwaysResolved)
			{
				service = default;
				return false;
			}

			if (LoadedObject == null)
			{
				Transform parentObject = null;

				if (NeedParentTransformForLoad)
				{
					parentObject = provider != null && provider.GetType().IsSubclassOf(typeof(Component))
						? ((Component)provider).transform
						: Services.ParentObject;
				}

				LoadedObject = Instantiate(parentObject);
			}

			TryInitializeService();

			if (!_instantiatedServices.ContainsKey(type))
				_instantiatedServices.Add(type, GetServiceFromServerObject(type, LoadedObject));

			service = _instantiatedServices[type];
			return true;
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

		protected abstract Object Instantiate(Transform parent);

		protected abstract object GetServiceFromServerObject(Type type, Object serverObject);

		public abstract string Name { get; }

		public abstract Object SourceObject { get; }

		public IEnumerable<Type> GetDynamicServiceTypes()
		{
			InitDynamicTypeDataIfNeeded();
			return _dynamicServiceTypes;
		}

		public IEnumerable<object> GetDynamicTags()
		{
			InitDynamicTypeDataIfNeeded();
			return _dynamicTaggers.SelectMany(tagger => tagger.GetTags()).Where(tag => tag != null);
		}

		public IReadOnlyList<Type> GetPossibleAdditionalTypes()
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
			_serverObject = (SourceObject as GameObject)?.GetComponent<ServerObject>();
			_resolvingConditions.Clear();
			_resolvingConditions.AddRange(GetTypesOf<IServiceSourceCondition>(SourceObject));
			_dynamicTaggers.Clear();
			_dynamicTaggers.AddRange(GetTypesOf<ITagged>(SourceObject));
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


				foreach (Type subclass in AllPossibleAdditionalSubclassesOf(concreteType))
					_possibleAdditionalTypes.Add(subclass);
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

		IEnumerable<Type> AllPossibleAdditionalSubclassesOf(Type type, bool includeInterfaces = true)
		{
			if (type == null)
				yield break;

			if (type == typeof(ServerObject))
				yield break;

			if (type == typeof(LocalServiceInstaller))
				yield break;

			yield return type;

			if (includeInterfaces)
			{
				foreach (Type interfaceType in type.GetInterfaces())
				{
					if (interfaceType == typeof(IInitializable))
						continue;
					if (interfaceType == typeof(IServiceSourceCondition))
						continue;
					if (interfaceType == typeof(ITagged))
						continue;
					if (interfaceType == typeof(IServiceSourceProvider))
						continue;
					yield return interfaceType;
				}
			}

			Type baseType = type.BaseType;
			if (baseType == null)
				yield break;
			if (baseType == typeof(ScriptableObject))
				yield break;
			if (baseType == typeof(Component))
				yield break;
			if (baseType == typeof(Behaviour))
				yield break;
			if (baseType == typeof(MonoBehaviour))
				yield break;

			foreach (Type b in AllPossibleAdditionalSubclassesOf(baseType, includeInterfaces: false))
				yield return b;
		}

		protected abstract IEnumerable<Type> GetNonAbstractTypes();
		public abstract object GetServiceOnSourceObject(Type type);

		public abstract ServiceSourceTypes SourceType { get; }
		public abstract IEnumerable<ServiceSourceTypes> AlternativeSourceTypes { get; }
		public List<IServiceSourceCondition> Conditions => _resolvingConditions;

		public void ClearCachedInstancesAndTypes()
		{
			LoadedObject = null;
			_instantiatedServices.Clear();
			ClearCachedTypes();
		}

		public void ClearCachedTypes()
		{
			if (Application.isPlaying)
				return;
			_isDynamicTypeDataInitialized = false;
		}
	}
}