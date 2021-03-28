using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class PlayerDriver : MonoBehaviour
    {
        [SerializeField] private float shiftingPedalChangeTime;
        [SerializeField] private float shiftingGearshiftChangeTime;

        private Car car;
        private bool playerHasControl = true;

        private void Awake()
        {
            car = GetComponent<Car>();
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            car.steeringWheelPosition = Input.GetAxis("Steering");

            if (playerHasControl)
            {
                car.acceleratorPedalPosition = Input.GetAxis("Accelerator");
                car.brakePedalPosition = Input.GetAxis("Brake");
                car.clutchPedalPosition = Input.GetAxis("Clutch");

                if (car.clutchPedalPosition == 1)
                {
                    if (Input.GetButtonDown("Shift up"))
                    {
                        car.gearshiftPosition++;
                    }

                    if (Input.GetButtonDown("Shift down"))
                    {
                        car.gearshiftPosition--;
                    }
                }
                else
                {
                    if (Input.GetButtonDown("Shift up"))
                    {
                        StartCoroutine(ShiftingCoroutine(1));
                    }

                    if (Input.GetButtonDown("Shift down"))
                    {
                        StartCoroutine(ShiftingCoroutine(-1));
                    }
                }
            }

            if (Input.GetButtonDown("Ignition"))
            {
                if (car.engineState == Car.EngineState.Off)
                {
                    StartCoroutine(IgnitionCoroutine());
                }
                else
                {
                    car.ignitionSwitchPosition = Car.IgnitionSwitchPosition.Lock;
                }
            }
        }

        IEnumerator ShiftingCoroutine(int direction)
        {
            playerHasControl = false;

            while (car.clutchPedalPosition < 1)
            {
                car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, 0, Time.deltaTime / shiftingPedalChangeTime);
                car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 1, Time.deltaTime / shiftingPedalChangeTime);
                yield return null;
            }

            yield return new WaitForSeconds(shiftingGearshiftChangeTime / 2);

            car.gearshiftPosition += direction;

            yield return new WaitForSeconds(shiftingGearshiftChangeTime / 2);

            while (car.clutchPedalPosition > 0)
            {
                car.acceleratorPedalPosition = Mathf.MoveTowards(car.acceleratorPedalPosition, Input.GetAxis("Accelerator"), Time.deltaTime / shiftingPedalChangeTime);
                car.clutchPedalPosition = Mathf.MoveTowards(car.clutchPedalPosition, 0, Time.deltaTime / shiftingPedalChangeTime);
                yield return null;
            }

            playerHasControl = true;
        }

        IEnumerator IgnitionCoroutine()
        {
            car.ignitionSwitchPosition = Car.IgnitionSwitchPosition.Start;

            while (car.engineState != Car.EngineState.On)
            {
                yield return null;
            }

            car.ignitionSwitchPosition = Car.IgnitionSwitchPosition.On;
        }
    }
}
