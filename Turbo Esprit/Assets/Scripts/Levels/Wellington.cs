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
            AddNorthSouthStreet(1, new[] { 1, 2, 3, 4, 5 }, 2);
            AddNorthSouthStreet(1, new[] { 6, 7, 9, 10 }, 2);
            AddNorthSouthStreet(1, new[] { 18, 19, 20 }, 2);
            AddNorthSouthStreet(1, new[] { 29, 30 }, 2);

            AddNorthSouthStreet(2, new[] { 2, 3, 4, 6, 7 }, 4);
            AddNorthSouthStreet(2, new[] { 9, 14, 16, 17, 19 }, 4);

            AddNorthSouthStreet(3, new[] { 2, 3, 4, 6, 7 }, 4);
            AddNorthSouthStreet(3, new[] { 16, 17, 19, 20, 21, 22, 24, 26, 28, 30 }, 4);

            AddNorthSouthStreet(4, new[] { 2, 3, 4 }, 2);
            AddNorthSouthStreet(4, new[] { 9, 10, 11 }, 2);
            AddNorthSouthStreet(4, new[] { 14, 15 }, 2);
            AddNorthSouthStreet(4, new[] { 17, 20, 21 }, 2);
            AddNorthSouthStreet(4, new[] { 23, 24, 25, 26, 27 }, 2);
            AddNorthSouthStreet(4, new[] { 29, 30 }, 2);

            AddNorthSouthStreet(5, new[] { 2, 3, 4, 6, 7, 10 }, 4);
            AddNorthSouthStreet(5, new[] { 26, 28 }, 4);

            AddNorthSouthStreet(6, new[] { 2, 3, 4, 6 }, 4);
            AddNorthSouthStreet(6, new[] { 24, 25, 26 }, 4);

            AddNorthSouthStreet(7, new[] { 10, 12, 14 }, 6);
            AddNorthSouthStreet(7, new[] { 17, 20, 22, 24 }, 6);

            AddNorthSouthStreet(8, new[] { 28, 26 }, 2, true);
            AddNorthSouthStreet(8, new[] { 20, 19, 17 }, 2, true);
            AddNorthSouthStreet(8, new[] { 16, 15 }, 2, true);
            AddNorthSouthStreet(8, new[] { 12, 10 }, 2, true);
            AddNorthSouthStreet(8, new[] { 8, 7, 6 }, 2, true);
            AddNorthSouthStreet(8, new[] { 4, 3 }, 2, true);

            AddNorthSouthStreet(9, new[] { 2, 3, 4, 6, 7 }, 4);
            AddNorthSouthStreet(9, new[] { 24, 26, 28 }, 4);

            AddNorthSouthStreet(10, new[] { 2, 3, 4, 6, 7 }, 4);
            AddNorthSouthStreet(10, new[] { 21, 24, 26, 28, 30 }, 4);

            AddNorthSouthStreet(11, new[] { 7, 8 }, 2, true);
            AddNorthSouthStreet(11, new[] { 10, 14 }, 2, true);
            AddNorthSouthStreet(11, new[] { 15, 16 }, 2, true);
            AddNorthSouthStreet(11, new[] { 17, 19, 21 }, 2, true);

            AddNorthSouthStreet(13, new[] { 3, 4, 5, 7, 9 }, 4);

            AddNorthSouthStreet(14, new[] { 1, 2, 3, 4, 5 }, 2);
            AddNorthSouthStreet(14, new[] { 8, 9, 10, 11 }, 2);
            AddNorthSouthStreet(14, new[] { 15, 16, 17 }, 2);
            AddNorthSouthStreet(14, new[] { 18, 19, 20 }, 2);
            AddNorthSouthStreet(14, new[] { 21, 23 }, 2);
            AddNorthSouthStreet(14, new[] { 28, 29, 30 }, 2);

            AddNorthSouthStreet(15, new[] { 21, 24, 25, 29 }, 4);

            AddNorthSouthStreet(16, new[] { 2, 4, 5, 8, 13, 14, 15, 16, 17, 19, 21, 22, 24, 25, 29 }, 6);

            AddNorthSouthStreet(17, new[] { 1, 2, 4, 5, 8, 12, 13, 14, 15, 16, 17, 18, 20, 22, 24, 25 }, 4);

            AddNorthSouthStreet(18, new[] { -10, 1, 4, 5, 8, 12, 13, 14, 15, 16, 17, 18, 20, 22, 24, 25, 29, 30, 40 }, 6);

            AddNorthSouthStreet(19, new[] { 1, 2 }, 2);
            AddNorthSouthStreet(19, new[] { 3, 4 }, 2);
            AddNorthSouthStreet(19, new[] { 19, 21, 22 }, 2);
            AddNorthSouthStreet(19, new[] { 24, 25 }, 2);
            AddNorthSouthStreet(19, new[] { 28, 30 }, 2);

            AddNorthSouthStreet(20, new[] { 4, 5, 8, 9, 13, 14, 15, 16, 17, 18 }, 6);

            AddNorthSouthStreet(21, new[] { 4, 5, 8, 9 }, 4);

            AddNorthSouthStreet(22, new[] { 2, 3, 4, 5 }, 2);
            AddNorthSouthStreet(22, new[] { 14, 15, 16 }, 2);
            AddNorthSouthStreet(22, new[] { 17, 18, 19, 20, 21, 22 }, 2);
            AddNorthSouthStreet(22, new[] { 24, 27, 28, 29 }, 2);

            AddNorthSouthStreet(23, new[] { 3, 8, 12 }, 2, true);
            AddNorthSouthStreet(23, new[] { 19, 20, 21, 22 }, 2, true);
            AddNorthSouthStreet(23, new[] { 27, 28 }, 2, true);

            AddNorthSouthStreet(24, new[] { 21, 22, 24, 25, 26, 27, 28, 30 }, 4);

            AddNorthSouthStreet(25, new[] { 1, 2 }, 2);
            AddNorthSouthStreet(25, new[] { 3, 6, 8, 9 }, 2);
            AddNorthSouthStreet(25, new[] { 14, 15 }, 2);
            AddNorthSouthStreet(25, new[] { 19, 20, 21, 22, 24 }, 2);
            AddNorthSouthStreet(25, new[] { 29, 30 }, 2);

            AddNorthSouthStreet(26, new[] { 16, 17, 19 }, 4);
            AddNorthSouthStreet(26, new[] { 25, 26, 27, 28 }, 4);

            AddNorthSouthStreet(27, new[] { 26, 25 }, 2, true);
            AddNorthSouthStreet(27, new[] { 22, 21 }, 2, true);
            AddNorthSouthStreet(27, new[] { 20, 19 }, 2, true);
            AddNorthSouthStreet(27, new[] { 15, 14 }, 2, true);
            AddNorthSouthStreet(27, new[] { 12, 9, 8 }, 2, true);

            AddNorthSouthStreet(28, new[] { 1, 3 }, 2);
            AddNorthSouthStreet(28, new[] { 9, 12, 13 }, 2);
            AddNorthSouthStreet(28, new[] { 14, 15, 16 }, 2);
            AddNorthSouthStreet(28, new[] { 17, 19, 20, 21, 22, 24 }, 2);

            AddNorthSouthStreet(29, new[] { 25, 26, 27, 28, 30 }, 4);

            AddNorthSouthStreet(30, new[] { 1, 3, 4 }, 2);
            AddNorthSouthStreet(30, new[] { 5, 6, 7 }, 2);
            AddNorthSouthStreet(30, new[] { 9, 12, 14 }, 2);
            AddNorthSouthStreet(30, new[] { 16, 19, 20, 21, 22, 23 }, 2);
            AddNorthSouthStreet(30, new[] { 24, 25, 26 }, 2);

            // Add east-west roads.
            AddEastWestStreet(1, new[] { 1, 2 }, 2);
            AddEastWestStreet(1, new[] { 13, 14, 15 }, 2);
            AddEastWestStreet(1, new[] { 16, 17, 18, 19, 25, 28, 30 }, 2);

            AddEastWestStreet(2, new[] { 1, 2, 3, 4, 5, 6, 9, 10, 14, 16, 17 }, 4);

            AddEastWestStreet(3, new[] { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11 }, 2);
            AddEastWestStreet(3, new[] { 12, 13, 14 }, 2);
            AddEastWestStreet(3, new[] { 19, 20 }, 2);
            AddEastWestStreet(3, new[] { 22, 23, 25 }, 2);
            AddEastWestStreet(3, new[] { 27, 28, 30 }, 2);

            AddEastWestStreet(4, new[] { 1, 2, 3, 4, 5, 6, 8, 9, 10, 13, 14, 16, 17, 18, 19, 20, 21, 22 }, 4);

            AddEastWestStreet(5, new[] { 13, 14, 16, 17, 18, 20, 21 }, 6);

            AddEastWestStreet(6, new[] { 2, 3, 5, 6, 8, 9, 10 }, 4);
            AddEastWestStreet(6, new[] { 25, 30 }, 4);

            AddEastWestStreet(7, new[] { 1, 2, 3, 5, 8, 9, 10, 11, 13 }, 4);

            AddEastWestStreet(8, new[] { 27, 25 }, 2, true);
            AddEastWestStreet(8, new[] { 23, 21 }, 2, true);
            AddEastWestStreet(8, new[] { 20, 18 }, 2, true);
            AddEastWestStreet(8, new[] { 17, 16 }, 2, true);
            AddEastWestStreet(8, new[] { 11, 8 }, 2, true);

            AddEastWestStreet(9, new[] { 1, 2, 3 }, 2);
            AddEastWestStreet(9, new[] { 12, 13, 14 }, 2);
            AddEastWestStreet(9, new[] { 20, 21, 22 }, 2);
            AddEastWestStreet(9, new[] { 25, 27, 28, 30 }, 2);

            AddEastWestStreet(10, new[] { 4, 5, 7, 8, 11, 14 }, 4);

            AddEastWestStreet(12, new[] { 7, 8 }, 2, true);
            AddEastWestStreet(12, new[] { 17, 18 }, 2, true);
            AddEastWestStreet(12, new[] { 23, 27 }, 2, true);
            AddEastWestStreet(12, new[] { 28, 30 }, 2, true);

            AddEastWestStreet(13, new[] { 16, 17, 18, 20 }, 4);

            AddEastWestStreet(14, new[] { -10, 2, 4, 7, 11, 16, 17, 18, 20, 22, 25, 27, 28, 40 }, 6);

            AddEastWestStreet(15, new[] { 4, 8, 11, 14 }, 2);
            AddEastWestStreet(15, new[] { 15, 16, 17, 18, 19 }, 2);
            AddEastWestStreet(15, new[] { 20, 22, 23 }, 2);
            AddEastWestStreet(15, new[] { 25, 27, 28 }, 2);

            AddEastWestStreet(16, new[] { 1, 2, 3, 4 }, 2);
            AddEastWestStreet(16, new[] { 6, 8, 11, 14 }, 2);
            AddEastWestStreet(16, new[] { 15, 16, 17, 18, 19 }, 2);
            AddEastWestStreet(16, new[] { 20, 22, 23 }, 2);
            AddEastWestStreet(16, new[] { 25, 26, 27 }, 2);
            AddEastWestStreet(16, new[] { 28, 30 }, 2);

            AddEastWestStreet(17, new[] { 2, 3, 4, 7, 8, 11, 14, 16, 17, 18, 20, 22, 26 }, 6);

            AddEastWestStreet(18, new[] { 17, 18, 20, 22 }, 4);

            AddEastWestStreet(19, new[] { 1, 2, 3 }, 2);
            AddEastWestStreet(19, new[] { 8, 9 }, 2);
            AddEastWestStreet(19, new[] { 10, 11, 12 }, 2);
            AddEastWestStreet(19, new[] { 14, 16 }, 2);
            AddEastWestStreet(19, new[] { 19, 20 }, 2);
            AddEastWestStreet(19, new[] { 22, 23, 25, 26, 27, 28, 30 }, 2);

            AddEastWestStreet(20, new[] { 3, 4 }, 2, true);
            AddEastWestStreet(20, new[] { 7, 8 }, 2, true);
            AddEastWestStreet(20, new[] { 17, 18 }, 2, true);
            AddEastWestStreet(20, new[] { 22, 23, 25, 27, 28, 30 }, 2, true);

            AddEastWestStreet(21, new[] { 2, 3, 4 }, 2);
            AddEastWestStreet(21, new[] { 9, 10, 11, 14, 15, 16 }, 2);
            AddEastWestStreet(21, new[] { 19, 20 }, 2);
            AddEastWestStreet(21, new[] { 22, 23, 24, 25, 27, 28, 30 }, 2);

            AddEastWestStreet(22, new[] { 30, 28, 27, 25, 24, 23, 22, 19, 18 }, 2, true);
            AddEastWestStreet(22, new[] { 17, 16 }, 2, true);
            AddEastWestStreet(22, new[] { 7, 3 }, 2, true);

            AddEastWestStreet(24, new[] { 3, 4, 6, 7, 9, 10 }, 6);
            AddEastWestStreet(24, new[] { 15, 16, 17, 18, 19, 22, 24 }, 6);

            AddEastWestStreet(25, new[] { 4, 6 }, 4);
            AddEastWestStreet(25, new[] { 15, 16, 17, 18 }, 4);
            AddEastWestStreet(25, new[] { 24, 26, 27, 29, 30 }, 4);

            AddEastWestStreet(26, new[] { 3, 4, 5, 6, 8, 9, 10 }, 4);
            AddEastWestStreet(26, new[] { 24, 26, 27, 29 }, 4);

            AddEastWestStreet(27, new[] { 22, 23, 24, 26, 29 }, 4);

            AddEastWestStreet(28, new[] { 3, 5, 8, 9, 10 }, 4);
            AddEastWestStreet(28, new[] { 22, 24, 26, 29 }, 4);

            AddEastWestStreet(29, new[] { 14, 15, 16, 18 }, 4);

            AddEastWestStreet(30, new[] { 1, 3, 4 }, 2);
            AddEastWestStreet(30, new[] { 9, 10, 11 }, 2);
            AddEastWestStreet(30, new[] { 13, 14, 15 }, 2);
            AddEastWestStreet(30, new[] { 18, 19 }, 2);
            AddEastWestStreet(30, new[] { 22, 24, 25 }, 2);
            AddEastWestStreet(30, new[] { 28, 29, 30 }, 2);
        }
    }
}
