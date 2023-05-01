using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public interface IConnection
{
    public void SetupConnection(IPAddress address, int port, ConnectionInterface connectionInterface);
    public void Shutdown();
    public void Send(byte[] dgram, int bytes, string hostname, int port);
    public void Receive(IAsyncResult result);
}

public class UDPConnection : IConnection
{
    private UdpClient client;
    private ConnectionInterface connectionInterface;

    public void Receive(IAsyncResult result)
    {
        if (result == null)
        {
            return;
        }

        try
        {
            if (client.Client == null)
                return;            

            //A little console log to know we have started listening
            Debug.Log("starting receive on " + connectionInterface.MyIp.ToString() + " and port " + connectionInterface.Port.ToString());
            IPEndPoint remote = connectionInterface.RemoteIpEndPoint;
            Byte[] receiveBytes = client.EndReceive(result, ref remote);
            //We grab our byte stream as UTF8 encoding
            string returnData = Encoding.UTF8.GetString(receiveBytes);

            connectionInterface.EnqueueMessage(returnData);
        }
        //Error handling
        catch (SocketException e)
        {
            // 10004 thrown when socket is closed
            if (e.ErrorCode != 10004) Debug.Log("Socket exception while receiving data from udp client: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.Log("Error receiving data from udp client: " + e.Message);
        }

        client.BeginReceive(new AsyncCallback(Receive), null);
    }

    public void Send(byte[] dgram, int bytes, string hostname, int port)
    {
        client.Send(dgram, bytes, hostname, port);
    }

    public void SetupConnection(IPAddress address, int port, ConnectionInterface connectionInterface)
    {
        this.connectionInterface= connectionInterface;

        try
        {
            client = new UdpClient(port);
            client.BeginReceive(new AsyncCallback(Receive), null);
        }
        catch (Exception e)
        {
            Debug.Log("Failed to listen for UDP at port " + port + ": " + e.Message);
            ConnectionFactory.statusLabel.text = "Failed to listen for UDP at port " + port + ": " + e.Message;

            return;
        }

        //Now we configure our udpClient to be able to broadcast
        client.EnableBroadcast = true;
    }

    public void Shutdown()
    {
        client.Close();
    }
}

public class TCPConnection : IConnection
{
    TcpListener listener;
    ConnectionInterface connectionInterface;

    public void Receive(IAsyncResult result)
    {
        if(result == null)
        {
            return;
        }

        if (this.listener.Server.LocalEndPoint == null)
            return;

        // Get the listener that handles the client request.
        TcpListener listener = (TcpListener)result.AsyncState;
        IPEndPoint endPoint = null;
        // End the operation and display the received data on
        // the console.
        using TcpClient client = listener.EndAcceptTcpClient(result);
        {
            Debug.Log("Connected!");

            // Get a stream object for reading and writing
            using NetworkStream stream = client.GetStream();

            Byte[] data = new Byte[1024];

            // String to store the response UTF-8 representation.
            String returnData = String.Empty;

            int i = 0;
            while ((i = stream.Read(data, 0, data.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                returnData = System.Text.Encoding.UTF8.GetString(data, 0, i);

                connectionInterface.EnqueueMessage(returnData);
            }

            endPoint = (IPEndPoint)(client.Client.RemoteEndPoint);
        }

        //stream.Close();
        //client.Close();

        listener.BeginAcceptTcpClient(new AsyncCallback(Receive), listener);
    }

    public void Send(byte[] dgram, int bytes, string hostname, int port)
    {
        SendAsync(dgram, bytes, hostname, port);
    }

    private async void SendAsync(byte[] dgram, int bytes, string hostname, int port)
    {
        using TcpClient client = new();
        try
        {
        await client.ConnectAsync(hostname, port);

        await using NetworkStream stream = client.GetStream();

        stream.Write(dgram, 0, dgram.Length);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    public void SetupConnection(IPAddress address, int port, ConnectionInterface connectionInterface)
    {
        this.connectionInterface = connectionInterface;

        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(Receive), listener);
        }
        catch (Exception e)
        {
            Debug.Log("Failed to establish TCP connection at port " + port + ": " + e.Message);
            ConnectionFactory.statusLabel.text = "Failed to establish TCP connection at port " + port + ": " + e.Message;

            return;
        }
    }

    public void Shutdown()
    {
        listener.Stop();
    }
}
