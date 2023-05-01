using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NetworkingEventsChannel", menuName = "ScriptableObjects/Channels/NetworkingEventsChannel", order = 1)]
public class NetworkingEventsChannel : ScriptableObject
{
    [Header("Server Messages")]
    public UnityAction<ServerStruct> OnServerUpdated;
    public UnityAction<ServerInfoStruct[]> OnServersFound;
    public UnityAction<ServerStruct> OnServerJoined;
    public UnityAction OnServerClosed;
    public UnityAction<GameStruct> OnGameStarted;
    public UnityAction<ChallengeMessage> OnChallengeReceived;
    public UnityAction<ChallengeAnswerMessage> OnChallengeAnswerReceived;
    public UnityAction<AnswerFeedbackMessage> OnFeedbackReceived;
    public UnityAction<EndGameMessage> OnGameOverReceived;
    public UnityAction<string> OnPlayerLeft;

    [Header("Game Internal Requests")]
    public UnityAction<GameStruct> OnGameUpdated;
    public UnityAction ServerStatusRequest;
    public UnityAction SendPlayerLeaveRequest;
    public UnityAction<ChallengeMessage> SendChallenge;
    public UnityAction<ChallengeAnswerMessage> SendChallengeAnswer;
    public UnityAction<AnswerFeedbackMessage> SendChallengeFeedback;
    public UnityAction<EndGameMessage> SendGameOver;
    public UnityAction<AudioClip> StartChallengeAudioClip;


    public void RaiseAction<T>(UnityAction<T> action, T payload)
    {
        if(action != null)
        {
            action.Invoke(payload);
        }
        else
        {
            Debug.LogWarning("Tried to raise event of type " + typeof(T) + " but no one was listening!");
        }
    }

    public void RaiseAction(UnityAction action)
    {
        if (action != null)
        {
            action.Invoke();
        }
        else
        {
            Debug.LogWarning("Tried to raise event but no one was listening!");
        }
    }
}
