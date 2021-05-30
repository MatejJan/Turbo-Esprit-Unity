using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class TrafficDriver : Driver
    {
        protected override void Update()
        {
            UpdateTargetSpeed();
            base.Update();
        }

        private void UpdateTargetSpeed()
        {
            targetSpeed = Traffic.speedPerLaneMph[Mathf.Clamp(carTracker.currentLane, 0, 3)] * PhysicsHelper.milesPerHourToMetersPerSecond;
        }
    }
}
