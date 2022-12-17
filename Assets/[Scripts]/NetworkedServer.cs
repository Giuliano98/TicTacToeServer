using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

    //#
    static string dirPath = Application.dataPath + Path.DirectorySeparatorChar;
    static string PlayerAccountsFile = "PlayerAccounts.txt";
    LinkedList<PlayerAccount> playerAccounts;
    int WaiterPlayerID = -1;
    LinkedList<GameRoom> gameRooms;

    void Start()
    {

        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);

        playerAccounts = new LinkedList<PlayerAccount>();
        LoadPlayerAccounts();
        gameRooms = new LinkedList<GameRoom>();
    }

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
        Debug.Log("msg Received = " + msg + " || Connection ID = " + id);

        string[] csv = msg.Split(',');
        ClientToServerSignifier _signifier = (ClientToServerSignifier)System.Enum.Parse(typeof(ClientToServerSignifier), csv[0]);


        if (_signifier == ClientToServerSignifier.CreatedAccount)
        {
            string n = csv[1];
            string p = csv[2];
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
                SendMessageToClient(ServerToClientSignifier.AccountCreationComplete + "", id);
                Debug.Log("New Player Account Added!!!");

                PlayerAccount newPlayerAccount = new PlayerAccount(n, p);
                playerAccounts.AddLast(newPlayerAccount);
                SavePlayerAccounts();
            }
            else
            {
                SendMessageToClient(ServerToClientSignifier.AccountCreationFailed + "", id);
                Debug.Log("Could NOT add new player. Account already exists!");
            }

        }
        else if (_signifier == ClientToServerSignifier.Login)
        {
            string n = csv[1];
            string p = csv[2];
            bool playerAccountFound = false;
            foreach (PlayerAccount pa in playerAccounts)
            {
                if ((n == pa.Name) && (p == pa.Password))
                {
                    SendMessageToClient(ServerToClientSignifier.LoginComplete + "", id);
                    Debug.Log("Player Login");
                    playerAccountFound = true;
                }
            }
            if (!playerAccountFound)
            {
                SendMessageToClient(ServerToClientSignifier.LoginFailed + "", id);
                Debug.Log("Player couldnt log-in");
            }
        }
        else if (_signifier == ClientToServerSignifier.JoinQueueFromGameRoom)
        {
            Debug.Log("Player on Queue Room");
            if (WaiterPlayerID == -1)
            {
                WaiterPlayerID = id;
            }
            else
            {
                Debug.Log("Both player enter. Start Game!");
                GameRoom gr = new GameRoom(WaiterPlayerID, id);
                gameRooms.AddLast(gr);
                SendMessageToClient(ServerToClientSignifier.GameStart + "", gr.Player1);
                SendMessageToClient(ServerToClientSignifier.GameStart + "", gr.Player2);
                WaiterPlayerID = -1;
            }
        }
        else if (_signifier == ClientToServerSignifier.TicTacToeGamePlay)
        {
            GameRoom gr = GetGameRoomWithClientID(id);
            if (gr != null)
            {
                if (gr.Player1 == id)
                    SendMessageToClient(ServerToClientSignifier.OpponentPlays + "", gr.Player2);
                if (gr.Player2 == id)
                    SendMessageToClient(ServerToClientSignifier.OpponentPlays + "", gr.Player1);
            }


        }
    }

    //# LOGIN AND CREATING ACCOUNT FUNCTIONS
    private void SavePlayerAccounts()
    {
        using (StreamWriter sw = new StreamWriter(dirPath + PlayerAccountsFile))
        {
            foreach (PlayerAccount pa in playerAccounts)
            {
                sw.WriteLine((int)LoadingAndSavingSignifier.NameAndPassword + "," + pa.Name + "," + pa.Password);
            }
        }
    }

    private void LoadPlayerAccounts()
    {
        if (!File.Exists(dirPath + PlayerAccountsFile))
            return;


        using (StreamReader sr = new StreamReader(dirPath + PlayerAccountsFile))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] csv = line.Split(',');
                int signifier = int.Parse(csv[0]);

                if (signifier == (int)LoadingAndSavingSignifier.NameAndPassword)
                {
                    PlayerAccount pa = new PlayerAccount(csv[1], csv[2]);
                    playerAccounts.AddLast(pa);
                }
            }
        }
    }

    private GameRoom GetGameRoomWithClientID(int id)
    {
        foreach (GameRoom gr in gameRooms)
        {
            if (gr.Player1 == id || gr.Player2 == id)
                return gr;
        }
        return null;
    }

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

public class GameRoom
{
    public int Player1, Player2;

    public GameRoom(int p1, int p2)
    {
        Player1 = p1;
        Player2 = p2;
    }
}