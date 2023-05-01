using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.UI.Slider;

[Serializable]
public struct ServerStruct
{
    public string ipAddress;
    public string serverName;
    public int maxPlayers;
    public int currentPlayers;
    public string[] playerNames;
    public Color[] playerColors;
    public int[] playerSpriteIndices;
}

[Serializable]
public struct ServerInfoStruct
{
    public string ipAddress;
    public string serverName;
    public int maxPlayers;
    public int currentPlayers;
}

public class Server : MonoBehaviour
{
    [Header("Gui Events")]
    public OnLobbyJoinedEvent OnLobbyJoinedEvent = new OnLobbyJoinedEvent();
    public UnityEvent OnLobbyLeftEvent = new UnityEvent();
    public OnGameStartedEvent OnGameStartedEvent = new OnGameStartedEvent();

    [Header("Setup")]
    public HostConnection connection;

    [Header("Lobby Settings")]
    private int maxRounds = 10;
    private float maxTimer = 0;

    public void SetConnection(HostConnection connection)
    {
        this.connection = connection;
        OnLobbyJoinedEvent.Invoke(connection.server);
        connection.channel.OnServerClosed += OnLobbyClosed;
    }

    public void StartGame()
    {
        GameStruct game = new GameStruct()
        {
            clientAddresses = new string[connection.server.currentPlayers],
            playerNames = new string[connection.server.currentPlayers],
            playerColors = new Color[connection.server.currentPlayers],
            playerSpriteIndices = new int[connection.server.currentPlayers],
            playerScores = new int[connection.server.currentPlayers],
            maxRounds = maxRounds,
            roundTimer = maxTimer,
            currentRound = 0
        };
        connection.players.Keys.CopyTo(game.clientAddresses, 0);
        connection.players.Values.CopyTo(game.playerNames, 0);
        game.playerColors = connection.server.playerColors;
        game.playerSpriteIndices = connection.server.playerSpriteIndices;

        if (connection.StartGame(game))
            OnGameStartedEvent.Invoke(game);
    }

    public void SetMaxRounds(float rounds)
    {
        maxRounds = (int)rounds * 5;
    }

    public void SetTimer(float rounds)
    {
        maxTimer = rounds * 15f;
    }

    public void CloseLobby()
    {
        connection.CloseLobby();
    }

    private void OnLobbyClosed()
    {
        OnLobbyLeftEvent.Invoke();
    }
}

[Serializable]
public class OnGameStartedEvent : UnityEvent<GameStruct> { }
