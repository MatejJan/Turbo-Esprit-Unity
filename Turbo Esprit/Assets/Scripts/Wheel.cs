using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Wheel : MonoBehaviour
    {
        [SerializeField] private WheelCollider wheelCollider;

        private void Update()
        {
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            Vector3 position;
            Quaternion rotation;

            wheelCollider.GetWorldPose(out position, out rotation);

            transform.position = position;
            transform.rotation = rotation;
        }
    }
}
