using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class StreetLayout
    {
        public List<Street> streets = new List<Street>();
        public Dictionary<Vector2Int, Intersection> intersections = new Dictionary<Vector2Int, Intersection>();

        protected Street AddStreet(Vector2Int fromBlock, Vector2Int toBlock, int lanesCount, bool isOneWay = false)
        {
            // Assert lane restrictions.
            if (lanesCount != 2 && lanesCount != 4 && lanesCount != 6)
            {
                Debug.LogError($"Street with invalid lanes count ({lanesCount}) added.");
                return null;
            }

            if (isOneWay && lanesCount != 2)
            {
                Debug.LogError($"One way streets can only have 2 lanes.");
                return null;
            }

            // Create the street.
            var street = new Street
            {
                lanesCount = lanesCount,
                isOneWay = isOneWay
            };

            streets.Add(street);

            // Determine one way direction and set start/end locations (converted from blocks to meters).
            Vector2Int start = fromBlock * 100;
            Vector2Int end = toBlock * 100;

            void FlipDirection()
            {
                (start, end) = (end, start);
                if (isOneWay) street.oneWayDirectionGoesToStart = true;
            }

            if (fromBlock.x == toBlock.x)
            {
                // This is a N-S road. Flip direction if necessary.
                if (fromBlock.y > toBlock.y)
                {
                    FlipDirection();
                }
            }
            else
            {
                // This is an E-W road. Flip direction if necessary.
                if (fromBlock.x > toBlock.x)
                {
                    FlipDirection();
                }
            }

            // Create intersections if necessary and assign them to the street.
            Intersection CreateIntersection(Vector2Int location)
            {
                var intersection = new Intersection { position = location };
                intersections[location] = intersection;
                return intersection;
            }

            street.startIntersection = intersections.ContainsKey(start) ? intersections[start] : CreateIntersection(start);
            street.endIntersection = intersections.ContainsKey(end) ? intersections[end] : CreateIntersection(end);

            // Assign the streets to the intersections.
            if (fromBlock.x == toBlock.x)
            {
                // This is a N-S road.
                street.startIntersection.northStreet = street;
                street.endIntersection.southStreet = street;
            }
            else
            {
                // This is an E-W road.
                street.startIntersection.eastStreet = street;
                street.endIntersection.westStreet = street;
            }

            return street;
        }
    }
}
