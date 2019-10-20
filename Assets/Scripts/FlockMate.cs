using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlockMate : MonoBehaviour
{
    public Vector3 baseRotation;

    [Range(0, 10)]
    public float maxSpeed = 1f;

    [Range(.1f, .5f)]
    public float maxForce = .03f;

    [Range(1, 10)]
    public float neighborhoodRadius = 1f;

    [Range(0, 3)]
    public float separationAmount = 1f;

    [Range(0, 3)]
    public float cohesionAmount = 1f;

    [Range(0, 3)]
    public float alignmentAmount = 1f;

    [Range(0, 10)]
    public float avoidAmount = 1f;

    public Vector2 acceleration;
    public Vector2 velocity;

    private Vector2 Position {
        get {
            return gameObject.transform.position;
        }
        set {
            gameObject.transform.position = value;
        }
    }

    private void Start()
    {
        float angle = Random.Range(0, 2 * Mathf.PI);
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle) + baseRotation);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(Position, neighborhoodRadius);
        //Gizmos.DrawLine(Position, Position + velocity);
    }

    private void Update()
    {
        Flock(FindFlockMates());
        UpdateVelocity();
        UpdatePosition();
        UpdateRotation();

        WrapAround();
    }

    private List<FlockMate> FindFlockMates()
    {
        List<Collider2D> boidColliders = Physics2D.OverlapCircleAll(Position, neighborhoodRadius).ToList();
        List<FlockMate> neighbors = new List<FlockMate>();
        foreach(Collider2D col in boidColliders)
        {
            if (col.gameObject.tag == "Crab")
                neighbors.Add(col.gameObject.GetComponent<FlockMate>());
        }
        neighbors.Remove(this);

        return neighbors;
    }
    private List<GameObject> FindObstacles()
    {
        List<Collider2D> boidColliders = Physics2D.OverlapCircleAll(Position, neighborhoodRadius).ToList();
        List<GameObject> obstacles = new List<GameObject>();
        foreach (Collider2D col in boidColliders)
        {
            if (col.gameObject.tag == "Obstacle")
                obstacles.Add(col.gameObject);
        }
        return obstacles;
    }


    private void Flock(IEnumerable<FlockMate> boids)
    {
        var alignment = Alignment(boids);
        var separation = Separation(boids);
        var cohesion = Cohesion(boids);

        var obstacles = FindObstacles();
        var avoid = Vector2.zero;
        if(obstacles.Count != 0)
            avoid = AvoidObstacles(obstacles);

        acceleration = 
            alignmentAmount * alignment + 
            cohesionAmount * cohesion + 
            separationAmount * separation + 
            avoidAmount * avoid;
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
        if (!boids.Any()) return Vector2.zero;

        var sumPositions = Vector2.zero;
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

    private Vector2 AvoidObstacles(List<GameObject> obs)
    {
        var direction = Vector2.zero;

        foreach (GameObject go in obs)
        {
            var difference = Position - (Vector2)go.transform.position;
            direction += difference.normalized / difference.magnitude;
        }
        direction /= obs.Count();

        var steer = Steer(direction.normalized * maxSpeed);
        return steer;

        return direction;
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