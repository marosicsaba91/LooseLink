using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{

	class DynamicServiceSourceFromPrefabFile : DynamicServiceSourceFromGO
	{
		internal DynamicServiceSourceFromPrefabFile(GameObject prefabFile) : base(prefabFile) { }


		public override Object LoadedObject
		{
			get => gameObject;
			set { }
		}


		public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromPrefabFile;

		public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes
		{ get { yield return ServiceSourceTypes.FromPrefabPrototype; } }

		protected override bool NeedParentTransformForLoad => false;

		protected override Object Instantiate(Transform parent) => gameObject;


		public sealed override Resolvability TypeResolvability => gameObject == null
			? new Resolvability(Resolvability.Type.Error, "No Prefab")
			: Resolvability.AlwaysResolved;

	}
}