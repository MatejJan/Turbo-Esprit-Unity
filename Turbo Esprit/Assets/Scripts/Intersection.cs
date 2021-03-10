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

            void CreateCorner(float rotation, float x, float z)
            {
                GameObject topLeftCorner = streetPieces.Instantiate(streetPieces.sidewalkCornerPrefab, intersectionGameObject);
                topLeftCorner.transform.localScale = new Vector3(City.sidewalkWidth, 1, City.sidewalkWidth);
                topLeftCorner.transform.localRotation = Quaternion.Euler(0, rotation, 0);
                topLeftCorner.transform.localPosition = new Vector3 { x = x, z = z };
            }

            CreateCorner(0, 0, 0);
            CreateCorner(90, 0, size.y);
            CreateCorner(180, size.x, size.y);
            CreateCorner(270, size.x, 0);
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
    }
}
