using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class PlayerDriver : MonoBehaviour
    {
        private Car car;

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
            car.acceleratorPedalPosition = Input.GetAxis("Accelerator");
            car.brakePedalPosition = Input.GetAxis("Brake");
            car.clutchPedalPosition = Input.GetAxis("Clutch");
            car.steeringWheelPosition = Input.GetAxis("Steering");

            if (Input.GetButtonDown("Shift up"))
            {
                car.gearshiftPosition++;
            }

            if (Input.GetButtonDown("Shift down"))
            {
                car.gearshiftPosition--;
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
