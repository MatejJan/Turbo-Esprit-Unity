using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public static class DirectionHelpers
    {
        public static readonly CardinalDirection[] cardinalDirections = { CardinalDirection.East, CardinalDirection.West, CardinalDirection.North, CardinalDirection.South }; 

        public static readonly Dictionary<CardinalDirection, Vector3> cardinalDirectionVectors = new Dictionary<CardinalDirection, Vector3>
        {
            { CardinalDirection.East, Vector3.right },
            { CardinalDirection.West, Vector3.left },
            { CardinalDirection.North, Vector3.forward },
            { CardinalDirection.South, Vector3.back }
        };

        public static readonly Dictionary<CardinalDirection, StreetOrientation> cardinalDirectionStreetOrientations = new Dictionary<CardinalDirection, StreetOrientation>
        {
            { CardinalDirection.East, StreetOrientation.EastWest },
            { CardinalDirection.West, StreetOrientation.EastWest },
            { CardinalDirection.North, StreetOrientation.NorthSouth },
            { CardinalDirection.South, StreetOrientation.NorthSouth }
        };

        public static CardinalDirection GetCardinalDirectionForVector(Vector3 vector)
        {
            float angleDegrees = Mathf.Atan2(vector.z, vector.x) * Mathf.Rad2Deg;

            if (angleDegrees > 135 || angleDegrees < -135) return CardinalDirection.West;
            else if (angleDegrees > 45) return CardinalDirection.North;
            else if (angleDegrees < -45) return CardinalDirection.South;
            else return CardinalDirection.East;
        }

        public static CardinalDirection GetOppositeDirection(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.East: return CardinalDirection.West;
                case CardinalDirection.West: return CardinalDirection.East;
                case CardinalDirection.North: return CardinalDirection.South;
                default: return CardinalDirection.North;
            }
        }
    }
}
