using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    [CreateAssetMenu(fileName = "SimpleControls", menuName = "Scriptable Objects/Simple Controls")]
    public class SimpleControls : Driver.Controller
    {
        // How much can the target speed be changed per second.
        [SerializeField] private float speedChangeRateMph;

        // The minimum amount the target speed has to be ahead of current speed. 
        [SerializeField] private float minSpeedChangeDifferenceMph;

        // At what steps should the driver try to hold speed at when controls are disengaged.
        [SerializeField] private float roundingSpeedStepMph;

        // The maximum angle the target direction will be away from current direction. 
        [SerializeField] private float maxAngleChangeDifferenceDegrees;

        // The amount of time when turning only affects lane change.
        [SerializeField] private float laneChangeOnlyDuration;

        private int speedSign = 0;
        private bool keepTargetSpeed = true;
        private float lastTurningInput = 0;
        private float turningInputNonZeroTime = 0;

        // Properties in SI units.

        private float speedChangeRate => speedChangeRateMph * PhysicsHelper.milesPerHourToMetersPerSecond;
        private float minSpeedChangeDifference => minSpeedChangeDifferenceMph * PhysicsHelper.milesPerHourToMetersPerSecond;
        private float roundingSpeedStep => roundingSpeedStepMph * PhysicsHelper.milesPerHourToMetersPerSecond;

        public override void Act(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            UpdateTargetSpeed(sensors, actuators);
            UpdateTargetDirection(sensors, actuators);
        }

        private void UpdateTargetSpeed(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            // Read player input.
            float speedChangeInput = Input.GetAxis("Speed Change");

            // When the car has stopped and we're not pressing anything, reset speed sign.
            if (sensors.car.speed == 0 && speedChangeInput == 0)
            {
                speedSign = 0;
            }

            // Nothing to do if we're keeping the current speed and no change is requested.
            if (keepTargetSpeed && speedChangeInput == 0) return;

            if (speedChangeInput != 0)
            {
                // If sign is not set when we're stopped, determine the direction we want to go in.
                if (speedSign == 0 && sensors.car.speed == 0)
                {
                    speedSign = Math.Sign(speedChangeInput);
                }

                // We want to change speed so calculate new target speed.
                float absoluteSpeedChangeInput = speedChangeInput * speedSign;
                float absoluteSpeedChange = absoluteSpeedChangeInput * speedChangeRate * Time.deltaTime;
                float newAbsoluteTargetSpeed = Mathf.Abs(actuators.targetSpeed) + absoluteSpeedChange;

                // Ensure minimum speed difference for faster reaction.
                float absoluteCarSpeed = Mathf.Abs(sensors.car.speed);

                if (absoluteSpeedChangeInput < 0)
                {
                    newAbsoluteTargetSpeed = Mathf.Clamp(newAbsoluteTargetSpeed, 0, Mathf.Max(0, absoluteCarSpeed - minSpeedChangeDifference));
                }
                else if (absoluteSpeedChangeInput > 0 && newAbsoluteTargetSpeed < absoluteCarSpeed + minSpeedChangeDifference)
                {
                    newAbsoluteTargetSpeed = Mathf.Max(newAbsoluteTargetSpeed, absoluteCarSpeed + minSpeedChangeDifference);
                }

                // Set new target speed.
                actuators.targetSpeed = newAbsoluteTargetSpeed * speedSign;

                // Don't keep the speed until we stop changing it.
                keepTargetSpeed = false;
            }
            else
            {
                // Round the current car speed as the new target.
                actuators.targetSpeed = Mathf.Round(sensors.car.speed / roundingSpeedStep) * roundingSpeedStep;
                keepTargetSpeed = true;
            }
        }

        private void UpdateTargetDirection(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            // Read player input.
            float turningInput = Input.GetAxis("Turning");

            if (turningInput == 0)
            {
                // If the input was just released, square to 90 degrees.
                if (lastTurningInput != 0)
                {
                    CardinalDirection cardinalDirection = DirectionHelpers.GetCardinalDirectionForVector(sensors.transform.forward);
                    actuators.targetDirection = DirectionHelpers.cardinalDirectionVectors[cardinalDirection];

                    // Reset turning input time for next time.
                    turningInputNonZeroTime = 0;
                }
            }
            else
            {
                // If the input was just changed, change lane.
                if (turningInput < 0 && lastTurningInput >= 0)
                {
                    // We want to go left, but if our current target was to go right, just return to current lane.
                    if (actuators.targetLane > sensors.carTracker.currentLane)
                    {
                        actuators.targetLane = sensors.carTracker.currentLane;
                    }
                    else
                    {
                        actuators.targetLane = Math.Max(sensors.carTracker.currentLane - 1, 0);
                    }
                }
                else if (turningInput > 0 && lastTurningInput <= 0)
                {
                    // We want to go right, but if our current target was to go left, just return to current lane.
                    if (actuators.targetLane < sensors.carTracker.currentLane)
                    {
                        actuators.targetLane = sensors.carTracker.currentLane;
                    }
                    else
                    {
                        actuators.targetLane = Math.Min(sensors.carTracker.currentLane + 1, sensors.carTracker.representativeStreet.lanesCount + 1);
                    }
                }

                // If turning has been active enough time, change target direction.
                turningInputNonZeroTime += Time.deltaTime;

                if (turningInputNonZeroTime > laneChangeOnlyDuration)
                {
                    float rotationAngleDegrees = turningInput * maxAngleChangeDifferenceDegrees * Mathf.Sign(sensors.car.speed);
                    Quaternion rotation = Quaternion.Euler(0, rotationAngleDegrees, 0);
                    actuators.targetDirection = rotation * sensors.transform.forward;
                }
            }

            lastTurningInput = turningInput;
        }
    }
}
