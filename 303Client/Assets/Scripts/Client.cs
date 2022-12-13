using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
    MissilePosition,
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

public class Client {


    public static Dictionary<int, Player> players = new Dictionary<int, Player>(); 

    //Client variables
    public static int bufferSize = 1024; 
    public static int id = 0;
    public int Port = 2002;
    public string IP = "127.0.0.1";
    private string username;


    Thread clientReceiveThread;

    //Tcp Variables
    private static TcpClient TcpSocket;
    private static NetworkStream stream;
    private Packet TcpReceivedData;
    private byte[] TcpReceiveBuffer;

    //Udp Variables
    private static UdpClient UdpSocket;
    private IPEndPoint endPoint;



    public void Connect(string ip, int port, string name)
    {
        //Set up client and server data
        IP = ip;
        Port = port;
        username = name;

        endPoint = new IPEndPoint(IPAddress.Parse(IP), Port);

        try
        {
            //A thread is used when handling tcp connecting and recieving data since we are using asynchronous functions
            //These functions can be called any time and have the possibility of returning an error
            //We dont want this to effect the main code which deals with game objects so we use a thread

            clientReceiveThread = new Thread(new ThreadStart(ConnectToServer));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception error)
        {
            Debug.Log("Error Connecting to Server: " + error); 
        }

    
    }

    #region TCP
    public void ConnectToServer()
    {
        //Initilize new Tcp socket
        TcpSocket = new TcpClient
        {
            ReceiveBufferSize = bufferSize,
            SendBufferSize = bufferSize
        };

        Debug.Log("Begin connecting");
        TcpReceiveBuffer = new byte[bufferSize];
        TcpSocket.BeginConnect(IP, Port, TcpConnectCallBack, TcpSocket); // Attempt To connect to the Server Asynchronously
    }

    private void TcpConnectCallBack(IAsyncResult result)
    {
        TcpSocket.EndConnect(result); //Ends connections attempt based of the result

        if (!TcpSocket.Connected) //return if the connection failed
        {
            Debug.Log("Connection Failed");
            //TODO maybe disconect here
            return;
        }

        Debug.Log("Connection Successful");

        // Initilize stream and packet
        stream = TcpSocket.GetStream(); 
        TcpReceivedData = new Packet(); 

        stream.BeginRead(TcpReceiveBuffer, 0, bufferSize, ListenCallBack, null); //Begin Waiting to recieve a packet from the erver Asynchronously

    }

    private void ListenCallBack(IAsyncResult result)
    {
        try
        {
            Debug.Log("Packet Recieved From the Server");
            int length = stream.EndRead(result);
            if (length < 1) //If invalid packet length then disconect as something has gone wrong with the connection itself vs sending an incorrect packet
            {
                Disconnect();
                return;
            }

            byte[] data = new byte[length]; // declare a data array the exact size of the message recieved
            Array.Copy(TcpReceiveBuffer, data, length); // copy the data recieved

            UnpackTcpData(data); //pass the exact data and hanlde it
            stream.BeginRead(TcpReceiveBuffer, 0, bufferSize, ListenCallBack, null); // go back to waiting for another message
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
            Disconnect();
        }
    }

    private void UnpackTcpData(byte[] data)
    {

        //Initilize Values
        int length = 0;
        TcpReceivedData.SetBytes(data);

        if (TcpReceivedData.UnreadLength() >= 4) // We want to make sure that the packet recieved is valid
        {
            length = TcpReceivedData.ReadInt();
            if (length <= 0)
            {
                TcpReceivedData.Reset(); // Reset receivedData instance to allow it to be reused
                return;
            }
        }

        while (length > 0 && length <= TcpReceivedData.UnreadLength()) // There may be more than one packet waiting in the received data so must loop through all the data
        {
            Packet packet = new Packet(TcpReceivedData.ReadBytes(length)); // copy just one packet from the recieved data into its own packet
            int packetID = packet.ReadInt(); // get what message type the packet is

            Debug.Log("Tcp Packet Recieved: " + packetID + " with a length: " + length);

            //Run the corrisponding function for that packet type
            switch (packetID)
            {
                case (int)ServerPacketsType.Conformation:
                    ConformationRecieved(packet);
                    break;
                case (int)ServerPacketsType.SpawnPlayer:
                    Dispatcher.instance.AddAction(() => SpawnPlayer(packet));
                    break;
                case (int)ServerPacketsType.PlayerHealth:
                    Dispatcher.instance.AddAction(() => players[packet.ReadInt()].SetHealth(packet.ReadFloat()));
                    break;
                case (int)ServerPacketsType.PlayerRespawned:
                    Dispatcher.instance.AddAction(() => players[packet.ReadInt()].Respawn());
                    break;
                case (int)ServerPacketsType.BulletSpawned:
                    Dispatcher.instance.AddAction(() => players[packet.ReadInt()].SpawnBullet(packet));
                    break;
                case (int)ServerPacketsType.BulletDisabled:
                    Dispatcher.instance.AddAction(() => players[packet.ReadInt()].DisableBullet(packet));
                    break;
                case (int)ServerPacketsType.MessageBroadcast:
                    Dispatcher.instance.AddAction(() => Menu.instance.RecieveMessage(packet));
                    break;
                case (int)ServerPacketsType.GameTime:
                    setGameTime(packet);
                    break;
                case (int)ServerPacketsType.SpawnMissile:
                    Dispatcher.instance.AddAction(() => players[packet.ReadInt()].SpawnMissile(packet));
                    break;
                case (int)ServerPacketsType.DisableMissile:
                    Dispatcher.instance.AddAction(() => players[packet.ReadInt()].missile.gameObject.SetActive(false));
                    break;
                case (int)ServerPacketsType.TcpPing:
                    Ping(packet, true);
                    break;
                case (int)ServerPacketsType.Winner:
                    Dispatcher.instance.AddAction(() => Menu.instance.DecideWinner(packet, id));
                    break;
                default:
                    Debug.Log("Invalid Packet ID Recieved: " + packetID);
                    break;
            }

            //After Reading a packet from the recieved data, check if there is another valid packet ready to read

            length = 0; ; // Reset packet length
            if (TcpReceivedData.UnreadLength() >= 4) 
            {
                // If client's received data contains another packet
                length = TcpReceivedData.ReadInt();
                if (length <= 0) //if the length of the packet is invalid then something has gone wrong on the server and not with the connection, so just ignore packet
                {
                    Debug.Log("Packet contains no data");
                    TcpReceivedData.Reset(); // Reset receivedData instance to allow it to be reused
                    return;
                }
            }

        }

        if (length <= 1)
        {
            TcpReceivedData.Reset(); // Reset receivedData instance to allow it to be reused
            return; 
        }

    }

    #endregion

    #region UDP

    public void UdpConnect(int port)
    {
        if(endPoint == null) //Make sure endPoint is set
        {
            Debug.Log("endPoint is null");
        }

        try
        {
            UdpSocket = new UdpClient(port);

            UdpSocket.Connect(endPoint); 
            UdpSocket.BeginReceive(UdpConnectCallback, null); // Begin Waiting For data

            Debug.Log("Connecting to Server Using Udp");
            Packet packet = new Packet();
            SendUDPData(packet); // Send a single packet with this clients id, to initlize the udp socket and end point on the server

        }  catch(Exception error)
        {
            Debug.Log(error);
        }
    }

    private void UdpConnectCallback(IAsyncResult result)
    {
        try
        {
            byte[] data = UdpSocket.EndReceive(result, ref endPoint); // read recieved data
            UdpSocket.BeginReceive(UdpConnectCallback, null); // Begin waiting for new data asyncronously while we process the new packet

            if (data.Length < 4) // Check if the packet is valid
            {
                Disconnect();
                return;
            }

            UnpackUdpData(data); // Unpack the Data
        }
        catch
        {
            Disconnect();
        }
    }


    public void UnpackUdpData(byte[] data)
    {
        Packet packet = new Packet(data); //convert the data to packet form

        int length = packet.ReadInt();
        int packetID = packet.ReadInt();

        //Run the corrisponding function for that packet type
        switch (packetID)
        {
            case (int)ServerPacketsType.PlayerPosition:
                Dispatcher.instance.AddAction(() => players[packet.ReadInt()].PlayerPosition(packet));
                break;
            case (int)ServerPacketsType.PlayerRotation:
                Dispatcher.instance.AddAction(() => players[packet.ReadInt()].PlayerRotation(packet));
                break;
            case (int)ServerPacketsType.BulletPosition:
                Dispatcher.instance.AddAction(() => players[packet.ReadInt()].BulletPosition(packet));
                break;
            case (int)ServerPacketsType.MissilePosition:
                Dispatcher.instance.AddAction(() => players[packet.ReadInt()].MissilePosition(packet));
                break;
            case (int)ServerPacketsType.BallPosition:
                Dispatcher.instance.AddAction(() => GameManager.instance.BallPosition(packet));
                break;
            case (int)ServerPacketsType.BallColor:
                UnityEngine.Color color = packet.ReadColor();
                Dispatcher.instance.AddAction(() => GameManager.instance.ball.GetComponent<Renderer>().material.SetColor("_Color", color));
                break;
            case (int)ServerPacketsType.UdpPing:
                Ping(packet, false);
                break;
            default:
                Debug.Log("Invalid Packet ID Recieved: " + packetID);
                break;
        }
    }

    #endregion

    #region Send

    void Ping(Packet recievedPacket,bool protocol)
    {
        //Read sample data but dont process anything
        float sample = recievedPacket.ReadFloat();
        float sample2 = recievedPacket.ReadFloat();
        float sample3 = recievedPacket.ReadFloat();

        if (protocol) // if its a tcp ping then send back using tcp
        {
            Packet packet = new Packet((int)ClientPacketType.TcpPing);
            packet.Write(sample);
            packet.Write(sample2);
            packet.Write(sample3);
            SendTCPData(packet);
        }
        else // if its a udp ping then send back using udp
        {
            Packet packet = new Packet((int)ClientPacketType.UdpPing);
            packet.Write(sample);
            packet.Write(sample2);
            packet.Write(sample3);
            SendUDPData(packet);
        }

     
    }

    private void Conformation()
    {
        try
        {
            //Send this clients username and id to the server
            Packet packet = new Packet((int)ClientPacketType.Conformation);
            packet.Write(id);
            packet.Write(username);
            SendTCPData(packet);
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

    }

    public static void PlayerMovement(int id, int messageID, bool[] playerMovements) 
    {
        //Send the movements to the Server
        Packet packet = new Packet((int)ClientPacketType.PlayerMovement);
        packet.Write(messageID);
        packet.Write(playerMovements.Length);
        foreach (bool input in playerMovements)
        {
            packet.Write(input);
        }
        packet.Write(players[id].transform.rotation);


        SendUDPData(packet);
    }

    public static void PlayerShoot(Vector3 forward)
    {
        //Shoot Bullet in aiming direction
        Packet packet = new Packet((int)ClientPacketType.BulletShoot);
        packet.Write(forward);
        SendTCPData(packet);
    }

    public static void PlayerMissileShoot(Vector3 forward)
    {
        //Shoot Missile in aiming direction
        Packet packet = new Packet((int)ClientPacketType.MissileShoot);
        packet.Write(forward);
        SendTCPData(packet);
    }

    public void SendMessage(string message) // TODO maybe change to menu script and just make send functions static
    {
        //Send Message to all players
        Packet packet = new Packet((int)ClientPacketType.Message);
        packet.Write(id);
        packet.Write(message);
        SendTCPData(packet);
    }

    #endregion

    #region Recieve

    private void ConformationRecieved(Packet packet)
    {
        //Read Data from the packet
        string message = packet.ReadString();
        int clientID = packet.ReadInt();

        Debug.Log("Message from server: " + message);
        id = clientID;
        Conformation(); // Send conformation back to the server to validate the connection

        //We connect through udp by using the tcp socket data, so we already know its correcy
        UdpConnect(((IPEndPoint)TcpSocket.Client.LocalEndPoint).Port);
    }

    void setGameTime(Packet packet)
    {
        //Read Data from the packet
        float time = packet.ReadFloat();
        float latency = packet.ReadFloat();
        bool protocol = packet.ReadBool();

        GameManager.GameTime = time; // synchronize the game time with the server

        //Update appropriate  ui element
        if (protocol) 
        {
            Dispatcher.instance.AddAction(() => Menu.instance.UpdateTcpLatency(latency));
        }
        else
        {
            Dispatcher.instance.AddAction(() => Menu.instance.UpdateUdpLatency(latency));
        }

    }

    public void SpawnPlayer(Packet packet)
    {
        //Read Data from the packet
        int playerID = packet.ReadInt();
        string username = packet.ReadString();
        Vector3 position = packet.ReadVector3();

        GameObject player;
        if (playerID == id) //if the player is controlled by this client
        {
            player = GameManager.instance.InstantiatePlayer(position);
        }
        else // the player is controlled by another client
        {
            player = GameManager.instance.InstantiateEnemy(position);
        }

        //Set up player
        player.GetComponent<Player>().Init(playerID, username);
        players.Add(playerID, player.GetComponent<Player>());

    }

    #endregion


    //Tcp and Udp underline send and recieve functions

    public static void SendTCPData(Packet packet)
    {

        packet.WriteLength();

        try
        {
            if (TcpSocket != null)
            {
                stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null); // Send data to server
            }
        }
        catch (Exception error)
        {
            Debug.Log("Tcp failed to send data: " + error);
        }
    }

    public static void SendUDPData(Packet packet)
    {
        try
        {
            packet.WriteLength();
            packet.InsertInt(id); // Insert the client's ID at the start of the packet
            if (UdpSocket != null)
            {
                UdpSocket.BeginSend(packet.ToArray(), packet.Length(), null, null);
            }
        }
        catch (Exception error)
        {
            Debug.Log("Udp failed to send data: " + error);
        }
    }


    public void Disconnect()
    {

        if (TcpSocket != null)
            TcpSocket.Close();

        if (UdpSocket != null)
            UdpSocket.Close();

        stream = null;
        TcpReceivedData = null;
        TcpReceiveBuffer = null;
        TcpSocket = null;

        endPoint= null;
        UdpSocket = null;   

        Debug.Log("Disconnected from server.");
    }
}
