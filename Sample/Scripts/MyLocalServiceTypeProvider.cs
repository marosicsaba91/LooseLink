using System;
using System.Collections.Generic;
using LooseServices;
using UnityEngine;
 
[CreateAssetMenu]
public class MyLocalServiceTypeProvider : ServiceTypeProvider
{
    public override IEnumerable<Type> LocalServiceTypes()
    {
        yield return typeof(Camera);
        yield return typeof(AudioListener); 
    }
}