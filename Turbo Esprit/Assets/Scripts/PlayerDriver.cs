using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class PlayerDriver : Driver
    {
        public float targetSpeedMph;

        [SerializeField] private float speedChangeRate;
        [SerializeField] private float roundingSpeedStep;

        private bool waitingForInputSignChange = false;
        private int speedChangeInputSignBeforeWaiting = 0;

        private bool keepTargetSpeed = true;

        protected override void Update()
        {
            UpdateTargetSpeed();

            base.Update();
        }

        private void UpdateTargetSpeed()
        {
            // Read player input.
            float speedChangeInput = Input.GetAxis("Speed Change");

            // When the car has stopped, we need to get a different input before we react to changes so that braking<->accelerating transition is deliberate.
            if (waitingForInputSignChange)
            {
                int speedChangeInputSign = Math.Sign(speedChangeInput);
                if (speedChangeInputSign == speedChangeInputSignBeforeWaiting) return;

                waitingForInputSignChange = false;
            }

            // Nothing to do if we're keeping the current speed and no change is requested.
            if (keepTargetSpeed && speedChangeInput == 0) return;

            if (speedChangeInput != 0)
            {
                // We want to change speed so calculate new target speed.
                float speedChange = speedChangeInput * speedChangeRate * Time.deltaTime;
                float newTargetSpeed = targetSpeed + speedChange;

                // Make car stop before changing direction.
                int newTargetSpeedSign = Math.Sign(newTargetSpeed);
                int carSpeedSign = Math.Sign(car.speed);

                if (carSpeedSign != 0 && newTargetSpeedSign != carSpeedSign)
                {
                    newTargetSpeed = 0;
                }

                // Set new target speed.
                targetSpeed = newTargetSpeed;

                // Start the waiting period for input to change its sign.
                if (targetSpeed == 0 && car.speed == 0)
                {
                    waitingForInputSignChange = true;
                    speedChangeInputSignBeforeWaiting = Math.Sign(speedChangeInput);
                }

                // Don't keep the speed until we stop changing it.
                keepTargetSpeed = false;
            }
            else
            {
                // Round the current car speed as the new target.
                targetSpeed = Mathf.Round(car.speed / roundingSpeedStep) * roundingSpeedStep;
                keepTargetSpeed = true;
            }

            targetSpeedMph = targetSpeed * PhysicsHelper.metersPerSecondToMilesPerHour;
        }
    }
}
