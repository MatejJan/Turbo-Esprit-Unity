using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class StreetPieces : MonoBehaviour
    {
        public GameObject roadPrefab;
        public GameObject sidewalkPrefab;
        public GameObject sidewalkCornerPrefab;
        public GameObject solidLinePrefab;
        public GameObject brokenLinePrefab;

        public GameObject Instantiate(GameObject prefab, GameObject parent)
        {
            return Instantiate<GameObject>(prefab, parent.transform);
        }
    }
}
