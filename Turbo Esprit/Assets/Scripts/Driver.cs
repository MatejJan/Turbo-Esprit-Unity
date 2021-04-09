using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Driver : MonoBehaviour
    {
        [SerializeField] private DriverProfile profile;

        protected Car car;

        private State state = State.Parked;
        private float idlingTime = 0;
        private float shiftingTime = 0;
        private float previousSpeed = 0;

        private Car.GearshiftPosition targetGearshiftPosition;

        public enum State
        {
            Parked,
            Starting,
            Idling,
            MovingOff,
            Driving,
            Shifting,
            Stopping
        }

        protected float targetSpeed { get; set; }

        private void Awake()
        {
            car = GetComponent<Car>();
        }

        protected virtual void Update()
        {
            UpdateDesiredGearshiftPosition();
            UpdateState();
            UpdateIgnitionSwitchPosition();
            UpdateAcceleratorPedalPosition();
            UpdateClutchPedalPosition();
            UpdateBrakePedalPosition();
            UpdateGearshiftPosition();
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
            else if (state != State.Shifting && state != State.MovingOff)
            {
                // We have to be at least in first gear.
                if (car.gearshiftPosition == Car.GearshiftPosition.Neutral)
                {
                    targetGearshiftPosition = Car.GearshiftPosition.FirstGear;
                }

                // Shift up when accelerating above specified limit.
                float activeUpshiftEngineRpm = Mathf.Abs(targetSpeed - car.speed) < profile.closeSpeedDifference ? profile.upshiftEngineRpmWhenCloseToTarget : profile.upshiftEngineRpm;
                if (targetSpeed > car.speed && car.engineRpm > activeUpshiftEngineRpm && car.gearshiftPosition < Car.GearshiftPosition.FifthGear)
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

        private void UpdateState()
        {
            switch (state)
            {
                case State.Parked:
                    // We move out of parked into starting when any speed is required.
                    if (targetSpeed != 0) state = State.Starting;
                    break;

                case State.Starting:
                    // When starting, we wait for the engine to turn on before going idle.
                    if (car.engineState == Car.EngineState.On) state = State.Idling;
                    break;

                case State.Idling:
                    // When idling, we wait for target speed to get a value before moving off.
                    if (targetSpeed != 0) state = State.MovingOff;

                    // When the car is not moving, we wait before shutting the engine off.
                    if (car.speed == 0)
                    {
                        idlingTime += Time.deltaTime;
                        if (idlingTime > profile.maxIdlingDuration) state = State.Parked;
                    }

                    // When leaving state, reset idling time for next time.
                    if (state != State.Idling) idlingTime = 0;

                    break;

                case State.MovingOff:
                case State.Shifting:
                    // When moving off or shifting, we wait until clutch is released in the right gear.
                    if (car.clutchPedalPosition == 0 && car.gearshiftPosition == targetGearshiftPosition) state = State.Driving;

                    // If target speed becomes zero, we should stop.
                    if (targetSpeed == 0) state = State.Stopping;
                    break;

                case State.Driving:
                    // When driving, if we're not in the right gear, start shifting.
                    if (car.gearshiftPosition != targetGearshiftPosition) state = State.Shifting;

                    // If target speed becomes zero, we should stop.
                    if (targetSpeed == 0) state = State.Stopping;
                    break;

                case State.Stopping:
                    // When stopping, if target speed changes, shift back into gear.
                    if (targetSpeed != 0) state = State.Shifting;

                    // When our speed stops, we go to idling.
                    if (car.speed == 0) state = State.Idling;
                    break;
            }
        }

        private void UpdateIgnitionSwitchPosition()
        {
            switch (state)
            {
                case State.Starting:
                    car.ignitionSwitchPosition = Car.IgnitionSwitchPosition.Start;
                    break;

                case State.Parked:
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

            switch (state)
            {
                case State.Idling:
                case State.Parked:
                    car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 0, profile.normalPedalSpeed * Time.deltaTime);
                    break;

                case State.MovingOff:
                case State.Shifting:
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

                case State.Driving:
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
                    float positionChange = accelerationDifference * profile.acceleratorChangeRate;
                    float targetPosition = Mathf.Clamp(car.acceleratorPedalPosition + positionChange, 0, 1);

                    car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, targetPosition, profile.normalPedalSpeed * Time.deltaTime);

                    break;

                case State.Stopping:
                    car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 0, profile.normalPedalSpeed * Time.deltaTime);
                    break;
            }

            previousSpeed = speed;
        }

        private void UpdateClutchPedalPosition()
        {
            switch (state)
            {
                case State.MovingOff:
                case State.Shifting:
                    if (car.gearshiftPosition == targetGearshiftPosition)
                    {
                        // When we're in the desired gearshift position, we should be releasing the clutch.
                        float pedalSpeed = state == State.MovingOff ? profile.movingOffPedalSpeed : profile.shiftingPedalSpeed;
                        car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 0, pedalSpeed * Time.deltaTime);
                    }
                    else
                    {
                        // When we're not in the desired gearshift position, we should be pressing the clutch.
                        car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 1, profile.shiftingPedalSpeed * Time.deltaTime);
                    }
                    break;

                case State.Stopping:
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

            car.brakePedalPosition = Mathf.MoveTowards(car.brakePedalPosition, targetPedalPosition, profile.normalPedalSpeed * Time.deltaTime);
        }

        private void UpdateGearshiftPosition()
        {
            switch (state)
            {
                case State.MovingOff:
                case State.Shifting:
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

                case State.Parked:
                case State.Idling:
                case State.Stopping:
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
    }
}
