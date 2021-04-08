using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Car : MonoBehaviour
    {
        [Range(0, 1)] public float accelerator = 0;
        [Range(0, 1)] public float brake = 0;
        [Range(0, 1)] public float clutch = 0;
        [Range(0, 5)] public int gear = 1;
        [Range(-1, 1)] public float steering = 0;
        [Range(-1000, 0)] public float loadTorque = 0;
        [Range(-1, 1000)] public float engineTorque = 0;
        [Range(-1, 3000)] public float wheelTorque = 0;
        [Range(-1, 1000)] public float engineWheelsAngularSpeed = 0;
        [Range(-1, 1000)] public float engineActualAngularSpeed = 0;

        // Constants

        private const float loadTorqueEstimationFactor = -0.3f;
        private const float wheelRpmSmoothingFactor = 0.8f;
        private const float minWheelRpmThreshold = 1;

        // Fields

        [SerializeField] private CarSpecifications specifications;

        [SerializeField] private WheelCollider wheelColliderFrontLeft;
        [SerializeField] private WheelCollider wheelColliderFrontRight;
        [SerializeField] private WheelCollider wheelColliderBackLeft;
        [SerializeField] private WheelCollider wheelColliderBackRight;

        private float engineAngularSpeed = 0;
        private float averageDriveWheelsRpm = 0;
        private float driveAxlesAngularSpeed = 0;
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

        public float GetTorqueForGearshiftPositionAtSpeed(GearshiftPosition gearshiftPosition, float speed)
        {
            // Calculate total drive ratio.
            float totalDriveRatio = 0;

            if (gearshiftPosition == GearshiftPosition.Reverse)
            {
                totalDriveRatio = specifications.reverseGearRatio * specifications.finalDriveRatio;
            }
            else if (gearshiftPosition != GearshiftPosition.Neutral)
            {
                int gear = (int)gearshiftPosition;
                totalDriveRatio = specifications.forwardGearRatios[gear] * specifications.finalDriveRatio;
            }
            float wheelAngularSpeed = speed / wheelColliderFrontLeft.radius;
            float engineAngularSpeed = wheelAngularSpeed * totalDriveRatio;
            float engineRpm = engineAngularSpeed * PhysicsHelper.angularSpeedToRpm;

            // Estimate load torque.
            float engineAngularSpeedDifference = engineAngularSpeed * loadTorqueEstimationFactor;
            float engineEqalizationTorque = engineAngularSpeedDifference * specifications.wheelsToEngineEqualizationFactor;
            float loadTorque = engineEqalizationTorque / Mathf.Abs(totalDriveRatio);

            // Calculate engine torque.
            float airFuelIntake = GetAirFuelIntake(engineRpm, 1);
            float combutionsTorque = GetCombustionTorque(engineRpm, airFuelIntake);
            float engineBrakingTorque = GetEngineBrakingTorque(engineAngularSpeed, airFuelIntake);

            return (loadTorque + combutionsTorque + engineBrakingTorque) * totalDriveRatio;
        }

        private void Awake()
        {
            drivenWheelsCount = specifications.drivetrainType == DrivetrainType.FourWheelDrive ? 4 : 2;
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

            accelerator = acceleratorPedalPosition;
            brake = brakePedalPosition;
            clutch = clutchPedalPosition;
            steering = steeringWheelPosition;
            gear = (int)gearshiftPosition;
        }

        private void UpdateDriveAxlesAngularSpeed()
        {
            // Calculate new average drive wheel RPM.
            float newAverageDriveWheelRpm = 0;

            if (specifications.drivetrainType != DrivetrainType.RearWheelDrive)
            {
                newAverageDriveWheelRpm += wheelColliderFrontLeft.rpm + wheelColliderFrontRight.rpm;
            }

            if (specifications.drivetrainType != DrivetrainType.FrontWheelDrive)
            {
                newAverageDriveWheelRpm += wheelColliderBackLeft.rpm + wheelColliderBackRight.rpm;
            }

            newAverageDriveWheelRpm /= drivenWheelsCount;

            // Smooth the RPM value to slowly change over time.
            float wheelRpmSmoothingParameter = CalculateSmoothingParameter(wheelRpmSmoothingFactor);
            averageDriveWheelsRpm = Mathf.Lerp(averageDriveWheelsRpm, newAverageDriveWheelRpm, wheelRpmSmoothingParameter);

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

                this.loadTorque = loadTorque;
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

            this.engineTorque = engineTorque;
            this.wheelTorque = wheelTorque;

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
