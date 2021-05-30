using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class CarCamera : GameCamera
    {
        [SerializeField] private Vector3 offsetFromCar;
        [SerializeField] private bool yRotationOnly;

        private void LateUpdate()
        {
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            transform.position = trackedCar.transform.TransformPoint(offsetFromCar);

            if (yRotationOnly)
            {
                float yAngle = trackedCar.transform.rotation.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0, yAngle, 0);
            }
            else
            {
                transform.rotation = trackedCar.transform.rotation;
            }
        }
    }
}
