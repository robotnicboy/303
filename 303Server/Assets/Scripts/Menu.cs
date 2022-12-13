using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class Menu : MonoBehaviour
{
    //UI elements
    public InputField serverPort;
    public InputField maxPlayers;
    public InputField IP;
    public GameObject panel;
    public GameObject server;

    public void StartServer()
    {
        //Turn of UI
        panel.SetActive(false);
        serverPort.interactable = false;
        maxPlayers.interactable = false;

        //Defualt Values 
        int max = 4;
        int port = 5005;
        string ip = "127.0.0.1"; // "0.0.0.0"

        if (int.TryParse(maxPlayers.text, out int maxResult)) // Try to read int values from string
        {
            if(maxResult > 0) // cant have less that one player
            {
                max = maxResult;
            }
            else
            {
                Debug.Log("Invalid Max Player Input");
            }
        }

        if (int.TryParse(serverPort.text, out int portResult))// Try to read int values from string
        {
            if (portResult > 0 && portResult <= 65535)//Only allow valid ports
            {
                port = portResult;
            }
            else
            {
                Debug.Log("Invalid Port Input");
            }
        }

 
        string[] stringArray = IP.text.Split('.');
        int[] intArrray= new int[stringArray.Length];
        if (intArrray.Length != 4)
        {
            Debug.Log("Invalid IP Input");
        }
        else
        {
            for (int i = 0; i < stringArray.Length; i++)
            {
                intArrray[i] = Int32.Parse(stringArray[i]);
                if (intArrray[i] < 0 || intArrray[i] > 255)
                {
                    Debug.Log("Invalid IP Input");
                    Debug.Log("Server Created on Port: " + port + " with a Player Max of " + max);
                    server.GetComponent<Server>().StartServer(ip, port, max); // Start Server on the entered port and IP
                    return;
                }
                ip = IP.text;
            }
        }


        Debug.Log("Server Created on Port: " + port + " with a Player Max of " + max);
        server.GetComponent<Server>().StartServer(ip, port, max); // Start Server on the entered port and IP

    }
    
      
}
