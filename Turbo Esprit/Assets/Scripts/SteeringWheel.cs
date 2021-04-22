using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class SteeringWheel : MonoBehaviour
    {
        [SerializeField] protected Car car;
        [SerializeField] private float maxDegrees;
        [SerializeField] private float rotationSmoothTime;

        [SerializeField] private Transform[] imageTransforms;

        private float rotationDegrees = 0;
        private float rotationVelocity = 0;

        private void Update()
        {
            UpdateRotation();
        }

        private void UpdateRotation()
        {
            float targetRotationDegrees = -car.steeringWheelPosition * maxDegrees;
            rotationDegrees = Mathf.SmoothDamp(rotationDegrees, targetRotationDegrees, ref rotationVelocity, rotationSmoothTime);

            foreach (Transform transform in imageTransforms)
            {
                transform.localRotation = Quaternion.Euler(0, 0, rotationDegrees);
            }
        }
    }
}
