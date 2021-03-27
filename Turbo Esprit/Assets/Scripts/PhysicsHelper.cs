using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class PhysicsHelper
    {
        public const float metersPerSecondToMilesPerHour = 2.23694f;
        public const float angularSpeedToRpm = 30 / Mathf.PI;
        public const float rpmToAngularSpeed = Mathf.PI / 30;
        public const float airDensity = 1.225f;
    }
}
