using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Message
{
    //Message Class is for store information about previous position data
    public Message(Vector3 newPosition, Quaternion newRotation, float newTime)
    {
        position = newPosition;
        rotation = newRotation;
        time = newTime;
    }
    public Vector3 position;
    public Quaternion rotation;
    public float time;
}


public class Object : MonoBehaviour
{
    protected List<Message> messages = new List<Message>();

    public void Start()
    {
        ResetMessages(); 
    }

    public void AddMessage(Vector3 position, Quaternion rotation, float time)
    {
        //insert the new message so that the newsest is always at the front
        messages.Insert(0,new Message(position, rotation, time));
        if (messages.Count > 3) // we wont ever need to store more than 3 messages for prediciton
        {
            messages.RemoveAt(messages.Count - 1); //Remove oldest message
        }
 
    }
    //Reset messages so we arent predicting of a objects previous life cycle
    public void ResetMessages()
    {
        messages.Clear();
    }

    //Linear prediction is used for objects that arenet accellerating e.g bullet
    public void LinearPrediction()
    {
        if(messages.Count > 1)
        {
            float timeDifference = GameManager.GameTime - messages[0].time;
            float time = timeDifference / Time.fixedDeltaTime;
            time += 1;
            transform.position = Vector3.LerpUnclamped(messages[1].position, messages[0].position, time);
            transform.rotation = Quaternion.SlerpUnclamped(messages[1].rotation, messages[0].rotation, time);

            return;
        }

        foreach (Message message in messages) // we dont want to have to wait 2 message to set the objects first position, so manually set the position to the newest data recieved
        {
            gameObject.transform.position = message.position;
            gameObject.transform.rotation = message.rotation;

            return;
        }

    }

    //Quadratic Prediction is used for objects which are accellerating but also arent experiencing sudden changes in momentum
    public void QuadraticPrediction() 
    {
        try 
        {
            if (messages.Count > 2)// make sure thier is enought message history to perform the prediction
            {

                Vector3 v = (messages[0].position - messages[1].position) / (messages[0].time - messages[1].time); // newest velocity
                Vector3 u = (messages[1].position - messages[2].position) / (messages[1].time - messages[2].time); // old velcoity

                Vector3 accelartation = (v - u) / (messages[0].time - messages[2].time);
                float time = GameManager.GameTime - messages[0].time; //the time will be allows us to predict the position on the server right now, creating a consistant game world
                Vector3 displacement = (v * time) + (0.5f * Mathf.Pow(time, 2) * accelartation);

                transform.position = Vector3.LerpUnclamped(messages[1].position, messages[1].position + displacement, time);
                transform.rotation = Quaternion.SlerpUnclamped(messages[1].rotation, messages[0].rotation, time);

                return;
            }

            foreach (Message message in messages) // we dont want to have to wait 3 message to set the objects first position, so manually set the position to the newest data recieved
            {
                gameObject.transform.position = message.position;
                gameObject.transform.rotation = message.rotation;

                return;
            }
        }
        catch
        {

        }
      
    }

}
