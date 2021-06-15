using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace LooseServices
{
interface IServiceSourceSet
{
    List<ServiceSourceSetting> GetServiceSourceSettings();
    IEnumerable<ServiceSource> GetServiceSources();
    string Name { get; }
    Object Obj { get;}
    IServiceTypeProvider ServiceTypeProvider { get; }

    void Fresh();
}
}