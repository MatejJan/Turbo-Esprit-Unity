using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Intersection
    {
        public Vector2Int location;

        public Street northStreet;
        public Street southStreet;
        public Street eastStreet;
        public Street westStreet;

        public void Generate(City city)
        {
            // Create the intersection game object as a child of Intersections.
            GameObject intersectionGameObject = new GameObject($"Intersection ({location.x}, {location.y})");
            intersectionGameObject.transform.parent = city.transform.Find("Intersections");

            // Position in the game world.
            intersectionGameObject.transform.localPosition = new Vector3 { x = location.x, z = location.y };
        }
    }
}
