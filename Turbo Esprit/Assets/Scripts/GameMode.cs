using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public abstract class GameMode : MonoBehaviour
    {
        public abstract GameCamera currentCamera { get; }
        public abstract GameObject currentCarGameObject { get; }
    }
}
