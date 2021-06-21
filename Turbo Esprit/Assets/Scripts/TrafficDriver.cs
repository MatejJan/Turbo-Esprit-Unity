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
        private const float uTurnDriveToSidewalkDistance = 20;
        private const float turningTargetTime = 1;
        private const float turningTargetMinimumDistance = 2;
        private static readonly float[] turningSpeedPerLaneMph = new float[] { 0.5f, 5, 10, 15 };

        // The amount of time that should pass before this car reaches the position where the car in front is.
        [SerializeField] private float safeDistanceTime;

        // The minimal safe distance, no matter what the driving speed is.
        [SerializeField] private float minimumSafeDistance;

        // The minimal safe distance towards stationary obstacles.
        [SerializeField] private float minimumSafeDistanceForStationary;

        // At what multiple of safe distance should we aim to match obstacle speed.
        [SerializeField] private float speedMatchingSafeDistanceFactor;

        // How fast should the obstacle move to consider it as a moving obstacle.
        [SerializeField] private float minimumRecognizableObstacleSpeed;

        // How fast should the obstacle be moving away from us to disregard it in our reasoning.
        [SerializeField] private float minimumMovingAwayRelativeSpeed;

        // How far should we raycast.
        [SerializeField] private float maximumRaycastDistance;

        private float frontDistance;
        private float frontDistanceSmoothVelocity;

        private float frontObstacleSpeed;
        private float frontObstacleSpeedSmoothVelocity;

        private Driver.TurningIntent nextIntersectionTurningIntent;

        private CardinalDirection nextIntersectionExitingCardinalDirection;
        private Vector3 nextIntersectionExitingDirection;
        private Street nextIntersectionExitingStreet;
        private int nextIntersectionExitingLane;
        private Vector3 nextIntersectionExitingPoint;

        private CardinalDirection nextIntersectionEnteringCardinalDirection;
        private Vector3 nextIntersectionEnteringDirection;
        private Street nextIntersectionEnteringStreet;
        private int nextIntersectionEnteringLane;
        private Vector3 nextIntersectionEnteringPoint;

        private Street previousStreet;

        public Car waitingForCar { get; private set; }

        public override void Act(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            // By default we're intending to go straight.
            actuators.turningIntent = Driver.TurningIntent.Straight;

            UpdateNextIntersectionDirection(sensors);
            UpdateTargetSpeed(sensors, actuators);
            UpdateTargetLaneAndDirection(sensors, actuators);

            DrawDebugNextIntersectionDirection(sensors);
        }

        private void UpdateNextIntersectionDirection(Driver.Sensors sensors)
        {
            // We need to be on a street and not having been on a street previously (as we would have already decided where to go).
            Street currentStreet = sensors.carTracker.street;

            if (currentStreet != null && previousStreet == null)
            {
                // Determine where to go at the end of this street.
                CardinalDirection currentDirection = sensors.carTracker.streetCardinalDirection;

                Intersection targetIntersection = GetTargetIntersection(sensors);

                // See where we might want to go.
                var potentialDirections = new List<CardinalDirection>();
                CardinalDirection oppositeDirection = DirectionHelpers.GetOppositeDirection(currentDirection);

                foreach (CardinalDirection direction in DirectionHelpers.cardinalDirections)
                {
                    // Don't try to go backwards by default.
                    if (direction == oppositeDirection) continue;

                    // Direction is possible if the intersection has this street.
                    Street street = targetIntersection.GetStreetInDirection(direction);

                    if (street != null)
                    {
                        // Make sure we're not entering into a one-way street from the wrong direction.
                        if (street.isOneWay == false || !targetIntersection.HasStopLineForStreet(street))
                        {
                            potentialDirections.Add(direction);
                        }
                    }
                }

                // Only if no options were there, go back.
                if (potentialDirections.Count == 0) potentialDirections.Add(oppositeDirection);

                // Choose a random option.
                int randomIndex = UnityEngine.Random.Range(0, potentialDirections.Count);

                // Determine exiting values.
                nextIntersectionExitingCardinalDirection = potentialDirections[randomIndex];
                nextIntersectionExitingDirection = DirectionHelpers.cardinalDirectionVectors[nextIntersectionExitingCardinalDirection];
                nextIntersectionExitingStreet = targetIntersection.GetStreetInDirection(nextIntersectionExitingCardinalDirection);

                // Figure out which lane to go into.
                float angleToTarget = Vector3.SignedAngle(sensors.carTracker.streetDirection, DirectionHelpers.cardinalDirectionVectors[nextIntersectionExitingCardinalDirection], Vector3.up);
                Street nextStreet = targetIntersection.GetStreetInDirection(nextIntersectionExitingCardinalDirection);

                if (nextIntersectionExitingCardinalDirection == currentDirection)
                {
                    // Be in the middle (or right) lane when going straight.
                    nextIntersectionExitingLane = nextStreet.validLanesCount / 2 + 1;
                    nextIntersectionTurningIntent = Driver.TurningIntent.Straight;
                }
                else if (nextIntersectionExitingCardinalDirection == oppositeDirection)
                {
                    // Turn into leftmost lane for U-turns.
                    nextIntersectionExitingLane = 1;
                    nextIntersectionTurningIntent = Driver.TurningIntent.UTurn;
                }
                else if (angleToTarget < 0)
                {
                    // Be in leftmost lane after upcoming left turns.
                    nextIntersectionExitingLane = 1;
                    nextIntersectionTurningIntent = Driver.TurningIntent.Left;
                }
                else
                {
                    // Be in the rightmost lane after upcoming right turns.
                    nextIntersectionExitingLane = nextStreet.validLanesCount;
                    nextIntersectionTurningIntent = Driver.TurningIntent.Right;
                }

                nextIntersectionExitingPoint = nextIntersectionExitingStreet.GetCenterOfLanePosition(nextIntersectionExitingLane, 0, nextIntersectionExitingCardinalDirection);

                // Store entering values.
                nextIntersectionEnteringCardinalDirection = currentDirection;
                nextIntersectionEnteringDirection = DirectionHelpers.cardinalDirectionVectors[currentDirection];
                nextIntersectionEnteringStreet = currentStreet;

                // Figure out which lane to enter the intersection from.
                switch (nextIntersectionTurningIntent)
                {
                    case Driver.TurningIntent.Straight:
                        // Be in the middle (or right) lane when going straight.
                        nextIntersectionEnteringLane = currentStreet.validLanesCount / 2 + 1;
                        break;

                    case Driver.TurningIntent.Left:
                        // Be in leftmost lane for upcoming left turns.
                        nextIntersectionEnteringLane = 1;
                        break;

                    case Driver.TurningIntent.Right:
                        // Be in the rightmost lane for upcoming right turns.
                        nextIntersectionEnteringLane = currentStreet.validLanesCount;
                        break;

                    case Driver.TurningIntent.UTurn:
                        // Turn from the sidewalk for U-turns.
                        nextIntersectionEnteringLane = 0;
                        break;
                }

                nextIntersectionEnteringPoint = nextIntersectionEnteringStreet.GetCenterOfLanePosition(nextIntersectionEnteringLane, nextIntersectionEnteringStreet.length, nextIntersectionEnteringCardinalDirection);
            }

            // Update street.
            previousStreet = currentStreet;
        }

        private void UpdateTargetSpeed(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            Transform carTransform = sensors.car.transform;
            RaycastHit hitInfo;
            Vector3 origin;
            Color debugColor;

            // Calculate desired parameters.
            float safeDistance = CalculateSafeDistance(sensors);
            float matchingDistance = safeDistance * speedMatchingSafeDistanceFactor;

            int currentValidLane = Mathf.Clamp(sensors.carTracker.currentLane, 0, 3);
            float desiredLaneSpeedMph = sensors.driverProfile.respectsSpeedLimits ? Traffic.speedPerLaneMph[currentValidLane] : 150;
            float desiredTurningLaneSpeedMph = turningSpeedPerLaneMph[currentValidLane];

            // By default, we're not waiting on anyone.
            waitingForCar = null;

            // See if we'll be turning in the upcoming intersection.
            if (sensors.carTracker.streetCardinalDirection != nextIntersectionExitingCardinalDirection)
            {
                // If we're close enough to the interception, indicate turning intent.
                float distanceToIntersection = GetDistanceToTargetIntersection(sensors);

                if (sensors.carTracker.intersection != null || distanceToIntersection < sensors.driverProfile.distanceForTurning)
                {
                    actuators.turningIntent = nextIntersectionTurningIntent;

                    // For right turns, start checking for oncoming traffic.
                    if (nextIntersectionTurningIntent == Driver.TurningIntent.Right)
                    {
                        // Check each of the lanes we'll have to cross.
                        int currentLane = sensors.carTracker.currentLane;
                        int lastLane = nextIntersectionEnteringStreet.lanesCount;

                        for (int lane = currentLane + 1; lane <= lastLane; lane++)
                        {
                            origin = sensors.carTracker.GetCenterOfLanePosition(lane, nextIntersectionEnteringCardinalDirection) + Vector3.up * sensors.car.bounds.extents.y;
                            float distanceForTurning = sensors.driverProfile.distanceForTurning;
                            debugColor = Color.green;

                            if (Physics.Raycast(origin, nextIntersectionEnteringDirection, out hitInfo, distanceForTurning))
                            {
                                // Make sure the obstacle is a car.
                                GameObject obstacle = hitInfo.collider.gameObject;
                                if (obstacle.CompareTag(Tags.car))
                                {
                                    // Car was detected, see if we need to wait for it.
                                    bool shouldWaitForCar = true;

                                    // Don't wait if the car is already waiting for us.
                                    Driver driver = obstacle.GetComponentInParent<Driver>();
                                    TrafficDriver trafficDriver = driver?.controller as TrafficDriver;
                                    if (trafficDriver?.waitingForCar == sensors.car) shouldWaitForCar = false;

                                    // When turning right, see if other driver's intent can help us not wait.
                                    if (actuators.turningIntent == Driver.TurningIntent.Right && driver != null)
                                    {
                                        // Don't wait if the other car is also turning right.
                                        if (driver.turningIntent == Driver.TurningIntent.Right) shouldWaitForCar = false;

                                        // Don't wait if the other car is turning left and we're going right into a different lane.
                                        if (driver.turningIntent == Driver.TurningIntent.Left && nextIntersectionExitingLane > 1) shouldWaitForCar = false;
                                    }

                                    if (shouldWaitForCar)
                                    {
                                        // stop and wait before turning!
                                        desiredTurningLaneSpeedMph = 0;
                                        debugColor = Color.red;

                                        // Communicate that we're waiting for this car.
                                        waitingForCar = obstacle.GetComponentInParent<Car>();
                                        Debug.DrawLine(carTransform.position + Vector3.up, obstacle.transform.position + Vector3.up, Color.black);
                                    }
                                }
                            }

                            Debug.DrawRay(origin, nextIntersectionEnteringDirection * distanceForTurning, debugColor);
                        }
                    }
                }

                // See if we're already in the intersection.
                if (sensors.carTracker.intersection != null)
                {
                    // Just keep the turning speed.
                    desiredLaneSpeedMph = desiredTurningLaneSpeedMph;
                }
                else
                {
                    // Slow down towards desired turning speed when approaching an intersection.
                    desiredLaneSpeedMph = Mathf.Lerp(desiredTurningLaneSpeedMph, desiredLaneSpeedMph, (distanceToIntersection - matchingDistance) / matchingDistance);
                }
            }

            float desiredLaneSpeed = desiredLaneSpeedMph * PhysicsHelper.milesPerHourToMetersPerSecond;

            // Shoot a ray out to see how far the distance is to the obstacle in the front.
            origin = sensors.car.frontCenterPointWorld;
            Vector3 forward = carTransform.forward;
            forward.y = 0;
            forward.Normalize();

            float newFrontDistance;

            if (Physics.Raycast(origin, forward, out hitInfo, maximumRaycastDistance))
            {
                // Because the raycasts fluctuate, smooth the measurement for more stable behavior.
                newFrontDistance = Mathf.SmoothDamp(frontDistance, hitInfo.distance, ref frontDistanceSmoothVelocity, distanceSmoothTime);
            }
            else
            {
                newFrontDistance = maximumRaycastDistance;
            }

            // Only make any considerations if we can figure out the relative speed.
            if (frontDistance < maximumRaycastDistance && newFrontDistance < maximumRaycastDistance)
            {
                // Determine obstacle speed, based on what it is.
                if (hitInfo.collider.gameObject.CompareTag(Tags.car))
                {
                    // We hit a car. Because the raycasts fluctuate, smooth the measurement for more stable behavior.
                    float obstacleRelativeSpeed = (newFrontDistance - frontDistance) / Time.deltaTime;
                    float newObstacleSpeed = sensors.car.speed + obstacleRelativeSpeed;
                    frontObstacleSpeed = Mathf.SmoothDamp(frontObstacleSpeed, newObstacleSpeed, ref frontObstacleSpeedSmoothVelocity, obstacleSpeedSmoothTime);

                    // If speed is low enough, consider it to be stopped.
                    if (newObstacleSpeed < minimumRecognizableObstacleSpeed && frontObstacleSpeed < minimumRecognizableObstacleSpeed) frontObstacleSpeed = 0;
                }
                else
                {
                    // We didn't hit a car, assume it's stationary.
                    frontObstacleSpeed = 0;
                }

                // Recalculate safe distance for stationary obstacles.
                if (frontObstacleSpeed == 0)
                {
                    safeDistance = minimumSafeDistanceForStationary;
                    matchingDistance = safeDistance * speedMatchingSafeDistanceFactor;
                }

                // If we're closer than the safe distance to the obstacle, stop!
                float distanceToSafeDistance = newFrontDistance - safeDistance;

                if (distanceToSafeDistance < 0)
                {
                    actuators.targetSpeed = 0;
                }
                else
                {
                    // Nothing to do if the obstacle is moving away.
                    float obstacleRelativeSpeed = frontObstacleSpeed - sensors.car.speed;
                    if (obstacleRelativeSpeed > minimumMovingAwayRelativeSpeed)
                    {
                        actuators.targetSpeed = desiredLaneSpeed;
                    }
                    else
                    {
                        // Match obstacle's speed the closer you are to the safe distance (at which point you should exactly match it).
                        float fractionFromSafeDistance = (newFrontDistance - matchingDistance) / (maximumRaycastDistance - matchingDistance);
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
            /*
            debugColor = Color.yellow;
            if (actuators.targetSpeed == desiredLaneSpeed) debugColor = Color.green;
            if (actuators.targetSpeed == 0) debugColor = Color.red;

            Vector3 endOfRay = origin + forward * newFrontDistance;
            Debug.DrawLine(origin, endOfRay, debugColor);

            Vector3 safeDistanceFromOrigin = origin + forward * safeDistance;
            Debug.DrawLine(safeDistanceFromOrigin - Vector3.up, safeDistanceFromOrigin + Vector3.up, debugColor);

            Vector3 safeDistanceFromTarget = origin + forward * (newFrontDistance - safeDistance);
            Debug.DrawLine(safeDistanceFromTarget - Vector3.up, safeDistanceFromTarget + Vector3.up, debugColor);

            Debug.DrawLine(endOfRay + Vector3.up * 3, endOfRay + forward * frontObstacleSpeed + Vector3.up * 3, Color.red);
            */
        }

        private void UpdateTargetLaneAndDirection(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            Street currentStreet = sensors.carTracker.street;
            Intersection currentIntersection = sensors.carTracker.intersection;
            float distanceToIntersection = GetDistanceToTargetIntersection(sensors);
            float turningTargetDistance = Math.Max(turningTargetMinimumDistance, turningTargetTime * sensors.car.speed);

            // See if we should start performing a turn.
            bool performingTurn = false;
            switch (nextIntersectionTurningIntent)
            {
                case Driver.TurningIntent.Left:
                case Driver.TurningIntent.Right:
                    // When turning left and right, start turning when close enough to the intersection.
                    performingTurn = currentIntersection != null || distanceToIntersection < turningTargetDistance;
                    break;

                case Driver.TurningIntent.UTurn:
                    // When performing a U-turn, start in the intersection.
                    performingTurn = currentIntersection != null;
                    break;
            }

            if (performingTurn)
            {
                DrawDebugIntersectionArc();

                switch (nextIntersectionTurningIntent)
                {
                    case Driver.TurningIntent.Left:
                    case Driver.TurningIntent.Right:
                        // For right turns, follow an arc.
                        float xEnter = Vector3.Dot(nextIntersectionExitingDirection, nextIntersectionEnteringPoint);
                        float xExit = Vector3.Dot(nextIntersectionExitingDirection, nextIntersectionExitingPoint);
                        float xDelta = xExit - xEnter;

                        float zEnter = Vector3.Dot(nextIntersectionEnteringDirection, nextIntersectionEnteringPoint);
                        float zExit = Vector3.Dot(nextIntersectionEnteringDirection, nextIntersectionExitingPoint);
                        float zDelta = zExit - zEnter;

                        // Place a target in front of the car.
                        Vector3 carPosition = sensors.car.transform.position;
                        Vector3 target = carPosition + sensors.car.transform.forward * turningTargetDistance;
                        Debug.DrawLine(carPosition + Vector3.up * 2, target + Vector3.up * 2, Color.magenta);

                        float xTarget = Vector3.Dot(nextIntersectionExitingDirection, target) - xEnter;
                        float zTarget = Vector3.Dot(nextIntersectionEnteringDirection, target) - zEnter;

                        // Move target onto the arc.
                        float zTargetFraction = zTarget / zDelta;
                        float xTargetFraction = xTarget / xDelta;

                        float angle = Mathf.Atan2(zTargetFraction, 1 - xTargetFraction);

                        xTargetFraction = 1 - Mathf.Cos(angle);
                        zTargetFraction = xTargetFraction > 1 ? 1 : Mathf.Sin(angle);

                        xTarget = xTargetFraction * xDelta;
                        zTarget = zTargetFraction * zDelta;
                        target = nextIntersectionEnteringPoint + nextIntersectionExitingDirection * xTarget + nextIntersectionEnteringDirection * zTarget;

                        // Direct the car towards the adjusted target.
                        actuators.targetDirection = (target - carPosition).normalized;

                        Debug.DrawLine(carPosition + Vector3.up * 2, target + Vector3.up * 2, Color.blue);

                        break;

                    case Driver.TurningIntent.UTurn:
                        // For U-turns, start by turning right as tight as possible.
                        if (Vector3.Angle(nextIntersectionEnteringDirection, sensors.car.transform.forward) < 45)
                        {
                            actuators.targetDirection = Quaternion.AngleAxis(90, Vector3.up) * nextIntersectionEnteringDirection;
                        }
                        else
                        {
                            actuators.targetDirection = nextIntersectionExitingDirection;
                        }
                        break;
                }

                actuators.targetLane = nextIntersectionExitingLane;
            }
            else if (currentStreet != null)
            {
                // If we're on a street, go to correct turning lane.
                float angleToTarget = Vector3.SignedAngle(sensors.carTracker.streetDirection, DirectionHelpers.cardinalDirectionVectors[nextIntersectionExitingCardinalDirection], Vector3.up);

                CardinalDirection currentDirection = sensors.carTracker.streetCardinalDirection;
                CardinalDirection oppositeDirection = DirectionHelpers.GetOppositeDirection(currentDirection);

                int currentLane = sensors.carTracker.currentLane;
                int desiredLane = nextIntersectionEnteringLane;

                if (desiredLane != currentLane)
                {
                    // Start moving to the desired lane when close enough to the intersection.
                    int laneDifference = desiredLane - currentLane;
                    float minimumDistanceToIntersection = sensors.driverProfile.distanceForChangingLane * Mathf.Abs(laneDifference);

                    // For U-turns from the sidewalk, only drive onto the sidewalk in the last meters.
                    if (nextIntersectionTurningIntent == Driver.TurningIntent.UTurn && currentLane == 1) minimumDistanceToIntersection = uTurnDriveToSidewalkDistance;

                    if (distanceToIntersection < minimumDistanceToIntersection)
                    {
                        actuators.turningIntent = nextIntersectionTurningIntent;

                        // Make sure the neighbor lane is free in the safe distance region before and after the car.
                        int neighborLane = currentLane + Math.Sign(laneDifference);

                        float safeDistance = CalculateSafeDistance(sensors);
                        Vector3 streetDirection = sensors.carTracker.streetDirection;
                        float carLength = sensors.car.bounds.size.z;
                        Vector3 origin = sensors.carTracker.GetCenterOfLanePosition(neighborLane) + (safeDistance + carLength / 2) * streetDirection + Vector3.up * sensors.car.bounds.extents.y;
                        float checkDistance = 2 * safeDistance + carLength;

                        if (!Physics.Raycast(origin, -streetDirection, checkDistance))
                        {
                            actuators.targetLane = desiredLane;
                            Debug.DrawRay(origin, -streetDirection * checkDistance, Color.green, 1);
                        }
                        else
                        {
                            Debug.DrawRay(origin, -streetDirection * checkDistance, Color.red);
                        }
                    }
                }

                actuators.targetDirection = sensors.carTracker.streetDirection;
            }
        }

        private void DrawDebugIntersectionArc()
        {
            float xEnter = Vector3.Dot(nextIntersectionExitingDirection, nextIntersectionEnteringPoint);
            float xExit = Vector3.Dot(nextIntersectionExitingDirection, nextIntersectionExitingPoint);
            float xDelta = xExit - xEnter;

            float zEnter = Vector3.Dot(nextIntersectionEnteringDirection, nextIntersectionEnteringPoint);
            float zExit = Vector3.Dot(nextIntersectionEnteringDirection, nextIntersectionExitingPoint);
            float zDelta = zExit - zEnter;

            Vector3 lastDebugPoint = nextIntersectionEnteringPoint;

            for (int i = 1; i <= 18; i++)
            {
                float angle = i * 5 * Mathf.Deg2Rad;
                float xDebug = (1 - Mathf.Cos(angle)) * xDelta;
                float zDebug = Mathf.Sin(angle) * zDelta;
                Vector3 debugPoint = nextIntersectionEnteringPoint + nextIntersectionExitingDirection * xDebug + nextIntersectionEnteringDirection * zDebug;
                Debug.DrawLine(debugPoint + Vector3.up, lastDebugPoint + Vector3.up, Color.white);
                lastDebugPoint = debugPoint;
            }
        }

        private Intersection GetTargetIntersection(Driver.Sensors sensors)
        {
            if (sensors.carTracker.intersection != null) return sensors.carTracker.intersection;

            Street currentStreet = sensors.carTracker.street;
            CardinalDirection currentDirection = sensors.carTracker.streetCardinalDirection;

            return currentDirection == CardinalDirection.West || currentDirection == CardinalDirection.South ? currentStreet.startIntersection : currentStreet.endIntersection;
        }

        private float GetDistanceToTargetIntersection(Driver.Sensors sensors)
        {
            Intersection targetIntersection = GetTargetIntersection(sensors);
            return Mathf.Sqrt(targetIntersection.bounds.SqrDistance(sensors.car.transform.position));
        }

        private float CalculateSafeDistance(Driver.Sensors sensors)
        {
            return Mathf.Max(minimumSafeDistance, safeDistanceTime * sensors.car.speed);
        }

        private void DrawDebugNextIntersectionDirection(Driver.Sensors sensors)
        {
            Debug.DrawRay(sensors.car.transform.position + Vector3.up * 2, DirectionHelpers.cardinalDirectionVectors[nextIntersectionExitingCardinalDirection] * 5, Color.magenta);
        }
    }
}
