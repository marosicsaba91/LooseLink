using System;

namespace LooseLink
{

	[Serializable]
	public struct InstallerPriority
	{
		public enum Type
		{
			HighestAtInstallation,
			ConcreteValue
		}

		public Type type;
		public int priorityValueSetting;
		public int priorityValueAtInstallation;

		public int Value
		{
			get => type == Type.ConcreteValue ? priorityValueSetting : priorityValueAtInstallation;
			set
			{
				if (priorityValueSetting == value)
					return;
				priorityValueSetting = value;
				type = Type.ConcreteValue;
				Services.Environment.SortInstallers();
			}
		}

		public void SetInstallationValue(int value) =>
			priorityValueAtInstallation = value;
	}
}