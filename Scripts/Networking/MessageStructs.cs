using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum MessageType
{
    None,
    Confirmation,
    ExploreServers,
    ServerGreeting,
    ServerUpdate,
    JoinServer,
    JoinAccepted,
    JoinDenied,
    LeaveServer,
    ServerEnded,
    GameStarted,
    GameEnded,
    Challenge,
    ChallengeAnswer,
    AnswerFeedback,
    Error
}

[Serializable]
public struct MessageHeader
{
    public MessageType messageType;
    public string senderAddress;
    public string guid;
    public bool answerFlag;
    public string payload;
}

[Serializable]
public struct ConfirmationMessage
{
    public string guid;
}

[Serializable]
public struct ServerGreetingMessage
{
    public ServerInfoStruct serverInfo;
}

[Serializable]
public struct JoinRequestMessage
{
    public ClientStruct client;
}

[Serializable]
public struct JoinAcceptedMessage
{
    public ServerStruct server;
}

[Serializable]
public struct ChallengeMessage
{
    public ChallengeType challengeType;
    public string challengeText;
    public string[] answerOptions;
    public ChallengeMediaType mediaType;
    public string mediaAddress;
}

[Serializable]
public struct ChallengeAnswerMessage
{
    public string ipAddress;
    public float answerIndex;
}

[Serializable]
public struct AnswerFeedbackMessage
{
    public string playerAddress;
    public bool challengeSucceeded;
    public int[] playerScores;
}

[Serializable]
public struct EndGameMessage
{
    public int[] winnerIndex;
    public string[] playerNames;
    public int[] playerScores;
    public Color[] playerColors;
    public int[] playerSpriteIndices;
}