using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Intersection
    {
        public Vector2Int position;

        public Street northStreet;
        public Street southStreet;
        public Street eastStreet;
        public Street westStreet;

        public bool hasTrafficLights
        {
            get
            {
                // Traffic lights appear only in 4-way intersections where both streets have at least 4 lanes and at least one has 6 lanes.
                if (!isFourWayIntersection) return false;

                Street northSouthStreet = northStreet ?? southStreet;
                Street eastWestStreet = eastStreet ?? westStreet;

                return northSouthStreet.lanesCount >= 4 && eastWestStreet.lanesCount >= 4 && (northSouthStreet.lanesCount == 6 || eastWestStreet.lanesCount == 6);
            }
        }

        private bool isFourWayIntersection => streetsCount == 4;
        private bool isTIntersection => streetsCount == 3;
        private bool isCorner => streetsCount == 2;
        private bool isDeadEnd => streetsCount == 1;

        private int streetsCount
        {
            get
            {
                int count = 0;
                if (northStreet != null) count++;
                if (southStreet != null) count++;
                if (eastStreet != null) count++;
                if (westStreet != null) count++;
                return count;
            }
        }

        public void Generate(City city)
        {
            // Create the intersection game object as a child of Intersections.
            GameObject intersectionGameObject = new GameObject($"Intersection ({position.x}, {position.y})");
            intersectionGameObject.transform.parent = city.transform.Find("Intersections");

            // Position in the game world.
            Vector2 size = GetSize();

            intersectionGameObject.transform.localPosition = new Vector3
            {
                x = position.x - size.x / 2,
                z = position.y - size.y / 2
            };

            // Place prefabs.
            StreetPieces streetPieces = city.streetPieces;

            GameObject road = streetPieces.Instantiate(streetPieces.roadPrefab, intersectionGameObject);
            road.transform.localScale = new Vector3(size.x, 1, size.y);

            // Place sidewalk corners or sides.
            void CreateCorner(float rotation, float x, float z)
            {
                GameObject corner = streetPieces.Instantiate(streetPieces.sidewalkCornerPrefab, intersectionGameObject);
                corner.transform.localScale = new Vector3(City.sidewalkWidth, 1, City.sidewalkWidth);
                corner.transform.localRotation = Quaternion.Euler(0, rotation, 0);
                corner.transform.localPosition = new Vector3 { x = x, z = z };
            }

            if (southStreet != null && westStreet != null) CreateCorner(0, 0, 0);
            if (northStreet != null && westStreet != null) CreateCorner(90, 0, size.y);
            if (northStreet != null && eastStreet != null) CreateCorner(180, size.x, size.y);
            if (southStreet != null && eastStreet != null) CreateCorner(270, size.x, 0);

            if (northStreet == null)
            {
                GameObject sidewalk = streetPieces.Instantiate(streetPieces.sidewalkPrefab, intersectionGameObject);
                sidewalk.transform.localScale = new Vector3(size.x, 1, City.sidewalkWidth);
                sidewalk.transform.localPosition = new Vector3 { z = size.y - City.sidewalkWidth };
            }

            if (southStreet == null)
            {
                GameObject sidewalk = streetPieces.Instantiate(streetPieces.sidewalkPrefab, intersectionGameObject);
                sidewalk.transform.localScale = new Vector3(size.x, 1, City.sidewalkWidth);
            }

            if (eastStreet == null)
            {
                GameObject sidewalk = streetPieces.Instantiate(streetPieces.sidewalkPrefab, intersectionGameObject);
                sidewalk.transform.localScale = new Vector3(City.sidewalkWidth, 1, size.y);
                sidewalk.transform.localPosition = new Vector3 { x = size.x - City.sidewalkWidth };
            }

            if (westStreet == null)
            {
                GameObject sidewalk = streetPieces.Instantiate(streetPieces.sidewalkPrefab, intersectionGameObject);
                sidewalk.transform.localScale = new Vector3(City.sidewalkWidth, 1, size.y);
            }

            // Place lines if one street has priority.
            bool hasLinesNorthSouth = false;
            bool hasLinesEastWest = false;

            Street northSouthStreet = northStreet ?? southStreet;
            Street eastWestStreet = eastStreet ?? westStreet;

            if (isFourWayIntersection)
            {
                // 4-way intersections have lines where there are no stop signs on both ends.
                hasLinesEastWest = !HasStopLineForStreet(eastStreet) && !HasStopLineForStreet(westStreet);
                hasLinesNorthSouth = !HasStopLineForStreet(northStreet) && !HasStopLineForStreet(southStreet);
            }
            else if (isTIntersection)
            {
                // T intersections always have priority on the street that doesn't end.
                hasLinesEastWest = northStreet == null || southStreet == null;
                hasLinesNorthSouth = eastStreet == null || westStreet == null;
            }

            if (hasLinesNorthSouth)
            {
                var brokenLineXCoordinates = new List<float>();

                if (northSouthStreet.isOneWay)
                {
                    brokenLineXCoordinates.Add(size.x / 2);
                }
                else
                {
                    GameObject centerLine = streetPieces.Instantiate(streetPieces.solidLinePrefab, intersectionGameObject);
                    centerLine.transform.localScale = new Vector3(1, 1, size.y);
                    centerLine.transform.localPosition = new Vector3 { x = size.x / 2 };
                }

                for (int i = 2; i < northSouthStreet.lanesCount; i += 2)
                {
                    float sideOffset = City.sidewalkWidth + City.laneWidth * (i / 2);
                    brokenLineXCoordinates.Add(sideOffset);
                    brokenLineXCoordinates.Add(size.x - sideOffset);
                }

                foreach (float xCoordinate in brokenLineXCoordinates)
                {
                    GameObject laneLine = streetPieces.Instantiate(streetPieces.brokenLinePrefab, intersectionGameObject);
                    laneLine.transform.localScale = new Vector3(1, 1, size.y);
                    laneLine.transform.localPosition = new Vector3 { x = xCoordinate };
                    StreetPieces.ChangeBrokenLineTiling(laneLine);
                }
            }

            if (hasLinesEastWest)
            {
                var brokenLineZCoordinates = new List<float>();

                if (eastWestStreet.isOneWay)
                {
                    brokenLineZCoordinates.Add(size.y / 2);
                }
                else
                {
                    GameObject centerLine = streetPieces.Instantiate(streetPieces.solidLinePrefab, intersectionGameObject);
                    centerLine.transform.localScale = new Vector3(1, 1, size.x);
                    centerLine.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    centerLine.transform.localPosition = new Vector3 { z = size.y / 2 };
                }

                for (int i = 2; i < eastWestStreet.lanesCount; i += 2)
                {
                    float sideOffset = City.sidewalkWidth + City.laneWidth * (i / 2);
                    brokenLineZCoordinates.Add(sideOffset);
                    brokenLineZCoordinates.Add(size.y - sideOffset);
                }

                foreach (float zCoordinate in brokenLineZCoordinates)
                {
                    GameObject laneLine = streetPieces.Instantiate(streetPieces.brokenLinePrefab, intersectionGameObject);
                    laneLine.transform.localScale = new Vector3(1, 1, size.x);
                    laneLine.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    laneLine.transform.localPosition = new Vector3 { z = zCoordinate };
                    StreetPieces.ChangeBrokenLineTiling(laneLine);
                }
            }
        }

        public Vector2 GetSize()
        {
            int widthLanesCount = 2;
            int heightLanesCount = 2;

            Street northSouthStreet = northStreet ?? southStreet;
            Street eastWestStreet = eastStreet ?? westStreet;

            if (northSouthStreet != null)
            {
                widthLanesCount = northSouthStreet.lanesCount;
            }

            if (eastWestStreet != null)
            {
                heightLanesCount = eastWestStreet.lanesCount;
            }

            float width = 2 * City.sidewalkWidth + widthLanesCount * City.laneWidth;
            float height = 2 * City.sidewalkWidth + heightLanesCount * City.laneWidth;

            return new Vector2(width, height);
        }

        public bool HasStopLineForStreet(Street street)
        {
            if (isFourWayIntersection)
            {
                // Intersections with traffic lights (4-6 lanes) always have stop lines.
                if (hasTrafficLights) return true;

                // When two 4-lane streets cross, east-west has priority;
                Street northSouthStreet = northStreet ?? southStreet;
                Street eastWestStreet = eastStreet ?? westStreet;

                if (northSouthStreet.lanesCount == 4 && eastWestStreet.lanesCount == 4)
                {
                    return street == northStreet || street == southStreet;
                }

                // When two one-way streets cross, east-west has priority;
                if (northSouthStreet.isOneWay && eastWestStreet.isOneWay)
                {
                    return street == northStreet || street == southStreet;
                }

                // One-way streets crossing non-one way streets always have stop signs if entering the intersection.
                if (street.isOneWay)
                {
                    bool streetIsEnteringIfGointToStart = street == northStreet || street == eastStreet;
                    return streetIsEnteringIfGointToStart && street.oneWayDirectionGoesToStart;
                }

                // Street with 2 lanes always has a stop sign except if the other is a one-way street.
                if (street.lanesCount == 2)
                {
                    if (street == northStreet || street == southStreet)
                    {
                        return !eastWestStreet.isOneWay;
                    }
                    else
                    {
                        return !northSouthStreet.isOneWay;
                    }
                }

                // This must be a higher priority road so it has no stop sign.
                return false;
            }
            else if (isTIntersection)
            {
                bool hasStopSign = false;

                // Street that doesn't continue has the stop sign.
                if (street == northStreet && southStreet == null) hasStopSign = true;
                if (street == southStreet && northStreet == null) hasStopSign = true;
                if (street == eastStreet && westStreet == null) hasStopSign = true;
                if (street == westStreet && eastStreet == null) hasStopSign = true;

                if (hasStopSign)
                {
                    // Make sure this is not a one-way street that is exiting the intersection.
                    if (street.isOneWay)
                    {
                        bool streetIsEnteringIfGointToStart = street == northStreet || street == eastStreet;
                        return streetIsEnteringIfGointToStart && street.oneWayDirectionGoesToStart;
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }
    }
}
