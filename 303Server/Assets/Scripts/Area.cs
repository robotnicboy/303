using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Area : MonoBehaviour
{
    //Constant Member Variables
    private int playerID;
    private Color areaColor;
    private float timeToCapture = 10;

    private GameObject ball;
    private Color ballColor;
    private float time = 0;

    public void Init(int id, Color color)
    {
        playerID = id;
        areaColor = color;
    }

    void OnTriggerStay(Collider collider)
    {
        //If the Ball is within a players area
        if (collider.gameObject.CompareTag("Ball"))
        {
            if (ball == null)
            {
                ball = collider.gameObject; //copy a reference of the ball
                ballColor = collider.gameObject.GetComponent<Renderer>().material.GetColor("_Color"); // get the ball color
            }
               

            time += Time.deltaTime * 2; // mutiply time by 2 as its also being deducted in update

            //smoothly change color from defualt to the players color
            Color color = Color.Lerp(ballColor, areaColor, time / timeToCapture);
            collider.gameObject.GetComponent<Renderer>().material.SetColor("_Color", color);

            if(time > timeToCapture) // if the ball is has stay withing a player area for enought time then this player has won
            {
                Debug.Log("Winner");
                Server.Winner(playerID); // send who won to all players
                Application.Quit(); // Quit as the game has finished
            }
        }
    }

    public void FixedUpdate()
    {
        if (ball != null)
            Server.BallColour(Color.Lerp(ballColor, areaColor, time / timeToCapture));
    }

    private void Update()
    {
        // while the ball is not in the area, smooth back to the origional color
        if (ball != null)
        {
            time -= Time.deltaTime; 
            Color color = Color.Lerp(ballColor, areaColor, time / timeToCapture);
            ball.GetComponent<Renderer>().material.SetColor("_Color", color);

            if(time <= 0)
            {
                ball = null; // forget the ball object so that the timer dosent go below 0 but also that 2 areas arent effecting the color
            }

        }
     
    }

 

}
