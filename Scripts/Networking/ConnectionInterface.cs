using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net;
using System;
using System.Text;
using UnityEngine.Events;
using System.Text.RegularExpressions;

[Serializable]
public struct ReceivedMessage
{
    public long timestamp;
    public string messageGUID;
}

[Serializable]
public struct WaitingMessage
{
    public string targetAddress;
    public MessageHeader message;
}

[Serializable]
public enum ConnectionProtocol
{
    UDP,
    TCP
}

public class ConnectionInterface : MonoBehaviour
{
    public NetworkingEventsChannel channel;
    public ConnectionProtocol protocol;

    public static string BroadcastAddress = "255.255.255.255";//"192.168.0.255";
    public IPAddress MyIp { get; internal set; }
    public int Port { get; internal set; } = 1111;
    public int DiscoveryPort { get; internal set; } = 1110;
    public static bool isHost;
    public IPEndPoint RemoteIpEndPoint { get; internal set; }

    private float secondsPerLoop = 0.2f;
    private int loopsUntilResend = 2;
    private int loopsUntilCacheCleared = 6;

    protected IConnection clientConnection;
    protected IConnection discoveryConnection;
    protected Coroutine receiveThread;
    private bool threadRunning;

    protected List<WaitingMessage> awaitConfirmationList;
    protected List<ReceivedMessage> confirmedMessages;
    protected string messageCache = "";

    protected readonly Queue<string> incomingQueue = new Queue<string>();

    protected void Setup(NetworkingEventsChannel channel, string name = "")
    {
        this.channel = channel;
        awaitConfirmationList = new List<WaitingMessage>();
        confirmedMessages = new List<ReceivedMessage>();

        SetupConnection();
        SetupDiscovery();
    }

    private void SendMessage(WaitingMessage message)
    {
        Byte[] sendBytes = Encoding.UTF8.GetBytes(MessageConverter.GetHeaderAsString(message.message));

        clientConnection.Send(sendBytes, sendBytes.Length, message.targetAddress, Port);
    }

    public virtual void SendMessage(string targetAddress, MessageType type, object payload, bool answerFlag = false) { }

    /**
     * UDP only
     */
    public virtual void BroadcastMessage<T>(MessageType type, T payload) { }

    public virtual void ReceiveMessage(string message) { }

    void SetupConnection()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                MyIp = ip;
            }
        }

        SetupConnectionProtocol(ref clientConnection, protocol, Port);

        StartReceiveThread();
    }

    void SetupDiscovery()
    {
        SetupConnectionProtocol(ref discoveryConnection, ConnectionProtocol.UDP, DiscoveryPort);
    }

    void SetupConnectionProtocol(ref IConnection connection, ConnectionProtocol protocol, int port)
    {
        Debug.Log("Try adding a connection...");
        ConnectionFactory.statusLabel.text = "Try adding a connection...";

        if (protocol == ConnectionProtocol.TCP)
            connection = new TCPConnection();
        else if (protocol == ConnectionProtocol.UDP)
            connection = new UDPConnection();

        connection.SetupConnection(MyIp, port, this);
        ConnectionFactory.statusLabel.text = "Socket added.";

        Debug.Log("Added a connection.");
        ConnectionFactory.statusLabel.text = "Added a " + protocol.ToString() + " connection." + string.Format("IP: {0}, Port: {1}", MyIp.ToString(), port);
        Debug.Log(string.Format("IP: {0}, Port: {1}", MyIp.ToString(), port));
    }

    public void UpdateBroadcastAddress(string address)
    {
        BroadcastAddress = address;
    }

    public void StartReceiveThread()
    {
        //We note that it is running so we don't forget to turn it off
        threadRunning = true;
        StartCoroutine(Listen());
    }

    public void EnqueueMessage(string message)
    {
        lock (incomingQueue)
        {
            incomingQueue.Enqueue(message);
        }
    }

    private IEnumerator Listen()
    {
        int i_resend = 0;
        while (threadRunning)
        {            
            while(incomingQueue.Count > 0)
            {
                string message = incomingQueue.Dequeue();
                
                //Do not handle message if it has been recently handled
                string cachedGUID = (messageCache != "") ? MessageConverter.ReadHeader(messageCache).guid : "";
                if (MessageConverter.ReadHeader(message).guid != cachedGUID)
                {
                    ReceiveMessage(message);
                    messageCache = message;
                }
            }

            CheckCache();

            if (i_resend == 0)
                ResendMessages();

            yield return new WaitForSeconds(secondsPerLoop);
            i_resend = (i_resend + 1) % loopsUntilResend;
        }
    }

    private void CheckCache()
    {
        long timeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        List<ReceivedMessage> messagesToRemove = new List<ReceivedMessage>();

        foreach(var msg in confirmedMessages)
        {
            if (timeNow - msg.timestamp > secondsPerLoop * loopsUntilCacheCleared * 1000)
                messagesToRemove.Add(msg);
        }

        foreach (var msg in messagesToRemove)
            confirmedMessages.Remove(msg);
    }

    private void ResendMessages()
    {
        foreach(var message in awaitConfirmationList)
        {
            SendMessage(message);
        }
    }

    protected void SendConfirmation(MessageHeader header)
    {
        if (confirmedMessages.Exists(rm => header.guid == rm.messageGUID))
            return;

        ConfirmationMessage confirmation = new ConfirmationMessage { guid = header.guid };

        Byte[] sendBytes = Encoding.UTF8.GetBytes(MessageConverter.BuildHeaderAsString(MyIp.ToString(), MessageType.Confirmation, confirmation));

        clientConnection.Send(sendBytes, sendBytes.Length, header.senderAddress, Port);

        ReceivedMessage receivedMessage = new ReceivedMessage { timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(), messageGUID = header.guid };
        confirmedMessages.Add(receivedMessage);
    }

    protected void HandleConfirmation(ConfirmationMessage confirmation)
    {
        WaitingMessage messageToRemove = new WaitingMessage();
        bool msgFound = false;
        foreach(var msg in awaitConfirmationList)
        {
            if (msg.message.guid == confirmation.guid)
            {
                messageToRemove = msg;
                msgFound = true;
            }
        }

        if (msgFound)
            awaitConfirmationList.Remove(messageToRemove);
    }

    protected void Shutdown()
    {
        if (threadRunning == true)
        {
            threadRunning = false;
        }

        clientConnection.Shutdown();
        discoveryConnection.Shutdown();

        Debug.Log("Connection closed.");
    }
}