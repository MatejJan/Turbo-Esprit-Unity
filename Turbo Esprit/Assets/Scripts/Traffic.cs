using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurboEsprit
{
    public class Traffic : MonoBehaviour
    {
        public const float density = 0.003f; // cars per meter
        public const float safetyDistanceTime = 2;
        public static readonly float[] speedPerLaneMph = new float[] { 5, 20, 25, 30 };

        [SerializeField] private GameMode gameMode;
        [SerializeField] private GameObject trafficCarPrefab;

        private City city;
        private Transform carTrafficRoot;

        private Street focusedStreet;
        private Intersection focusedIntersection;

        private HashSet<Street> simulatedStreets = new HashSet<Street>();
        private HashSet<Intersection> simulatedIntersections = new HashSet<Intersection>();

        private HashSet<Street> newSimulatedStreets = new HashSet<Street>();
        private HashSet<Intersection> newSimulatedIntersections = new HashSet<Intersection>();

        private List<Street> streetsToBeStopped = new List<Street>();
        private List<Intersection> intersectionsToBeStopped = new List<Intersection>();

        private List<CarTracker> trafficCarTrackers = new List<CarTracker>();
        private List<CarTracker> carTrackersToBeRemoved = new List<CarTracker>();

        private void Awake()
        {
            city = GetComponent<City>();

            // Create car traffic empty object.
            var carTrafficGameObject = new GameObject("Car Traffic");
            carTrafficRoot = carTrafficGameObject.transform;
            carTrafficRoot.parent = transform;
        }

        private void Update()
        {
            // See if game camera is in the same location as before.
            CarTracker focusedCarTracker = gameMode.currentCamera.trackedCar;
            if (focusedCarTracker.street != null && focusedCarTracker.street != focusedStreet || focusedCarTracker.intersection != null && focusedCarTracker.intersection != focusedIntersection)
            {
                // We have moved to a different street or intersection so we need to recalculate the simulated streets and intersections.
                focusedIntersection = focusedCarTracker.intersection;
                focusedStreet = focusedCarTracker.street;

                void AddIntersectionVicinity(Intersection intersection, int distance)
                {
                    if (distance == 0) return;

                    newSimulatedIntersections.Add(intersection);
                    if (intersection.northStreet != null) AddStreetVicinity(intersection.northStreet, distance);
                    if (intersection.southStreet != null) AddStreetVicinity(intersection.southStreet, distance);
                    if (intersection.eastStreet != null) AddStreetVicinity(intersection.eastStreet, distance);
                    if (intersection.westStreet != null) AddStreetVicinity(intersection.westStreet, distance);
                }

                void AddStreetVicinity(Street street, int distance)
                {
                    if (distance == 0) return;

                    newSimulatedStreets.Add(street);
                    AddIntersectionVicinity(street.startIntersection, distance - 1);
                    AddIntersectionVicinity(street.endIntersection, distance - 1);
                }

                if (focusedStreet != null)
                {
                    AddStreetVicinity(focusedStreet, 3);
                }
                else
                {
                    AddIntersectionVicinity(focusedIntersection, 2);
                }

                // Update simulated streets.
                foreach (Street street in simulatedStreets)
                {
                    if (!newSimulatedStreets.Contains(street))
                    {
                        // This street is no longer being simulated, so we can remove it, including all the cars left in it.
                        streetsToBeStopped.Add(street);
                    }
                }

                foreach (Street street in streetsToBeStopped)
                {
                    StopSimulatingStreet(street);
                }

                streetsToBeStopped.Clear();

                foreach (Street street in newSimulatedStreets)
                {
                    if (!simulatedStreets.Contains(street))
                    {
                        // This street isn't being simulated, so start doing it.
                        StartSimulatingStreet(street);
                    }
                }

                newSimulatedStreets.Clear();

                // Update simulated intersections.
                foreach (Intersection intersection in simulatedIntersections)
                {
                    if (!newSimulatedIntersections.Contains(intersection))
                    {
                        // This street is no longer being simulated, so we can remove it, including all the cars left in it.
                        intersectionsToBeStopped.Add(intersection);
                    }
                }

                foreach (Intersection intersection in intersectionsToBeStopped)
                {
                    StopSimulatingIntersection(intersection);
                }

                intersectionsToBeStopped.Clear();

                foreach (Intersection intersection in newSimulatedIntersections)
                {
                    if (!simulatedIntersections.Contains(intersection))
                    {
                        // This street isn't being simulated, so start doing it.
                        StartSimulatingIntersection(intersection);
                    }
                }

                newSimulatedIntersections.Clear();
            }

            // Remove cars that left the simulation area.
            foreach (CarTracker carTracker in trafficCarTrackers)
            {
                if (carTracker.street != null && !simulatedStreets.Contains(carTracker.street) ||
                    carTracker.intersection != null && !simulatedIntersections.Contains(carTracker.intersection))
                {
                    Destroy(carTracker.gameObject);
                    carTrackersToBeRemoved.Add(carTracker);
                }
            }

            foreach (CarTracker carTracker in carTrackersToBeRemoved)
            {
                trafficCarTrackers.Remove(carTracker);
            }

            carTrackersToBeRemoved.Clear();

            // Add cars at the edges of the simulation areas.
            foreach (Street street in simulatedStreets)
            {
                if (street.isOneWay)
                {
                    if (street.oneWayDirectionGoesToStart)
                    {
                        if (!simulatedIntersections.Contains(street.endIntersection))
                        {
                            SimulateStreetEntrance(street, false);
                        }
                    }
                    else
                    {
                        if (!simulatedIntersections.Contains(street.startIntersection))
                        {
                            SimulateStreetEntrance(street, true);
                        }
                    }
                }
                else
                {
                    if (!simulatedIntersections.Contains(street.endIntersection))
                    {
                        SimulateStreetEntrance(street, false);
                    }

                    if (!simulatedIntersections.Contains(street.startIntersection))
                    {
                        SimulateStreetEntrance(street, true);
                    }
                }
            }
        }

        private void StartSimulatingStreet(Street street)
        {
            simulatedStreets.Add(street);

            // Initialize traffic on this street.
            if (street.isOneWay)
            {
                for (int lane = 1; lane <= street.lanesCount; lane++)
                {
                    InitializeLane(street, lane, !street.oneWayDirectionGoesToStart);
                }
            }
            else
            {
                for (int lane = 1; lane <= street.lanesCount / 2; lane++)
                {
                    InitializeLane(street, lane, true);
                    InitializeLane(street, lane, false);
                }
            }
        }

        private void StopSimulatingStreet(Street street)
        {
            simulatedStreets.Remove(street);

            foreach (CarTracker carTracker in trafficCarTrackers)
            {
                if (carTracker.street == street)
                {
                    Destroy(carTracker.gameObject);
                    carTrackersToBeRemoved.Add(carTracker);
                }
            }

            foreach (CarTracker carTracker in carTrackersToBeRemoved)
            {
                trafficCarTrackers.Remove(carTracker);
            }

            carTrackersToBeRemoved.Clear();
        }

        private void StartSimulatingIntersection(Intersection intersection)
        {
            simulatedIntersections.Add(intersection);
        }

        private void StopSimulatingIntersection(Intersection intersection)
        {
            simulatedIntersections.Remove(intersection);

            foreach (CarTracker carTracker in trafficCarTrackers)
            {
                if (carTracker.intersection == intersection)
                {
                    Destroy(carTracker.gameObject);
                    carTrackersToBeRemoved.Add(carTracker);
                }
            }

            foreach (CarTracker carTracker in carTrackersToBeRemoved)
            {
                trafficCarTrackers.Remove(carTracker);
            }

            carTrackersToBeRemoved.Clear();
        }

        private void InitializeLane(Street street, int lane, bool directionTowardsEnd)
        {
            float speedInLane = speedPerLaneMph[lane] * PhysicsHelper.milesPerHourToMetersPerSecond;
            float safetyDistance = safetyDistanceTime * speedInLane;

            var emptySections = new List<(float, float)>();
            emptySections.Add((safetyDistance, street.length - safetyDistance));

            int carsCount = Mathf.RoundToInt(density * street.length + Random.value);

            for (int i = 0; i < carsCount; i++)
            {
                // Stop if there are no more empty sections.
                if (emptySections.Count == 0) break;

                // Place a car at a random position in a random empty section.
                int emptySectionIndex = Random.Range(0, emptySections.Count);
                (float start, float end) = emptySections[emptySectionIndex];
                emptySections.RemoveAt(emptySectionIndex);

                float position = Mathf.Lerp(start, end, Random.value);
                AddCar(street, lane, position, directionTowardsEnd);

                // Create two new empty sections, if they're long enough.
                if (position - safetyDistance > start) emptySections.Add((start, position - safetyDistance));
                if (position + safetyDistance < end) emptySections.Add((position + safetyDistance, end));
            }
        }

        private void AddCar(Street street, int lane, float positionAlongStreet, bool directionTowardsEnd)
        {
            Vector3 positionStreet;
            Vector3 directionStreet;

            float sidePosition = City.sidewalkWidth + (lane - 0.5f) * City.laneWidth;

            if (directionTowardsEnd)
            {
                positionStreet = new Vector3(sidePosition, 0, positionAlongStreet);
                directionStreet = Vector3.forward;
            }
            else
            {
                positionStreet = new Vector3(street.width - sidePosition, 0, street.length - positionAlongStreet);
                directionStreet = Vector3.back;
            }

            Vector3 position = street.transform.TransformPoint(positionStreet);
            Vector3 direction = street.transform.TransformDirection(directionStreet);
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject car = Instantiate(trafficCarPrefab, position, rotation, carTrafficRoot);

            CarTracker carTracker = car.GetComponent<CarTracker>();
            carTracker.city = city;
            trafficCarTrackers.Add(carTracker);

            CarAudio carAudio = car.GetComponent<CarAudio>();
            carAudio.gameMode = gameMode;

            float initialSpeed = speedPerLaneMph[lane] * PhysicsHelper.milesPerHourToMetersPerSecond;
            car.GetComponent<Car>().InitializeSpeed(initialSpeed);
        }

        private void SimulateStreetEntrance(Street street, bool directionTowardsEnd)
        {
            int simulateLanesCount = street.lanesCount;
            if (!street.isOneWay) simulateLanesCount /= 2;

            for (int lane = 1; lane <= simulateLanesCount; lane++)
            {
                SimulateLaneEntrance(street, lane, directionTowardsEnd);
            }
        }

        private void SimulateLaneEntrance(Street street, int lane, bool directionTowardsEnd)
        {
            float entranceRate = density * speedPerLaneMph[lane] * PhysicsHelper.milesPerHourToMetersPerSecond;

            // Use a Poisson process to determine if a car should enter in this time step.
            float probabilityOfNoEntrance = Mathf.Exp(-entranceRate * Time.deltaTime);
            if (Random.value < probabilityOfNoEntrance) return;

            // Car should be added at the start of the street in the given direction. Push it a bit inwards so it gets tracked to correct street.
            AddCar(street, lane, 0.01f, directionTowardsEnd);
        }
    }
}
