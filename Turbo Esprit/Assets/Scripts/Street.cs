using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Street
    {
        public int lanesCount;
        public bool isOneWay;
        public bool oneWayDirectionGoesToStart;

        public Intersection startIntersection;
        public Intersection endIntersection;

        public float width { get; private set; }
        public Bounds bounds { get; private set; }

        private GameObject gameObject;

        public StreetOrientation orientation => startIntersection.position.y == endIntersection.position.y ? StreetOrientation.EastWest : StreetOrientation.NorthSouth;

        public void Generate(City city)
        {
            // Create the street game object as a child of Streets.
            gameObject = new GameObject();
            gameObject.transform.parent = city.transform.Find("Streets");

            // Calculate size and position in the game world.
            Vector2 startIntersectionHalfSize = startIntersection.GetSize() / 2;
            Vector2 endIntersectionHalfSize = endIntersection.GetSize() / 2;

            float roadWidth = lanesCount * City.laneWidth;
            width = roadWidth + 2 * City.sidewalkWidth;
            float length;

            Vector3 boundsSize;

            if (orientation == StreetOrientation.EastWest)
            {
                gameObject.name = $"Street ({startIntersection.position.x}-{startIntersection.position.x}, {endIntersection.position.y})";

                length = endIntersection.position.x - startIntersection.position.x - startIntersectionHalfSize.x - endIntersectionHalfSize.x;

                gameObject.transform.localPosition = new Vector3
                {
                    x = startIntersection.position.x + startIntersectionHalfSize.x,
                    z = startIntersection.position.y + width / 2
                };

                // Rotate the road so the local Z axis goes in the positive global X direction.
                gameObject.transform.localRotation = Quaternion.Euler(0, 90, 0);

                boundsSize = new Vector3(length, City.boundsHeight, width);
            }
            else
            {
                gameObject.name = $"Street ({startIntersection.position.x}, {startIntersection.position.y}-{endIntersection.position.y})";

                length = endIntersection.position.y - startIntersection.position.y - startIntersectionHalfSize.y - endIntersectionHalfSize.y;

                gameObject.transform.localPosition = new Vector3
                {
                    x = startIntersection.position.x - width / 2,
                    z = startIntersection.position.y + startIntersectionHalfSize.y
                };

                boundsSize = new Vector3(width, City.boundsHeight, length);
            }

            // Set bounds.
            bounds = new Bounds
            {
                min = gameObject.transform.localPosition + new Vector3(0, City.boundsBaseY, 0),
                max = gameObject.transform.localPosition + boundsSize
            };

            // Place prefabs.
            StreetPieces streetPieces = city.streetPieces;

            GameObject road = streetPieces.Instantiate(streetPieces.roadPrefab, gameObject);
            road.transform.localScale = new Vector3(roadWidth, 1, length);
            road.transform.localPosition = new Vector3 { x = City.sidewalkWidth };

            GameObject sidewalkLeft = streetPieces.Instantiate(streetPieces.sidewalkPrefab, gameObject);
            sidewalkLeft.transform.localScale = new Vector3(City.sidewalkWidth, 1, length);

            GameObject sidewalkRight = streetPieces.Instantiate(streetPieces.sidewalkPrefab, gameObject);
            sidewalkRight.transform.localScale = new Vector3(City.sidewalkWidth, 1, length);
            sidewalkRight.transform.localPosition = new Vector3 { x = width - City.sidewalkWidth };

            // Place lane division lines.
            var brokenLineXCoordinates = new List<float>();

            if (isOneWay)
            {
                brokenLineXCoordinates.Add(width / 2);
            }
            else
            {
                GameObject centerLine = streetPieces.Instantiate(streetPieces.solidLinePrefab, gameObject);
                centerLine.transform.localScale = new Vector3(1, 1, length);
                centerLine.transform.localPosition = new Vector3 { x = width / 2 };
            }

            for (int i = 2; i < lanesCount; i += 2)
            {
                float sideOffset = City.sidewalkWidth + City.laneWidth * (i / 2);
                brokenLineXCoordinates.Add(sideOffset);
                brokenLineXCoordinates.Add(width - sideOffset);
            }

            foreach (float xCoordinate in brokenLineXCoordinates)
            {
                GameObject laneLine = streetPieces.Instantiate(streetPieces.brokenLinePrefab, gameObject);
                laneLine.transform.localScale = new Vector3(1, 1, length);
                laneLine.transform.localPosition = new Vector3 { x = xCoordinate };
                StreetPieces.ChangeBrokenLineTiling(laneLine);
            }

            GameObject CreateStopLine()
            {
                GameObject stopLine = streetPieces.Instantiate(streetPieces.solidLinePrefab, gameObject);

                float lineLength = roadWidth;
                if (!isOneWay) lineLength /= 2;
                stopLine.transform.localScale = new Vector3(1, 1, lineLength);

                return stopLine;
            }

            // Place intersection stop lines.
            if (endIntersection.HasStopLineForStreet(this))
            {
                GameObject stopLine = CreateStopLine();
                stopLine.transform.localRotation = Quaternion.Euler(0, 90, 0);
                stopLine.transform.localPosition = new Vector3 { x = City.sidewalkWidth, z = length - City.lineWidth / 2 };
            }

            if (startIntersection.HasStopLineForStreet(this))
            {
                GameObject stopLine = CreateStopLine();
                stopLine.transform.localRotation = Quaternion.Euler(0, 270, 0);
                stopLine.transform.localPosition = new Vector3 { x = width - City.sidewalkWidth, z = City.lineWidth / 2 };
            }
        }
    }
}
