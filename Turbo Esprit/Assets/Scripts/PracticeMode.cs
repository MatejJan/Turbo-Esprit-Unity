using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class PracticeMode : GameMode
    {
        public override GameCamera currentCamera => cameras[currentCameraIndex];
        public override GameObject currentCarGameObject => currentCarType.car;

        [SerializeField] private CarType[] carTypes;
        [SerializeField] private GameCamera[] cameras;
        [SerializeField] private Driver.Controller[] controllers;
        [SerializeField] private Vector2Int startPosition;
        [SerializeField] private City city;
        [SerializeField] private Driver playerDriver;
        [SerializeField] private GameObject[] debugGameObjects;

        private CarType currentCarType;
        private int currentCameraIndex;
        private int currentControllerIndex;

        private void Start()
        {
            SwitchCarType(0);
            SwitchCamera(0);
            SwitchController(0);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SwitchCarType(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SwitchCarType(1);
            }

            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.V))
            {
                SwitchCamera((currentCameraIndex + 1) % cameras.Length);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Vector3 rotationAngles = currentCarGameObject.transform.localRotation.eulerAngles;
                currentCarGameObject.transform.localRotation = Quaternion.Euler(rotationAngles.x, rotationAngles.y, 0);
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                foreach (GameObject gameObject in debugGameObjects)
                {
                    gameObject.SetActive(!gameObject.activeSelf);
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SwitchController((currentControllerIndex + 1) % controllers.Length);
            }
        }

        private void SwitchCarType(int index)
        {
            // Copy position and rotation from previous car if possible.
            Vector3 position = new Vector3 { x = startPosition.x, z = startPosition.y };
            Quaternion rotation = Quaternion.identity;

            if (currentCarType != null)
            {
                position = currentCarType.car.transform.position;
                rotation = currentCarType.car.transform.rotation;

                currentCarType.car.SetActive(false);
                currentCarType.dashboard.SetActive(false);
            }

            // Setup new car.
            currentCarType = carTypes[index];
            currentCarType.car.GetComponent<CarTracker>().city = city;
            currentCarType.car.SetActive(true);
            currentCarType.dashboard.SetActive(true);

            currentCarType.car.transform.position = position;
            currentCarType.car.transform.rotation = rotation;

            currentCarType.car.GetComponent<CarAudio>().gameMode = this;

            // Move driver to new car.
            playerDriver.car = currentCarType.car.GetComponent<Car>();

            // Make camera track the new car.
            currentCamera.trackedCar = currentCarType.car.GetComponent<CarTracker>();
        }

        private void SwitchCamera(int index)
        {
            currentCamera.gameObject.SetActive(false);

            currentCameraIndex = index;
            currentCamera.gameObject.SetActive(true);
            currentCamera.trackedCar = currentCarType.car.GetComponent<CarTracker>();
        }

        private void SwitchController(int index)
        {
            currentControllerIndex = index;
            playerDriver.controller = controllers[currentControllerIndex];
        }
    }
}
