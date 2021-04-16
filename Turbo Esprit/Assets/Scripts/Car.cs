using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Car : MonoBehaviour
    {
        // Constants

        private const float averageDriveWheelsRpmSmoothingDuration = 1;
        private const float minWheelRpmThreshold = 1;

        // Fields

        [SerializeField] private CarSpecifications specifications;

        [SerializeField] private WheelCollider wheelColliderFrontLeft;
        [SerializeField] private WheelCollider wheelColliderFrontRight;
        [SerializeField] private WheelCollider wheelColliderBackLeft;
        [SerializeField] private WheelCollider wheelColliderBackRight;

        private float engineAngularSpeed = 0;
        private float driveAxlesAngularSpeed = 0;

        private float[] averageDriveWheelsRpmHistory;
        private int nextAverageDriveWheelsRpmHistoryIndex = 0;
        private float averageDriveWheelsRpm = 0;

        private int drivenWheelsCount;
        private new Rigidbody rigidbody;
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
                if (_ignitionSwitchPosition == value) return;

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

        public float speed => driveAxlesAngularSpeed * wheelColliderFrontLeft.radius;
        public float engineRpm => engineAngularSpeed * PhysicsHelper.angularSpeedToRpm;
        public EngineState engineState { get; private set; }

        // Methods

        public float GetSteeringWheelPositionForDirection(Vector3 direction)
        {
            float angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
            return angle / specifications.maxSteeringAngleDegrees;
        }

        private void Awake()
        {
            drivenWheelsCount = specifications.drivetrainType == DrivetrainType.FourWheelDrive ? 4 : 2;
            rigidbody = GetComponent<Rigidbody>();

            int averageDriveWheelsRpmHistoryLength = Mathf.CeilToInt(averageDriveWheelsRpmSmoothingDuration / Time.fixedDeltaTime);
            averageDriveWheelsRpmHistory = new float[averageDriveWheelsRpmHistoryLength];
        }

        private void FixedUpdate()
        {
            UpdateDriveAxlesAngularSpeed();
            HandleSteering();
            HandleGearshift();
            HandleEngine();
            HandleBrakes();
            ApplyAerodynamicForces();
        }

        private void UpdateDriveAxlesAngularSpeed()
        {
            // Calculate the new average RPM of drive wheels.
            float newAverageDriveWheelsRpm = 0;

            if (specifications.drivetrainType != DrivetrainType.RearWheelDrive)
            {
                newAverageDriveWheelsRpm += wheelColliderFrontLeft.rpm + wheelColliderFrontRight.rpm;
            }

            if (specifications.drivetrainType != DrivetrainType.FrontWheelDrive)
            {
                newAverageDriveWheelsRpm += wheelColliderBackLeft.rpm + wheelColliderBackRight.rpm;
            }

            newAverageDriveWheelsRpm /= drivenWheelsCount;

            // Calculate the average drive wheels RPM as a moving average of previous values. 
            averageDriveWheelsRpmHistory[nextAverageDriveWheelsRpmHistoryIndex] = newAverageDriveWheelsRpm;
            nextAverageDriveWheelsRpmHistoryIndex = (nextAverageDriveWheelsRpmHistoryIndex + 1) % averageDriveWheelsRpmHistory.Length;

            averageDriveWheelsRpm = 0;
            foreach (float averageDriveWheelsRpmEntry in averageDriveWheelsRpmHistory)
            {
                averageDriveWheelsRpm += averageDriveWheelsRpmEntry;
            }
            averageDriveWheelsRpm /= averageDriveWheelsRpmHistory.Length;

            // Apply minimum treshold.
            if (Mathf.Abs(averageDriveWheelsRpm) < minWheelRpmThreshold) averageDriveWheelsRpm = 0;

            // Calculate new drive axles angular speed.
            driveAxlesAngularSpeed = averageDriveWheelsRpm * PhysicsHelper.rpmToAngularSpeed;
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

        private void HandleGearshift()
        {
            // Don't allow to shift into reverse until the car is stopped.
            if (gearshiftPosition == GearshiftPosition.Reverse && driveAxlesAngularSpeed > 0)
            {
                gearshiftPosition = GearshiftPosition.Neutral;
            }

            // Don't allow to shift into forward until the car is stopped.
            if (gearshiftPosition > GearshiftPosition.Neutral && driveAxlesAngularSpeed < 0)
            {
                gearshiftPosition = GearshiftPosition.Neutral;
            }
        }

        private void HandleEngine()
        {
            // Calculate total drive ratio and engine to transmission factor.
            float totalDriveRatio = GetTotalDriveRatio(gearshiftPosition);
            float engineToTransmissionFactor = 1 - clutchPedalPosition;

            if (gearshiftPosition == GearshiftPosition.Neutral)
            {
                engineToTransmissionFactor = 0;
            }

            // Calculate torque acting on the engine.
            float engineTorque = 0;

            // Add starter torque.
            if (engineState == EngineState.Starting)
            {
                engineTorque += specifications.starterTorque;
            }

            // Calculate needed torque applied to the engine to match drive axles angular speed.
            if (totalDriveRatio != 0)
            {
                float engineTargetAngularSpeed = driveAxlesAngularSpeed * totalDriveRatio;
                float engineAngularSpeedDifference = engineTargetAngularSpeed - engineAngularSpeed;
                float engineEqalizationTorque = engineAngularSpeedDifference * specifications.wheelsToEngineEqualizationFactor;

                // Calculate how much torque can actually be sent through the transmission.
                float loadTorque = engineEqalizationTorque * engineToTransmissionFactor / Mathf.Abs(totalDriveRatio);
                engineTorque += loadTorque;
            }

            // Calculate air/fuel intake.
            float airFuelIntake = 0;

            if (engineState != EngineState.Off)
            {
                // Add torque from combustion.
                airFuelIntake = GetAirFuelIntake(engineRpm, acceleratorPedalPosition);
                engineTorque += GetCombustionTorque(engineRpm, airFuelIntake);
            }

            // Add torque from engine braking.
            engineTorque += GetEngineBrakingTorque(engineAngularSpeed, airFuelIntake);

            // Apply torque to engine.
            float engineAngularAcceleration = engineTorque / specifications.engineAngularMass;
            engineAngularSpeed += engineAngularAcceleration * Time.fixedDeltaTime;

            // Turn off engine if it gets under zero.
            if (engineAngularSpeed < 0)
            {
                engineAngularSpeed = 0;
                engineState = EngineState.Off;
            }

            // Disengage starter.
            if (engineState == EngineState.Starting && engineRpm > specifications.starterStopRpm)
            {
                engineState = EngineState.On;
            }

            // Calculate needed torque applied to wheels to match engine angular speed.
            float wheelsTorque;

            if (totalDriveRatio != 0)
            {
                float driveAxlesTargetAngularSpeed = engineAngularSpeed / totalDriveRatio;
                float driveAxlesAngularSpeedDifference = driveAxlesTargetAngularSpeed - driveAxlesAngularSpeed;

                float driveAxlesEqalizationTorque = driveAxlesAngularSpeedDifference * specifications.engineToWheelsEqualizationFactor;

                // Calculate how much torque can actually be sent through the transmission.
                wheelsTorque = driveAxlesEqalizationTorque * engineToTransmissionFactor * Mathf.Abs(totalDriveRatio);
            }
            else
            {
                wheelsTorque = 0;
            }

            float wheelTorque = wheelsTorque / drivenWheelsCount;

            // Apply torque to front wheels.
            if (specifications.drivetrainType != DrivetrainType.RearWheelDrive)
            {
                wheelColliderFrontLeft.motorTorque = wheelTorque;
                wheelColliderFrontRight.motorTorque = wheelTorque;
            }

            // Apply torque to rear wheels.
            if (specifications.drivetrainType != DrivetrainType.FrontWheelDrive)
            {
                wheelColliderBackLeft.motorTorque = wheelTorque;
                wheelColliderBackRight.motorTorque = wheelTorque;
            }
        }

        private void HandleBrakes()
        {
            float brakeTorque = specifications.maxBrakingTorque * brakePedalPosition;

            wheelColliderFrontLeft.brakeTorque = brakeTorque;
            wheelColliderFrontRight.brakeTorque = brakeTorque;
            wheelColliderBackLeft.brakeTorque = brakeTorque;
            wheelColliderBackRight.brakeTorque = brakeTorque;
        }

        private void ApplyAerodynamicForces()
        {
            // Add downforce.
            float aerodynamicForceFactor = 0.5f * PhysicsHelper.airDensity * rigidbody.velocity.sqrMagnitude;

            Vector3 downforce = -transform.up * specifications.downforceCoefficient * aerodynamicForceFactor;
            rigidbody.AddForce(downforce);

            // Add drag force.
            Vector3 dragForce = -rigidbody.velocity.normalized * specifications.dragForceCoefficient * specifications.frontalArea * aerodynamicForceFactor;
            rigidbody.AddForce(dragForce);
        }

        private float GetTotalDriveRatio(GearshiftPosition gearshiftPosition)
        {
            switch (gearshiftPosition)
            {
                case GearshiftPosition.Reverse:
                    return specifications.reverseGearRatio * specifications.finalDriveRatio;

                case GearshiftPosition.Neutral:
                    return 0;

                default:
                    int gear = (int)gearshiftPosition;
                    return specifications.forwardGearRatios[gear] * specifications.finalDriveRatio;
            }
        }

        private float GetAirFuelIntake(float engineRpm, float acceleratorPedalPosition)
        {
            // Apply idle air control that boost up the intake at low RPM.
            float minAirFuelIntake = Mathf.Lerp(1, 0, engineRpm / specifications.idleAirControlStopRpm);

            // Apply rev limiter that reduces intake at high RPM.
            float revLimiterRangeRpm = specifications.revLimiterStopRpm - specifications.revLimiterStartRpm;
            float maxAirFuelIntake = Mathf.Lerp(1, 0, (engineRpm - specifications.revLimiterStartRpm) / revLimiterRangeRpm);

            return Mathf.Lerp(minAirFuelIntake, maxAirFuelIntake, acceleratorPedalPosition);
        }

        private float GetCombustionTorque(float engineRpm, float airFuelIntake)
        {
            float maxCombustionTorque = GetMaxCombustionTorque(engineRpm);
            return maxCombustionTorque * airFuelIntake;
        }

        private float GetMaxCombustionTorque(float engineRpm)
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

        private float GetEngineBrakingTorque(float engineAngularSpeed, float airFuelIntake)
        {
            float maxEngineBrakingTorque = -engineAngularSpeed * specifications.engineBrakingCoefficient;
            return maxEngineBrakingTorque * (1 - airFuelIntake);
        }
    }
}
