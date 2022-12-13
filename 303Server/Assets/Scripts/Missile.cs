using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Missile : MonoBehaviour
{
    public Vector3 shootDirection;
    public Rigidbody body;

    public float speed = 0.5f;
    public float maxSpeed = 5f;

    public int playerID;

    public void Init(int parentID, Vector3 direction)
    {
        //Initilize Member Variables
        playerID = parentID;
        shootDirection = direction;
        gameObject.SetActive(false);
        body.velocity = new Vector3(0, 0, 0);
    }
    public void Move()
    {

        float minDistance = 500000; //Level is 50 meters wide so if there is a valid player it will always beat this default value
        Vector3 closestPlayerPosition = shootDirection; // if no other players then just make the target the default shoot direction

        foreach (Client client in Server.clients)
        {
            if (client.GetID() != playerID && client.player != null) // dont target the owner of the missile and if the player is dead
            {
                if ((transform.position - client.player.transform.position).magnitude < minDistance) // compare the distance to all players
                {
                    closestPlayerPosition = (client.player.transform.position - transform.position);
                    minDistance = closestPlayerPosition.magnitude;
                }
            }
        }

        //apply a force in the direction of the closest player
        body.AddForce(closestPlayerPosition.normalized * speed);

        if (body.velocity.magnitude > maxSpeed) // maintain the speed of the player so that eneimies are still able to run away 
        {
            //Calculte the force need to maintain speed without explicitly setting the velcoity
            Vector3 normalisedVelocity = body.velocity.normalized;
            Vector3 brakeVelocity = normalisedVelocity * maxSpeed;  // make the brake Vector3 value
            Vector3 Velocity = body.velocity - brakeVelocity;  // make the brake Vector3 value


            body.AddForce(-Velocity);  // apply opposing brake force
        }

        //point the missile in the direction the missile is moving
        transform.rotation = Quaternion.LookRotation(body.velocity);

    }

    void OnCollisionEnter(Collision collision)
    {
        try
        {
            //Check for collisions with the player
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<Player>().TakeDamage(50f); // Damage player
            }
            else if (collision.gameObject.CompareTag("Bullet"))
            {
                //if a player shoots thier own missile dont disable it 
                if (collision.gameObject.GetComponent<Bullet>().playerID == playerID)
                {
                    return;
                }
            }
            Disable(); // disable the missile if it has hit anything else (e.g a wall)
        }
        catch (Exception error)
        {
            Debug.Log(error);
        }

    }

    //Disables the missile and stop collisions and physics calculations
    public void Disable()
    {
        gameObject.SetActive(false);
        body.velocity = new Vector3(0, 0, 0);
        body.Sleep();
        Server.DisableMissile(this);
    }

    //Enables the missile and starts collisions and physics calculations
    public void Enable()
    {
        gameObject.SetActive(true);
        body.WakeUp();
        transform.rotation = Quaternion.LookRotation(shootDirection);
        body.rotation = transform.rotation;
        body.AddForce(shootDirection * speed);
        Server.SpawnMissile(this);
    }


}
