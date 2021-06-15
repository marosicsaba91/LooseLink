using System;
using System.Collections.Generic;
using LooseServices;
 
[GlobalServiceType]
public class MyGlobalServiceTypeProvider : ServiceTypeProvider
{
    public override IEnumerable<Type> LocalServiceTypes()
    { 
        yield return typeof(IScreenSizeProvider);
        yield return typeof(SystemTimeProvider);
    }
}