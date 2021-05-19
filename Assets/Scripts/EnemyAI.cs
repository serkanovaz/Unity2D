using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using AForge.Fuzzy;
public class EnemyAI : MonoBehaviour
{
    //fuzzydistance
    FuzzySet fsNear, fsMed, fsFar;
    LinguisticVariable lvDistance;

    //fuzzyspped
    FuzzySet fsSlow, fsMedium, fsFast;
    LinguisticVariable lvSpeed;

    //fuzzy
    Database database;
    InferenceSystem infSystem;
    

    Transform player;
    float distance, speed;
    NavMeshAgent agent;
    public Transform[] wayPoints;
    public Transform rayOrigin;
    int currentWayPointIndex = 0;
    Animator fsm; 
    Vector3[] wayPointsPos = new Vector3[3];
    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        for (int i = 0; i < wayPoints.Length; i++)
            wayPointsPos[i] = wayPoints[i].position;

        fsm = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(wayPointsPos[currentWayPointIndex]);

        StartCoroutine("CheckPlayer");
    }

    private void Initialize()
    {
        SetDistanceFuzzySets();
        SetSpeedFuzzySets();
        AddToDataBase();

    }

    private void SetDistanceFuzzySets()
    {
        fsNear = new FuzzySet("Near",new TrapezoidalFunction(20,40,TrapezoidalFunction.EdgeType.Right));
        fsMed = new FuzzySet("Med", new TrapezoidalFunction(20, 40, 50, 70));
        fsFar = new FuzzySet("Far", new TrapezoidalFunction(50, 70, TrapezoidalFunction.EdgeType.Left));
        lvDistance = new LinguisticVariable("Distance", 0, 100);
        lvDistance.AddLabel(fsNear);
        lvDistance.AddLabel(fsMed);
        lvDistance.AddLabel(fsFar);
    }

    private void SetSpeedFuzzySets()
    {
        fsSlow = new FuzzySet("Slow", new TrapezoidalFunction(30, 50, TrapezoidalFunction.EdgeType.Right));
        fsMedium = new FuzzySet("Medium", new TrapezoidalFunction(30, 50, 80, 100));
        fsFast = new FuzzySet("Fast", new TrapezoidalFunction(80, 100, TrapezoidalFunction.EdgeType.Left));
        lvSpeed = new LinguisticVariable("Speed", 0, 120);
        lvSpeed.AddLabel(fsSlow);
        lvSpeed.AddLabel(fsMedium);
        lvSpeed.AddLabel(fsFast);
    }

    private void AddToDataBase()
    {
        database = new Database();
        database.AddVariable(lvDistance);
        database.AddVariable(lvSpeed);

        infSystem = new InferenceSystem(database, new CentroidDefuzzifier(120));
        infSystem.NewRule("Rule 1", "IF Distance IS Near THEN Speed IS Slow");
        infSystem.NewRule("Rule 2", "IF Distance IS Med THEN Speed IS Medium");
        infSystem.NewRule("Rule 3", "IF Distance IS Far THEN Speed IS Fast");
    }


    private void Update()
    {
        Evaluate(); 
    }

    private void Evaluate()
    {
        if (player)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            distance = Vector3.Distance(transform.position, player.position);
            infSystem.SetInput("Distance", distance);
            speed = infSystem.Evaluate("Speed");
            agent.speed = speed * 0.25f;
            
        }
    }

    IEnumerator CheckPlayer()
    {
        CheckVisibility();
        CheckDistance();
        CheckDistanceFromCurrentWaypoint();
        yield return new WaitForSeconds(0.1f);
        yield return CheckPlayer();
    }

    private void CheckDistanceFromCurrentWaypoint()
    {
        float distance = Vector3.Distance(wayPointsPos[currentWayPointIndex], rayOrigin.position);
        //(player.position - transform.position).magnitude;

        fsm.SetFloat("distanceFromCurrentWaypoint", distance);
    }

    private void CheckDistance()
    {
        if (player)
        {
            float distance = Vector3.Distance(player.position, rayOrigin.position);
            //(player.position - transform.position).magnitude;

            fsm.SetFloat("distance", distance);
        }
    }

    private void CheckVisibility()
    {
        float maxDistance = 20;
        if (player)
        {
            Vector3 direction = (player.position - rayOrigin.position).normalized;
            Debug.DrawRay(rayOrigin.position, direction * maxDistance, Color.red);
            //Vector3 direction2 = (player.position - transform.position) / (player.position - transform.position).magnitude;

            if (Physics.Raycast(rayOrigin.position, direction, out RaycastHit info, maxDistance))
            {
                if (info.transform.tag == "Player")
                    fsm.SetBool("isVisible", true);

                else
                    fsm.SetBool("isVisible", false);
            }
        }
        else
            fsm.SetBool("isVisible", false);
    }


    public void SetLookRotation()
    {
        if (player)
        {
            Vector3 dir = (player.position - transform.position).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(dir);

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 0.1f);
        }
        
    }


    public void Shoot()
    {
        float shootFreq = 5;
        GetComponent<ShootBehaviour>().Shoot(shootFreq);
    }

    public void Patrol()
    {

      
    }

    public void Chase()
    {
        if (player)
            agent.SetDestination(player.position);
    }


    public void SetNewWayPoint()
    {
        switch (currentWayPointIndex)
        {
            case 0:
                currentWayPointIndex = 1;
                break;
            case 1:
                currentWayPointIndex = 2;
                break;
            case 2:
                currentWayPointIndex = 0;
                break;
        }
        agent.SetDestination(wayPointsPos[currentWayPointIndex]);
    }
}
