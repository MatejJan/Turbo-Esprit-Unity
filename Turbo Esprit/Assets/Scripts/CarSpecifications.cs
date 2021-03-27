using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    [CreateAssetMenu(fileName = "CarSpecifications", menuName = "Scriptable Objects/Car Specifications")]
    public class CarSpecifications : ScriptableObject
    {
        public Vector2[] torqueCurve;
        public float reverseGearRatio;
        public float[] forwardGearRatios;
        public float finalDriveRatio;
        public float redlineValueRpm;
        public float engineAngularMass;
        public float wheelsToEngineEqualizationTime;
        public float engineToWheelsEqualizationTime;
        public float engineBrakingCoefficient;
        public float maxSteeringAngleDegrees;
        public float maxSteeringAngleDeltaRateDegrees;
        public float maxBrakingTorque;
        public float starterTorque;
        public float starterStopRpm;
        public float idleAirControlStopRpm;
        public float revLimiterStartRpm;
        public float revLimiterStopRpm;
    }
}
