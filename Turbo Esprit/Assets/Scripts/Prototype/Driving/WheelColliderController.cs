using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit.Prototype.Driving
{
    public class WheelColliderController : MonoBehaviour
    {
        public GameObject wheelModel;

        private WheelCollider wheelCollider;

        private void Awake()
        {
            wheelCollider = GetComponent<WheelCollider>();
        }

        private void FixedUpdate()
        {
            Vector3 position;
            Quaternion rotation;

            wheelCollider.GetWorldPose(out position, out rotation);

            wheelModel.transform.position = position;
            wheelModel.transform.rotation = rotation;
        }
    }
}
