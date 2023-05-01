using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageConverter
{
    public static MessageHeader ReadHeader(string message)
    {
        MessageHeader header;
        try
        {
            header = JsonUtility.FromJson<MessageHeader>(message);
        }
        catch
        {
            header = new MessageHeader { messageType = MessageType.None, payload = message };
        }

        return header;
    }

    public static T ReadPayload<T>(string message)
    {
        return JsonUtility.FromJson<T>(message);
    }

    public static MessageHeader BuildMessageHeader(string ipAddress, MessageType type, string message = "", bool answerFlag = false)
    {
        var header = new MessageHeader() { senderAddress = ipAddress, messageType = type, guid = System.Guid.NewGuid().ToString(), answerFlag = answerFlag, payload = message };
        return header;
    }

    public static string BuildHeaderAsString<T>(string ipAddress, MessageType type, string message = "", bool answerFlag = false)
    {
        var header = BuildMessageHeader(ipAddress, type, message, answerFlag);
        return JsonUtility.ToJson(header);
    }

    public static string GetHeaderAsString(MessageHeader header)
    {
        return JsonUtility.ToJson(header);
    }

    public static MessageHeader BuildMessageHeader<T>(string ipAddress, MessageType type, T message, bool answerFlag = false)
    {
        var header = new MessageHeader() { senderAddress = ipAddress, messageType = type, guid = System.Guid.NewGuid().ToString(), answerFlag = answerFlag, payload = JsonUtility.ToJson(message) };
        return header;
    }

    public static string BuildHeaderAsString<T>(string ipAddress, MessageType type, T message, bool answerFlag = false)
    {
        var header = BuildMessageHeader(ipAddress, type, message, answerFlag);
        return JsonUtility.ToJson(header);
    }

    /*
    public static string BuildMessageHeader(string ipAddress, MessageType type, object message)
    {
        string buildMessage = "Error!";
        switch(type)
        {
            case MessageType.Confirmation:
                buildMessage = BuildHeaderAsString(ipAddress, type, (ConfirmationMessage)message);
                break;
            case MessageType.ExploreServers:
                buildMessage = BuildHeaderAsString(ipAddress, type, (string)message);
                break;
            case MessageType.ServerGreeting:
                buildMessage = BuildHeaderAsString(ipAddress, type, (ServerGreetingMessage)message);
                break;
            case MessageType.ServerUpdate:
                buildMessage = BuildHeaderAsString(ipAddress, type, (ServerStruct)message);
                break;
            case MessageType.JoinServer:
                buildMessage = BuildHeaderAsString(ipAddress, type, (JoinRequestMessage)message);
                break;
            case MessageType.JoinAccepted:
                buildMessage = BuildHeaderAsString(ipAddress, type, (JoinAcceptedMessage)message);
                break;
            case MessageType.JoinDenied:
                buildMessage = BuildHeaderAsString(ipAddress, type, (string)message);
                break;
            case MessageType.LeaveServer:
                buildMessage = BuildHeaderAsString(ipAddress, type, (string)message);
                break;
            case MessageType.ServerEnded:
                buildMessage = BuildHeaderAsString(ipAddress, type, (string)message);
                break;
            case MessageType.GameStarted:
                buildMessage = BuildHeaderAsString(ipAddress, type, (GameStruct)message);
                break;
            case MessageType.GameEnded:
                buildMessage = BuildHeaderAsString(ipAddress, type, (EndGameMessage)message);
                break;
            case MessageType.Challenge:
                buildMessage = BuildHeaderAsString(ipAddress, type, (ChallengeMessage)message);
                break;
            case MessageType.ChallengeAnswer:
                buildMessage = BuildHeaderAsString(ipAddress, type, (ChallengeAnswerMessage)message);
                break;
            case MessageType.AnswerFeedback:
                buildMessage = BuildHeaderAsString(ipAddress, type, (AnswerFeedbackMessage)message);
                break;
            default:
                Debug.LogError("Message of type " + type + "could not be build!");
                break;
        }

        return buildMessage;
    }
    */
}
