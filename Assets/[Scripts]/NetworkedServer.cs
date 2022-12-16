using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public enum ClientToServerSignifier
{
    CreatedAccount = 0,
    Login
}

public enum ServerToClientSignifier
{
    LoginComplete = 0,
    LoginFailed,
    AccountCreationComplete,
    AccountCreationFailed,

}



public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

    //#
    LinkedList<PlayerAccount> playerAccounts;

    // Start is called before the first frame update
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);

        playerAccounts = new LinkedList<PlayerAccount>();
    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);



        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }

    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        string[] csv = msg.Split(',');
        int signifier = int.Parse(csv[0]);
        string n = csv[1];
        string p = csv[2];

        if (signifier == (int)ClientToServerSignifier.CreatedAccount)
        {
            //Debug.Log("created account!");
            bool availableName = true;

            foreach (PlayerAccount pa in playerAccounts)
            {
                if (pa.Name == n)
                {
                    availableName = false;
                    break;
                }
            }
            if (availableName)
            {
                PlayerAccount newPlayerAccount = new PlayerAccount(n, p);
                playerAccounts.AddLast(newPlayerAccount);
                SendMessageToClient(ServerToClientSignifier.AccountCreationComplete + "", id);
                Debug.Log("New Player Account Added!!!");
            }
            else
            {
                SendMessageToClient(ServerToClientSignifier.AccountCreationFailed + "", id);
                Debug.Log("Could NOT add new player. Account already exists!");
            }

        }
        if (signifier == (int)ClientToServerSignifier.Login)
        {
            Debug.Log("trying to login");
        }

    }

    //# LOGIN AND CREATING ACCOUNT FUNCTIONS



}

public class PlayerAccount
{
    private string _name;
    public string Name
    {
        get { return _name; }
        set { _name = value; }
    }

    private string _password;
    public string Password
    {
        get { return _password; }
        set { _password = value; }
    }

    public PlayerAccount(string name, string password)
    {
        Name = name;
        Password = password;
    }

}