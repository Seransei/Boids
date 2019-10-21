using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlockMate : MonoBehaviour
{
    public Vector3 baseRotation;

    [Header("Parameters")]
    [Range(0, 10)]
    public float maxSpeed = 1f;
    [Range(.1f, .5f)]
    public float maxForce = .03f;
    [Range(10, 100)]
    public float maxEnergy = 10;
    [Range(1, 10)]
    public float neighborhoodRadius = 1f;
    [Range(0, 4)]
    public float obstacleAvoidanceRadius = 0.5f;

    WaitForSeconds updateStateRate = new WaitForSeconds(1 / 2);
    WaitForSeconds energyUpdateRate= new WaitForSeconds(1);

    [Header("Coefficient")]
    [Range(0, 3)]
    public float separationAmount = 1f;
    [Range(0, 3)]
    public float cohesionAmount = 1f;
    [Range(0, 3)]
    public float alignmentAmount = 1f;
    [Range(0, 10)]
    public float avoidAmount = 1f;
    [Range(0, 10)]
    public float chaseAmount = 1f;
    [Range(0, 1)]
    public float idleAmount = 1f;

    [Header("Attributes")]
    public Vector2 acceleration;
    public Vector2 velocity;

    private Vector2 Position 
    {
        get {
            return gameObject.transform.position;
        }
        set {
            gameObject.transform.position = value;
        }
    }

    private float energy;
    public float Energy
    {
        get
        {
            return energy;
        }
        set
        {
            energy = Mathf.Clamp(value, 0, maxEnergy);
        }
    }

    enum State
    {
        S_IDLE,
        S_FLOCKING,
        S_CHASING
    }

    private State CurrentState { get; set; }

    private void Start()
    {
        CurrentState = State.S_IDLE;

        float angle = UnityEngine.Random.Range(0, 2 * Mathf.PI);
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle) + baseRotation);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Energy = maxEnergy + UnityEngine.Random.Range(0, 5);

        StartCoroutine("UpdateEnergy");
        StartCoroutine("UpdateState");
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(Position, obstacleAvoidanceRadius);
        //Gizmos.DrawLine(Position, Position + velocity);
    }

    private void Update()
    {
        acceleration = Vector2.zero;
        acceleration += Avoid(FindObstacles());

        if (CurrentState == State.S_FLOCKING)
            acceleration += Flock(FindFlockMates());
        else if (CurrentState == State.S_CHASING)
            acceleration += Chase(FindPlayer());
        else if (CurrentState == State.S_IDLE)
            acceleration += Idle();

        UpdateVelocity();
        UpdatePosition();
        UpdateRotation();

        WrapAround();
    }

    private IEnumerator UpdateState()
    {
        while (true)
        {
            if (Energy >= 0.8 * maxEnergy && FindPlayer() != null)
            {
                if(CurrentState != State.S_CHASING)
                {
                    CurrentState = State.S_CHASING;
                    gameObject.GetComponent<SpriteRenderer>().color = Color.red;
                }
            }
            else if (Energy <= 0.2 * maxEnergy || FindPlayer() == null)
            {
                if (FindFlockMates().Any())
                {
                    if (CurrentState != State.S_FLOCKING)
                    {
                        CurrentState = State.S_FLOCKING;
                        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
                    }
                }
                else if (CurrentState != State.S_IDLE)
                {
                    CurrentState = State.S_IDLE;
                    gameObject.GetComponent<SpriteRenderer>().color = Color.black;
                }
            }

            yield return updateStateRate;
        }
    }

    private IEnumerator UpdateEnergy()
    {
        while (true)
        {
            if (CurrentState == State.S_FLOCKING)
            {
                Energy += 1;
            }
            else if (CurrentState == State.S_CHASING)
            {
                Energy -= 1.5f;
            }

            yield return energyUpdateRate;
        }
    }

    private List<Collider2D> GetColliders(float radius)
    {
        return Physics2D.OverlapCircleAll(Position, radius).ToList();
    }

    private List<FlockMate> FindFlockMates()
    {
        List<Collider2D> boidColliders = GetColliders(neighborhoodRadius);
        List<FlockMate> neighbors = new List<FlockMate>();
        foreach(Collider2D col in boidColliders)
        {
            if (col.gameObject.tag == gameObject.tag)
                neighbors.Add(col.gameObject.GetComponent<FlockMate>());
        }
        neighbors.Remove(this);

        return neighbors;
    }
    private List<GameObject> FindObstacles()
    {
        List<Collider2D> boidColliders = GetColliders(obstacleAvoidanceRadius);
        List<GameObject> obstacles = new List<GameObject>();
        foreach (Collider2D col in boidColliders)
        {
            if (col.gameObject.tag == "Obstacle")
                obstacles.Add(col.gameObject);
        }
        return obstacles;
    }

    private PlayerController FindPlayer()
    {
        List<Collider2D> boidColliders = GetColliders(neighborhoodRadius);
        foreach (Collider2D col in boidColliders)
        {
            if (col.gameObject.tag == "Player")
                return col.gameObject.GetComponent<PlayerController>();
        }
        return null;
    }

    private Vector2 Flock(List<FlockMate> boids)
    {
        var alignment = Vector2.zero;
        var separation = Vector2.zero; 
        var cohesion = Vector2.zero;
        if(boids.Count != 0)
        {
            alignment = Alignment(boids);
            separation = Separation(boids);
            cohesion = Cohesion(boids);
        }

        return
            alignmentAmount * alignment +
            cohesionAmount * cohesion +
            separationAmount * separation;
    }

    public void UpdateVelocity()
    {
        velocity += acceleration;
        velocity = LimitMagnitude(velocity, maxSpeed);
    }

    private void UpdatePosition()
    {
        Position += velocity * Time.deltaTime;
    }

    private void UpdateRotation()
    {
        var angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle) + baseRotation);
    }

    private Vector2 Alignment(IEnumerable<FlockMate> boids)
    {
        var velocity = Vector2.zero;

        if (!boids.Any()) return velocity;

        foreach (var boid in boids)
        {
            velocity += boid.velocity;
        }
        velocity /= boids.Count();

        var steer = Steer(velocity.normalized * maxSpeed);
        return steer;
    }

    private Vector2 Cohesion(IEnumerable<FlockMate> boids)
    {
        var sumPositions = Vector2.zero;

        if (!boids.Any()) return sumPositions;

        foreach (var boid in boids)
        {
            sumPositions += boid.Position;
        }
        var average = sumPositions / boids.Count();
        var direction = average - Position;

        var steer = Steer(direction.normalized * maxSpeed);
        return steer;
    }

    private Vector2 Separation(IEnumerable<FlockMate> boids)
    {
        var direction = Vector2.zero;
        boids = boids.Where(o => DistanceTo(o) <= neighborhoodRadius / 2);

        if (!boids.Any()) return direction;

        foreach (var boid in boids)
        {
            var difference = Position - boid.Position;
            direction += difference.normalized / difference.magnitude;
        }
        direction /= boids.Count();

        var steer = Steer(direction.normalized * maxSpeed);
        return steer;
    }

    private Vector2 Avoid(List<GameObject> obs)
    {
        var direction = Vector2.zero;

        if (!obs.Any()) return direction;

        foreach (GameObject go in obs)
        {
            var objPos = (Vector2)go.transform.position;
            var difference = Position - objPos;
            direction += difference.normalized / difference.magnitude;
        }
        direction /= obs.Count();

        direction = RotateVector2(direction, UnityEngine.Random.Range(-100, 100));

        var steer = Steer(direction.normalized * maxSpeed);
        return avoidAmount * steer;
    }

    private Vector2 Chase(PlayerController player)
    {
        if (player == null) return Vector2.zero;

        var direction = player.Position - Position;
        var steer = Steer(direction.normalized * maxSpeed);
        return chaseAmount * steer;
    }

    private Vector2 Idle()
    {
        var direction = new Vector2(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1));
        var steer = Steer(direction.normalized * maxSpeed);
        return idleAmount * steer;
    }

    private Vector2 Steer(Vector2 desired)
    {
        var steer = desired - velocity;
        steer = LimitMagnitude(steer, maxForce);

        return steer;
    }

    private float DistanceTo(FlockMate boid)
    {
        return Vector3.Distance(boid.transform.position, Position);
    }

    private Vector2 LimitMagnitude(Vector2 baseVector, float maxMagnitude)
    {
        if (baseVector.sqrMagnitude > maxMagnitude * maxMagnitude)
        {
            baseVector = baseVector.normalized * maxMagnitude;
        }
        return baseVector;
    }

    private Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        float tx = v.x;
        float ty = v.y;

        return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
    }

    private void WrapAround()
    {
        if (Position.x < -14) Position = new Vector2(14, Position.y);
        if (Position.y < -8) Position = new Vector2(Position.x, 8);
        if (Position.x > 14) Position = new Vector2(-14, Position.y);
        if (Position.y > 8) Position = new Vector2(Position.x, -8);
    }
}