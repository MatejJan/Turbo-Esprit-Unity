using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;

    public void Navigate(Vector3 destination)
    {
        agent.destination = destination;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
}
