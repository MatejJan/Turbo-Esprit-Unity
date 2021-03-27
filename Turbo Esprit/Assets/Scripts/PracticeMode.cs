using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class PracticeMode : MonoBehaviour
    {
        [SerializeField] private Vector2Int startPosition;

        [SerializeField] private City city;
        [SerializeField] private GameObject playerCar;

        private void Start()
        {
            playerCar.transform.position = new Vector3 { x = startPosition.x, z = startPosition.y };
        }
    }
}
