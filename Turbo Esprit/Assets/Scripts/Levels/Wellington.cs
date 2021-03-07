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
            AddStreet(new Vector2Int(18, 0), new Vector2Int(18, 1), 6);
            AddStreet(new Vector2Int(18, 1), new Vector2Int(18, 4), 6);

            AddStreet(new Vector2Int(18, 1), new Vector2Int(17, 1), 2);
            AddStreet(new Vector2Int(17, 1), new Vector2Int(16, 1), 2);

            AddStreet(new Vector2Int(16, 1), new Vector2Int(16, 2), 4);

            AddStreet(new Vector2Int(18, 1), new Vector2Int(19, 1), 2);
            AddStreet(new Vector2Int(19, 1), new Vector2Int(25, 1), 2);

            AddStreet(new Vector2Int(19, 1), new Vector2Int(19, 2), 2);
        }
    }
}
