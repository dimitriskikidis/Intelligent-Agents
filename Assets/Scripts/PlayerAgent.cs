using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PlayerAgent : Agent
{
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private GameObject goal, ball, enemy, agentBall;
    private bool hasBall;
    private float maxDistance, existentialPenalty;
    private Vector2 lastAgentDir;
    private readonly float agentMoveSpeed = +6f;
    private readonly float enemyMoveSpeed = +2f;

    private void Start()
    {
        maxDistance = (new Vector2(-16, -10) - new Vector2(16, 10)).sqrMagnitude;
        existentialPenalty = -1f / MaxStep;
    }

    public override void OnEpisodeBegin()
    {
        // Reset the scene
        transform.localPosition = new Vector3(Random.Range(-5f, -3f), 0f, Random.Range(-4.5f, +4.5f));
        transform.localRotation = Quaternion.identity;
        Vector2 agentPos = new Vector2(transform.localPosition.x, transform.localPosition.z);
        Vector2 ballPos = new Vector2(ball.transform.localPosition.x, ball.transform.localPosition.z);
        lastAgentDir = (ballPos - agentPos).normalized;
        goal.transform.localPosition = new Vector3(-7.5f, 0f, Random.Range(-3f, +3f));
        ball.transform.localPosition = new Vector3(+7.5f, 0f, Random.Range(-3f, +3f));
        enemy.transform.localPosition = new Vector3(Random.Range(0f, +2f), 0f, Random.Range(-4f, +4f));
        hasBall = false;
        goal.SetActive(false);
        ball.SetActive(true);
        agentBall.SetActive(false);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector2 agentPos = new Vector2(transform.localPosition.x, transform.localPosition.z);

        // Observations for the ball
        int hasBallInt = hasBall ? 1 : 0;
        Vector2 ballPos = new Vector2(ball.transform.localPosition.x, ball.transform.localPosition.z);
        Vector2 dirToBall = ballPos - agentPos;
        float distanceToBall = dirToBall.sqrMagnitude / maxDistance;
        Vector2 dirToBallNormalized = dirToBall.normalized;
        sensor.AddObservation(hasBallInt);
        sensor.AddObservation(distanceToBall);
        sensor.AddObservation(dirToBallNormalized.x);
        sensor.AddObservation(dirToBallNormalized.y);

        // Observations for the goal
        Vector2 goalPos = new Vector2(goal.transform.localPosition.x, goal.transform.localPosition.z);
        Vector2 dirToGoal = goalPos - agentPos;
        float distanceToGoal = dirToGoal.sqrMagnitude / maxDistance;
        Vector2 dirToGoalNormalized = dirToGoal.normalized;
        sensor.AddObservation(distanceToGoal);
        sensor.AddObservation(dirToGoalNormalized.x);
        sensor.AddObservation(dirToGoalNormalized.y);

        // Observations for the enemy
        Vector2 enemyPos = new Vector2(enemy.transform.localPosition.x, enemy.transform.localPosition.z);
        Vector2 dirToEnemy = enemyPos - agentPos;
        float distanceToEnemy = (enemyPos - agentPos).sqrMagnitude / maxDistance;
        Vector2 dirToEnemyNormalized = dirToEnemy.normalized;
        float agentDirAngleToEnemy = Vector2.Angle(lastAgentDir, dirToEnemyNormalized) / 180f;
        sensor.AddObservation(distanceToEnemy);
        sensor.AddObservation(dirToEnemyNormalized.x);
        sensor.AddObservation(dirToEnemyNormalized.y);
        sensor.AddObservation(agentDirAngleToEnemy);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Move the agent
        int moveX = actions.DiscreteActions[0] - 1; // -1 -> Left, 0 -> No Move, +1 -> Right
        int moveZ = actions.DiscreteActions[1] - 1; // -1 -> Back, 0 -> No Move, +1 -> Forward
        Vector3 force = new Vector3(moveX, 0f, moveZ);
        Vector2 lastAgentPos = transform.localPosition;
        transform.localPosition += agentMoveSpeed * Time.deltaTime * force;

        // Out of bounds constraints
        float newX = transform.localPosition.x;
        float newZ = transform.localPosition.z;
        if (newX >= +7.5f) newX = +7.5f;
        if (newX <= -7.5f) newX = -7.5f;
        if (newZ >= +4.5f) newZ = +4.5f;
        if (newZ <= -4.5f) newZ = -4.5f;
        transform.localPosition = new Vector3(newX, 0f, newZ);

        // Calculate the direction of the agent
        lastAgentDir = (new Vector2(transform.localPosition.x, transform.localPosition.z) - lastAgentPos).normalized;

        // Move the enemy
        Vector2 agentPos = new Vector2(transform.localPosition.x, transform.localPosition.z);
        Vector2 enemyPos = new Vector2(enemy.transform.localPosition.x, enemy.transform.localPosition.z);
        Vector2 enemyDirToPlayer = (agentPos - enemyPos).normalized;
        Vector3 enemyForce = new Vector3(enemyDirToPlayer.x, 0f, enemyDirToPlayer.y);
        enemy.transform.localPosition += enemyMoveSpeed * Time.deltaTime * enemyForce;

        // Add an existential penalty
        AddReward(existentialPenalty);
    }

    // If the agent has not been assigned a trained model,
    // the player controls the agent's movement.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Mathf.RoundToInt(Input.GetAxisRaw("Horizontal")) + 1;
        discreteActions[1] = Mathf.RoundToInt(Input.GetAxisRaw("Vertical")) + 1;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball")) // The agent touched the ball
        {
            hasBall = true;
            ball.SetActive(false);
            goal.SetActive(true);
            agentBall.SetActive(true);
            AddReward(+0.5f);
        }
        else if (other.CompareTag("Goal")) // The agent touched the goal
        {
            AddReward(+0.5f);
            EndEpisode();
        }
        else if (other.CompareTag("Enemy")) // The enemy touched the agent
        {
            SetReward(-1f);
            EndEpisode();
        }
    }
}
