using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class City : MonoBehaviour
    {
        public const float speedLimit = 40;
        public const float laneWidth = 4;
        public const float sidewalkWidth = 3;
        public const float lineWidth = 0.2f;
        public const float brokenLineSpacing = 5;

        public StreetLayout streetLayout;
        public StreetPieces streetPieces;

        private void Awake()
        {
            streetPieces = GetComponent<StreetPieces>();
        }

        private void Start()
        {
            // Generate the Wellington layout.
            streetLayout = new Levels.Wellington();
            Generate();
        }

        private void Generate()
        {
            // Create streets and intersection empty objects.
            var streets = new GameObject("Streets");
            streets.transform.parent = transform;

            var intersections = new GameObject("Intersections");
            intersections.transform.parent = transform;

            // Generate all streets.
            foreach (Street street in streetLayout.streets)
            {
                street.Generate(this);
            }

            // Generate all intersections.
            foreach (Intersection intersection in streetLayout.intersections.Values)
            {
                intersection.Generate(this);
            }
        }
    }
}
