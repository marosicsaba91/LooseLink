using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace LooseLink
{
	internal interface IPermanentProxy
	{
		void Reconnect();
		void Reset();
	}

	public sealed class PermanentProxy<T> : IPermanentProxy
	{
		T _target;
		bool _hasTarget;
		bool _isTracking;

		internal PermanentProxy()
		{
			Services.Environment.SubscribeToEnvironmentChange<T>(OnEnvironmentChanged);
		}

		public T Target
		{
			get
			{
				if (TryGetTarget(out T target))
					return target;

				return (T)Services.CantFindService(typeof(T));
			}
		}

		public bool HasTarget => _hasTarget && IsTargetAlive(_target);

		public event Action<T, T> BindingChanged;

		public bool TryGetTarget(out T target)
		{
			_isTracking = true;
			if (!HasTarget)
				RefreshBinding();

			target = _target;
			return HasTarget;
		}

		void OnEnvironmentChanged()
		{
			if (_isTracking)
				RefreshBinding();
		}

		void RefreshBinding()
		{
			bool hasTarget = Services.TryGet(out T target) && IsTargetAlive(target);
			SetBinding(target, hasTarget);
		}

		void SetBinding(T target, bool hasTarget)
		{
			if (_hasTarget == hasTarget && (!hasTarget || IsSameTarget(_target, target)))
				return;

			T previousTarget = _target;
			_target = target;
			_hasTarget = hasTarget;
			BindingChanged?.Invoke(previousTarget, _target);
		}

		static bool IsTargetAlive(T target)
		{
			if (target is Object unityObject)
				return unityObject != null;
			return target is not null;
		}

		static bool IsSameTarget(T target1, T target2)
		{
			if (typeof(T).IsValueType)
				return EqualityComparer<T>.Default.Equals(target1, target2);
			return ReferenceEquals(target1, target2);
		}

		void IPermanentProxy.Reconnect()
		{
			Services.Environment.UnSubscribeToEnvironmentChange<T>(OnEnvironmentChanged);
			Services.Environment.SubscribeToEnvironmentChange<T>(OnEnvironmentChanged);
			if (_isTracking)
				RefreshBinding();
		}

		void IPermanentProxy.Reset()
		{
			Services.Environment.UnSubscribeToEnvironmentChange<T>(OnEnvironmentChanged);
			_target = default;
			_hasTarget = false;
			_isTracking = false;
			BindingChanged = null;
		}
	}
}
