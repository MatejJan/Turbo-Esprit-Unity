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
                    if (trackedCar.carWorldDirection.x > 0)
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
                    if (trackedCar.carWorldDirection.z > 0)
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
                viewDirection = DirectionHelpers.cardinalDirectionVectors[trackedCar.streetCardinalDirection];
            }

            // Set rotation to look along view direction.
            transform.rotation = Quaternion.LookRotation(viewDirection);

            // Update position to be in the center of the street/intersection, trailing behind the car by the specified offset.
            float cameraY = transform.position.y;

            if (trackedCar.street != null)
            {
                Bounds bounds = trackedCar.street.bounds;

                if (trackedCar.street.orientation == StreetOrientation.EastWest)
                {
                    transform.position = new Vector3(trackedCar.carWorldPosition.x, cameraY, bounds.center.z);
                }
                else
                {
                    transform.position = new Vector3(bounds.center.x, cameraY, trackedCar.carWorldPosition.z);
                }
            }
            else
            {
                Bounds bounds = trackedCar.intersection.bounds;

                if (trackedCar.streetCardinalDirection == CardinalDirection.East || trackedCar.streetCardinalDirection == CardinalDirection.West)
                {
                    transform.position = new Vector3(trackedCar.carWorldPosition.x, cameraY, bounds.center.z);
                }
                else
                {
                    transform.position = new Vector3(bounds.center.x, cameraY, trackedCar.carWorldPosition.z);
                }
            }

            transform.position += viewDirection * offsetFromCar;
        }

        private void OnDrawGizmos()
        {
            if (trackedCar.street != null)
            {
                Bounds bounds = trackedCar.street.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
            else
            {
                Bounds bounds = trackedCar.intersection.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
