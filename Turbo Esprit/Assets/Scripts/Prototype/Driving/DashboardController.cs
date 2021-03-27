using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit.Prototype.Driving
{
    public class DashboardController : MonoBehaviour
    {
        // Speedometer has 60 mph per 90 degrees.
        private const float speedometerMphToDegrees = -90f / 60f;
        private const float speedometerZeroMphDegrees = 225;
        private const float speedometerMaxMph = 180;

        // Tachometer has 2500 rpm per 90 degrees.
        private const float tachometerRpmToDegrees = -90f / 2500f;
        private const float tachometerZeroRpmDegrees = 216;
        private const float tachometerMaxRpm = 7000;

        public GameObject providerGameObject;

        public Transform speedometerDialTransform;
        public Transform tachometerDialTransform;

        private IDashboardProvider provider;

        private float currentSpeedometerDegrees = speedometerZeroMphDegrees;
        private float currentTachometerDegrees = tachometerZeroRpmDegrees;
        private float maxDialAngularSpeedDegrees = 360f;

        private void Awake()
        {
            provider = providerGameObject.GetComponent<IDashboardProvider>();
        }

        private void Update()
        {
            // Update speed.
            float speedMph = Mathf.Clamp(provider.speedMph, 0, speedometerMaxMph);
            float newSpeedometerDegrees = speedometerZeroMphDegrees + speedometerMphToDegrees * speedMph;

            currentSpeedometerDegrees = GetDialDegreesAnimated(currentSpeedometerDegrees, newSpeedometerDegrees);
            speedometerDialTransform.localRotation = Quaternion.Euler(0, 0, currentSpeedometerDegrees);

            // Update engine RPM.
            float engineRpm = Mathf.Clamp(provider.engineRpm, 0, tachometerMaxRpm);
            float newTachometerDegrees = tachometerZeroRpmDegrees + tachometerRpmToDegrees * engineRpm;

            currentTachometerDegrees = GetDialDegreesAnimated(currentTachometerDegrees, newTachometerDegrees);
            tachometerDialTransform.localRotation = Quaternion.Euler(0, 0, currentTachometerDegrees);
        }

        private float GetDialDegreesAnimated(float currentValue, float newValue)
        {
            float change = newValue - currentValue;
            float absoluteChange = Mathf.Abs(change);
            float maxChange = maxDialAngularSpeedDegrees * Time.deltaTime;

            float relativeChange = absoluteChange > maxChange ? maxChange / absoluteChange : 1;
            return currentValue + change * relativeChange;
        }
    }
}
