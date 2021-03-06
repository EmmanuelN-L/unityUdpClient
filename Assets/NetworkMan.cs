﻿/*
By: Emmanuel Nofuente-Loblack 
Student#: 101126071
Assignment 1 Game-3110 
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using UnityEngine.UIElements;

/// <summary>
/// A class that takes care of talking to our server
/// </summary>
public class NetworkMan : MonoBehaviour
{
    public UdpClient udp; // an instance of the UDP client
    public GameObject playerGO; // our player object
    public string myAddress; // my address = (IP, PORT)
    public Dictionary<string, GameObject> currentPlayers; // A list of currently connected players
    public List<string> newPlayers, droppedPlayers; // a list of new players, and a list of dropped players
    public GameState lastestGameState; // the last game state received from server
    public ListOfPlayers initialSetofPlayers; // initial set of players to spawn
    public LocalPositionData sendJson;

    public MessageType latestMessage; // the last message received from the server


    // Start is called before the first frame update
    void Start()
    {
        // Initialize variables
        newPlayers = new List<string>();
        droppedPlayers = new List<string>();
        currentPlayers = new Dictionary<string, GameObject>();
        initialSetofPlayers = new ListOfPlayers();
        // Connect to the client.
        // All this is explained in Week 1-4 slides
        udp = new UdpClient();
        Debug.Log("Connecting...");
        udp.Connect("localhost", 12345);
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);
        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }
    //Not really necessary but still here
    // <summary>
    // A structure that replicates our server color dictionary
    // </summary>
    [Serializable]
    public struct receivedColor
    {
        public float R;
        public float G;
        public float B;
    }

    //Struct with variables to hold other players positions 
    [Serializable]
    public struct otherPlayerPosition
    {
        public float x;
        public float y;
        public float z; //Not using the z axis but including it for practice
    } 
    //Struct for sending position to server
    [Serializable]
    public struct sentLocalPos
    {
        public float x;
        public float y;
        public float z;//Not using the z axis but including it for practice
    }
   
    [Serializable]
    public class LocalPositionData
    {

        public sentLocalPos localPos;

    }


    /// <summary>
    /// A structure that replicates our player dictionary on server
    /// </summary>
    [Serializable]
    public class Player
    {
        public string id;
        public receivedColor color;
        public otherPlayerPosition pos;

    }

    [Serializable]
    public class ListOfPlayers
    {
        public Player[] players;

        public ListOfPlayers()
        {
            players = new Player[0];
        }
    }
    [Serializable]
    public class ListOfDroppedPlayers
    {
        public string[] droppedPlayers;
    }

    /// <summary>
    /// A structure that replicates our game state dictionary on server
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int pktID;
        public Player[] players;
    }

    /// <summary>
    /// A structure that replicates the mesage dictionary on our server
    /// </summary>
    [Serializable]
    public class MessageType
    {
        public commands cmd;
    }

    /// <summary>
    /// Ordererd enums for our cmd values
    /// </summary>
    public enum commands
    {
        PLAYER_CONNECTED,       //0
        GAME_UPDATE,            // 1
        PLAYER_DISCONNECTED,    // 2
        CONNECTION_APPROVED,    // 3
        LIST_OF_PLAYERS,        // 4
    };

    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;

        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);

        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        // Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<MessageType>(returnData);

        Debug.Log(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.PLAYER_CONNECTED:
                    ListOfPlayers latestPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in latestPlayer.players)
                    {
                        newPlayers.Add(player.id);
                    }
                    break;
                case commands.GAME_UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.PLAYER_DISCONNECTED:
                    ListOfDroppedPlayers latestDroppedPlayer = JsonUtility.FromJson<ListOfDroppedPlayers>(returnData);
                    foreach (string player in latestDroppedPlayer.droppedPlayers)
                    {
                        droppedPlayers.Add(player);
                    }
                    break;
                case commands.CONNECTION_APPROVED:
                    ListOfPlayers myPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    Debug.Log(returnData);
                    foreach (Player player in myPlayer.players)
                    {
                        newPlayers.Add(player.id);
                        myAddress = player.id;
                    }
                    break;
                case commands.LIST_OF_PLAYERS:
                    initialSetofPlayers = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    break;
                default:
                    Debug.Log("Error: " + returnData);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {
        if (newPlayers.Count > 0)
        {
            foreach (string playerID in newPlayers)
            {
                currentPlayers.Add(playerID, Instantiate(playerGO, new Vector3(0, 0, 0), Quaternion.identity));
                currentPlayers[playerID].name = playerID;
                //Had an issue when multiple players were in the game you'd be able to control them locally now it adds a controler to specific players
                if (playerID == myAddress)
                {
                    currentPlayers[playerID].AddComponent<PlayerController>();
                }
            }
            newPlayers.Clear();
        }
        if (initialSetofPlayers.players.Length > 0)
        {
            Debug.Log(initialSetofPlayers);
            foreach (Player player in initialSetofPlayers.players)
            {
                if (player.id == myAddress)
                    continue;
                currentPlayers.Add(player.id, Instantiate(playerGO, new Vector3(0, 0, 0), Quaternion.identity));
                currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R, player.color.G, player.color.B);
                currentPlayers[player.id].name = player.id;

            }
            initialSetofPlayers.players = new Player[0];
        }
    }

    void UpdatePlayers()
    {
        if (lastestGameState.players.Length > 0)
        {
            foreach (NetworkMan.Player player in lastestGameState.players)
            {
                string playerID = player.id;
                //No more disco cube
                //currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R, player.color.G, player.color.B);
                //This makes sure that the server doesn't change my position back to where it was 
                if (player.id != myAddress)
                {   //This is how you see the updated position of other players
                    currentPlayers[player.id].GetComponent<Transform>().position = new Vector3(player.pos.x, player.pos.y, player.pos.z);
                }
            }
            //Sending the server information of our cubes position so that they can update other cubes of our location. Pretty cool man!
            foreach (Player player in lastestGameState.players)
            {
                if (player.id == myAddress)
                {    //Getting the transform of the players cube to send to server 
                    LocalPositionData playerLocalPosition = new LocalPositionData();
                    playerLocalPosition.localPos.x = currentPlayers[player.id].GetComponent<Transform>().position.x;
                    playerLocalPosition.localPos.y = currentPlayers[player.id].GetComponent<Transform>().position.y;
                    playerLocalPosition.localPos.z = currentPlayers[player.id].GetComponent<Transform>().position.z;
                    //Making the position into a json and sending it to the server
                    string positionBytes = JsonUtility.ToJson(playerLocalPosition);
                    Byte[] sendingPositionBytes = Encoding.UTF8.GetBytes(positionBytes);
                    udp.Send(sendingPositionBytes, sendingPositionBytes.Length);
                }
            }
            lastestGameState.players = new Player[0];
        }
    }

    void DestroyPlayers()
    {
        if (droppedPlayers.Count > 0)
        {
            foreach (string playerID in droppedPlayers)
            {
                Debug.Log(playerID);
                Debug.Log(currentPlayers[playerID]);
                Destroy(currentPlayers[playerID].gameObject);
                currentPlayers.Remove(playerID);
            }
            droppedPlayers.Clear();
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
