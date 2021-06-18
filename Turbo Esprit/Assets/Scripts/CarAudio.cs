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

        [SerializeField] private AudioSource turnSignalAudioSource;
        [SerializeField] private AudioClip turnSignalStart;
        [SerializeField] private AudioClip turnSignalEnd;

        [SerializeField] private float volumeFactor = 1;

        [SerializeField] private float enabledDistance;

        private Car.EngineState previousEngineState = Car.EngineState.Off;
        private bool audioEnabled;
        private float playerCarDistance;
        private Car.TurnSignalsPosition previousTurnSignalsPosition = Car.TurnSignalsPosition.Off;

        public GameMode gameMode { get; set; }

        private void Update()
        {
            UpdateAudioEnabled();

            if (audioEnabled)
            {
                UpdateEngineSound();
                UpdateVolume();
                HandleEngineState();
                HandleTurnSignalsPosition();
            }
        }

        private void UpdateAudioEnabled()
        {
            playerCarDistance = (transform.position - gameMode.currentCarGameObject.transform.position).magnitude;
            audioEnabled = playerCarDistance < enabledDistance;

            engineOpenedThrottleAudioSource.enabled = audioEnabled;
            engineClosedThrottleAudioSource.enabled = audioEnabled;
            ignitionAudioSource.enabled = audioEnabled;
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

        private void UpdateVolume()
        {
            // Create a very sharp fade out curve.
            float fadeFactor = 1 - Mathf.Pow(playerCarDistance / enabledDistance, 10);

            engineOpenedThrottleAudioSource.volume *= fadeFactor * volumeFactor;
            engineClosedThrottleAudioSource.volume *= fadeFactor * volumeFactor;
            ignitionAudioSource.volume = fadeFactor * volumeFactor;
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

        private void HandleTurnSignalsPosition()
        {
            // Nothing to do for cars that don't have a turning signal sound.
            if (turnSignalAudioSource == null) return;

            if (car.turnSignalsPosition != Car.TurnSignalsPosition.Off)
            {
                if (car.turnSignalsPosition != previousTurnSignalsPosition)
                {
                    // The signals have just been changed. Play the start sound.
                    turnSignalAudioSource.PlayOneShot(turnSignalStart);
                }

                if (previousTurnSignalsPosition == Car.TurnSignalsPosition.Off)
                {
                    // Start the loop after the start sound has finished.
                    turnSignalAudioSource.PlayDelayed(turnSignalStart.length);
                }
            }
            else
            {
                if (previousTurnSignalsPosition != Car.TurnSignalsPosition.Off)
                {
                    // End the loop.
                    turnSignalAudioSource.Stop();

                    // The signals have just been turned off. Play the end sound.
                    turnSignalAudioSource.PlayOneShot(turnSignalEnd);
                }
            }

            previousTurnSignalsPosition = car.turnSignalsPosition;
        }
    }

    [System.Serializable]
    public struct VolumeLimits
    {
        [Range(0, 1)] public float min;
        [Range(0, 1)] public float max;
    }
}
