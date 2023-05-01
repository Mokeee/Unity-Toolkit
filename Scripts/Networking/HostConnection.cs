using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Text;
using UnityEngine.Events;

public class HostConnection : ConnectionInterface
{
    public ServerStruct server;

    public Dictionary<string, string> players;

    public bool gameInProgess;

    public void SetupHost(NetworkingEventsChannel channel, ServerStruct server)
    {
        isHost = true;
        base.Setup(channel);
        ConnectionFactory.addressLabel.text = string.Format("IP: {0}, Port: {1} \n Broadcast-Address: {2}", MyIp.ToString(), Port, BroadcastAddress);

        players = new Dictionary<string, string>();
        this.server = new ServerStruct()
        {
            ipAddress = MyIp.ToString(),
            currentPlayers = 0,
            playerColors = new Color[server.maxPlayers],
            playerSpriteIndices = new int[server.maxPlayers],
            playerNames = new string[server.maxPlayers]
        };
        this.server.serverName = server.serverName;
        this.server.maxPlayers = server.maxPlayers;

        //Setup listeners
        channel.SendChallenge += SendChallenge;
        channel.SendChallengeFeedback += SendFeedback;
        channel.SendGameOver += SendGameOver;
    }

    public override void BroadcastMessage<T>(MessageType type, T payload)
    {
        Byte[] sendBytes = Encoding.UTF8.GetBytes(MessageConverter.BuildHeaderAsString(MyIp.ToString(), type, payload));
        clientConnection.Send(sendBytes, sendBytes.Length, BroadcastAddress, Port);
    }

    public override void SendMessage(string targetAddress, MessageType type, object payload, bool answerFlag = false)
    {
        var message = MessageConverter.BuildMessageHeader(MyIp.ToString(), type, payload, answerFlag);
        Byte[] sendBytes = Encoding.UTF8.GetBytes(MessageConverter.GetHeaderAsString(message));

        if (answerFlag)
        {
            var m = new WaitingMessage { targetAddress = targetAddress, message = message };
            awaitConfirmationList.Add(m);
        }

        clientConnection.Send(sendBytes, sendBytes.Length, targetAddress, Port);
    }

    private void SendMessageToLobby(MessageType type, object payload, bool answerFlag = false)
    {
        foreach (var client in players.Keys)
            SendMessage(client, type, payload, answerFlag);
    }

    public override void ReceiveMessage(string message)
    {
        Debug.Log(message);
        ConnectionFactory.statusLabel.text = message;

        MessageHeader header = MessageConverter.ReadHeader(message);

        if (header.answerFlag)
            SendConfirmation(header);

        switch (header.messageType)
        {
            case MessageType.Confirmation:
                HandleConfirmation(MessageConverter.ReadPayload<ConfirmationMessage>(header.payload));
                break;
            case MessageType.ExploreServers:
                ExchangeGreeting(header.senderAddress);
                break;
            //case MessageType.ServerGreeting:
            //    break;
            case MessageType.ServerUpdate:
                channel.RaiseAction(channel.OnServerUpdated, MessageConverter.ReadPayload<ServerStruct>(header.payload));
                break;
            case MessageType.JoinServer:
                HandleJoinRequest(MessageConverter.ReadPayload<JoinRequestMessage>(header.payload));
                break;
            //case MessageType.JoinAccepted:
            //    break;
            //case MessageType.JoinDenied:
            //    break;
            case MessageType.LeaveServer:
                break;
            //case MessageType.ServerEnded:
            //    break;
            //case MessageType.Challenge:
            //    break;
            case MessageType.ChallengeAnswer:
                channel.RaiseAction(channel.OnChallengeAnswerReceived, MessageConverter.ReadPayload<ChallengeAnswerMessage>(header.payload));
                break;
            //case MessageType.AnswerFeedback:
            //    break;
            default:
                Debug.LogWarning("The host cannot handle messages of type: " + header.messageType);
                break;
        }
    }

    //<--- Lobby messaging --->
    public void ExchangeGreeting(string clientAddress)
    {
        if (gameInProgess)
            return;

        ServerInfoStruct serverInfo = new ServerInfoStruct()
        {
            ipAddress = server.ipAddress,
            serverName = server.serverName,
            maxPlayers = server.maxPlayers,
            currentPlayers = server.currentPlayers
        };

        var greeting = new ServerGreetingMessage() { serverInfo = serverInfo };
        ConnectionFactory.statusLabel.text = JsonUtility.ToJson(greeting);

        SendMessage(clientAddress, MessageType.ServerGreeting, greeting);
    }

    public void HandleJoinRequest(JoinRequestMessage joinRequest)
    {
        string status = "Player is joining...";
        string clientAddress = joinRequest.client.ipAddress;

        if(gameInProgess)
        {
            SendMessage(clientAddress, MessageType.JoinDenied, "Game is already ongoing");
            status = "Player tried to join ongoing game";
            ConnectionFactory.statusLabel.text = status;
            return;
        }

        if(players.Count >= server.maxPlayers)
        {
            SendMessage(clientAddress, MessageType.JoinDenied, "Server is full");
            status = "Server full already";
        }
        else
        {
            if (players.ContainsKey(clientAddress))
            {
                SendMessage(clientAddress, MessageType.JoinDenied, "Client is already on the server");
                status = "Client is already on the server";
            }
            else
            {
                players.Add(clientAddress, joinRequest.client.playerName);
                server.playerNames[server.currentPlayers] = joinRequest.client.playerName;
                server.playerColors[server.currentPlayers] = joinRequest.client.playerColor;
                server.playerSpriteIndices[server.currentPlayers] = joinRequest.client.avatarIndex;
                server.currentPlayers++;

                channel.RaiseAction(channel.OnServerUpdated, server);

                SendMessage(clientAddress, MessageType.JoinAccepted, new JoinAcceptedMessage() { server = this.server }, true);

                SendMessageToLobby(MessageType.ServerUpdate, server);

                status = "Player has joined!";
            }
        }
        ConnectionFactory.statusLabel.text = status;
    }

    public void HandleClientLeave(string clientAddress)
    {
        channel.RaiseAction(channel.OnPlayerLeft, players[clientAddress]);

        players.Remove(clientAddress);
        server.currentPlayers--;

        channel.RaiseAction(channel.OnServerUpdated, server);
    }

    public void CloseLobby()
    {
        SendMessageToLobby(MessageType.ServerEnded, "Lobby was closed!");
        channel.RaiseAction(channel.OnServerClosed);
        Destroy(this);
    }

    //<--- Game messaging --->
    public bool StartGame(GameStruct game)
    {
        if (players.Count <= 0)
            return false;

        gameInProgess = true;
        SendMessageToLobby(MessageType.GameStarted, game, true);
        channel.RaiseAction(channel.OnGameStarted, game);

        return true;
    }
    public void SendChallenge(ChallengeMessage message)
    {
        SendMessageToLobby(MessageType.Challenge, message, true);
        channel.RaiseAction(channel.OnChallengeReceived, message);
    }

    public void SendFeedback(AnswerFeedbackMessage message)
    {
        SendMessage(message.playerAddress, MessageType.AnswerFeedback, message, true);
    }

    public void SendGameOver(EndGameMessage message)
    {
        SendMessageToLobby(MessageType.GameEnded, message, false);
        channel.RaiseAction(channel.OnGameOverReceived, message);
        StartCoroutine(WaitAndCloseLobby());
    }

    private IEnumerator WaitAndCloseLobby()
    {
        yield return new WaitForSeconds(10.0f);
        CloseLobby();
    }

    private void OnDestroy()
    {
        channel.SendChallenge -= SendChallenge;
        channel.SendChallengeFeedback -= SendFeedback;
        channel.SendGameOver -= SendGameOver;
        Shutdown();
    }
}
