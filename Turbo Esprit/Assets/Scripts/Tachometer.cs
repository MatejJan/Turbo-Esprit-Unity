using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Tachometer : Meter
    {
        protected override float GetValue()
        {
            return car.engineRpm;
        }
    }
}
