﻿using System;
using System.Collections.Generic;

namespace LooseServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class IgnoreServiceAttribute : Attribute
    {
        public readonly bool includeSubClasses;

        public IgnoreServiceAttribute()
        {
            includeSubClasses = false;
        }

        public IgnoreServiceAttribute(bool includeSubClasses)
        {
            this.includeSubClasses = includeSubClasses;
        }
    }

    static class IgnoreServiceAttributeHelper
    {
        internal static void RemoveIgnoredTypes(IList<Type> allAbstractServiceTypes)
        {
            for (int i = allAbstractServiceTypes.Count - 1; i >= 0; i--) 
                if(allAbstractServiceTypes[i].IsIgnoredLooseServices())
                    allAbstractServiceTypes.RemoveAt(i);
        }

        internal static bool IsIgnoredLooseServices(this Type type)
        {
            if (type.ContainsGenericParameters) return true;
            
            var inheritedAttribute = (IgnoreServiceAttribute)
                Attribute.GetCustomAttribute(type, typeof(IgnoreServiceAttribute), inherit: true);
            if (inheritedAttribute == null) return false;
            if (inheritedAttribute.includeSubClasses)
                return true;

            var attribute = (IgnoreServiceAttribute)
                Attribute.GetCustomAttribute(type, typeof(IgnoreServiceAttribute), inherit: false);
            return attribute != null;
        }
    }
}