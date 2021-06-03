using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    [CreateAssetMenu(fileName = "TrafficDriver", menuName = "Scriptable Objects/Traffic Driver")]
    public class TrafficDriver : Driver.Controller
    {
        public override void Act(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            UpdateTargetSpeed(sensors, actuators);
        }

        private void UpdateTargetSpeed(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            actuators.targetSpeed = Traffic.speedPerLaneMph[Mathf.Clamp(sensors.carTracker.currentLane, 0, 3)] * PhysicsHelper.milesPerHourToMetersPerSecond;
        }
    }
}
