using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkedSignifier : MonoBehaviour
{

}

public enum ClientToServerSignifier
{
    CreatedAccount = 0,
    Login,
    JoinQueueFromGameRoom,
    TicTacToeGamePlay,
    PlayerPlayed,
    StringTable,
}

public enum ServerToClientSignifier
{
    LoginComplete = 0,
    LoginFailed,
    AccountCreationComplete,
    AccountCreationFailed,
    GameStart,
    OpponentPlays,
    SetSymbols,
    YourTurn,
    UpdateMarks
}

public enum LoadingAndSavingSignifier
{
    NameAndPassword = 0,
}
public enum GameStates
{
    MainMenu = 0,
    OnQueue,
    TicTacToe
}