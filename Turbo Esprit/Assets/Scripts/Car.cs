using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Car : MonoBehaviour
    {
        // Constants

        private const float wheelRpmSmoothingFactor = 0.8f;

        // Fields

        [SerializeField] private CarSpecifications specifications;

        [SerializeField] private WheelCollider wheelColliderFrontLeft;
        [SerializeField] private WheelCollider wheelColliderFrontRight;
        [SerializeField] private WheelCollider wheelColliderBackLeft;
        [SerializeField] private WheelCollider wheelColliderBackRight;

        private float wheelCircumference;
        private float wheelAngularMass;

        private float engineAngularSpeed = 0;
        private float driveShaftRpm = 0;

        private IgnitionSwitchPosition _ignitionSwitchPosition;

        // Enums

        public enum GearshiftPosition
        {
            Reverse = -1,
            Neutral = 0,
            FirstGear = 1,
            SecondGear = 2,
            ThirdGear = 3,
            FourthGear = 4,
            FifthGear = 5
        }

        public enum IgnitionSwitchPosition
        {
            Lock,
            Accessory,
            On,
            Start
        }

        public enum EngineState
        {
            Off,
            Starting,
            On
        }

        // Properties

        public float acceleratorPedalPosition { get; set; }
        public float brakePedalPosition { get; set; }
        public float clutchPedalPosition { get; set; }
        public float steeringWheelPosition { get; set; }
        public GearshiftPosition gearshiftPosition { get; set; }

        public IgnitionSwitchPosition ignitionSwitchPosition
        {
            get => _ignitionSwitchPosition;
            set
            {
                _ignitionSwitchPosition = value;

                if (_ignitionSwitchPosition == IgnitionSwitchPosition.Start && engineState == EngineState.Off)
                {
                    engineState = EngineState.Starting;
                }

                if (_ignitionSwitchPosition == IgnitionSwitchPosition.Lock || _ignitionSwitchPosition == IgnitionSwitchPosition.Accessory)
                {
                    engineState = EngineState.Off;
                }
            }
        }

        public float speed => driveShaftRpm / 60 * wheelCircumference;
        public float engineRpm => engineAngularSpeed * PhysicsHelper.angularSpeedToRpm;
        public EngineState engineState { get; private set; }

        // Methods

        private void Awake()
        {
            wheelCircumference = wheelColliderFrontLeft.radius * 2 * Mathf.PI;
            wheelAngularMass = wheelColliderFrontLeft.mass * Mathf.Pow(wheelColliderFrontLeft.radius, 2);
        }

        private void FixedUpdate()
        {
            HandleSteering();
            HandleEngine();
            HandleBrakes();
            HandleGearshift();
            ApplyAerodynamicForces();
        }

        private void HandleSteering()
        {
            float currentSteerAngleDegrees = wheelColliderFrontLeft.steerAngle;
            float targetSteerAngleDegrees = steeringWheelPosition * specifications.maxSteeringAngleDegrees;

            float maxSteeringAngleDeltaDegrees = specifications.maxSteeringAngleDeltaRateDegrees * Time.fixedDeltaTime;
            float newSteerAngleDegrees = Mathf.MoveTowards(currentSteerAngleDegrees, targetSteerAngleDegrees, maxSteeringAngleDeltaDegrees);

            wheelColliderFrontLeft.steerAngle = newSteerAngleDegrees;
            wheelColliderFrontRight.steerAngle = newSteerAngleDegrees;
        }

        private void HandleEngine()
        {
            // Calculate torque acting on the engine.
            float engineTorque = 0;

            // Add starter torque.
            if (engineState == EngineState.Starting)
            {
                engineTorque += specifications.starterTorque;
            }

            // Calculate air/fuel intake.
            float airFuelIntake = 0;

            if (engineState != EngineState.Off)
            {
                // Apply idle air control that boost up the intake at low RPM.
                float minAirFuelIntake = Mathf.Lerp(1, 0, engineRpm / specifications.idleAirControlStopRpm);

                // Apply rev limiter that reduces intake at high RPM.
                float revLimiterRangeRpm = specifications.revLimiterStopRpm - specifications.revLimiterStartRpm;
                float maxAirFuelIntake = Mathf.Lerp(1, 0, (engineRpm - specifications.revLimiterStartRpm) / revLimiterRangeRpm);

                airFuelIntake = Mathf.Lerp(minAirFuelIntake, maxAirFuelIntake, acceleratorPedalPosition);

                // Add torque from combustion.
                float maxCombustionTorque = GetMaxCombustionTorque();
                float combustionTorque = airFuelIntake * maxCombustionTorque;

                engineTorque += combustionTorque;
            }

            // Add torque from engine braking.
            float maxEngineBrakingTorque = -engineAngularSpeed * specifications.engineBrakingCoefficient;
            float engineBrakingTorque = maxEngineBrakingTorque * (1 - airFuelIntake);

            engineTorque += engineBrakingTorque;

            // Apply torque to engine.
            float engineAngularAcceleration = engineTorque / specifications.engineAngularMass;
            engineAngularSpeed += engineAngularAcceleration * Time.fixedDeltaTime;

            // Disengage starter.
            if (engineState == EngineState.Starting && engineRpm > specifications.starterStopRpm)
            {
                engineState = EngineState.On;
            }
        }

        private void HandleBrakes()
        {

        }

        private void HandleGearshift()
        {

        }

        private void ApplyAerodynamicForces()
        {

        }

        private float CalculateSmoothingParameter(float value)
        {
            return 1 - Mathf.Pow(1 - value, Time.fixedDeltaTime);
        }

        private float GetMaxCombustionTorque()
        {
            Vector2[] torqueCurve = specifications.torqueCurve;

            for (int i = 0; i < torqueCurve.Length - 1; i++)
            {
                if (engineRpm < torqueCurve[i + 1].x || i == torqueCurve.Length - 2)
                {
                    float sectionRpmRatio = (engineRpm - torqueCurve[i].x) / (torqueCurve[i + 1].x - torqueCurve[i].x);

                    float torque = Mathf.LerpUnclamped(torqueCurve[i].y, torqueCurve[i + 1].y, sectionRpmRatio);
                    return torque;
                }
            }

            return 0;
        }
    }
}
