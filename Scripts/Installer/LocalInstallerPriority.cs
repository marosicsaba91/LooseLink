using System;

namespace UnityServiceLocator
{

[Serializable]
public struct LocalInstallerPriority
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
            if (priorityValueSetting == value) return;
            priorityValueSetting = value;
            type = Type.ConcreteValue;
            ServiceLocator.Environment.SortInstallers();
        }
    }

    public void SetInstallationValue(int value) =>
        priorityValueAtInstallation = value;
}
}