using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace LooseLink
{
	internal interface IPermanentProxy
	{
		void Reset();
	}

	public sealed class PermanentProxy<T> : IPermanentProxy
	{
		T _target;
		bool _hasTarget;
		int _resolvedRevision = -1;

		public T Target
		{
			get
			{
				if (TryGetTarget(out T target))
					return target;

				return (T)Services.CantFindService(typeof(T));
			}
		}

		public bool HasTarget =>
			_resolvedRevision == Services.Environment.RevisionCounter &&
			_hasTarget &&
			IsTargetAlive(_target);

		/// <summary>
		/// Invoked lazily when accessing <see cref="Target"/> or <see cref="TryGetTarget"/>
		/// discovers that the effective binding changed.
		/// </summary>
		public event Action<T, T> BindingChanged;

		public bool TryGetTarget(out T target)
		{
			if (!HasTarget)
				RefreshBinding();

			target = _target;
			return _hasTarget && IsTargetAlive(_target);
		}

		void RefreshBinding()
		{
			bool hasTarget = Services.TryGet(out T target) && IsTargetAlive(target);
			SetBinding(target, hasTarget, Services.Environment.RevisionCounter);
		}

		void SetBinding(T target, bool hasTarget, int resolvedRevision)
		{
			T previousTarget = _target;
			bool bindingChanged = _hasTarget != hasTarget ||
				(hasTarget && !IsSameTarget(previousTarget, target));

			_target = target;
			_hasTarget = hasTarget;
			_resolvedRevision = resolvedRevision;

			if (bindingChanged)
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

		void IPermanentProxy.Reset()
		{
			_target = default;
			_hasTarget = false;
			_resolvedRevision = -1;
			BindingChanged = null;
		}
	}
}
