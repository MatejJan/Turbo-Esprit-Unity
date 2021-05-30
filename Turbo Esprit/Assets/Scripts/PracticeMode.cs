using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class PracticeMode : GameMode
    {
        public override GameCamera currentCamera => cameras[currentCameraIndex];

        [SerializeField] private CarType[] carTypes;
        [SerializeField] private GameCamera[] cameras;
        [SerializeField] private Vector2Int startPosition;
        [SerializeField] private City city;

        private CarType currentCarType;
        private int currentCameraIndex;

        private void Start()
        {
            SwitchCarType(0);
            SwitchCamera(0);
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

            if (Input.GetKeyDown(KeyCode.C))
            {
                SwitchCamera((currentCameraIndex + 1) % cameras.Length);
            }
        }

        private void SwitchCarType(int index)
        {
            Vector3 position = new Vector3 { x = startPosition.x, z = startPosition.y };
            Quaternion rotation = Quaternion.identity;

            if (currentCarType != null)
            {
                position = currentCarType.car.transform.position;
                rotation = currentCarType.car.transform.rotation;

                currentCarType.car.SetActive(false);
                currentCarType.dashboard.SetActive(false);
            }

            currentCarType = carTypes[index];
            currentCarType.car.GetComponent<CarTracker>().city = city;
            currentCarType.car.SetActive(true);
            currentCarType.dashboard.SetActive(true);

            currentCarType.car.transform.position = position;
            currentCarType.car.transform.rotation = rotation;

            currentCamera.trackedCar = currentCarType.car.GetComponent<CarTracker>();
        }

        private void SwitchCamera(int index)
        {
            currentCamera.gameObject.SetActive(false);

            currentCameraIndex = index;
            currentCamera.gameObject.SetActive(true);
            currentCamera.trackedCar = currentCarType.car.GetComponent<CarTracker>();
        }
    }
}
