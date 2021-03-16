using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TurboEsprit.Prototype.Navigation
{
    public class MouseManager : MonoBehaviour
    {
        // Store which objects are clickable.
        public LayerMask clickableLayer;

        public EventVector3 onClick;

        void Update()
        {
            RaycastHit hit;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 50, clickableLayer.value))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    onClick.Invoke(hit.point);
                }
            }
        }
    }

    [System.Serializable]
    public class EventVector3 : UnityEvent<Vector3> { }
}
