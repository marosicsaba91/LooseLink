using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{

	class DynamicServiceSourceFromScriptableObjectFile : DynamicServiceSource
	{
		public ScriptableObject instance;

		internal DynamicServiceSourceFromScriptableObjectFile(ScriptableObject instance)
		{
			this.instance = instance;
		}

		public override Object LoadedObject
		{
			get => instance;
			set { }
		}
		protected override IEnumerable<Type> GetNonAbstractTypes()
		{
			if (instance != null)
				yield return instance.GetType();
		}

		public override ServiceSourceTypes SourceType => ServiceSourceTypes.FromScriptableObjectFile;

		public override IEnumerable<ServiceSourceTypes> AlternativeSourceTypes
		{ get { yield return ServiceSourceTypes.FromScriptableObjectPrototype; } }


		public override Resolvability TypeResolvability => instance == null
			? new Resolvability(Resolvability.Type.Error, "No ScriptableObject instance")
			: Resolvability.AlwaysResolved;

		protected override bool NeedParentTransformForLoad => false;
		protected override Object Instantiate(Transform parent) => instance;

		protected override object GetServiceFromServerObject(Type type, Object serverObject) => serverObject;

		public override object GetServiceOnSourceObject(Type type) => instance;

		public override string Name => instance != null ? instance.name : string.Empty;
		public override Object SourceObject => instance;


	}
}