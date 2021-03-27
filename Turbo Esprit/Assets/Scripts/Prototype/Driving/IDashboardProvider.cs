using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit.Prototype.Driving
{
    public interface IDashboardProvider
    {
        float speedMph { get; }
        float engineRpm { get; }
    }
}
