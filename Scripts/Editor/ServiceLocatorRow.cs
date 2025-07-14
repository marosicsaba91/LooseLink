#if UNITY_EDITOR
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LooseLink
{
	class ServiceLocatorRow
	{
		public enum RowCategory
		{
			Set,
			Source
		}

		public RowCategory Category { get; }

		public IServiceSourceProvider provider;
		public ServiceSource source;
		public Type type;
		public Object resolvedInstance;
		public Resolvability resolvability;

		public ServiceLocatorRow(RowCategory category)
		{
			Category = category;
			provider = null;
			source = null;
			type = null;

			resolvedInstance = null;
			resolvability = new Resolvability(Resolvability.Type.Resolvable);
		}

		public Object SelectionObject
		{
			get
			{
				switch (Category)
				{
					case RowCategory.Set:
						return provider?.ProviderObject;
					case RowCategory.Source:
						return source.ServiceSourceObject;
					default:
						return null;
				}
			}
		}

		public bool enabled;

		public override string ToString()
		{
			string i = provider == null ? "-" : provider.Name;
			string st = source == null ? "-" : source.GetType().ToString();
			string s = source == null ? "-" : source.Name;
			string t = type == null ? "-" : type.Name;
			return $"{i},{st}:{s},{t}";
		}

		public GUIContent GetGUIContent()
		{
			switch (Category)
			{
				case RowCategory.Set:
					return new
						GUIContent(provider.Name,
						IconHelper.GetIconOfObject(provider.ProviderObject),
						IconHelper.GetTooltipForISet(provider));
				case RowCategory.Source:
					return new GUIContent(source.Name, source.Icon, IconHelper.GetTooltipForServiceSource(source.SourceType));
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public GUIContent GetGUIContentForCategory(bool isShort)
		{
			switch (Category)
			{
				case RowCategory.Set:
					return new GUIContent(IconHelper.GetTooltipForISet(provider));
				case RowCategory.Source:
					string text;
					if (source.IsSourceAndNotSet)
						text = isShort
							? IconHelper.GetShortNameForServiceSourceCategory(source.SourceType)
							: IconHelper.GetNameForServiceSourceCategory(source.SourceType);
					else if (source.ServiceSourceObject == null)
						text = isShort ? "No Obj." : "No Source Object";
					else
						text = isShort ? "Wrong Obj." : "Wrong Source Object";

					return new GUIContent(text, IconHelper.GetTooltipForServiceSource(source.SourceType));
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
#endif