using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Driver : MonoBehaviour
    {
        [SerializeField] private float normalPedalSpeed;
        [SerializeField] private float shiftingPedalSpeed;
        [SerializeField] private float movingOffPedalSpeed;
        [SerializeField] private float shiftingDuration;
        [SerializeField] private float maxIdlingDuration;
        [SerializeField] private float preventShiftingMaxSpeedDifference;
        [SerializeField] private float acceleratorChangeRate;

        protected Car car;

        public State state = State.Parked;
        private float idlingTime = 0;
        private float shiftingTime = 0;

        public Car.GearshiftPosition targetGearshiftPosition;
        private Car.GearshiftPosition[] bestGearshiftPositionTable = new Car.GearshiftPosition[67];

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

            // Precalculate the best gearshift positions.
            Car.GearshiftPosition bestGear = Car.GearshiftPosition.FirstGear;

            for (int speed = 0; speed < bestGearshiftPositionTable.Length; speed++)
            {
                if (bestGear < Car.GearshiftPosition.FifthGear)
                {
                    float torqueInCurrentBestGear = car.GetTorqueForGearshiftPositionAtSpeed(bestGear, speed);
                    float torqueInNextGear = car.GetTorqueForGearshiftPositionAtSpeed(bestGear + 1, speed);

                    if (torqueInNextGear > torqueInCurrentBestGear) bestGear++;
                }

                bestGearshiftPositionTable[speed] = bestGear;
            }
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
            else if (Mathf.Abs(targetSpeed - car.speed) < preventShiftingMaxSpeedDifference)
            {
                // When you're close enough to target speed, don't change gear.
                targetGearshiftPosition = car.gearshiftPosition;
            }
            else
            {
                // We should be in the gear that produces the most torque.
                int tableIndex = Mathf.Clamp(Mathf.RoundToInt(car.speed), 0, bestGearshiftPositionTable.Length - 1);
                int bestGear = (int)bestGearshiftPositionTable[tableIndex];

                // Don't downshift just one gear if trying to accelerate.
                if (targetSpeed > car.speed && bestGear == (int)targetGearshiftPosition - 1) return;

                targetGearshiftPosition = (Car.GearshiftPosition)bestGear;
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
                        if (idlingTime > maxIdlingDuration) state = State.Parked;
                    }

                    // When leaving state, reset idling time for next time.
                    if (state != State.Idling) idlingTime = 0;

                    break;

                case State.MovingOff:
                case State.Shifting:
                    // When moving off or shifting, we wait until clutch is released in the right gear.
                    if (car.clutchPedalPosition == 0 && car.gearshiftPosition == targetGearshiftPosition) state = State.Driving;
                    break;

                case State.Driving:
                    // When driving, if we're not in the right gear, start shifting.
                    if (car.gearshiftPosition != targetGearshiftPosition) state = State.Shifting;
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
            switch (state)
            {
                case State.MovingOff:
                case State.Shifting:
                    if (car.gearshiftPosition == targetGearshiftPosition)
                    {
                        // When we're in the desired gearshift position, we should be pressing the accelerator.
                        car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 1, shiftingPedalSpeed * Time.deltaTime);
                    }
                    else
                    {
                        // When we're not in the desired gearshift position, we should be releasing the accelerator.
                        car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 0, shiftingPedalSpeed * Time.deltaTime);
                    }
                    break;

                case State.Driving:
                    float speedDifference = targetSpeed - car.speed;
                    float positionChange = speedDifference * acceleratorChangeRate;
                    float targetPosition = Mathf.Clamp(car.acceleratorPedalPosition + positionChange, 0, 1);

                    car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, targetPosition, normalPedalSpeed * Time.deltaTime);

                    break;
            }
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
                        float pedalSpeed = state == State.MovingOff ? movingOffPedalSpeed : shiftingPedalSpeed;
                        car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 0, pedalSpeed * Time.deltaTime);
                    }
                    else
                    {
                        // When we're not in the desired gearshift position, we should be pressing the clutch.
                        car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 1, shiftingPedalSpeed * Time.deltaTime);
                    }
                    break;
            }
        }

        private void UpdateBrakePedalPosition()
        {
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
                            if (shiftingTime > shiftingDuration)
                            {
                                car.gearshiftPosition = targetGearshiftPosition;
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
                            }
                        }
                    }
                    break;
            }
        }
    }
}
