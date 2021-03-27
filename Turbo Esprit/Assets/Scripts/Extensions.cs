using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public static class Extensions
    {
        public static CardinalDirection GetCardinalDirection(this Vector3 vector)
        {
            return DirectionHelpers.GetCardinalDirectionForVector(vector);
        }
    }
}
