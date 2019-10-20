using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Range(0, 5)]
    public float speed = 1f;

    [HideInInspector]
    public Vector2 Position
    {
        get
        {
            return gameObject.transform.position;
        }
        set
        {
            gameObject.transform.position = value;
        }
    }

    void Update()
    {
        Vector2 movement = Vector2.zero;

        if (Input.GetKey(KeyCode.Z))
            movement += new Vector2(0, 1);

        if (Input.GetKey(KeyCode.S))
            movement += new Vector2(0, -1);

        if (Input.GetKey(KeyCode.Q))
            movement += new Vector2(-1, 0);

        if (Input.GetKey(KeyCode.D))
            movement += new Vector2(1, 0);

        Position += movement.normalized * speed * Time.deltaTime;
    }
}
