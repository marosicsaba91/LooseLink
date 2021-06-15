using System;
using System.Collections.Generic;

namespace LooseServices
{
public interface IServiceTypeProvider
{
    Dictionary<Type, List<Type>> ServiceToNonAbstractTypeMap { get; }
    Dictionary<Type, List<Type>> NonAbstractToServiceTypeMap { get; }

    IEnumerable<Type> ServiceTypes { get; }
    
}
}