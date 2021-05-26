﻿using System.Collections.Generic;
using UnityEngine;

namespace LooseServices
{
interface IServiceInstaller
{
    IEnumerable<ServiceSource> GetServiceSources();
    string Name { get; }
    Object Obj { get;}
}
}