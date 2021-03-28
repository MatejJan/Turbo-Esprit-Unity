using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Car : MonoBehaviour
    {
        // Constants

        private const float wheelRpmSmoothingFactor = 0.8f;
        private const float minWheelRpmThreshold = 1;

        // Fields

        [SerializeField] private CarSpecifications specifications;

        [SerializeField] private WheelCollider wheelColliderFrontLeft;
        [SerializeField] private WheelCollider wheelColliderFrontRight;
        [SerializeField] private WheelCollider wheelColliderBackLeft;
        [SerializeField] private WheelCollider wheelColliderBackRight;

        private float engineAngularSpeed = 0;
        private float averageDriveWheelRpm = 0;
        private float driveAxlesAngularSpeed = 0;
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

        public float speed => Mathf.Abs(driveAxlesAngularSpeed) * wheelColliderFrontLeft.radius;
        public float engineRpm => engineAngularSpeed * PhysicsHelper.angularSpeedToRpm;
        public EngineState engineState { get; private set; }

        // Methods

        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
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
            // Calculate new average drive wheel RPM.
            float newAverageDriveWheelRpm = 0;
            float drivenWheelsCount = 0;

            if (specifications.drivetrainType != DrivetrainType.RearWheelDrive)
            {
                newAverageDriveWheelRpm += wheelColliderFrontLeft.rpm + wheelColliderFrontRight.rpm;
                drivenWheelsCount += 2;
            }

            if (specifications.drivetrainType != DrivetrainType.FrontWheelDrive)
            {
                newAverageDriveWheelRpm += wheelColliderBackLeft.rpm + wheelColliderBackRight.rpm;
                drivenWheelsCount += 2;
            }

            newAverageDriveWheelRpm /= drivenWheelsCount;

            // Smooth the RPM value to slowly change over time.
            float wheelRpmSmoothingParameter = CalculateSmoothingParameter(wheelRpmSmoothingFactor);
            averageDriveWheelRpm = Mathf.Lerp(averageDriveWheelRpm, newAverageDriveWheelRpm, wheelRpmSmoothingParameter);

            // Apply minimum treshold.
            if (Mathf.Abs(averageDriveWheelRpm) < minWheelRpmThreshold) averageDriveWheelRpm = 0;

            // Calculate new drive axles angular speed.
            driveAxlesAngularSpeed = averageDriveWheelRpm * PhysicsHelper.rpmToAngularSpeed;
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
            float totalDriveRatio = 0;
            float engineToTransmissionFactor = 1 - clutchPedalPosition;

            if (gearshiftPosition == GearshiftPosition.Neutral)
            {
                engineToTransmissionFactor = 0;
            }
            else if (gearshiftPosition == GearshiftPosition.Reverse)
            {
                totalDriveRatio = specifications.reverseGearRatio * specifications.finalDriveRatio;
            }
            else
            {
                int gear = (int)gearshiftPosition;
                totalDriveRatio = specifications.forwardGearRatios[gear] * specifications.finalDriveRatio;
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
            float maxCombustionTorque = GetMaxCombustionTorque();

            if (engineState != EngineState.Off)
            {
                // Apply idle air control that boost up the intake at low RPM.
                float minAirFuelIntake = Mathf.Lerp(1, 0, engineRpm / specifications.idleAirControlStopRpm);

                // Apply rev limiter that reduces intake at high RPM.
                float revLimiterRangeRpm = specifications.revLimiterStopRpm - specifications.revLimiterStartRpm;
                float maxAirFuelIntake = Mathf.Lerp(1, 0, (engineRpm - specifications.revLimiterStartRpm) / revLimiterRangeRpm);

                airFuelIntake = Mathf.Lerp(minAirFuelIntake, maxAirFuelIntake, acceleratorPedalPosition);

                // Add torque from combustion.
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
            float wheelTorque;

            if (totalDriveRatio != 0)
            {
                float driveAxlesTargetAngularSpeed = engineAngularSpeed / totalDriveRatio;
                float driveAxlesAngularSpeedDifference = driveAxlesTargetAngularSpeed - driveAxlesAngularSpeed;
                float driveAxlesEqalizationTorque = driveAxlesAngularSpeedDifference * specifications.engineToWheelsEqualizationFactor;

                // Calculate how much torque can actually be sent through the transmission.
                wheelTorque = driveAxlesEqalizationTorque * engineToTransmissionFactor * Mathf.Abs(totalDriveRatio);
            }
            else
            {
                wheelTorque = 0;
            }

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
