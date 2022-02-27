using System;
using System.Collections.Generic;
using System.Linq;
using LooseLink;
using MUtility;
using UnityEngine;

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
        Services.Environment.SubscribeToEnvironmentChange<BoxCollider2D>(BoxColliderChanged);
    }    
    void OnDisable()
    {
        Services.Environment.SubscribeToEnvironmentChange<BoxCollider2D>(BoxColliderChanged);
    }

    void BoxColliderChanged()
    {
        Services.TryGet(out BoxCollider2D c);
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
            parentObject._located = Services.Get(type, parentObject.tags.Cast<object>().ToArray());
            DateTime time2 = DateTime.Now;
            parentObject.timeInTicks = (time2 - time1).Ticks;
        }
    } 
    
    [Serializable]
    class LocatedObject : InspectorString<ServiceLocatorTester>
    {
        protected override bool IsEnabled(ServiceLocatorTester parentObject) => false;

        protected override string GetValue(ServiceLocatorTester parentObject) =>
            parentObject._located?.ToString();
    }
    
    [Serializable]
    class LoadableTypeObject : InspectorString<ServiceLocatorTester>
    {
        public Type GetTypeOfString()
        {
            if (string.IsNullOrEmpty(value))
                return null;
            
            return Type.GetType(value); 
        }

        protected override IList<string> PopupElements(ServiceLocatorTester container) => 
            Services.Environment.GetAllInstalledServiceTypes().Select(t =>t.FullName).ToList();
    }

}
