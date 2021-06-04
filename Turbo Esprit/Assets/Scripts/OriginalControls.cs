using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    [CreateAssetMenu(fileName = "OriginalControls", menuName = "Scriptable Objects/Original Controls")]
    public class OriginalControls : Driver.Controller
    {
        // How much can the target speed be changed per second.
        [SerializeField] private float speedChangeRateMph;

        // The minimum amount the target speed has to be ahead of current speed. 
        [SerializeField] private float minSpeedChangeDifferenceMph;

        // At what steps should the driver try to hold speed at when controls are disengaged.
        [SerializeField] private float roundingSpeedStepMph;

        private int speedSign = 0;
        private bool keepTargetSpeed = true;

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

        protected virtual void UpdateTargetDirection(Driver.Sensors sensors, Driver.Actuators actuators)
        {
            float turningInput = Input.GetAxis("Turning");
            bool performTurn = false;

            if (Input.GetButtonDown("Turning"))
            {
                // See if the turn button is down.
                if (Input.GetButton("Turn"))
                {
                    // We want to make a turn.
                    performTurn = true;
                }
                else
                {
                    // Turn button is not down, so do a lane switch.
                    if (turningInput < 0)
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
                    else
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
                }
            }

            // If turn button is pressed and we have a direction, we should turn.
            if (Input.GetButtonDown("Turn") && turningInput != 0)
            {
                performTurn = true;
            }

            // Perform the turn if controls dictated it.
            if (performTurn)
            {
                // Determine to which side we're currently turning.
                Vector3 forward = sensors.car.transform.forward;
                Quaternion currentTargetDelta = Quaternion.FromToRotation(forward, actuators.targetDirection);

                // Use delta angle to wrap the angle to -180..180 range.
                float currentTargetDeltaAngle = Mathf.DeltaAngle(0, currentTargetDelta.eulerAngles.y);

                Vector3 newDirection;

                if (turningInput < 0)
                {
                    // We want to go left, but if our current target was to go right, just return to current direction.
                    if (currentTargetDeltaAngle > 45)
                    {
                        newDirection = forward;
                    }
                    else
                    {
                        newDirection = Quaternion.Euler(0, -90, 0) * forward;
                    }
                }
                else
                {
                    // We want to go right, but if our current target was to go left, just return to current direction.
                    if (currentTargetDeltaAngle < -45)
                    {
                        newDirection = forward;
                    }
                    else
                    {
                        newDirection = Quaternion.Euler(0, 90, 0) * forward;
                    }
                }

                // Square rotation to 90 degrees.
                CardinalDirection cardinalDirection = DirectionHelpers.GetCardinalDirectionForVector(newDirection);
                actuators.targetDirection = DirectionHelpers.cardinalDirectionVectors[cardinalDirection];
            }
        }
    }
}
