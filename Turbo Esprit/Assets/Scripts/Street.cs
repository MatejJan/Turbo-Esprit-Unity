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

        public void Generate(City city)
        {

        }
    }
}
