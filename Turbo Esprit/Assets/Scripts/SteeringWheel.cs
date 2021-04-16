using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class SteeringWheel : MonoBehaviour
    {
        [SerializeField] protected Car car;
        [SerializeField] private float maxDegrees;
        [SerializeField] private float maxAngularSpeedDegrees;
        [SerializeField] private float deadZoneDegrees;

        [SerializeField] private Transform[] imageTransforms;

        private float rotationDegrees = 0;

        private void Update()
        {
            UpdateRotation();
        }

        private void UpdateRotation()
        {
            float targetRotationDegrees = -car.steeringWheelPosition * maxDegrees;
            if (Mathf.Abs(targetRotationDegrees) < deadZoneDegrees) targetRotationDegrees = 0;

            rotationDegrees = Mathf.MoveTowardsAngle(rotationDegrees, targetRotationDegrees, maxAngularSpeedDegrees * Time.deltaTime);

            foreach (Transform transform in imageTransforms)
            {
                transform.localRotation = Quaternion.Euler(0, 0, rotationDegrees);
            }
        }
    }
}
