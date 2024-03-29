﻿using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LooseLink
{
	[Serializable]
	public class SerializableType : ISerializationCallbackReceiver
	{
		[SerializeField] string typeName;
		[SerializeField] string fullTypeName;
		[SerializeField] string assemblyQualifiedName;
		[SerializeField] Object monoScript;

		Type _type;
		bool _isTypeSet = false;

		// This is a hack:
		[SerializeField] bool switchable = false;

		public SerializableType(Type type)
		{
			Type = type;
		}

		public SerializableType(Object monoScriptObject)
		{
#if UNITY_EDITOR
			if (monoScriptObject is MonoScript script)
				Type = script.GetClass();
#endif
		}

		public Type Type
		{
			get
			{
				if (_type != null || _isTypeSet)
					return _type;

				_type = FindType();

				return _type;
			}
			set
			{
				_type = value;
				UpdateSerializedValues();
				switchable = !switchable;
			}
		}

		public string Name => typeName;
		public string FullName => fullTypeName;

		public void OnBeforeSerialize()
		{
			if (_type == null)
				return;
			if (typeName != null)
				return;
			UpdateSerializedValues();
		}


		void UpdateSerializedValues()
		{
			assemblyQualifiedName = _type?.AssemblyQualifiedName;
			fullTypeName = _type?.FullName;
			typeName = _type?.Name;

			monoScript = TypeToFileHelper.GetObject(_type);
		}



		public void OnAfterDeserialize()
		{
			_isTypeSet = false;
			_type = null;
		}

		Type FindType()
		{
			Type type;
			_isTypeSet = true;
			try
			{
				type = Type.GetType(assemblyQualifiedName);
				if (type != null)
					return type;
			}
			catch (Exception)
			{
				/* ignored */
			}

			type = TypeToFileHelper.GetType(monoScript);
			if (type != null)
				return type;

			try
			{
				type = Type.GetType(fullTypeName);
				if (type != null)
					return type;
			}
			catch (Exception)
			{
				/* ignored */
			}
			return type;
		}
	}
}