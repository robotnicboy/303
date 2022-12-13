using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine;

public enum ServerPacketsType
{
    Conformation = 0,
    SpawnPlayer,
    PlayerPosition,
    PlayerRotation,
    PlayerDisconnected,
    PlayerHealth,
    PlayerRespawned,
    BulletPosition,
    BulletSpawned,
    MessageBroadcast,
    BulletDisabled,
    GameTime,
    MissileMove,
    SpawnMissile,
    DisableMissile,
    TcpPing,
    UdpPing,
    BallPosition,
    BallColor,
    Winner
}
public enum ClientPacketType
{
    Conformation = 0,
    PlayerMovement,
    BulletShoot,
    Message,
    MissileShoot,
    TcpPing,
    UdpPing
}


public class Server : MonoBehaviour
{
    //Member Variables Default
    public static int MaxPlayers = 4;
    public static Client[] clients;
    public static int Port = 2002; 
    public static float GameTime = 0f;

    private static TcpListener tcpListener;
    private Thread tcpListenerThread;

    public static UdpClient udpListener; //TODO maybe dont need static / make a getter
    private Thread udpListenerThread;

    public void StartServer(string ip , int port, int max)
    {
        //Initilize Server Values
        MaxPlayers = max;
        Port = port;
        GameTime = 0f;

        clients = new Client[MaxPlayers]; //Create a Client variable for each client that could join

        InitClients(); // Initilize the client ID's

        //A thread is used when handling both tcp and udp, connecting and recieving data since we are using asynchronous functions
        //These functions can be called any time and have the possibility of returning an error
        //We dont want this to effect the main code which deals with game objects so we use a thread


        //Make the threads background so that if the server closes then this thread can continue, this is important because we dont want to close any sockets if we are waiting asycronously
        //After detecting that the clients have disconted the asycronous will return and then properly close
        tcpListenerThread = new Thread(new ThreadStart(TcpListen));
        tcpListenerThread.IsBackground = true; 
        tcpListenerThread.Start(); // Begin Listening for new tcp connections

        udpListenerThread = new Thread(new ThreadStart(UdpListen));
        udpListenerThread.IsBackground = true; 
        udpListenerThread.Start(); // Begin Listening for new udp connections and data recieved

        InvokeRepeating("RePing", 2.0f, 5f); // we want to ping each client every 5 seconds to get thier latency
    }

    void InitClients()
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            clients[i] = new Client(i);
        }
    }

    private void UdpListen()
    {
        try
        {
            //Wait for a udp message
            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UdpConnectCallBack, null);
        }
        catch (SocketException error)
        {
            Debug.Log(error);
        }
    }

    private static void UdpConnectCallBack(IAsyncResult result)
    {

        try
        {
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpListener.EndReceive(result, ref sender); // properly end recieve based off the result
            udpListener.BeginReceive(UdpConnectCallBack, null); // recursivly call connect to proccess new connections

            if (data.Length < 4) // the packet must be invalid 
            {
                Debug.Log("Packet is too small for valid data");
                return;
            }

            Packet packet = new Packet(data); // convert the data into packet form to be read
            int id = packet.ReadInt(); // get the client id who sent it

            if (id == -1 || id >= MaxPlayers)// if the client id is invalid the igonre the message
            {

                Debug.Log("Invalid ID: " + id);
                return;
            }

            if (clients[id].GetIPEndPoint() == null)// if this is the first time the client is connecting with udp then set up the client udp data
            {
                // If this is a new connection
                Debug.Log("Client " + id + " has connected using udp");
                clients[id].UdpConnect(sender);
                return;
            }

            if (clients[id].GetIPEndPoint().ToString() == sender.ToString()) 
            {
                // Ensures that the client is not being impersonated by another by sending a false clientID
                clients[id].UnpackUdpData(packet);
            }

        }
        catch (Exception error)
        {
            Debug.Log("Error receiving UDP data: " + error);
        }
    }


    private void TcpListen()
    {
        try
        {
            // Create listener and wait for a new connections
            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(TcpConnectCallBack, null); // begin a async connect 
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    private static void TcpConnectCallBack(IAsyncResult result)
    {

        TcpClient tcpClient = tcpListener.EndAcceptTcpClient(result); //accept new client
        tcpListener.BeginAcceptTcpClient(TcpConnectCallBack, null); // begin waiting for a new client connection again asynchronous
        Debug.Log("Client Connected");

        foreach (Client client in clients)
        {
           if(client.getSocket() == null)
            {
                Debug.Log("Client given ID: " + client.GetID());
                client.TcpConnect(tcpClient);
                break;
            }
        }
    }

    //TCP is used for any packets that we know wont be sent often and onces we want to guarantee make it 
    #region ServerTCPSendToAll

    public static void SpawnBullet(Bullet bullet)
    {
        Packet packet = new Packet((int)ServerPacketsType.BulletSpawned);

        //Send bullet info to spawn on all clients
        packet.Write(bullet.playerID);
        packet.Write(bullet.id);
        packet.Write(bullet.transform.position);
        packet.Write(bullet.transform.rotation);

        SendDataToAll(packet);

    }

    public static void DisableBullet(Bullet bullet)
    {
        Packet packet = new Packet((int)ServerPacketsType.BulletDisabled);

        //Disable bullet on all clients
        packet.Write(bullet.playerID);
        packet.Write(bullet.id);

        SendDataToAll(packet);
    }

    public static void SpawnMissile(Missile missile)
    {
        Packet packet = new Packet((int)ServerPacketsType.SpawnMissile);

        //Send missile info to spawn on all clients
        packet.Write(missile.playerID);
        packet.Write(missile.transform.position);
        packet.Write(missile.transform.rotation);

        SendDataToAll(packet);
    }

    public static void DisableMissile(Missile missile)
    {
        //Disable missile on all clients
        Packet packet = new Packet((int)ServerPacketsType.DisableMissile);
        packet.Write(missile.playerID);
        SendDataToAll(packet);
    }

    public static void PlayerHealth(Player player)
    {
        Packet packet = new Packet((int)ServerPacketsType.PlayerHealth);
        
        //Send the health of a specific player to all clients
        packet.Write(player.id);
        packet.Write(player.health);

        SendDataToAll(packet);
    }

    public static void PlayerRespawn(Player player)
    {
        Packet packet = new Packet((int)ServerPacketsType.PlayerRespawned);

        //Respawn a specific player on all clients
        packet.Write(player.id);
        SendDataToAll(packet);

    }

    public static void Winner(int winnerID)
    {
        Packet packet = new Packet((int)ServerPacketsType.Winner);
        //Declare a winner and send thier id to all clients
        packet.Write(winnerID);
        SendDataToAll(packet);

    }

    public static void SendMessageToAll(Packet message)
    {

        Packet packet = new Packet((int)ServerPacketsType.MessageBroadcast);
        //Read the message recieved from the client and then redistribute that message to everyone else (inluding themself) with the id of who sent it
        packet.Write(message.ReadInt());
        packet.Write(message.ReadString());

        SendDataToAll(packet);

    }

    public void RePing()
    {
        foreach (Client client in clients)
        {
            if (client.player != null)
            {
                client.Ping();
            }
        }
    }

    #endregion

    //UDP is used when we are sending many messages regularly and when we dont perticullarly care about guarantee the order they arrive
    //UDP dosent have cogestion handling built in though, so we still dont want to overload the udp socket
    //Therefor we only send messages in Fixed Update at 30 ticks per second, eg 30 messages per second for each kind

    #region ServerUDPSendToAll

    public static void PlayerPosition(Player player)
    {
        Packet packet = new Packet((int)ServerPacketsType.PlayerPosition);
        
        //Send all data need to process a player movement to all clients
        packet.Write(player.id);
        packet.Write(player.messageID);
        packet.Write(player.transform.position);
        packet.Write(player.transform.rotation);
        packet.Write(GameTime);

        SendDataToAll(packet, false);

    }

    public static void SpherePosition(GameObject ball)
    {
        Packet packet = new Packet((int)ServerPacketsType.BallPosition);

        //Send the ball transfrom data as well as the current game time for prediction to all clients
        packet.Write(GameTime);
        packet.Write(ball.transform.position);
        packet.Write(ball.transform.rotation);

        SendDataToAll(packet, false);

    }

    public static void PlayerRotation(Player player) // TODO remove
    {
        Packet packet = new Packet((int)ServerPacketsType.PlayerRotation);

        packet.Write(player.id);
        packet.Write(player.transform.rotation);

        SendDataToAll(packet, false);

    }


    public static void MoveBullet(Bullet bullet)
    {
        Packet packet = new Packet((int)ServerPacketsType.BulletPosition);

        //Send the bullet transfrom data as well as the current game time for prediction to all clients
        packet.Write(bullet.playerID);
        packet.Write(bullet.id);
        packet.Write(GameTime);
        packet.Write(bullet.transform.position);
        packet.Write(bullet.transform.rotation); 

        SendDataToAll(packet, false);

    }



    public static void MoveMissile(Missile missile)
    {
        Packet packet = new Packet((int)ServerPacketsType.MissileMove);

        //Send the missile transfrom data as well as the current game time for prediction to all clients
        packet.Write(missile.playerID);
        packet.Write(GameTime);
        packet.Write(missile.transform.position);
        packet.Write(missile.transform.rotation);

        SendDataToAll(packet, false);

    }

    public static void BallColour(Color color)
    {
        Packet packet = new Packet((int)ServerPacketsType.BallColor);
        //send the color of the ball to all clients
        packet.Write(color);
        SendDataToAll(packet, false);

    }

    #endregion



    public void Update() // todo maybe dont need this here / move else where
    {
        GameTime += Time.deltaTime;
    }

    //This function allows either Udp or Tcp to be sent
    private static void SendDataToAll(Packet packet, bool SendProtocol = true)
    {
        if (SendProtocol)
        {
            packet.WriteLength();
            for (int i = 0; i < MaxPlayers; i++)
            {
               clients[i].SendTCPData(packet);
            }
        }
        else
        {
            packet.WriteLength();
            for (int i = 0; i < MaxPlayers; i++)
            {
                clients[i].SendUDPData(packet);
            }
        }
    }
    
    //We want all sockets to be closed properly so disconect every socket when quiting
    private void OnApplicationQuit()
    {
        foreach(Client client in clients)
        {
            client.Disconnect();
        }

        tcpListener.Stop(); // clears the beginConnect of any pending connections and notifies them that the connect is invalid now
        udpListener.Close();
    }

}
