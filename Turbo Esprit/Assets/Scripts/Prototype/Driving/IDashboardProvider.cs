using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public interface IDashboardProvider
    {
        float speedMph { get; }
        float engineRpm { get; }
    }
}
