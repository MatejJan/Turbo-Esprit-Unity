using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Speedometer : Meter
    {
        protected override float GetValue()
        {
            return Mathf.Abs(car.speed) * PhysicsHelper.metersPerSecondToMilesPerHour;
        }
    }
}
