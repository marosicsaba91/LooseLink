using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{

	class DynamicServiceSourceFromPrefabPrototype : DynamicServiceSourceFromGO
	{
		internal DynamicServiceSourceFromPrefabPrototype(GameObject prototypePrefab) : base(prototypePrefab) { }

		public override Resolvability TypeResolvability
		{
			get
			{
				if (gameObject == null)
					return new Resolvability(Resolvability.Type.Error, "No Prefab");
				if (!Application.isPlaying)
					return new Resolvability(
						Resolvability.Type.BlockedInEditorTime,
						"You can't instantiate prefab through Service Locator in Editor Time");
				return Resolvability.Resolvable;
			}
		}

		public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromPrefabPrototype;

		public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes
		{ get { yield return ServiceSourceTypes.FromPrefabFile; } }

		protected override bool NeedParentTransformForLoad => true;

		protected override Object Instantiate(Transform parent)
		{
			GameObject newInstance = Object.Instantiate(gameObject, parent);
			newInstance.name = gameObject.name;
			newInstance.transform.localPosition = Vector3.zero;
			newInstance.transform.localRotation = Quaternion.identity;
			newInstance.transform.localScale = Vector3.one;

			return newInstance;
		}


	}
}