using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class CarTracker : MonoBehaviour
    {
        // Fields

        [SerializeField] private City city;

        private Street _street;
        private Intersection _intersection;

        // Properties

        public Street street
        {
            get => _street;
            set
            {
                _street = value;
                _intersection = null;
            }
        }

        public Intersection intersection
        {
            get => _intersection;
            set
            {
                _intersection = value;
                _street = null;
            }
        }

        public CardinalDirection cardinalDirection { get; private set; }
        public Quaternion worldToRelativeRotation { get; private set; }

        public Vector3 position => transform.position;
        public Vector3 direction => transform.forward;

        /// <summary>
        /// Returns the direction relative to the cardinal direction.
        /// </summary>
        public Vector3 relativeDirection
        {
            get
            {
                return worldToRelativeRotation * direction;
            }
        }

        // Methods

        private void Awake()
        {
            cardinalDirection = CardinalDirection.North;
            worldToRelativeRotation = Quaternion.identity;
        }

        private void Update()
        {
            UpdatePosition();
            UpdateTravelDirection();
        }

        private void UpdatePosition()
        {
            if (street == null && intersection == null)
            {
                // We need to set the position for the first time.
                UpdatePositionFromFullStreetLayout();
            }
            else if (street != null)
            {
                // See if we're still on the street.
                if (!IsInBoundsOf(street))
                {
                    UpdatePositionFromStreetIntersections();
                }
            }
            else
            {
                // See if we're still in the intersection.
                if (!IsInBoundsOf(intersection))
                {
                    UpdatePositionFromIntersectionStreets();
                }
            }
        }

        private void UpdatePositionFromFullStreetLayout()
        {
            // Check all streets.
            foreach (Street street in city.streetLayout.streets)
            {
                if (IsInBoundsOf(street))
                {
                    this.street = street;
                    return;
                }
            }

            // Check all intersections.
            foreach (KeyValuePair<Vector2Int, Intersection> intersectionEntry in city.streetLayout.intersections)
            {
                if (IsInBoundsOf(intersectionEntry.Value))
                {
                    intersection = intersectionEntry.Value;
                    return;
                }
            }

            Debug.LogError($"Unable to track car in the city network. #{transform.position}");
        }

        private void UpdatePositionFromStreetIntersections()
        {
            if (IsInBoundsOf(street.startIntersection))
            {
                intersection = street.startIntersection;
            }
            else if (IsInBoundsOf(street.endIntersection))
            {
                intersection = street.endIntersection;
            }
            else
            {
                UpdatePositionFromFullStreetLayout();
            }
        }

        private void UpdatePositionFromIntersectionStreets()
        {
            if (intersection.northStreet != null && IsInBoundsOf(intersection.northStreet))
            {
                street = intersection.northStreet;
            }
            else if (intersection.southStreet != null && IsInBoundsOf(intersection.southStreet))
            {
                street = intersection.southStreet;
            }
            else if (intersection.eastStreet != null && IsInBoundsOf(intersection.eastStreet))
            {
                street = intersection.eastStreet;
            }
            else if (intersection.westStreet != null && IsInBoundsOf(intersection.westStreet))
            {
                street = intersection.westStreet;
            }
            else
            {
                UpdatePositionFromFullStreetLayout();
            }
        }

        private bool IsInBoundsOf(Street street)
        {
            return IsInBoundsOf(street.bounds);
        }

        private bool IsInBoundsOf(Intersection intersection)
        {
            return intersection.bounds.Contains(transform.position);
        }

        private bool IsInBoundsOf(Bounds bounds)
        {
            return bounds.Contains(transform.position);
        }

        private void UpdateTravelDirection()
        {
            CardinalDirection previousCardinalDireciton = cardinalDirection;
            cardinalDirection = transform.forward.GetCardinalDirection();

            if (previousCardinalDireciton != cardinalDirection)
            {
                Vector3 streetDirection = DirectionHelpers.cardinalDirectionVectors[cardinalDirection];
                worldToRelativeRotation = Quaternion.FromToRotation(streetDirection, Vector3.forward);
            }
        }
    }
}
