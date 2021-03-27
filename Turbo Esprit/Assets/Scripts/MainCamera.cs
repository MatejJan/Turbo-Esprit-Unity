using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class MainCamera : MonoBehaviour
    {
        [SerializeField] private CarTracker trackedCar;
        [SerializeField] private float offsetFromCar;

        private void LateUpdate()
        {
            UpdateTransform();
        }

        private void UpdateTransform()
        {
            Vector3 viewDirection;

            if (trackedCar.street != null)
            {
                // Follow along the street in the direction the car is facing.
                if (trackedCar.street.orientation == StreetOrientation.EastWest)
                {
                    if (trackedCar.direction.x > 0)
                    {
                        viewDirection = Vector3.right;
                    }
                    else
                    {
                        viewDirection = Vector3.left;
                    }
                }
                else
                {
                    if (trackedCar.direction.z > 0)
                    {
                        viewDirection = Vector3.forward;
                    }
                    else
                    {
                        viewDirection = Vector3.back;
                    }
                }
            }
            else
            {
                // Follow behind the car's cardinal direction.
                viewDirection = DirectionHelpers.cardinalDirectionVectors[trackedCar.cardinalDirection];
            }

            // Set rotation to look along view direction.
            transform.rotation = Quaternion.LookRotation(viewDirection);

            Debug.DrawLine(trackedCar.position + Vector3.up, trackedCar.position + viewDirection + Vector3.up, Color.yellow);

            // Update position to be in the center of the street/intersection, trailing behind the car by the specified offset.
            float cameraY = transform.position.y;

            if (trackedCar.street != null)
            {
                Bounds bounds = trackedCar.street.bounds;
                Debug.DrawLine(bounds.min, bounds.min + Vector3.up * 10, Color.red);
                Debug.DrawLine(bounds.max, bounds.max + Vector3.up * -10, Color.blue);

                if (trackedCar.street.orientation == StreetOrientation.EastWest)
                {
                    transform.position = new Vector3(trackedCar.position.x, cameraY, bounds.center.z);
                }
                else
                {
                    transform.position = new Vector3(bounds.center.x, cameraY, trackedCar.position.z);
                }
            }
            else
            {
                Bounds bounds = trackedCar.intersection.bounds;
                Debug.DrawLine(bounds.min, bounds.min + Vector3.up * 10, Color.red);
                Debug.DrawLine(bounds.max, bounds.max + Vector3.up * -10, Color.blue);

                if (trackedCar.cardinalDirection == CardinalDirection.East || trackedCar.cardinalDirection == CardinalDirection.West)
                {
                    transform.position = new Vector3(trackedCar.position.x, cameraY, bounds.center.z);
                }
                else
                {
                    transform.position = new Vector3(bounds.center.x, cameraY, trackedCar.position.z);
                }
            }

            transform.position += viewDirection * offsetFromCar;
        }
    }
}
