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
        public DrivetrainType drivetrainType;
        public float redlineValueRpm;
        public float engineAngularMass;
        public float wheelsToEngineEqualizationFactor;
        public float engineToWheelsEqualizationFactor;
        public float engineBrakingCoefficient;
        public float maxSteeringAngleDegrees;
        public float maxSteeringAngleDeltaRateDegrees;
        public float maxBrakingTorque;
        public float starterTorque;
        public float starterStopRpm;
        public float idleAirControlStopRpm;
        public float revLimiterStartRpm;
        public float revLimiterStopRpm;
        public float downforceCoefficient;
        public float dragForceCoefficient;
        public float frontalArea;
    }

    public enum DrivetrainType
    {
        FrontWheelDrive,
        RearWheelDrive,
        FourWheelDrive
    }
}
