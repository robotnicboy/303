using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class Player : MonoBehaviour
{

    public CharacterController controller;
    public Rigidbody body;// todo test without rigid body 

    public Missile missile;
    public List<Bullet> bullets = new List<Bullet>(); // TODO change to disctionary

    public int id;
    public int messageID = -1;
    public string username;
    public float health;
    public Vector3 spawnPoint;
    private GameObject spawnArea;

    public float gravity = -9.81f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 5f;
    public int bulletMax = 10;

    private bool[] moveDirection;
    private float velocity = 0;
    private int disableIndex = 0;

    public void Init(int clientID, string playerName)
    {
        //Initlize Member Variables
        id = clientID; // player id will always be equivilant to its parents id
        username = playerName;
        health = 100;

        moveDirection = new bool[5]; //stores player movement to be passed to the server
        Color color;


        //Give each player thier color and spawn based of thier id
        switch (id)
        {
            case 0:
                color = Color.red;
                spawnPoint = new Vector3(18.5f, 0, 18.5f);
                break;
            case 1:
                color = Color.green;
                spawnPoint = new Vector3(-18.5f, 0, 18.5f);
                break;
            case 2:
                color = Color.blue;
                spawnPoint = new Vector3(-18.5f, 0, -18.5f);
                break;
            case 3:
                color = new Color(1f, 0f, 1f, 1f);
                spawnPoint = new Vector3(18.5f, 0, -18.5f);
                break;
            default:
                color = new Color(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255), 1);
                spawnPoint = new Vector3(0, 1, 0);
                break;
        }

        //Setup player and caputre area
        GetComponent<Renderer>().material.SetColor("_Color", color);
        spawnArea = GameManager.instance.InstantiateArea(spawnPoint);
        spawnArea.GetComponent<Area>().Init(id, color);

        controller.enabled = false;
        body.transform.position = spawnPoint;
        transform.position = spawnPoint;
        controller.enabled = true;

        //Create Missile since thier is guaranteed to be one
        missile = GameManager.instance.InstantiateMissile();
        missile.Init(id, new Vector3(0, 1, 0)); // todo maybe dont need shoot at passed on init / same for bullet

    }

    // Start is called before the first frame update
    private void Start()
    {
        //Movement will take place in fixed timestep so mutiply by fixedDeltatime to keep these values consistant with normal update
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime; // mutliply twice since its units are m/s^2 
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if (health <= 0f) // TODO test without since player is disabled when dead anyway
        {
            return;
        }
        
        //Take the bool move values and then convert that to a form the controller can understand
        Vector2 inputDirection = Convert(moveDirection); // change the movement from bool to vector2 form
        Vector3 direction = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        direction *= moveSpeed;

        //Only allow the player to jump while touching the ground
        if (controller.isGrounded)
        {
            velocity = 0f;
            if (moveDirection[4])
            {
                velocity = jumpSpeed;
            }
        }
        velocity += gravity;
        direction.y = velocity;
        controller.Move(direction);
        body.velocity = direction;

        //Send updated position and rotation to all clients
        Server.PlayerPosition(this);
        //Server.PlayerRotation(this); // TODO remove on client and server

        //Update every active bullet to set roation and detect a timeout
        foreach (Bullet bullet in bullets)
        {
            if (bullet.gameObject.activeSelf)
            {
                bullet.Move();
                Server.MoveBullet(bullet); // Send bullet info to all players
            }
        }

        //Update the missile if its active and calculate the closest player
        if (missile.gameObject.activeSelf)
        {
            missile.Move();
            Server.MoveMissile(missile);// Send missile info to all players
        }

    }

    private Vector2 Convert(bool[] direction)
    {
        //Convert the bool array into a vector we can actually use
        Vector2 inputDirection = Vector2.zero;
        if (direction[0])
        {
            inputDirection.y += 1;
        }
        if (direction[1])
        {
            inputDirection.y -= 1;
        }
        if (direction[2])
        {
            inputDirection.x -= 1;
        }
        if (direction[3])
        {
            inputDirection.x += 1;
        }

        return inputDirection;
    }
    //Update move direction to what was recieved from the client
    public void SetMovementDirction(int newMessageID, bool[] clientInputs, Quaternion rotation)
    {

        messageID = newMessageID;
        moveDirection = clientInputs;
        transform.rotation = rotation;
    }

    //player has requested to shoot a bullet so enbale the bullet and initilize it with the values recieved
    public void ShootMissile(Packet packet)
    {
        Vector3 direction = packet.ReadVector3();
        missile.body.velocity = new Vector3(0, 0, 0); // reset velcoity
        missile.transform.position = transform.position + (direction * 2); // have the bullet spawn far enough away from the players camera
        missile.transform.rotation = transform.rotation;
        missile.shootDirection = direction; // if a closest player cant be found then it will travel in shoot direction instead
        missile.Enable();
    }

    public void PlayerMovement(Packet packet)
    {

        int messageID = packet.ReadInt(); // Message ID is used for interpolation on the client side
        bool[] movements = new bool[packet.ReadInt()]; //read the player movements
        for (int i = 0; i < movements.Length; i++)
        {
            movements[i] = packet.ReadBool();

        }
        Quaternion rotation = packet.ReadQuaternion(); // read rotation

        Dispatcher.instance.AddAction(() => SetMovementDirction(messageID, movements, rotation)); // appply all of this on the main thread
    }

    public void ShootBullet(Packet packet)
    {
        Vector3 direction = packet.ReadVector3();

        if (bullets.Count > bulletMax) // once the player has reached the bullet max we want to start forcing old bullets to be reused
        {
            //Keeps i in a range of bullet max
            disableIndex = disableIndex % bulletMax;

            bullets[disableIndex].Disable(); // force disable that bullet
            disableIndex++; // iterate i so that we arent always disabling the same bullet
        }
        
        //Search all already spawned bullets for one that is disabled
        foreach (Bullet bullet in bullets)
        {
            if (!bullet.gameObject.activeSelf)
            {
                //Then set up the bullet with new client data
                bullet.transform.position = transform.position + (direction * 2);
                bullet.transform.rotation = transform.rotation;
                bullet.shootDirection = direction;
                bullet.Enable();
                return;
            }
        }

        //if no new bullet can be found then created a new bullet and initilize it
        Bullet newBullet = GameManager.instance.InstantiateBullet(transform.position + (direction * 2), transform.rotation);
        newBullet.Init(id, bullets.Count, direction);
        newBullet.Enable();
        bullets.Add(newBullet);
    }

    public void TakeDamage(float damage)
    {
        if (health <= 0f) // TODO maybe not need as player shouldnt be hit once they are dead and health will be reset anyways
        {
            return;
        }

        health -= damage; // decrement health
        if (health <= 0f) // check if player has died
        {
            //Disable everything about the player
            health = 0f;
            controller.enabled = false;
            body.useGravity = false;
            body.transform.position = spawnPoint;
            body.Sleep();
            transform.position = spawnPoint;
            Server.PlayerPosition(this); // send player back to thier spawnPoint
            StartCoroutine(Respawn()); // Respawn after a certain time
        }

        Server.PlayerHealth(this); //the client can calcualte when they have died themself so only send health
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5f);

        Debug.Log("Player Respawned");
        //Reset everything about the player
        health = 100;
        body.useGravity = true;
        body.transform.position = spawnPoint;
        controller.enabled = true;
        body.WakeUp();
        Server.PlayerRespawn(this); //How long the player stays dead is controlled by the server so the server must explicitly tell the player to respawn
    }

}
