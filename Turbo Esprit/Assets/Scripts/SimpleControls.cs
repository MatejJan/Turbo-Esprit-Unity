using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    [CreateAssetMenu(fileName = "SimpleControls", menuName = "Scriptable Objects/Simple Controls")]
    public class SimpleControls : OriginalControls
    {
        // The maximum angle the target direction will be away from current direction. 
        [SerializeField] private float maxAngleChangeDifferenceDegrees;

        // The amount of time when turning only affects lane change.
        [SerializeField] private float laneChangeOnlyDuration;

        private float lastTurningInput = 0;
        private float turningInputNonZeroTime = 0;

        protected override void UpdateTargetDirection(Driver.Sensors sensors, Driver.Actuators actuators)
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
