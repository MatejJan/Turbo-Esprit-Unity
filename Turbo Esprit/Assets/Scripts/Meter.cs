using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public abstract class Meter : MonoBehaviour
    {
        [SerializeField] protected Car car;

        [SerializeField] private float zeroPositionDegrees;
        [SerializeField] private float unitDegrees;
        [SerializeField] private float maxUnits;
        [SerializeField] private float maxDialAngularSpeedDegrees;

        [SerializeField] private Transform dialTransform;

        private float dialDegrees;

        protected abstract float GetValue();

        private void Awake()
        {
            dialDegrees = zeroPositionDegrees;
        }

        private void Update()
        {
            UpdateDialTransform();
        }

        private void UpdateDialTransform()
        {
            float clampedValue = Mathf.Clamp(GetValue(), 0, maxUnits);
            float newDialDegrees = zeroPositionDegrees + clampedValue * unitDegrees;

            float change = newDialDegrees - dialDegrees;
            float absoluteChange = Mathf.Abs(change);

            float maxChange = maxDialAngularSpeedDegrees * Time.deltaTime;
            float relativeChange = absoluteChange > maxChange ? maxChange / absoluteChange : 1;

            dialDegrees += change * relativeChange;

            dialTransform.localRotation = Quaternion.Euler(0, 0, dialDegrees);
        }
    }
}
