using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurboEsprit;

namespace TurboEsprit.Levels
{
    public class Wellington : StreetLayout
    {
        public Wellington()
        {
            // Add north-south roads.
            AddNorthSouthStreet(13, new[] { 3, 4, 5, 7, 9 }, 2);

            AddNorthSouthStreet(14, new[] { 1, 2, 3, 4, 5 }, 2);
            AddNorthSouthStreet(14, new[] { 8, 9, 10, 11 }, 2);

            AddNorthSouthStreet(16, new[] { 2, 4, 5, 8, 11 }, 6);

            AddNorthSouthStreet(17, new[] { 1, 2, 4, 5, 8, 11 }, 4);

            AddNorthSouthStreet(18, new[] { 0, 1, 4, 5, 8, 11, 22, 33, 44, 55, 66 }, 6);

            AddNorthSouthStreet(19, new[] { 1, 2 }, 2);
            AddNorthSouthStreet(19, new[] { 3, 4 }, 2);

            AddNorthSouthStreet(20, new[] { 4, 5, 8, 9, 11 }, 6);

            AddNorthSouthStreet(21, new[] { 4, 5, 8, 9 }, 4);

            AddNorthSouthStreet(22, new[] { 2, 3, 4, 5 }, 2);

            AddNorthSouthStreet(23, new[] { 3, 8, 11 }, 2, true);

            // Add east-west roads.
            AddEastWestStreet(1, new[] { 13, 14, 15 }, 2);
            AddEastWestStreet(1, new[] { 16, 17, 18, 19, 25 }, 2);

            AddEastWestStreet(2, new[] { 10, 14, 16, 17 }, 4);

            AddEastWestStreet(3, new[] { 12, 13, 14 }, 2);
            AddEastWestStreet(3, new[] { 19, 20 }, 2);
            AddEastWestStreet(3, new[] { 22, 23, 25 }, 2);

            AddEastWestStreet(4, new[] { 10, 13, 14, 16, 17, 18, 19, 20, 21, 22 }, 4);

            AddEastWestStreet(5, new[] { 13, 14, 16, 17, 18, 20, 21 }, 6);

            AddEastWestStreet(7, new[] { 11, 13 }, 4);

            AddEastWestStreet(8, new[] { 23, 21 }, 2, true);
            AddEastWestStreet(8, new[] { 20, 18 }, 2, true);
            AddEastWestStreet(8, new[] { 17, 16 }, 2, true);

            AddEastWestStreet(9, new[] { 12, 13, 14 }, 2);
            AddEastWestStreet(9, new[] { 20, 21, 22 }, 2);

            AddEastWestStreet(10, new[] { 11, 14 }, 4);
        }
    }
}
