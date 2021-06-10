using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    [CreateAssetMenu(fileName = "TrafficDriver", menuName = "Scriptable Objects/Traffic Driver")]
    public class TrafficDriver : Driver.Controller
    {
        private const float obstacleSpeedSmoothTime = 0.5f;
        private const float distanceSmoothTime = 0.5f;

        // The amount of time that should pass before this car reaches the position where the car in front is.
        [SerializeField] private float safeDistanceTime;

        // The minimal safe distance, no matter what the speed is.
        [SerializeField] private float minSafeDistance;

        // At what multiple of safe distance should we aim to match obstacle speed.
        [SerializeField] private float speedMatchingSafeDistanceFactor;

        // How fast should the obstacle move to consider it as a moving obstacle.
        [SerializeField] private float minimumRecognizableObstacleSpeed;

        // How fast should the obstacle be moving away from us to disregard it in our reasoning.
        [SerializeField] private float minimumMovingAwayRelativeSpeed;

        // Where should the front raycasts originate from.
        [SerializeField] private Vector3 frontRaycastOriginOffset;

        // How far should we raycast.
        [SerializeField] private float maxRaycastDistance;

        private float frontDistance;
        private float frontDistanceSmoothVelocity;

        private float frontObstacleSpeed;
        private float frontObstacleSpeedSmoothVelocity;

        public override void Act(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            UpdateTargetSpeed(sensors, actuators);
        }

        private void UpdateTargetSpeed(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            // Calculate desired parameters.
            float desiredLaneSpeed = Traffic.speedPerLaneMph[Mathf.Clamp(sensors.carTracker.currentLane, 0, 3)] * PhysicsHelper.milesPerHourToMetersPerSecond;

            float safeDistance = Mathf.Max(minSafeDistance, safeDistanceTime * sensors.car.speed);

            // Shoot a ray out to see how far the distance is to the obstacle in the front.
            Transform carTransform = sensors.car.transform;

            RaycastHit hitInfo;
            Vector3 origin = carTransform.position + carTransform.TransformVector(frontRaycastOriginOffset);
            Vector3 forward = carTransform.forward;
            forward.y = 0;
            forward.Normalize();

            float newFrontDistance;

            if (Physics.Raycast(origin, forward, out hitInfo, maxRaycastDistance))
            {
                // Because the raycasts fluctuate, smooth the measurement for more stable behavior.
                newFrontDistance = Mathf.SmoothDamp(frontDistance, hitInfo.distance, ref frontDistanceSmoothVelocity, distanceSmoothTime);
            }
            else
            {
                newFrontDistance = maxRaycastDistance;
            }

            // Only make any considerations if we can figure out the relative speed.
            if (frontDistance < maxRaycastDistance && newFrontDistance < maxRaycastDistance)
            {
                // Determine obstacle speed. Because the raycasts fluctuate, smooth the measurement for more stable behavior.
                float obstacleRelativeSpeed = (newFrontDistance - frontDistance) / Time.deltaTime;
                float newObstacleSpeed = sensors.car.speed + obstacleRelativeSpeed;
                frontObstacleSpeed = Mathf.SmoothDamp(frontObstacleSpeed, newObstacleSpeed, ref frontObstacleSpeedSmoothVelocity, obstacleSpeedSmoothTime);

                // If speed is low enough, consider it to be a static obstacle.
                if (newObstacleSpeed < minimumRecognizableObstacleSpeed && frontObstacleSpeed < minimumRecognizableObstacleSpeed) frontObstacleSpeed = 0;

                // If we're closer than the safe distance to the obstacle, stop!
                float distanceToSafeDistance = newFrontDistance - safeDistance;

                if (distanceToSafeDistance < 0)
                {
                    actuators.targetSpeed = 0;
                }
                else
                {
                    // Nothing to do if the obstacle is moving away.
                    if (obstacleRelativeSpeed > minimumMovingAwayRelativeSpeed)
                    {
                        actuators.targetSpeed = desiredLaneSpeed;
                    }
                    else
                    {
                        // Match obstacle's speed the closer you are to the safe distance (at which point you should exactly match it).
                        float matchingDistance = safeDistance * speedMatchingSafeDistanceFactor;
                        float fractionFromSafeDistance = (newFrontDistance - matchingDistance) / (maxRaycastDistance - matchingDistance);
                        actuators.targetSpeed = Mathf.Lerp(frontObstacleSpeed, desiredLaneSpeed, fractionFromSafeDistance);
                    }
                }
            }
            else
            {
                // There is no obstacle, simply drive at the lane speed.
                actuators.targetSpeed = desiredLaneSpeed;
                frontObstacleSpeed = 0;
            }

            // Store front distance for next frame.
            frontDistance = newFrontDistance;

            // Draw debug information.
            Color debugColor = Color.yellow;
            if (actuators.targetSpeed == desiredLaneSpeed) debugColor = Color.green;
            if (actuators.targetSpeed == 0) debugColor = Color.red;

            Vector3 endOfRay = origin + forward * newFrontDistance;
            Debug.DrawLine(origin, endOfRay, debugColor);

            Vector3 safeDistanceFromOrigin = origin + forward * safeDistance;
            Debug.DrawLine(safeDistanceFromOrigin - Vector3.up, safeDistanceFromOrigin + Vector3.up, debugColor);

            Vector3 safeDistanceFromTarget = origin + forward * (newFrontDistance - safeDistance);
            Debug.DrawLine(safeDistanceFromTarget - Vector3.up, safeDistanceFromTarget + Vector3.up, debugColor);

            Debug.DrawLine(endOfRay + Vector3.up * 3, endOfRay + forward * frontObstacleSpeed + Vector3.up * 3, Color.red);
        }
    }
}
