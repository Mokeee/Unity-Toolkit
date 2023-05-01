using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Net;
using UnityEngine.Events;

public class ClientConnection : ConnectionInterface
{
    public ClientStruct client;
    public ServerStruct currentServer;

    private List<ServerInfoStruct> foundServers;

    public void SetupClient(NetworkingEventsChannel channel, ClientStruct client)
    {
        isHost = false;
        base.Setup(channel);
        ConnectionFactory.addressLabel.text = string.Format("IP: {0}, Port: {1} \n Broadcast-Address: {2}", MyIp.ToString(), Port, BroadcastAddress);

        this.client = client;
        this.client.ipAddress = MyIp.ToString();

        channel.SendChallengeAnswer += SendAnswer;
    }

    public override void BroadcastMessage<T>(MessageType type, T payload)
    {
        Byte[] sendBytes = Encoding.UTF8.GetBytes(MessageConverter.BuildHeaderAsString(MyIp.ToString(), type, payload));
        discoveryConnection.Send(sendBytes, sendBytes.Length, BroadcastAddress, DiscoveryPort);
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

    public override void ReceiveMessage(string message)
    {
        Debug.Log(message);
        ConnectionFactory.statusLabel.text = message;

        MessageHeader header = MessageConverter.ReadHeader(message);

        if (header.answerFlag)
            SendConfirmation(header);
        
        switch(header.messageType)
        {
            case MessageType.Confirmation:
                HandleConfirmation(MessageConverter.ReadPayload<ConfirmationMessage>(header.payload));
                break;
            //case MessageType.ExploreServers:
            //    break;
            case MessageType.ServerGreeting:
                OnServerFound(MessageConverter.ReadPayload<ServerGreetingMessage>(header.payload).serverInfo);
                break;
            case MessageType.ServerUpdate:
                channel.RaiseAction(channel.OnServerUpdated, MessageConverter.ReadPayload<ServerStruct>(header.payload));
                break;
            //case MessageType.JoinServer:
            //    break;
            case MessageType.JoinAccepted:
                var acceptMsg = MessageConverter.ReadPayload<JoinAcceptedMessage>(header.payload);
                channel.RaiseAction(channel.OnServerJoined, acceptMsg.server);
                currentServer = acceptMsg.server;
                break;
            case MessageType.JoinDenied:
                throw new NotImplementedException("Join Denied not implemented yet");
                break;
            //case MessageType.LeaveServer:
            //    break;
            case MessageType.ServerEnded:
                channel.RaiseAction(channel.OnServerClosed);
                currentServer = new ServerStruct();
                break;
            case MessageType.GameStarted:
                channel.RaiseAction(channel.OnGameStarted, MessageConverter.ReadPayload<GameStruct>(header.payload));
                break;
            case MessageType.GameEnded:
                channel.RaiseAction(channel.OnGameOverReceived, MessageConverter.ReadPayload<EndGameMessage>(header.payload));
                break;
            case MessageType.Challenge:
                channel.RaiseAction(channel.OnChallengeReceived, MessageConverter.ReadPayload<ChallengeMessage>(header.payload));
                break;
            //case MessageType.ChallengeAnswer:
            //    break;
            case MessageType.AnswerFeedback:
                channel.RaiseAction(channel.OnFeedbackReceived, MessageConverter.ReadPayload<AnswerFeedbackMessage>(header.payload));
                break;
            default:
                Debug.LogWarning("The client cannot handle messages of type: " + header.messageType);
                break;
        }
    }

    public void DiscoverServers()
    {
        foundServers = new List<ServerInfoStruct>();
        BroadcastMessage(MessageType.ExploreServers, "");
    }

    private void OnServerFound(ServerInfoStruct serverInfo)
    {
        foundServers.Add(serverInfo);
        channel.RaiseAction(channel.OnServersFound, foundServers.ToArray());
    }

    public void LeaveLobby()
    {
        currentServer = new ServerStruct();
        SendMessage(currentServer.ipAddress, MessageType.LeaveServer, "", true);
    }

    public void SendAnswer(ChallengeAnswerMessage answer)
    {
        answer.ipAddress = MyIp.ToString();
        SendMessage(currentServer.ipAddress, MessageType.ChallengeAnswer, answer, true);
    }

    public void JoinServer(string serverAddress)
    {
        Debug.Log("Joining Server...");
        SendMessage(serverAddress, MessageType.JoinServer, new JoinRequestMessage { client = client });
    }

    private void OnDestroy()
    {
        channel.SendChallengeAnswer -= SendAnswer;
        Shutdown();
    }
}
