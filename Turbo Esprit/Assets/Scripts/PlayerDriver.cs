using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class PlayerDriver : Driver
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

        protected override void Update()
        {
            UpdateTargetSpeed();
            base.Update();
        }

        private void UpdateTargetSpeed()
        {
            // Read player input.
            float speedChangeInput = Input.GetAxis("Speed Change");

            // When the car has stopped and we're not pressing anything, reset speed sign.
            if (car.speed == 0 && speedChangeInput == 0)
            {
                speedSign = 0;
            }

            // Nothing to do if we're keeping the current speed and no change is requested.
            if (keepTargetSpeed && speedChangeInput == 0) return;

            if (speedChangeInput != 0)
            {
                // If sign is not set when we're stopped, determine the direction we want to go in.
                if (speedSign == 0 && car.speed == 0)
                {
                    speedSign = Math.Sign(speedChangeInput);
                }

                // We want to change speed so calculate new target speed.
                float absoluteSpeedChangeInput = speedChangeInput * speedSign;
                float absoluteSpeedChange = absoluteSpeedChangeInput * speedChangeRate * Time.deltaTime;
                float newAbsoluteTargetSpeed = Mathf.Abs(targetSpeed) + absoluteSpeedChange;

                // Ensure minimum speed difference for faster reaction.
                float absoluteCarSpeed = Mathf.Abs(car.speed);

                if (absoluteSpeedChangeInput < 0)
                {
                    newAbsoluteTargetSpeed = Mathf.Clamp(newAbsoluteTargetSpeed, 0, Mathf.Max(0, absoluteCarSpeed - minSpeedChangeDifference));
                }
                else if (absoluteSpeedChangeInput > 0 && newAbsoluteTargetSpeed < absoluteCarSpeed + minSpeedChangeDifference)
                {
                    newAbsoluteTargetSpeed = Mathf.Max(newAbsoluteTargetSpeed, absoluteCarSpeed + minSpeedChangeDifference);
                }

                // Set new target speed.
                targetSpeed = newAbsoluteTargetSpeed * speedSign;

                // Don't keep the speed until we stop changing it.
                keepTargetSpeed = false;
            }
            else
            {
                // Round the current car speed as the new target.
                targetSpeed = Mathf.Round(car.speed / roundingSpeedStep) * roundingSpeedStep;
                keepTargetSpeed = true;
            }
        }
    }
}
