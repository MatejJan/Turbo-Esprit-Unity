using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit.Prototype.Driving.Simulation
{
    public class CarController : MonoBehaviour, IDashboardProvider
    {
        public WheelCollider wheelColliderFrontLeft;
        public WheelCollider wheelColliderFrontRight;
        public WheelCollider wheelColliderBackLeft;
        public WheelCollider wheelColliderBackRight;

        [Range(-1, 10000)] public float desiredEngineRpm;
        [Range(-1, 10000)] public float calculatedEngineRpm;
        [Range(-1, 2000)] public float frontWheelRpm;
        [Range(-1, 2000)] public float backWheelRpm;
        [Range(-1, 2000)] public float calculatedWheelRpm;
        [Range(-350, 350)] public float torque;
        [Range(-3500, 3500)] public float wheelTorque;
        [Range(-1, 1000)] public float brakeTorque;
        [Range(0, 1)] public float accelerator = 0;
        [Range(0, 1)] public float brake = 0;
        [Range(0, 1)] public float clutch = 0;
        [Range(0, 5)] public int gear = 1;
        [Range(-1, 1)] public float steering = 0;

        private new Rigidbody rigidbody;

        private Vector2[] torqueCurve = new[] {
            new Vector2(0, 0),
            new Vector2(2500, 320),
            new Vector2(4000, 350),
            new Vector2(6000, 310),
            new Vector2(7000, 260)
        };

        private float[] forwardGearRatios = new[] { 0, 2.92f, 1.94f, 1.32f, 0.97f, 0.75f };
        private float reverseGearRatio = -3.15f;
        private float finalDriveRatio = 3.88f;

        private float rpmRedlineValue = 7200;
        private float maxDesiredRpm = 7000;
        private float minDesiredRpm = 700;

        public float speedMph { get; private set; }
        public float engineRpm { get; private set; }

        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
        }

        private void FixedUpdate()
        {
            // Determine speed.
            float speed = Vector3.Dot(rigidbody.velocity, transform.forward);
            float absoluteSpeed = rigidbody.velocity.magnitude;

            float metersPerSecondToMilesPerHour = 2.23694f;
            speedMph = absoluteSpeed * metersPerSecondToMilesPerHour;

            // Control steering.
            float maxSteeringAngle = 30;
            float reducedMaxSteeringAngle = maxSteeringAngle - absoluteSpeed * 0.4f;

            steering = Input.GetAxis("Steering");
            float steerAngle = steering * reducedMaxSteeringAngle;

            wheelColliderFrontLeft.steerAngle = steerAngle;
            wheelColliderFrontRight.steerAngle = steerAngle;

            // Control torque.
            float totalDriveRatio = forwardGearRatios[gear] * finalDriveRatio;

            accelerator = Input.GetAxis("Accelerator");
            brake = Input.GetAxis("Brake");
            clutch = Input.GetAxis("Clutch");

            desiredEngineRpm = Mathf.Lerp(minDesiredRpm, maxDesiredRpm, accelerator);

            float wheelCircumference = Mathf.PI * 0.6f;
            calculatedWheelRpm = speed / wheelCircumference * 60;
            calculatedEngineRpm = calculatedWheelRpm * totalDriveRatio;

            float unboundEngineRpm = Mathf.Lerp(calculatedEngineRpm, desiredEngineRpm, clutch);
            engineRpm = Mathf.Min(unboundEngineRpm, rpmRedlineValue);

            float accelerationTorque = GetTorqueForRpm(engineRpm) * accelerator;

            float engineBrakingCoefficient = 0.74f;
            float engineBrakingTorque = engineBrakingCoefficient * engineRpm / 60;
            if (Mathf.Abs(engineBrakingTorque) < 1) engineBrakingTorque = 0;

            torque = accelerationTorque - engineBrakingTorque;
            wheelTorque = torque * totalDriveRatio * (1 - clutch);

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

            frontWheelRpm = wheelColliderFrontLeft.rpm;
            backWheelRpm = wheelColliderBackLeft.rpm;
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
