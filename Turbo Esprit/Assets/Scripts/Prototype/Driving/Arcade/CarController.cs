using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit.Prototype.Driving.Arcade
{
    public class CarController : MonoBehaviour
    {
        public Transform wheelFrontLeftTransform;
        public Transform wheelFrontRightTransform;
        public Transform wheelBackLeftTransform;
        public Transform wheelBackRightTransform;

        public float mass = 1225;
        public float steeringFactor = 1;
        public float steeringPower = -1;

        [Range(0, 150)] public float speedMph;
        [Range(-1, 10000)] public float engineRpm;
        [Range(-350, 350)] public float torque;

        [Range(0, 1)] public float accelerator = 0;
        [Range(0, 1)] public float brake = 0;
        [Range(0, 5)] public int gear = 1;
        [Range(-1, 1)] public float steering = 0;

        private float speed = 0;
        private float carRotationDegrees = 0;
        private float wheelRotationDegrees = 0;

        private Vector2[] torqueCurve = new[] {
            new Vector2(0, 0),
            new Vector2(2500, 320),
            new Vector2(4000, 350),
            new Vector2(6000, 310),
            new Vector2(7000, 260),
            new Vector2(7200, 0)
        };

        private float[] forwardGearRatios = new[] { 0, 2.92f, 1.94f, 1.32f, 0.97f, 0.75f };
        private float reverseGearRatio = -3.15f;
        private float finalDriveRatio = 3.88f;

        private float rpmRedlineValue = 7200;
        private float maxDesiredRpm = 7000;
        private float minDesiredRpm = 700;

        private void FixedUpdate()
        {
            // Determine speed.
            float absoluteSpeed = Mathf.Abs(speed);

            float metersPerSecondToMilesPerHour = 2.23694f;
            speedMph = speed * metersPerSecondToMilesPerHour;

            // Apply rotation to wheels.
            float wheelRadius = 0.3f;
            float wheelCircumference = 2 * Mathf.PI * wheelRadius;
            float distanceMoved = speed * Time.fixedDeltaTime;
            float wheelMovedRotations = distanceMoved / wheelCircumference;
            float wheelMovedRotationDegrees = wheelMovedRotations * 360;
            wheelRotationDegrees += wheelMovedRotationDegrees;

            wheelBackLeftTransform.localRotation = Quaternion.Euler(wheelRotationDegrees, 0, 0);
            wheelBackRightTransform.localRotation = Quaternion.Euler(wheelRotationDegrees, 0, 0);

            // Control steering.
            steering = Input.GetAxis("Steering");

            float maxSteeringAngle = 30;
            float steerAngle = steering * (maxSteeringAngle - absoluteSpeed * 0.4f);

            wheelFrontLeftTransform.localRotation = Quaternion.Euler(wheelRotationDegrees, steerAngle, 0);
            wheelFrontRightTransform.localRotation = Quaternion.Euler(wheelRotationDegrees, steerAngle, 0);

            float turningAmountDegrees = steering * steeringFactor * Mathf.Pow(speed, steeringPower);
            carRotationDegrees += turningAmountDegrees * Time.fixedDeltaTime;

            transform.localRotation = Quaternion.Euler(0, carRotationDegrees, 0);

            // Control acceleration.
            float totalDriveRatio = forwardGearRatios[gear] * finalDriveRatio;

            float calculatedWheelRpm = speed / wheelCircumference * 60;
            float calculatedEngineRpm = calculatedWheelRpm * totalDriveRatio;

            engineRpm = Mathf.Max(calculatedEngineRpm, minDesiredRpm);

            accelerator = Input.GetAxis("Accelerator");
            float accelerationTorque = accelerator * GetTorqueForRpm(engineRpm);

            float engineBrakingCoefficient = 0.74f;
            float engineBrakingTorque = engineBrakingCoefficient * calculatedEngineRpm / 60;

            torque = accelerationTorque - engineBrakingTorque;
            float wheelsTorque = torque * totalDriveRatio;
            float wheelsForce = wheelsTorque / wheelRadius;

            // Add drag force.
            float airDensity = 1.225f;
            float aerodynamicForceFactor = 0.5f * airDensity * Mathf.Pow(speed, 2);
            float dragForceCoefficient = 0.3f;
            float frontalArea = 1.85f;
            float dragForce = -dragForceCoefficient * frontalArea * aerodynamicForceFactor;

            // Change speed.
            float acceleration = (wheelsForce + dragForce) / mass;

            float maxBrakingDeceleration = 8;
            float brakingSpeedMultiplier = Mathf.Min(1, speed);

            brake = Input.GetAxis("Brake");
            float brakingDeceleration = brake * maxBrakingDeceleration * brakingSpeedMultiplier;

            speed += (acceleration - brakingDeceleration) * Time.fixedDeltaTime;

            // Move car.
            transform.position += transform.forward * speed * Time.fixedDeltaTime;
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
                    return Mathf.Max(torque, 0);
                }
            }

            return 0;
        }
    }
}
