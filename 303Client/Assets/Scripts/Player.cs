
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Player : Object
{
    //Member Variables
    public MeshRenderer model;
    public int id;
    public string username;
    private float health;
    public static float maxHealth = 100;

    //Weapon Variables
    public Dictionary<int, Object> bullets = new Dictionary<int, Object>(); //TODO make disctionary
    public Object missile;


    public void Init(int playerID, string playerName)
    {
        id = playerID;
        username = playerName;
        health = maxHealth;

        if (gameObject.GetComponent<Controller>() != null) // if this is the user controlled player then initlize the controller
        {
            gameObject.GetComponent<Controller>().Init(playerID);
        }

        //Set color based of order of joined
        switch (id)
        {
            case 0:
                GetComponent<Renderer>().material.SetColor("_Color", Color.red);
                break;
            case 1:
                GetComponent<Renderer>().material.SetColor("_Color", Color.green);
                break;
            case 2:
                GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
                break;
            case 3:
                GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 0f, 1f, 1f));
                break;
            default:
                GetComponent<Renderer>().material.SetColor("_Color", new Color(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255), 1));
                break;
        }

        // Instasiate the missile straight away since there is only one
        missile = GameManager.instance.InstantiateMissile(new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Object>(); 
        missile.gameObject.SetActive(false);

    }

    public void Update()
    {

        LinearPrediction();

        //Update each active bullets position client side only using Linear Prediction
        foreach(var bullet in bullets)
        {

            if(bullet.Value.gameObject.activeSelf)
            {
                bullet.Value.LinearPrediction();
            }
        }

        //Update the missiles position client side only using Quadratic Prediction
        if (missile.gameObject.activeSelf)
        {
            missile.QuadraticPrediction();
        }

        //Regenerate Lost Health Over time 
        health += 1 * Time.deltaTime; // TODO should be handles server side
        if (gameObject.GetComponent<Controller>() != null) // if this is the user controlled player then update the ui
        {
            Menu.instance.UpdateHealthBar(health);
        }
    }


    #region ServerPacketHandlers

    //Server handles damage detection so client only needs to update the value accordinly
    public void SetHealth(float newHealth)
    {
        health = newHealth;
        if (gameObject.GetComponent<Controller>() != null) // if this is the user controlled player then update the ui
        {
            Menu.instance.UpdateHealthBar(health);
        }
       
        // Detect If the player has died to save the server needing to send another packet
        if (health <= 0) 
        {
            model.enabled = false;
            gameObject.SetActive(false);
        }

    }

    // How long the player stays dead is handled by the player so player will only respawn after recieving conformation
    public void Respawn() 
    {
        Debug.Log("Player Respawned");
        model.enabled = true;
        SetHealth(maxHealth);
        gameObject.SetActive(true);
        if (gameObject.GetComponent<Controller>() != null) // if this is the user controlled player then update the ui
        {
            Menu.instance.UpdateHealthBar(health);
        }
    }


    public void PlayerPosition(Packet packet)
    {
        try
        {
            int messageID = packet.ReadInt();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();
            float time = packet.ReadFloat();

           
            if (gameObject.GetComponent<Controller>() != null) // if this is the user controlled player then use interpolation
            {
                gameObject.GetComponent<Controller>().CheckMovement(messageID, position, rotation);
            }
            else
            {
                AddMessage(position, rotation, time);
            }


        }
        catch (Exception error)
        {
            Debug.Log("Error reading player position: " + error);
        }

    }

    public void PlayerRotation(Packet packet) //TODO maybe not even needed
    {

        try
        {
            Quaternion rotation = packet.ReadQuaternion();

            transform.rotation = rotation;
        }
        catch (Exception error)
        {
            Debug.Log("Error reading player rotation: " + error);
        }

    }


    //Spawn Bullet but only Instasiate a new bullet if there is none already created, disabled
    //And bullet limit is enforced on server
    public void SpawnBullet(Packet packet)
    {
        //Read Data From Packet
        int bulletID = packet.ReadInt();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();
        Vector3 direction;

        if (bullets.Count > bulletID) // Check if server is respawning an old bullet
        {
            bullets[bulletID].transform.position = position;
            bullets[bulletID].transform.rotation = rotation;
            bullets[bulletID].gameObject.SetActive(true);
            bullets[bulletID].ResetMessages(); //Reset old messages so prediction isnt using old data
            bullets[bulletID].AddMessage(position, rotation, GameManager.GameTime); // Add first position so that bullet can be seen infront of the player instantly
            direction = bullets[bulletID].transform.forward * 2f;
            bullets[bulletID].AddMessage(position + direction, rotation, GameManager.GameTime + Time.fixedDeltaTime);

            return;
        }


        //Create a new bullet
        Object newBullet = GameManager.instance.InstantiateBullet(position, rotation).GetComponent<Object>();
        newBullet.ResetMessages(); //Reset messages so prediction isnt using invalid data
        newBullet.AddMessage(position, rotation, GameManager.GameTime); // Add first position so that bullet can be seen infront of the player instantly
        direction = newBullet.transform.forward * 2f;
        newBullet.AddMessage(position + direction, rotation, GameManager.GameTime + Time.fixedDeltaTime);

        bullets.Add(bulletID,newBullet);

    }

    public void BulletPosition(Packet packet)
    {
        try
        {
            int bulletID = packet.ReadInt();

            float time = packet.ReadFloat();
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();


            bullets[bulletID].AddMessage(position, rotation, time);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    //Disable corresponding bullet
    public void DisableBullet(Packet packet)
    {
        int id = packet.ReadInt();
        bullets[id].gameObject.SetActive(false);
    }

    public void SpawnMissile(Packet packet)
    {
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        missile.transform.position = position;
        missile.transform.rotation = rotation;
        missile.ResetMessages();
        missile.gameObject.SetActive(true);

    }


    public void MissilePosition(Packet packet)
    {
        float time = packet.ReadFloat();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        missile.AddMessage(position, rotation, time);
    }


    #endregion

}
