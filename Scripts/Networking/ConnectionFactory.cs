using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionFactory : MonoBehaviour
{
    public ConnectionProtocol connectionProtocol;
    public Text s_statusLabel;
    public Text s_addressLabel;
    public static Text statusLabel;
    public static Text addressLabel;

    public Client clientObject;
    public Server serverObject;

    public NetworkingEventsChannel eventsChannel;

    public static ConnectionInterface con;

    private void Start()
    {
        statusLabel = s_statusLabel;
        addressLabel = s_addressLabel;
    }

    public void CreateHostConnection(ServerStruct server)
    {
        con = gameObject.AddComponent<HostConnection>();
        var h_con = (HostConnection)con;
        h_con.protocol = connectionProtocol;
        h_con.SetupHost(eventsChannel, server);
        serverObject.SetConnection(h_con);
    }

    public void CreateClientConnection(ClientStruct client)
    {
        con = gameObject.AddComponent<ClientConnection>();
        var c_con = (ClientConnection)con;
        c_con.protocol = connectionProtocol;
        c_con.SetupClient(eventsChannel, client);
        clientObject.SetConnection(c_con);
    }

    public void UpdateBroadcastAddress(string address)
    {
        con.UpdateBroadcastAddress(address);
    }
}
