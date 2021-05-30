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

        private Street dummyStreet = new Street();

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

        /// <summary>
        /// Returns a street you are on, or the street towards which we're looking in (or coming from).
        /// </summary>
        public Street representativeStreet
        {
            get
            {
                if (street != null) return street;

                Street intersectionStreet = intersection.GetStreetInOrientation(streetOrientation);
                if (intersectionStreet != null) return intersectionStreet;

                // This intersection is a dead end, so use the dummy street.
                dummyStreet.startIntersection = intersection;
                dummyStreet.endIntersection = intersection;
                return dummyStreet;
            }
        }

        // Street space is rotated along the street we're driving down (or closest cardinal direction in intersections).
        public CardinalDirection streetCardinalDirection { get; private set; }
        public StreetOrientation streetOrientation => DirectionHelpers.cardinalDirectionStreetOrientations[streetCardinalDirection];
        public Vector3 streetDirection => DirectionHelpers.cardinalDirectionVectors[streetCardinalDirection];
        public Quaternion worldToStreetRotation { get; private set; }

        public Vector3 carWorldPosition => transform.position;
        public Vector3 carWorldDirection => transform.forward;

        /// <summary>
        /// Returns the car direction in street space.
        /// </summary>
        public Vector3 carStreetDirection
        {
            get
            {
                return worldToStreetRotation * carWorldDirection;
            }
        }

        /// <summary>
        /// Returns the position from the left edge of the street.
        /// </summary>
        public float sidewaysPosition
        {
            get
            {
                float streetHalfWidth = representativeStreet.width / 2;
                Vector2Int intersectionWorldPosition = representativeStreet.startIntersection.position;

                switch (streetCardinalDirection)
                {
                    case CardinalDirection.North:
                        return carWorldPosition.x - (intersectionWorldPosition.x - streetHalfWidth);

                    case CardinalDirection.South:
                        return (intersectionWorldPosition.x + streetHalfWidth) - carWorldPosition.x;

                    case CardinalDirection.West:
                        return carWorldPosition.z - (intersectionWorldPosition.y - streetHalfWidth);

                    case CardinalDirection.East:
                        return (intersectionWorldPosition.y + streetHalfWidth) - carWorldPosition.z;

                    default:
                        return 0;
                }
            }
        }

        public int currentLane
        {
            get
            {
                if (sidewaysPosition < City.sidewalkWidth)
                {
                    return 0;
                }
                else if (sidewaysPosition > representativeStreet.width - City.sidewalkWidth)
                {
                    return representativeStreet.lanesCount + 1;
                }
                else
                {
                    return Mathf.CeilToInt((sidewaysPosition - City.sidewalkWidth) / City.laneWidth);
                }
            }
        }

        // Methods

        public float GetCenterOfLaneSidewaysPosition(int lane)
        {
            float sidewalkHalfWidth = City.sidewalkWidth / 2;
            if (lane == 0)
            {
                return sidewalkHalfWidth;
            }
            else if (lane == representativeStreet.lanesCount + 1)
            {
                return representativeStreet.width - sidewalkHalfWidth;
            }
            else
            {
                return City.sidewalkWidth + (lane - 0.5f) * City.laneWidth;
            }
        }

        private void Awake()
        {
            streetCardinalDirection = CardinalDirection.North;
            worldToStreetRotation = Quaternion.identity;
            dummyStreet.lanesCount = 2;
        }

        private void Start()
        {
            UpdatePosition();
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
            CardinalDirection previousStreetCardinalDireciton = streetCardinalDirection;

            if (street != null)
            {
                // On streets, the only directions possible are along the street.
                if (street.orientation == StreetOrientation.EastWest)
                {
                    if (carWorldDirection.x > 0)
                    {
                        streetCardinalDirection = CardinalDirection.East;
                    }
                    else
                    {
                        streetCardinalDirection = CardinalDirection.West;
                    }
                }
                else
                {
                    if (carWorldDirection.z > 0)
                    {
                        streetCardinalDirection = CardinalDirection.North;
                    }
                    else
                    {
                        streetCardinalDirection = CardinalDirection.South;
                    }
                }
            }
            else
            {
                // In intersections, all 4 directions are possible, so choose the nearest one.
                streetCardinalDirection = transform.forward.GetCardinalDirection();
            }

            if (previousStreetCardinalDireciton != streetCardinalDirection)
            {
                Vector3 streetDirection = DirectionHelpers.cardinalDirectionVectors[streetCardinalDirection];
                worldToStreetRotation = Quaternion.FromToRotation(streetDirection, Vector3.forward);
            }
        }
    }
}
