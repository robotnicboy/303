
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;



public class Client
{

    //Tcp Variables
    private TcpClient socket;
    private Packet TcpReceivedData;
    private byte[] TcpReceiveBuffer;
    private NetworkStream stream;

    //Udp Variables
    IPEndPoint endPoint;

    public static int bufferSize = 1024 * 8;
    private int id;

    //Game variables
    public Player player;

    //Game Time Variables
    private DateTime tcpStartTime;
    private DateTime udpStartTime;

    public Client(int ID)
    {
        id = ID;
    }

    public void  TcpConnect(TcpClient client)
    {
        //Each Client will hanlde thier own socket
        
        //Initlize the tcp scockets and steam info
        socket = client;
        socket.ReceiveBufferSize = bufferSize;
        socket.SendBufferSize = bufferSize;

        stream = socket.GetStream();
        TcpReceivedData = new Packet();
        TcpReceiveBuffer = new byte[bufferSize];

        stream.BeginRead(TcpReceiveBuffer, 0, bufferSize, ListenCallBack, null); // Begin Asyyncronously waiting for data from thier client counterpart

        //Once the connection has been esablished server side and we are waiting for data in parallel, we then send and conformation to test the connection
        Debug.Log("Sending Welcome Message to Client " + id);
        SendConformation();
    }


    private void ListenCallBack(IAsyncResult result)
    {
        try
        {
            Debug.Log("Packet Recieved From Client " + id);

            int length = stream.EndRead(result);
            if (length < 1) //If invalid packet length then disconect as something has gone wrong with the connection itself vs sending an incorrect packet
            {
                Disconnect(); // if there was an error with the connection itself then disconect the client from the server
                return;
            }

            byte[] data = new byte[length]; // declare a data array the exact size of the message recieved
            Array.Copy(TcpReceiveBuffer, data, length); // copy the data recieved

            UnpackTcpData(data); // pass the exact data and hanlde it
            stream.BeginRead(TcpReceiveBuffer, 0, bufferSize, ListenCallBack, null); // go back to waiting for another message
        }
        catch (Exception exception)
        {
            Debug.Log(exception);
            Disconnect(); // if there was an error with the connection itself then disconect the client from the server
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
                case (int)ClientPacketType.Conformation:
                    Dispatcher.instance.AddAction(()=> ConformationRecieverd(packet));  
                    break;
                case (int)ClientPacketType.BulletShoot:
                    Dispatcher.instance.AddAction(() => player.ShootBullet(packet));
                    break;
                case (int)ClientPacketType.Message:
                    Dispatcher.instance.AddAction(() => Server.SendMessageToAll(packet));
                    break;
                case (int)ClientPacketType.MissileShoot:
                    Dispatcher.instance.AddAction(() => player.ShootMissile(packet));
                    break;
                case (int)ClientPacketType.TcpPing:
                    SendGameTime((DateTime.Now - tcpStartTime).Milliseconds, true);
                    break;
                default:
                    Debug.Log("Invalid Packet ID Recieved: " + packetID);
                    break;
            }

            //After Reading a packet from the recieved data, check if there is another valid packet ready to read

            length = 0; // Reset packet length
            if (TcpReceivedData.UnreadLength() >= 4)
            {
                // If client's received data contains another packet
                length = TcpReceivedData.ReadInt();
                if (length <= 0)
                {
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

    public void UdpConnect(IPEndPoint clientEndPoint)
    {
        endPoint = clientEndPoint;
    }
    public void UnpackUdpData(Packet recievedPacket)
    {
        //Udp packet can only be read one at a time so no need to check for multiple packets

        int length = recievedPacket.ReadInt(); // TODO no need to put length here could just use recieved packet
        Packet packet = new Packet(recievedPacket.ReadBytes(length));
        int packetID = packet.ReadInt();

        //Call appropriate udp function for the message type
        switch (packetID)
        {
            case (int)ClientPacketType.PlayerMovement:
                player.PlayerMovement(packet);
                break;
            case (int)ClientPacketType.UdpPing:
                SendGameTime((DateTime.Now - udpStartTime).Milliseconds, false);
                break;
            default:
                Debug.Log("Invalid Packet ID Recieved: " + packetID);
                break;
        }
    }


    #region TcpRecieve

    private void ConformationRecieverd(Packet packet)
    {
        //once we know that the connect is working, we want to ping the client a basic packet in order to measure thier latency
        tcpStartTime = DateTime.Now;
        Ping();

        //Read packet data
        int recievedID = packet.ReadInt();
        string username = packet.ReadString();

        Debug.Log("Client Connected successfully and is now player " + recievedID + " with username: " + username); // tODO test if this is needed
        if (id != recievedID)
        {
            Debug.Log("Clinet Has assumed the wrong ID(" + recievedID + ") != " + id);
        }
        SpawnNewPlayer(username); //we know begin spawning all server objects and other players on the new client
    }


    #endregion


    #region TcpSend

    public void Ping()
    {
        tcpStartTime = DateTime.Now;

        //Ping the client and calculate thier latency
        Packet tcpPacket = new Packet((int)ServerPacketsType.TcpPing);
        tcpPacket.Write(10f);// will will be sending 2 floats with the game time so its best to time a ping with 2 float packets
        tcpPacket.Write(20f);
        tcpPacket.Write(30f);
        SendData(id, tcpPacket);

        udpStartTime = DateTime.Now;

        //Ping the client and calculate thier latency
        Packet udpPacket = new Packet((int)ServerPacketsType.UdpPing);
        udpPacket.Write(10f);// will will be sending 2 floats with the game time so its best to time a ping with 2 float packets
        udpPacket.Write(20f);
        udpPacket.Write(30f);
        SendData(id, udpPacket, false);

    }

    public void SpawnNewPlayer(string playerName)
    {
        //Create a new player gameObject and initilize it
        player = GameManager.instance.InstantiatePlayer();
        player.Init(id, playerName);

        //We then want to spawn all the existing gameObjects in the new clients world, else they would recieve position data for objects that dont exist
        Debug.Log("Sending existing players to new client");
        for (int i = 0; i < Server.MaxPlayers; i++)
        {
            if (Server.clients[i].player != null)
            {
                if (Server.clients[i].id != id)
                {
                    SpawnPlayer(id, Server.clients[i].player);
                    SendBulletsIntoGame(id);
                }
            }
        }

        // Send the new player to all players (including himself)
        Debug.Log("Sending new player to all clients");
        for (int i = 0; i < Server.MaxPlayers; i++)
        {
            if (Server.clients[i].player != null)
            {
                SpawnPlayer(Server.clients[i].id, player);
            }
        }
    }

    private void SendBulletsIntoGame(int newPlayerID)
    {
        foreach (Bullet bullet in player.bullets)
        {
            if (bullet.gameObject.activeSelf)
            {
                Packet bulletPacket = new Packet((int)ServerPacketsType.BulletSpawned);

                bulletPacket.Write(bullet.playerID);
                bulletPacket.Write(bullet.id);
                bulletPacket.Write(bullet.transform.position);
                bulletPacket.Write(bullet.transform.rotation);

                SendData(newPlayerID, bulletPacket, true); // send any active each bullets to the new player
            }
        }

        if (player.missile.gameObject.activeSelf)
        {
            Packet missilePacket = new Packet((int)ServerPacketsType.SpawnMissile);

            missilePacket.Write(player.id);
            missilePacket.Write(player.missile.transform.position);
            missilePacket.Write(player.missile.transform.rotation);

            SendData(newPlayerID, missilePacket, true); // send active missile to new player
        }

    }

    public void SpawnPlayer(int id, Player player)
    {
        //Send all data needed to create the player on the client
        Packet packet = new Packet((int)ServerPacketsType.SpawnPlayer);
        packet.Write(player.id);
        packet.Write(player.username);
        packet.Write(player.spawnPoint);

        SendData(id, packet, true);

    }

    private void SendConformation()
    {
        try
        {
            Packet packet = new Packet(((int)ServerPacketsType.Conformation));
            packet.Write("Connection Sucessful, Welcome to the Game!");  // weclome message
            packet.Write(id); // write the id of the client
            SendData(id, packet, true); // send packet through tcp since it is an important message
        }
        catch (Exception error)
        {
            Debug.Log(error);
        }

    }

    public void SendGameTime(float ping, bool protocol, bool resetGameTime = false) // todo check if necessary
    {
        if (resetGameTime) // if we are resyncronising the game time we would want to set it back to 0 to stop it from getting very large
        {
            Server.GameTime = 0;
        }

        Debug.Log("Client " + id + " has a latency of " + ping + "ms");
        Packet packet = new Packet((int)ServerPacketsType.GameTime);
        float time = ((ping / 2) / 1000) + Server.GameTime; // ping is the time taken both ways but we only want to account for one way time
        packet.Write(time);
        packet.Write(ping / 2);
        packet.Write(protocol);
        SendData(id, packet, true);
    }

    #endregion


    #region UnderLineSendFunctions
    
    //This function allows either Udp or Tcp to be sent
    private void SendData(int id, Packet packet, bool SendProtocol = true)
    {
        if(SendProtocol) // Send Tcp
        {
            packet.WriteLength();
            Server.clients[id].SendTCPData(packet);
        }
        else // Send Udp
        {
            packet.WriteLength();
            Server.clients[id].SendUDPData(packet);
        }
    }
  
    //Send Data through TCP
    public void SendTCPData(Packet packet)
    {
        try
        {
            if (socket != null)
            {
                stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null); // Send data to appropriate client
            }
        }
        catch (Exception error)
        {
            Debug.Log("Error Sending Data Through TCP: " + error);
        }
    }

    //Send Data through UDP
    public void SendUDPData(Packet packet)
    {
        try
        {
            if (endPoint != null)
            {
                Server.udpListener.BeginSend(packet.ToArray(), packet.Length(), endPoint, null, null); // Send data to appropriate client
            }
        }
        catch (Exception error)
        {
            Debug.Log("Error Sending Data Through UDP: " + error);
        }
    }

    #endregion

    //Disconnecting closes all sockets and makes it so another player can take this client slot afterwards
    public void Disconnect()
    {
        Dispatcher.instance.AddAction(() =>
        {
            UnityEngine.Object.Destroy(player.gameObject);
            player = null;
        });

        socket.Close();
        stream = null;
        TcpReceivedData = null;
        TcpReceiveBuffer = null;
        socket = null;
        endPoint = null;

        Debug.Log("Disconecting From Client");
    }


    #region GettersSetters
    public IPEndPoint GetIPEndPoint()
    {
        return endPoint;
    }

    public TcpClient getSocket()
    {
        return socket;
    }

    public int GetID()
    {
        return id;
    }

    #endregion

}
