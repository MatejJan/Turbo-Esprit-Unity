using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    [CreateAssetMenu(fileName = "DriverProfile", menuName = "Scriptable Objects/Driver Profile")]
    public class DriverProfile : ScriptableObject
    {
        public float normalPedalSpeed;
        public float shiftingPedalSpeed;
        public float movingOffPedalSpeed;
        public float brakingPedalSpeed;

        public float acceleratorChangeFactor;
        public float speedEqualizationDuration;

        public float minBrakingSpeedDifferenceMph;
        public float maxBrakingSpeedDifferenceMph;

        public float upshiftEngineRpm;
        public float upshiftEngineRpmWhenCloseToTarget;
        public float downshiftEngineRpm;
        public float closeSpeedDifferenceMph;
        public float shiftingDuration;

        public float maxIdlingDuration;

        public float steeringWheelSpeed;
        public float steeringWheelLimitHalvingSpeedMph;

        public float drivingStraightBaseAllowedAngleDegrees;
        public float drivingStraightAllowedAngleHalvingSpeedMph;

        // Properties in SI units

        public float minBrakingSpeedDifference => minBrakingSpeedDifferenceMph * PhysicsHelper.milesPerHourToMetersPerSecond;
        public float maxBrakingSpeedDifference => maxBrakingSpeedDifferenceMph * PhysicsHelper.milesPerHourToMetersPerSecond;

        public float closeSpeedDifference => closeSpeedDifferenceMph * PhysicsHelper.milesPerHourToMetersPerSecond;

        public float steeringWheelLimitHalvingSpeed => steeringWheelLimitHalvingSpeedMph * PhysicsHelper.milesPerHourToMetersPerSecond;
        public float drivingStraightAllowedAngleHalvingSpeed => drivingStraightAllowedAngleHalvingSpeedMph * PhysicsHelper.milesPerHourToMetersPerSecond;
    }
}
