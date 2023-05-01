using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct ClientStruct
{
    public string ipAddress;
    public string playerName;
    public Color playerColor;
    public int avatarIndex;
}

public class Client : MonoBehaviour
{
    [Header("Gui Events")]
    public OnLobbyJoinedEvent OnLobbyJoinedEvent = new OnLobbyJoinedEvent();
    public UnityEvent OnLobbyLeftEvent = new UnityEvent();
    public UnityEvent OnServerClosed = new UnityEvent();
    public OnGameStartedEvent OnGameStartedEvent = new OnGameStartedEvent();

    [Header("Setup")]
    public ClientConnection connection;

    public void SetConnection(ClientConnection connection)
    {
        this.connection = connection;
        connection.channel.OnServerJoined += OnLobbyJoinedEvent.Invoke;
        connection.channel.OnGameStarted += OnGameStartedEvent.Invoke;
        connection.channel.OnServerClosed += OnServerClosed.Invoke;
    }

    public void LeaveLobby()
    {
        OnLobbyLeftEvent.Invoke();
        connection.LeaveLobby();
    }
}

[Serializable]
public class OnLobbyJoinedEvent : UnityEvent<ServerStruct> { }
