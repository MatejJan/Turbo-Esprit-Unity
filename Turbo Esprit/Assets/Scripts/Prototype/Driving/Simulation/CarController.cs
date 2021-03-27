using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit.Prototype.Driving.Simulation
{
    public class CarController : MonoBehaviour, IDashboardProvider
    {
        // Car constants

        private readonly Vector2[] torqueCurve = new[] {
            new Vector2(0, 0),
            new Vector2(2500, 320),
            new Vector2(4000, 350),
            new Vector2(6000, 310),
            new Vector2(7000, 260)
        };

        private readonly float[] forwardGearRatios = new[] { 0, 2.92f, 1.94f, 1.32f, 0.97f, 0.75f };
        private const float reverseGearRatio = -3.15f;
        private const float finalDriveRatio = 3.88f;
        private const int poweredWheelsCount = 2;
        private const float wheelCircumference = Mathf.PI * 0.6f;
        private const float idleRpm = 600;
        private const float idleAngularSpeed = idleRpm / 30 * Mathf.PI;
        private const float redlineValueRpm = 7200;
        private const float engineAngularMass = 1;

        // Driver constants

        private const float maxDesiredRpm = 7000;
        private const float minDesiredRpm = 700;

        // Other constants

        private const float wheelRpmSmoothingFactor = 0.8f;
        private const float metersPerSecondToMilesPerHour = 2.23694f;

        private const float engineEqualizationFactor = 0.95f;
        private const float torqueTransferEqualizationTime = 0.1f;

        // Fields

        public WheelCollider wheelColliderFrontLeft;
        public WheelCollider wheelColliderFrontRight;
        public WheelCollider wheelColliderBackLeft;
        public WheelCollider wheelColliderBackRight;

        [Range(-1, 1000)] public float engineAngularSpeed;
        [Range(-1, 1000)] public float wheelBasedEngineAngularSpeed;
        [Range(-1, 2000)] public float frontWheelRpm;
        [Range(-1, 2000)] public float backWheelRpm;
        [Range(-1, 2000)] public float backWheelRpmSmooth;
        [Range(-100, 500)] public float wheelAngularSpeed;
        [Range(-100, 500)] public float engineBasedWheelAngularSpeed;
        [Range(-1, 2000)] public float wheelRpmAssumingFullTraction;
        [Range(-350, 350)] public float torque;
        [Range(-3500, 3500)] public float wheelTorque;
        [Range(-1, 1000)] public float brakeTorque;
        [Range(0, 1)] public float accelerator = 0;
        [Range(0, 1)] public float brake = 0;
        [Range(0, 1)] public float clutch = 0;
        [Range(0, 5)] public int gear = 1;
        [Range(-1, 1)] public float steering = 0;

        private new Rigidbody rigidbody;
        private float wheelAngularMass;

        // Properties

        public float speedMph { get; private set; }
        public float engineRpm { get; private set; }

        // Initialization

        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            wheelAngularMass = wheelColliderFrontLeft.mass * Mathf.Pow(wheelColliderFrontLeft.radius, 2);
        }

        // Update

        private void FixedUpdate()
        {
            // Calculate derived variables.
            float totalDriveRatio = forwardGearRatios[gear] * finalDriveRatio;

            // Read values from other systems.
            frontWheelRpm = wheelColliderFrontLeft.rpm;
            backWheelRpm = wheelColliderBackLeft.rpm;

            float wheelRpmSmoothingParameter = GetSmoothingParameter(wheelRpmSmoothingFactor);
            backWheelRpmSmooth = Mathf.Lerp(backWheelRpmSmooth, backWheelRpm > 0.1 ? backWheelRpm : 0, wheelRpmSmoothingParameter);
            wheelAngularSpeed = backWheelRpmSmooth / 30 * Mathf.PI;

            // Determine speed.
            wheelRpmAssumingFullTraction = engineRpm / totalDriveRatio;
            float speed = backWheelRpmSmooth / 60 * wheelCircumference;

            speedMph = speed * metersPerSecondToMilesPerHour;

            float absoluteSpeed = Mathf.Abs(speed);

            // Control steering.
            float maxSteeringAngle = 30;
            float reducedMaxSteeringAngle = maxSteeringAngle - absoluteSpeed * 0.4f;

            steering = Input.GetAxis("Steering");
            float steerAngle = steering * reducedMaxSteeringAngle;

            wheelColliderFrontLeft.steerAngle = steerAngle;
            wheelColliderFrontRight.steerAngle = steerAngle;

            // Control torque.

            accelerator = Input.GetAxis("Accelerator");
            brake = Input.GetAxis("Brake");
            clutch = Input.GetAxis("Clutch");

            // Equalize engine and wheel rotation
            wheelBasedEngineAngularSpeed = wheelAngularSpeed * totalDriveRatio;

            if (wheelBasedEngineAngularSpeed < engineAngularSpeed)
            {
                float equalizationParameter = GetSmoothingParameter(engineEqualizationFactor) * (1 - clutch);
                engineAngularSpeed = Mathf.Lerp(engineAngularSpeed, wheelBasedEngineAngularSpeed, equalizationParameter);
            }

            // Calculate new engine angular speed.
            engineRpm = engineAngularSpeed * 30 / Mathf.PI;

            float engineTorque = GetTorqueForRpm(engineRpm);
            float accelerationTorque = engineTorque * accelerator;

            float engineBrakingCoefficient = 2;
            float engineRpmAboveIdle = Mathf.Max(0, engineRpm - idleRpm);
            float engineBrakingTorque = engineBrakingCoefficient * engineRpmAboveIdle / 60;

            torque = accelerationTorque - engineBrakingTorque;

            float engineAngularAcceleration = torque / engineAngularMass;
            engineAngularSpeed += engineAngularAcceleration * Time.fixedDeltaTime;

            // Transfer torque to the wheels.
            engineBasedWheelAngularSpeed = engineAngularSpeed / totalDriveRatio;

            float angularSpeedDifference = engineBasedWheelAngularSpeed - wheelAngularSpeed;
            float equalizationAngularAcceleration = angularSpeedDifference / torqueTransferEqualizationTime;
            float eqalizationTorque = equalizationAngularAcceleration * wheelAngularMass;

            wheelTorque = Mathf.Clamp(eqalizationTorque, 0, engineTorque * totalDriveRatio) * (1 - clutch);

            wheelColliderBackLeft.motorTorque = wheelTorque;
            wheelColliderBackRight.motorTorque = wheelTorque;

            // Apply braking.
            float maxManualBrakingTorque = 1000;
            brakeTorque = maxManualBrakingTorque * brake;

            wheelColliderFrontLeft.brakeTorque = brakeTorque;
            wheelColliderFrontRight.brakeTorque = brakeTorque;
            wheelColliderBackLeft.brakeTorque = brakeTorque;
            wheelColliderBackRight.brakeTorque = brakeTorque;

            // Add downforce.
            float airDensity = 1.225f;
            float aerodynamicForceFactor = 0.5f * airDensity * rigidbody.velocity.sqrMagnitude;

            float downforceCoefficient = 2;
            Vector3 downforce = -transform.up * downforceCoefficient * aerodynamicForceFactor;
            rigidbody.AddForce(downforce);

            // Add drag force.
            float dragForceCoefficient = 0.3f;
            float frontalArea = 1.85f;
            Vector3 dragForce = -rigidbody.velocity.normalized * dragForceCoefficient * frontalArea * aerodynamicForceFactor;
            rigidbody.AddForce(dragForce);

        }

        private void Update()
        {
            if (Input.GetButtonDown("Shift up"))
            {
                gear++;
            }

            if (Input.GetButtonDown("Shift down"))
            {
                gear--;
            }

            if (Input.GetButtonDown("Ignition"))
            {
                StartCoroutine(IgnitionCoroutine());
            }
        }

        IEnumerator IgnitionCoroutine()
        {
            while (engineAngularSpeed < idleAngularSpeed)
            {
                engineAngularSpeed += idleAngularSpeed * Time.deltaTime;
                yield return null;
            }
        }

        // Helpers

        private float GetSmoothingParameter(float value)
        {
            return 1 - Mathf.Pow(1 - value, Time.fixedDeltaTime);
        }

        private float GetTorqueForRpm(float rpm)
        {
            for (int i = 0; i < torqueCurve.Length - 1; i++)
            {
                if (rpm < torqueCurve[i + 1].x || i == torqueCurve.Length - 2)
                {
                    float sectionRpmRatio = (rpm - torqueCurve[i].x) / (torqueCurve[i + 1].x - torqueCurve[i].x);

                    float torque = Mathf.LerpUnclamped(torqueCurve[i].y, torqueCurve[i + 1].y, sectionRpmRatio);
                    return torque;
                }
            }

            return 0;
        }
    }
}
