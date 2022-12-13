using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dispatcher : MonoBehaviour
{
    //In order to change gameObject attributes, code must be ran on the main thread
    //But since the listen and recieve functions are contained within a thread, it cant effect the game object
    //So this class stores an action that would be process in the thread, to then be executed later in the main thread
    //E.g Dispatcher.instance.AddAction(() => position = Vector3.zero)


    //Instance implementation so that the class can be acessed from anywhere but also only have one version of itself
    public static Dispatcher instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }

    public List<Action> pending = new List<Action>(); // list of all code needed to be ran on the main thread

    private void Update()
    {
        instance.InvokePending();
    }

    // add to the action list using a lock since threads will be acessing it
    public void AddAction(Action action)
    {
        lock (pending)
        {
            pending.Add(action);
        }
    }

    // in the main update function run all code
    public void InvokePending()
    {
        lock (pending)
        {
            foreach (Action action in pending)
            {
                action();
            }

            pending.Clear();
        }
    }

}
