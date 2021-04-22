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
            // Look along the street space cardinal direction, but only turn 180 degrees if the car is more than 95 degrees turned to prevent camera jumping back and forth when driving perpendicular to the street.
            Vector3 streetDirection = DirectionHelpers.cardinalDirectionVectors[trackedCar.streetCardinalDirection];

            if (Vector3.Angle(transform.forward, streetDirection) < 135 || Vector3.Angle(transform.forward, trackedCar.carWorldDirection) > 95)
            {
                // Set rotation to look along the street.
                transform.rotation = Quaternion.LookRotation(streetDirection);
            }

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

            transform.position += transform.forward * offsetFromCar;
        }

        private void OnDrawGizmos()
        {
            void DrawWireCube(Bounds bounds)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            if (trackedCar.street != null)
            {
                DrawWireCube(trackedCar.street.bounds);
                if (trackedCar.street.startIntersection != null) DrawWireCube(trackedCar.street.startIntersection.bounds);
                if (trackedCar.street.endIntersection != null) DrawWireCube(trackedCar.street.endIntersection.bounds);
            }
            else
            {
                DrawWireCube(trackedCar.intersection.bounds);
                if (trackedCar.intersection.northStreet != null) DrawWireCube(trackedCar.intersection.northStreet.bounds);
                if (trackedCar.intersection.southStreet != null) DrawWireCube(trackedCar.intersection.southStreet.bounds);
                if (trackedCar.intersection.eastStreet != null) DrawWireCube(trackedCar.intersection.eastStreet.bounds);
                if (trackedCar.intersection.westStreet != null) DrawWireCube(trackedCar.intersection.westStreet.bounds);
            }
        }
    }
}
