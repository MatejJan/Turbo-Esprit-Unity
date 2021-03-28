using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TurboEsprit
{
    public class Gear : MonoBehaviour
    {
        [SerializeField] private Car car;

        private TextMeshProUGUI textMeshProUGui;

        private void Awake()
        {
            textMeshProUGui = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            string gearText;

            switch (car.gearshiftPosition)
            {
                case Car.GearshiftPosition.Reverse:
                    gearText = "R";
                    break;

                case Car.GearshiftPosition.Neutral:
                    gearText = "N";
                    break;

                default:
                    gearText = ((int)car.gearshiftPosition).ToString();
                    break;
            }

            textMeshProUGui.text = gearText;
        }
    }
}
