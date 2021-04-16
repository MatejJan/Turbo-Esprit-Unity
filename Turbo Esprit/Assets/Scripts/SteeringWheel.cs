using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class SteeringWheel : MonoBehaviour
    {
        [SerializeField] protected Car car;
        [SerializeField] private float maxDegrees;

        [SerializeField] private Transform[] imageTransforms;

        private void Update()
        {
            UpdateRotation();
        }

        private void UpdateRotation()
        {
            float rotationDegrees = -car.steeringWheelPosition * maxDegrees;

            foreach (Transform transform in imageTransforms)
            {
                transform.localRotation = Quaternion.Euler(0, 0, rotationDegrees);
            }
        }
    }
}
