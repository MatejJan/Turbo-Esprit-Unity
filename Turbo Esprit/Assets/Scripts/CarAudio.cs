using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class CarAudio : MonoBehaviour
    {
        [SerializeField] private Car car;

        [SerializeField] private AudioSource engineOpenedThrottleAudioSource;
        [SerializeField] private float engineOpenedThrottleAudioClipRpm;
        [SerializeField] private VolumeLimits engineOpenedThrottleAudioSourceVolumeLimits;

        [SerializeField] private AudioSource engineClosedThrottleAudioSource;
        [SerializeField] private float engineClosedThrottleAudioClipRpm;
        [SerializeField] private VolumeLimits engineClosedThrottleAudioSourceVolumeLimits;

        [SerializeField] private AudioSource ignitionAudioSource;
        private Car.EngineState previousEngineState = Car.EngineState.Off;

        private void Update()
        {
            UpdateEngineSound();
            HandleEngineState();
        }

        private void UpdateEngineSound()
        {
            // Update pitch.
            engineOpenedThrottleAudioSource.pitch = car.engineRpm / engineOpenedThrottleAudioClipRpm;
            engineClosedThrottleAudioSource.pitch = car.engineRpm / engineClosedThrottleAudioClipRpm;

            // Crossfade between the opened and closed clips depending on the gas pedal position.
            VolumeLimits openedLimits = engineOpenedThrottleAudioSourceVolumeLimits;
            VolumeLimits closedLimits = engineClosedThrottleAudioSourceVolumeLimits;

            engineOpenedThrottleAudioSource.volume = Mathf.Lerp(openedLimits.min, openedLimits.max, car.acceleratorPedalPosition);
            engineClosedThrottleAudioSource.volume = Mathf.Lerp(closedLimits.min, closedLimits.max, 1 - car.acceleratorPedalPosition);
        }

        private void HandleEngineState()
        {
            // Start ignition when starting.
            if (car.engineState == Car.EngineState.Starting && previousEngineState != Car.EngineState.Starting)
            {
                ignitionAudioSource.Play();
            }

            previousEngineState = car.engineState;
        }
    }

    [System.Serializable]
    public struct VolumeLimits
    {
        [Range(0, 1)] public float min;
        [Range(0, 1)] public float max;
    }
}
