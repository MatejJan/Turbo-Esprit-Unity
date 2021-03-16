using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit.Prototype.Driving
{
    public class CameraController : MonoBehaviour
    {
        public Transform targetTransform;
        public Vector3 positionOffset;

        private void Start()
        {
            positionOffset = transform.position - targetTransform.position;
        }

        private void FixedUpdate()
        {
            transform.position = targetTransform.position + positionOffset;
        }
    }
}
