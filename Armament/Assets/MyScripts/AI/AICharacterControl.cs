using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    [RequireComponent(typeof (NavMeshAgent))]
    [RequireComponent(typeof(ThirdPersonCharacter))]
    [RequireComponent(typeof(PlayerManager))]
    [RequireComponent(typeof(Camera))]
    public class AICharacterControl : MonoBehaviourPunCallbacks
    {
        #region Public Fields

        public NavMeshAgent Agent { get; private set; }             // the navmesh agent required for the path finding
        public ThirdPersonCharacter Character { get; private set; } // the character we are controlling
        public Transform target;                                    // target to navigate to

        #endregion Public Fields

        #region Private Fields

        private const bool DEBUG = true;
        private const bool DEBUG_EnemyIsInCrosshairs = false;
        private const bool DEBUG_OnStage1TimerIsExpired = true;
        private const bool DEBUG_OnStage2TimerIsExpired = true;
        private const bool DEBUG_OnTriggerEnter = true; 

         PlayerManager pm; // keeps a reference of the player manager attached to the player GM
        Camera fpsCam;

        private bool isStage1 = true;

        private int[,] graph; // the graph of distances used in dijkstras algorithm
        private int V; // keeps track of the number of vertices in dijkstras algorithm
        private int[] dist; // keeps track of the distances of the root to all the vertices in the graph
        Transform[] dijkstraTransforms; 

        #endregion Private Fields

        void Start()
        {
            // Get the components on the object we need ( should not be null due to require component so no need to check )
            Agent = GetComponentInChildren<NavMeshAgent>();
            Character = GetComponent<ThirdPersonCharacter>();
            pm = GetComponent<PlayerManager>();
            fpsCam = GetComponentInChildren<Camera>();

            Agent.updateRotation = true; // Alex's note to self: look into why this is set to false
	        Agent.updatePosition = true;


            // Create distance graph of AI player and weapon spawn points to use in Dijkstra's algorithm
            CreateDistanceGraph();

            // Find the distances to all the guns 
            PlaceHolderDistanceFormula(graph, 0, out dist);
        }

        void CreateDistanceGraph()
        {
            Transform[] allWeaponSpawnPoints = GameManager.Instance.WeaponSpawnPoints;

            // SLOPPY SLOPPY SLOPPY
            // Get the position of the weapon that was spawned instead of the actual spawn point (because they're a little different)
            ArrayList weaponsList = GameManager.Instance.SpawnedWeaponsList;
            for (int i = 0; i < allWeaponSpawnPoints.Length; i++)
            {
                allWeaponSpawnPoints[i] = ((GameObject)weaponsList[i]).transform;
            }

            int numberOfTeamSpawnPoints = allWeaponSpawnPoints.Length / 2;

            // Create a new array half the size of the old array
            dijkstraTransforms = new Transform[numberOfTeamSpawnPoints + 1];

            // Set the number of vertices for dijkstras algorithm
            V = dijkstraTransforms.Length;
            Debug.LogFormat("AICharacterControl: CreateDistanceGraph() V={0}", V);
            
            // Add the AI player's transform to the list the root position
            dijkstraTransforms[0] = transform;

            // If the AI player is on Team A...
            if (pm.GetTeam().Equals(PlayerManager.VALUE_TEAM_NAME_A))
            {
                // Copy the Team A weapon spawn points
                Array.Copy(allWeaponSpawnPoints, 0, dijkstraTransforms, 1, numberOfTeamSpawnPoints);
            }
            // If the AI player is on Team B...
            else
            {
                // Copy the Team B weapon spawn points
                Array.Copy(allWeaponSpawnPoints, numberOfTeamSpawnPoints, dijkstraTransforms, 1, numberOfTeamSpawnPoints);
            }

            // Create a distance graph for the player and all the weapon spawn points
            // *** Note: For simplicity, the distance we're using pretends the AI player can walk in a straight line to every spawn point
            // *** There might be a better way to calculate this that incorporates the nav mesh agent's path distance
            graph = new int[numberOfTeamSpawnPoints, numberOfTeamSpawnPoints];
            for (int i = 0; i < numberOfTeamSpawnPoints; i++)
                for (int j = 0; j < numberOfTeamSpawnPoints; j++)
                    graph[i, j] = Convert.ToInt32(Vector3.Distance(dijkstraTransforms[i].position, dijkstraTransforms[j].position));
        }

        void OnStage1TimerIsExpired()
        {
            if (DEBUG && DEBUG_OnStage1TimerIsExpired) Debug.LogFormat("AICharacterControl: OnStage1TimerIsExpired() ");
            isStage1 = false;
            target = null;

            // Clear references 
            dijkstraTransforms = new Transform[0];
        }

        void OnStage2TimerIsExpired()
        {
            if (DEBUG && DEBUG_OnStage2TimerIsExpired) Debug.LogFormat("AICharacterControl: OnStage2TimerIsExpired() ");
            isStage1 = true;
            target = null;

            // Create distance graph of AI player and weapon spawn points to use in Dijkstra's algorithm
            CreateDistanceGraph();
        }

        public override void OnEnable()
        {
            // Setup event callbacks for the ending of the two stages of the game
            CountdownTimer.OnCountdownTimer1HasExpired += OnStage1TimerIsExpired;
            CountdownTimer.OnCountdownTimer2HasExpired += OnStage2TimerIsExpired;
        }

        Transform FindNextSpawnPointToTarget()
        {
            // Find the min distance
            int minIndex = -1;
            int minDist = int.MaxValue;
            // Go through all the weapon spawn points in the graph
            for (int i = 1; i < V; i++)
            {
                int d = dist[i];
                if(d < minDist)
                {
                    minDist = d;
                    minIndex = i;
                }
            }

            Transform t;

            if (minIndex == -1)
                t = null;
            else
            {
                t = dijkstraTransforms[minIndex];
                dist[minIndex] = int.MaxValue;
            }
            return t;
        }

        void OnTriggerEnter(Collider other)
        {
            // If this client controls this player...
            if (photonView.IsMine)
            {
                // If this player collided with a Weapon... 
                if (other.CompareTag("Weapon"))
                {
                    if (DEBUG && DEBUG_OnTriggerEnter) Debug.LogFormat("AICharacterControl: OnTriggerEnter() Collided with WEAPON with name \"{0}\"," +
                        " photonView.Owner.NickName = {1}", other.GetComponentInParent<Gun>().name, photonView.Owner.NickName);

                    target = null;
                }
            }
        }

        void AccomplishStageBasedGoals()
        {
            // If we're currently in Stage 1
            if (isStage1)
            {
                // Look for guns
                //
                // If we don't have a target...
                if (target == null)
                {
                    // Find the next weapon spawn point to target
                    Transform t = FindNextSpawnPointToTarget();
                    if (t != null)
                    {
                        Debug.LogFormat("AICharacterControl: AccomplishStageBasedGoals() NEXT TARGET = {0} Agent.stoppingDistance = {1}", t, Agent.stoppingDistance);
                        SetTarget(t);
                        // Set the destination based on the target's current position
                        Agent.SetDestination(target.position);
                    }
                }

            }
            // If we're currently in Stage 2
            else
            {
                // If we don't have a target OR our target is not in view...
                if (target == null || !pm.EnemiesInView.Contains(target))
                {
                    // If there are opponents in view...
                    if (pm.EnemiesInView.Count > 0)
                    {
                        // Pick a random enemy in view to target
                        SetTarget(((PlayerManager)pm.EnemiesInView[new System.Random().Next(0, pm.EnemiesInView.Count)]).transform);
                    }
                }

                // If enemy player is in AI player's crosshairs...
                if (EnemyIsInCrosshairs(out PlayerManager enemy))
                {
                    // If we found an enemy in our cross hairs...
                    SetTarget(enemy.transform);
                    // Shoot the gun (if we have a gun to shoot)
                    ShootGun();
                }
            }

            // If we have a target for the AI
            if (target != null)
            {
                // Set the destination based on the target's current position
                Agent.SetDestination(target.position);
            }

            // If AI player is outside of stopping distance from target
            if (Agent.remainingDistance > Agent.stoppingDistance)
            {
                Character.Move(Agent.desiredVelocity, false, false);
            }
            // If AI player is within stopping distance of target
            else
            {
                // Stop moving
                // *** Alex: Not sure if this is needed.
                Character.Move(Vector3.zero, false, false);

               // Reset target reference
               // target = null;
            }
        }

        void Update()
        {
            AccomplishStageBasedGoals();
        }

        /// <summary>
        /// Raycasts from player's camera to check if enemy (player on a different team) is in this player's crosshairs.
        /// </summary>
        /// <param name="enemy">Returns true if we have an enemy in our crosshairs. Also returns that (non-null) enemy.
        /// Returns false if we do not have an enemy in our crosshairs. Also returns enemy = null.</param>
        /// <returns></returns>
        private bool EnemyIsInCrosshairs(out PlayerManager enemy)
        {
            // Create a raycast from fps camera position in the direction it is facing (limit raycast to 'range' distance away)
            // Get back the 'hit' value (what got hit)
            // If ray cast hit something...
            if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, 200f))
            {
                if (DEBUG && DEBUG_EnemyIsInCrosshairs) Debug.LogFormat("AICharacterControl: EnemyIsInCrosshairs() RAYCAST HIT");

                PlayerManager possibleEnemy = hit.transform.GetComponent<PlayerManager>();

                // If we have a possible enemy in our cross hairs...
                if (possibleEnemy != null)
                {
                    if (DEBUG && DEBUG_EnemyIsInCrosshairs) Debug.LogFormat("AICharacterControl: EnemyIsInCrosshairs() POSSIBLE ENEMY FOUND");
                    // If player we are aiming at is on a different team...
                    if (!pm.GetTeam().Equals(possibleEnemy.GetTeam()))
                    {
                        // We found an enemy in our crosshairs
                        enemy = possibleEnemy;
                        return true;
                    }
                }
            }

            // We did not find enemy in our crosshairs
            enemy = null;
            return false;
        }

        /// <summary>
        /// Shoots active gun if player has an active gun to shoot.
        /// </summary>
        private void ShootGun()
        {
            // If player has an active gun...
            if (pm.ActiveGun != null)
            {
                // Shoot the gun
                pm.CallShootRPC();
            }
        }

        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        // A utility function to find the 
        // vertex with minimum distance 
        // value, from the set of vertices 
        // not yet included in shortest 
        // path tree 
        int MinDistance(int[] dist, bool[] sptSet)
        {
            // Initialize min value 
            int min = int.MaxValue, min_index = -1;

            for (int v = 0; v < V; v++)
                if (sptSet[v] == false && dist[v] <= min)
                {
                    min = dist[v];
                    min_index = v;
                }

            return min_index;
        }

        // A utility function to print the constructed distance array 
        void PrintSolution(int[] dist, int n)
        {
            Console.Write("Vertex     Distance from Source\n");
            for (int i = 0; i < V; i++)
                Console.Write(i + " \t\t " + dist[i] + "\n");
        }

        void PlaceHolderDistanceFormula(int[,] graph, int src, out int[] dist)
        {
            for (int i = 0; i < (dist = new int[V]).Length; i++)
                dist[i] = i;
        }

        // Funtion that implements Dijkstra's single source shortest path algorithm 
        // for a graph represented using adjacency matrix representation 
        void Dijkstra(int[,] graph, int src, out int[] dist)
        {
            dist = new int[V]; // The output array. dist[i] will hold the shortest distance from src to i 

            // sptSet[i] will true if vertex i is included in shortest path tree or shortest distance from src to i is finalized 
            bool[] sptSet = new bool[V];

            // Initialize all distances as INFINITE and stpSet[] as false 
            for (int i = 0; i < V; i++)
            {
                dist[i] = int.MaxValue;
                sptSet[i] = false;
            }

            // Distance of source vertex from itself is always 0 
            dist[src] = 0;

            // Find shortest path for all vertices 
            for (int count = 0; count < V - 1; count++)
            {
                // Pick the minimum distance vertex from the set of vertices not yet processed. u is always equal to src in first iteration. 
                int u = MinDistance(dist, sptSet);

                // Mark the picked vertex as processed 
                sptSet[u] = true;

                // Update dist value of the adjacent vertices of the picked vertex. 
                for (int v = 0; v < V; v++) {

                    // Update dist[v] only if is not in sptSet AND there is an edge from u to v AND total weight of path 
                    // from src to v through u is smaller than current value of dist[v] 
                    bool a = !sptSet[v];
                    bool b = graph[u, v] != 0;
                    bool c = dist[u] != int.MaxValue;
                    bool d = dist[u] + graph[u, v] < dist[v];
                    if (a && b && c && d)
                        dist[v] = dist[u] + graph[u, v];
                }
            }

            // print the constructed distance array 
            //printSolution(dist, V);
        }
    }
}
