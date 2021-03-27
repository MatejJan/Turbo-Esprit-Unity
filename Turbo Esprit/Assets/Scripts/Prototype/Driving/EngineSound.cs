using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit.Prototype.Driving
{
    public class EngineSound : MonoBehaviour
    {
        public float baseRpm;

        public AudioSource audioSource;
        public GameObject providerGameObject;

        private IDashboardProvider provider;

        private void Awake()
        {
            provider = providerGameObject.GetComponent<IDashboardProvider>();
        }

        private void Update()
        {
            // Update speed.
            audioSource.pitch = provider.engineRpm / baseRpm;
        }
    }
}
