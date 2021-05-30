using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Driver : MonoBehaviour
    {
        [SerializeField] private DriverProfile profile;

        protected Car car;
        protected CarTracker carTracker;

        private DrivingState drivingState = DrivingState.Parked;
        private TurningState turningState = TurningState.Turning;
        private float idlingTime = 0;
        private float shiftingTime = 0;
        private float previousSpeed = 0;

        private Car.GearshiftPosition targetGearshiftPosition;

        public enum DrivingState
        {
            Parked,
            Starting,
            Idling,
            MovingOff,
            Driving,
            Shifting,
            Stopping
        }

        public enum TurningState
        {
            DrivingStraight,
            Turning
        }

        /// <summary>
        /// What speed should the driver be driving at.
        /// </summary>
        protected float targetSpeed { get; set; }

        /// <summary>
        /// In which lane should the driver be.
        /// </summary>
        protected int targetLane { get; set; }

        /// <summary>
        /// In which direction in world space should the driver orient the car.
        /// </summary>
        protected Vector3 targetDirection { get; set; }

        private void Awake()
        {
            car = GetComponent<Car>();
            carTracker = GetComponent<CarTracker>();
            targetDirection = transform.forward;
        }

        protected virtual void Update()
        {
            DrawDebugTarget();

            UpdateDesiredGearshiftPosition();
            UpdateDrivingState();
            UpdateTurningState();
            UpdateIgnitionSwitchPosition();
            UpdateAcceleratorPedalPosition();
            UpdateClutchPedalPosition();
            UpdateBrakePedalPosition();
            UpdateGearshiftPosition();
            UpdateSteeringWheelPosition();
        }

        private void UpdateDesiredGearshiftPosition()
        {
            if (targetSpeed == 0)
            {
                // When completely stopping, we always want to get to neutral.
                targetGearshiftPosition = Car.GearshiftPosition.Neutral;
            }
            else if (targetSpeed < 0)
            {
                // Only reverse gear can make us go backwards.
                targetGearshiftPosition = Car.GearshiftPosition.Reverse;
            }
            else if (drivingState != DrivingState.Shifting && drivingState != DrivingState.MovingOff)
            {
                // We have to be at least in first gear.
                if (car.gearshiftPosition == Car.GearshiftPosition.Neutral)
                {
                    targetGearshiftPosition = Car.GearshiftPosition.FirstGear;
                }

                // Shift up when accelerating above specified limit.
                float activeUpshiftEngineRpm = Mathf.Abs(targetSpeed - car.speed) < profile.closeSpeedDifference ? profile.upshiftEngineRpmWhenCloseToTarget : profile.upshiftEngineRpm;
                if (targetSpeed > car.speed && car.engineRpm > activeUpshiftEngineRpm && car.gearshiftPosition < car.topGear)
                {
                    targetGearshiftPosition = car.gearshiftPosition + 1;
                }

                // Shift down when below specified limit.
                if (car.engineRpm < profile.downshiftEngineRpm && car.gearshiftPosition > Car.GearshiftPosition.FirstGear)
                {
                    targetGearshiftPosition = car.gearshiftPosition - 1;
                }
            }
        }

        private void UpdateDrivingState()
        {
            switch (drivingState)
            {
                case DrivingState.Parked:
                    // We move out of parked into starting when any speed is required.
                    if (targetSpeed != 0) drivingState = DrivingState.Starting;
                    break;

                case DrivingState.Starting:
                    // When starting, we wait for the engine to turn on before going idle.
                    if (car.engineState == Car.EngineState.On) drivingState = DrivingState.Idling;
                    break;

                case DrivingState.Idling:
                    // When idling, we wait for target speed to get a value before moving off.
                    if (targetSpeed != 0) drivingState = DrivingState.MovingOff;

                    // When the car is not moving, we wait before shutting the engine off.
                    if (car.speed == 0)
                    {
                        idlingTime += Time.deltaTime;
                        if (idlingTime > profile.maxIdlingDuration) drivingState = DrivingState.Parked;
                    }

                    // When leaving state, reset idling time for next time.
                    if (drivingState != DrivingState.Idling) idlingTime = 0;

                    break;

                case DrivingState.MovingOff:
                case DrivingState.Shifting:
                    // When moving off or shifting, we wait until clutch is released in the right gear.
                    if (car.clutchPedalPosition == 0 && car.gearshiftPosition == targetGearshiftPosition) drivingState = DrivingState.Driving;

                    // If target speed becomes zero, we should stop.
                    if (targetSpeed == 0) drivingState = DrivingState.Stopping;
                    break;

                case DrivingState.Driving:
                    // When driving, if we're not in the right gear, start shifting.
                    if (car.gearshiftPosition != targetGearshiftPosition) drivingState = DrivingState.Shifting;

                    // If target speed becomes zero, we should stop.
                    if (targetSpeed == 0) drivingState = DrivingState.Stopping;
                    break;

                case DrivingState.Stopping:
                    // When stopping, if target speed changes, shift back into gear.
                    if (targetSpeed != 0) drivingState = DrivingState.Shifting;

                    // When our speed stops, we go to idling.
                    if (car.speed == 0) drivingState = DrivingState.Idling;
                    break;
            }
        }

        private void UpdateTurningState()
        {
            switch (turningState)
            {
                case TurningState.DrivingStraight:
                    // When direction isn't along the street, go into turning mode.
                    if (targetDirection != carTracker.streetDirection) turningState = TurningState.Turning;
                    break;


                case TurningState.Turning:
                    // When direction is along the street and we're back in allowed angle range, we go back to driving straight.
                    float carAngleDegrees = GetCarAngleDegrees(false);
                    float allowedStraightAngleDegrees = GetAllowedStraightAngleDegrees();
                    if (targetDirection == carTracker.streetDirection && carAngleDegrees < allowedStraightAngleDegrees)
                    {
                        turningState = TurningState.DrivingStraight;

                        // Align to the closest car lane (not sidewalk).
                        targetLane = Mathf.Clamp(1, carTracker.currentLane, carTracker.representativeStreet.lanesCount);
                    }
                    break;
            }
        }

        private void UpdateIgnitionSwitchPosition()
        {
            switch (drivingState)
            {
                case DrivingState.Starting:
                    car.ignitionSwitchPosition = Car.IgnitionSwitchPosition.Start;
                    break;

                case DrivingState.Parked:
                    car.ignitionSwitchPosition = Car.IgnitionSwitchPosition.Lock;
                    break;

                default:
                    car.ignitionSwitchPosition = Car.IgnitionSwitchPosition.On;
                    break;
            }
        }

        private void UpdateAcceleratorPedalPosition()
        {
            float speed = car.speed;

            switch (drivingState)
            {
                case DrivingState.Idling:
                case DrivingState.Parked:
                    car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 0, profile.normalPedalSpeed * Time.deltaTime);
                    break;

                case DrivingState.MovingOff:
                case DrivingState.Shifting:
                    if (car.gearshiftPosition == targetGearshiftPosition)
                    {
                        // When we're in the desired gearshift position, we should be pressing the accelerator.
                        car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 1, profile.shiftingPedalSpeed * Time.deltaTime);
                    }
                    else
                    {
                        // When we're not in the desired gearshift position, we should be releasing the accelerator.
                        car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 0, profile.shiftingPedalSpeed * Time.deltaTime);
                    }
                    break;

                case DrivingState.Driving:
                    // Calculate how much to accelerate, assuming we want to approach zero acceleration when we reach target speed.
                    // We can integrate acceleration over time (as it linearly approeches zero) to get the speed change.
                    //      a * Δt
                    // Δv = ------
                    //        2
                    // Now we just express a from the equation to get our target.
                    float absoluteSpeed = Mathf.Abs(speed);
                    float absoluteSpeedDifference = Mathf.Abs(targetSpeed) - absoluteSpeed;
                    float targetAcceleration = absoluteSpeedDifference / profile.speedEqualizationDuration * 2;

                    // Based on current acceleration, slowly move the acceleration padel to match the desired one.
                    float acceleration = (absoluteSpeed - Mathf.Abs(previousSpeed)) / Time.deltaTime;
                    float accelerationDifference = targetAcceleration - acceleration;
                    float positionChange = accelerationDifference * profile.acceleratorChangeFactor;
                    float targetPosition = Mathf.Clamp(car.acceleratorPedalPosition + positionChange, 0, 1);

                    car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, targetPosition, profile.normalPedalSpeed * Time.deltaTime);

                    break;

                case DrivingState.Stopping:
                    car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 0, profile.normalPedalSpeed * Time.deltaTime);
                    break;
            }

            previousSpeed = speed;
        }

        private void UpdateClutchPedalPosition()
        {
            switch (drivingState)
            {
                case DrivingState.MovingOff:
                case DrivingState.Shifting:
                    if (car.gearshiftPosition == targetGearshiftPosition)
                    {
                        // When we're in the desired gearshift position, we should be releasing the clutch.
                        float pedalSpeed = drivingState == DrivingState.MovingOff ? profile.movingOffPedalSpeed : profile.shiftingPedalSpeed;
                        car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 0, pedalSpeed * Time.deltaTime);
                    }
                    else
                    {
                        // When we're not in the desired gearshift position, we should be pressing the clutch.
                        car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 1, profile.shiftingPedalSpeed * Time.deltaTime);
                    }
                    break;

                case DrivingState.Stopping:
                    car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 1, profile.shiftingPedalSpeed * Time.deltaTime);
                    break;
            }
        }

        private void UpdateBrakePedalPosition()
        {
            float targetPedalPosition = 1;

            if (targetSpeed != 0)
            {
                float absoluteSpeedDifference = Mathf.Abs(car.speed) - Mathf.Abs(targetSpeed);
                targetPedalPosition = Mathf.InverseLerp(profile.minBrakingSpeedDifference, profile.maxBrakingSpeedDifference, absoluteSpeedDifference);
            }

            // Press slowly on the brakes, but release with normal speed.
            float pedalSpeed = targetPedalPosition > car.brakePedalPosition ? profile.brakingPedalSpeed : profile.normalPedalSpeed;
            car.brakePedalPosition = Mathf.MoveTowards(car.brakePedalPosition, targetPedalPosition, pedalSpeed * Time.deltaTime);
        }

        private void UpdateGearshiftPosition()
        {
            switch (drivingState)
            {
                case DrivingState.MovingOff:
                case DrivingState.Shifting:
                    // Wait until the clutch is pressed before doing any shifts.
                    if (car.clutchPedalPosition == 1)
                    {
                        // If we're in neutral, we add to shifting time.
                        if (car.gearshiftPosition == Car.GearshiftPosition.Neutral)
                        {
                            shiftingTime += Time.deltaTime;

                            // When shifting duration is reached, we can shift to the correct gear.
                            if (shiftingTime > profile.shiftingDuration)
                            {
                                car.gearshiftPosition = targetGearshiftPosition;
                                shiftingTime = profile.shiftingDuration;
                            }
                        }
                        // If we're in a gear that is not the target one, we need to remove from shifting time.
                        else if (car.gearshiftPosition != targetGearshiftPosition)
                        {
                            shiftingTime -= Time.deltaTime;

                            // When shifting duration is reversed, we can shift to neutral.
                            if (shiftingTime < 0)
                            {
                                car.gearshiftPosition = Car.GearshiftPosition.Neutral;
                                shiftingTime = 0;
                            }
                        }
                    }
                    break;

                case DrivingState.Parked:
                case DrivingState.Idling:
                case DrivingState.Stopping:
                    // When we're supposed to stand still, we shift out of gear to neutral.
                    if (car.gearshiftPosition != targetGearshiftPosition)
                    {
                        shiftingTime -= Time.deltaTime;

                        // When shifting duration is reversed, we can shift to neutral.
                        if (shiftingTime < 0)
                        {
                            car.gearshiftPosition = Car.GearshiftPosition.Neutral;
                        }
                    }
                    break;
            }
        }

        private void UpdateSteeringWheelPosition()
        {
            Vector3 targetWheelsDirectionInCarSpace = new Vector3();

            switch (turningState)
            {
                case TurningState.DrivingStraight:
                    // Calculate if we should straighten out, assuming we want to approach zero side velocity when we reach target position.
                    // We can integrate speed over time (as it linearly approeches zero) to get the position change.
                    //      v * Δt
                    // Δx = ------
                    //        2
                    // Now we just express v from the equation to get our target.
                    float targetSidewaysPosition = carTracker.GetCenterOfLaneSidewaysPosition(targetLane);
                    float currentSidewaysPosition = carTracker.sidewaysPosition;
                    float sidewaysPositionDifference = targetSidewaysPosition - currentSidewaysPosition;
                    float targetSidewaysSpeed = sidewaysPositionDifference / profile.drivingStraightEqualizationDuration * 2;

                    // Calculate the direction we should be going in to have the target sideways speed.
                    float speed = Math.Max(2, car.rigidbody.velocity.magnitude);
                    float targetAngleDegrees;
                    float maxAllowedAngleDegrees = GetAllowedStraightAngleDegrees();

                    // Check if we can achieve target sideways speed with any angle.
                    if (Mathf.Abs(targetSidewaysSpeed) < speed)
                    {
                        targetAngleDegrees = Mathf.Asin(targetSidewaysSpeed / speed) * Mathf.Rad2Deg;
                    }
                    else
                    {
                        // We can't, so just turn as much as possible.
                        targetAngleDegrees = maxAllowedAngleDegrees * Math.Sign(targetSidewaysSpeed);
                    }

                    float allowedTargetAngleDegrees = Mathf.Clamp(targetAngleDegrees, -maxAllowedAngleDegrees, maxAllowedAngleDegrees);
                    Vector3 targetDrivingDirectionInStreetSpace = Quaternion.Euler(0, allowedTargetAngleDegrees, 0) * Vector3.forward;

                    if (car.speed < 0) targetDrivingDirectionInStreetSpace.z *= -1;

                    // Convert it to direction in car space.
                    Quaternion streetToCar = Quaternion.FromToRotation(transform.forward, carTracker.streetDirection);
                    targetWheelsDirectionInCarSpace = streetToCar * targetDrivingDirectionInStreetSpace;

                    break;

                case TurningState.Turning:
                    Vector3 targetDrivingDirection = targetDirection;
                    if (car.speed < 0) targetDrivingDirection *= -1;

                    // Calculate where the wheels should be pointed in car space.
                    Quaternion worldToCar = Quaternion.FromToRotation(transform.forward, Vector3.forward);
                    targetWheelsDirectionInCarSpace = worldToCar * targetDrivingDirection;

                    break;
            }

            if (car.speed < 0) targetWheelsDirectionInCarSpace.z *= -1;

            // Calculate target steering wheel position.
            float targetSteeringWheelPosition = car.GetSteeringWheelPositionForDirection(targetWheelsDirectionInCarSpace);
            float maxSteeringWheelPosition = Mathf.Pow(0.5f, car.speed / profile.steeringWheelLimitHalvingSpeed);
            float possibleTargetSteeringWheelPosition = Mathf.Clamp(targetSteeringWheelPosition, -maxSteeringWheelPosition, maxSteeringWheelPosition);

            car.steeringWheelPosition = Mathf.MoveTowards(car.steeringWheelPosition, possibleTargetSteeringWheelPosition, profile.steeringWheelSpeed * Time.deltaTime);

            DrawDebugTargetCurrentDirection(targetWheelsDirectionInCarSpace);
        }

        private float GetCarAngleDegrees(bool signed = true)
        {
            if (signed)
            {
                return Vector3.SignedAngle(Vector3.forward, carTracker.carStreetDirection, Vector3.up);
            }
            else
            {
                return Vector3.Angle(Vector3.forward, carTracker.carStreetDirection);
            }
        }

        private float GetAllowedStraightAngleDegrees()
        {
            float angleReductionFactor = Mathf.Pow(0.5f, Math.Abs(car.speed) / profile.drivingStraightAllowedAngleHalvingSpeed);
            return profile.drivingStraightBaseAllowedAngleDegrees * angleReductionFactor;
        }

        private void DrawDebugTarget()
        {
            float targetSidewaysPosition = carTracker.GetCenterOfLaneSidewaysPosition(targetLane);
            Vector3 origin = carTracker.carWorldPosition + Vector3.up;

            float streetHalfWidth = carTracker.representativeStreet.width / 2;
            Vector2Int intersectionWorldPosition = carTracker.representativeStreet.startIntersection.position;

            switch (carTracker.streetCardinalDirection)
            {
                case CardinalDirection.North:
                    origin.x = intersectionWorldPosition.x - streetHalfWidth + targetSidewaysPosition;
                    break;

                case CardinalDirection.South:
                    origin.x = intersectionWorldPosition.x + streetHalfWidth - targetSidewaysPosition;
                    break;

                case CardinalDirection.West:
                    origin.z = intersectionWorldPosition.y - streetHalfWidth + targetSidewaysPosition;
                    break;

                case CardinalDirection.East:
                    origin.z = intersectionWorldPosition.y + streetHalfWidth - targetSidewaysPosition;
                    break;
            }

            Debug.DrawRay(origin, targetDirection * 100, Color.green);
        }

        private void DrawDebugTargetCurrentDirection(Vector3 directionInCarSpace)
        {
            Vector3 directionInWorldSpace = transform.localToWorldMatrix * directionInCarSpace;

            Debug.DrawRay(transform.position + Vector3.up, directionInWorldSpace * 10, Color.cyan);
        }
    }
}
