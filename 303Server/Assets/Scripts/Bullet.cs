using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class Bullet : MonoBehaviour
{
    public Vector3 shootDirection;
    public Rigidbody body;

    public float speed = 0.5f;
    private float timeout = 8;
    private float timer = 0;

    public int id;
    public int playerID;

    public void Init(int parentID, int bulletID, Vector3 direction)
    {
        //Initilize variables, they wont change for the duration of the bullets life cycle
        id = bulletID;
        playerID = parentID;
        shootDirection = direction;

        gameObject.SetActive(false);
        body.velocity = new Vector3(0, 0, 0);
        body.Sleep();

    }
    public void Move()
    {
        timer += Time.deltaTime;
        if (timer >= timeout) // disable bullets after a certain time time to reduce how may packets we are sending
        {
            //reset timer and disable the bullet
            timer = 0;
            Disable();
            return;
        }

        transform.rotation = body.rotation;

    }

    void OnCollisionEnter(Collision collision)
    {
        try
        {
            //Check for collisions with the player
            if (collision.gameObject.CompareTag("Player"))
            {
                Disable();  //Disable the bullet once its hit a player to stop it from hitting mutiple times
                collision.gameObject.GetComponent<Player>().TakeDamage(10f); // damage the player
            }
            else if (collision.gameObject.CompareTag("Floor")) //Check for collisions with the Floor TODO fiddle with
            {
                StartCoroutine(DelayDisable(1)); // let the bullet bounce first and then disable after a second
            }
        }
        catch (Exception error)
        {
            Debug.Log(error);
        }

    }

    //Disables the bullet and stop collisions and physics calculations
    public void Disable()
    {
        body.velocity = new Vector3(0, 0, 0);
        body.Sleep();
        gameObject.SetActive(false);
        Server.DisableBullet(this);
    }

    //Enables the bullet and starts collisions and physics calculations
    public void Enable()
    {
        gameObject.SetActive(true);
        body.WakeUp();
        transform.rotation = Quaternion.LookRotation(shootDirection);
        body.rotation = transform.rotation;
        body.AddForce(shootDirection * speed);
        //body.AddForce(new Vector3(0, 2, 0));

        Server.SpawnBullet(this);
    }

    private IEnumerator DelayDisable(float time)
    {
        yield return new WaitForSeconds(time);

        //Will Disable the bullet after a certaiin amount of time
        Disable();

    }

}
