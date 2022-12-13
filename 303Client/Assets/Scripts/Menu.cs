using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{

    //Instance implementation so that the class can be acessed from anywhere but also only have one version of itself
    public static Menu instance;

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

    //UI elements
    public GameObject panel;
    public GameObject chat;
    public GameObject win;
    public GameObject lose;
    public GameObject ui;
    public Text timeText;
    public Text tcpLatencyText;
    public Text udpLatencyText;

    public Slider health;

    public InputField username;
    public InputField message;
    public InputField serverPort;
    public InputField IP;

    //Prefabs
    public GameObject messagePrefab;

    //Member Variables
    [SerializeField] int MessageMax = 20; // How many Messages that can be displayed at once
    List<GameObject> previousMessages = new List<GameObject>();
    private Client client = new Client();


    public void Start()
    {
        //Turn of unnecessary UI for now
        lose.SetActive(false);
        win.SetActive(false);
        ui.SetActive(false);

        //Set up Health Bar
        health.maxValue = Player.maxHealth;
        health.value = Player.maxHealth;
      
    }

    public void Connect()
    {
        //Disable Start Menu
        panel.SetActive(false);
        username.interactable = false;
        serverPort.interactable = false;

        //Eneable Health and Chat
        ui.SetActive(true);



        int port = 5005; //Defualt port
        if (int.TryParse(serverPort.text, out int portResult)) // Try to read int values from string
        {
            Debug.Log(portResult);
            if (portResult > 0 && portResult <= 65535) //Only allow valid ports
            {
                port = portResult;
            }
            else
            {
                Debug.Log("Invalid Port Input");
            }
        }


        string ip = "127.0.0.1"; // "0.0.0.0"
        string[] stringArray = IP.text.Split('.');
        int[] intArrray = new int[stringArray.Length];
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
                    Debug.Log("Client Connected on Port: " + port + " with an IP of " + ip);
                    client.Connect(ip, port, username.text); //Begin Connecting on the entered port and IP
                    return;
                }
                ip = IP.text;
            }
        }

        Debug.Log("Client Connected on Port: " + port + " with an IP of " + ip);
        client.Connect(ip, port, username.text); //Begin Connecting on the entered port and IP

    }

    //Update UI
    public void UpdateHealthBar(float newHealth)
    {
        health.value = newHealth;
    }

    public void UpdateGameTime(float time)
    {
        timeText.text = "Game Time: " + time;
    }

    public void UpdateTcpLatency(float latency)
    {
        tcpLatencyText.text = "Tcp Latency: " + latency + "ms";
    }

    public void UpdateUdpLatency(float latency)
    {
        Debug.Log(latency);
        udpLatencyText.text = "Udp Latency: " + latency + "ms";
    }

    public void RecieveMessage(Packet packet)
    {
    
        if(previousMessages.Count > MessageMax) // Deleted oldest message if max is reacged
        {
            Destroy(previousMessages[0]);
            previousMessages.Remove(previousMessages[0]);
        }

        //Instaisiate new text object a
        GameObject newMessage = Instantiate(messagePrefab, chat.transform);
        Text text = newMessage.GetComponent<Text>();

        int id = packet.ReadInt();
        string message = packet.ReadString();

        text.text = Client.players[id].username + ": " + message; // get the username of the sender nd set the text to the recieved message
        previousMessages.Add(newMessage);

    }

    public void SendMessage()
    {
        client.SendMessage(message.text);
        message.text = ""; //Empty input field after message has been set

    }

    public void Quit() // Called from end screen
    {
        Application.Quit();
    }

    private void OnApplicationQuit() // Called whenever progeram quits 
    {
        client.Disconnect(); //Properly Close all tcp and upd connections
    }

    public void DecideWinner(Packet packet, int id)
    {
        int winnerID = packet.ReadInt();
        Debug.Log("The Winner is Player " + id);
        foreach (KeyValuePair<int, Player> player in Client.players)
        {
            player.Value.gameObject.SetActive(false);
        }


        //Unlock The mouse
        Cursor.visible = !Cursor.visible;
        Cursor.lockState = CursorLockMode.None;


        //Display the appropriote End Screen
        if (winnerID == id)
        {
          
            win.SetActive(true);
        }
        else
        {
            lose.SetActive(true);
        }
    }

}
