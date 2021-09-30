using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using UnityServiceLocator; 

public class ServiceLocatorTester : MonoBehaviour
{
    [SerializeField] LoadableTypeObject typeName;
    [SerializeField] LocateButton locate; 
    [SerializeField] string[] tags;

    [Space]
    [SerializeField] LocatedObject locatedProperty; 
    [SerializeField] float timeInTicks;

    void OnEnable()
    {
        ServiceLocator.Environment.SubscribeToEnvironmentChange<BoxCollider2D>(BoxColliderChanged);
    }    
    void OnDisable()
    {
        ServiceLocator.Environment.SubscribeToEnvironmentChange<BoxCollider2D>(BoxColliderChanged);
    }

    void BoxColliderChanged()
    {
        ServiceLocator.TryResolve(out BoxCollider2D c);
        Debug.Log($"Box Collider Changed: {(c == null ? "null" : c.name)}");
    }

    object _located = null;

    [Serializable]
    class LocateButton : InspectorButton<ServiceLocatorTester>
    {
        protected override void OnClick(ServiceLocatorTester parentObject)
        {
            Type type = parentObject.typeName.GetTypeOfString();
            if (type == null)
            { 
                Debug.Log("Wrong Type");
                return;
            }

            DateTime time1 = DateTime.Now;
            parentObject._located = ServiceLocator.Resolve(type, parentObject.tags.Cast<object>().ToArray());
            DateTime time2 = DateTime.Now;
            parentObject.timeInTicks = (time2 - time1).Ticks;
        }
    } 
    
    [Serializable]
    class LocatedObject : StringProperty<ServiceLocatorTester>
    {
        protected override bool IsEnabled(ServiceLocatorTester parentObject) => false;

        protected override string GetValue(ServiceLocatorTester parentObject) =>
            parentObject._located?.ToString();
    }
    
    [Serializable]
    class LoadableTypeObject : StringProperty<ServiceLocatorTester>
    {
        public Type GetTypeOfString()
        {
            if (string.IsNullOrEmpty(value))
                return null;
            
            var type = Type.GetType(value);
            if (type != null) return type;
            type = ServiceTypeHelper.SearchTypeWithFullName(value);
            if (type != null) return type;
            return ServiceTypeHelper.SearchTypeWithName(value);
        }

        protected override IList<string> PopupElements(ServiceLocatorTester container) => 
            ServiceLocator.Environment.GetAllInstalledServiceTypes().Select(t =>t.FullName).ToList();
    }

}
