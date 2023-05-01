using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Security.Cryptography;

/// <summary>
/// Game information provided by the lobby.
/// </summary>
[Serializable]
public struct GameStruct
{
    public string[] clientAddresses;
    public string[] playerNames;
    public Color[] playerColors;
    public int[] playerSpriteIndices;
    public int[] playerScores;

    public int maxRounds;
    public int currentRound;
    public float roundTimer;
}

[Serializable]
public enum ChallengeType
{
    MultipleChoice,
    GuessingQuestion
}

[Serializable]
public enum ChallengeMediaType
{
    None,
    Image,
    Sound
}

[Serializable]
public struct ChallengeStruct
{
    public ChallengeType challengeType;
    public string challengeText;
    public string[] answerOptions;
    public float correctAnswerIndex;
    public ChallengeMediaType mediaType;
    public string mediaAddress;

    public string GetHashString()
    {
        string text = challengeText;
        foreach (var str in answerOptions)
            text += str;
        text += challengeType.ToString();
        text += mediaType.ToString();

        // Uses SHA256 to create the hash
        using (var sha = new SHA256Managed())
        {
            // Convert the string to a byte array first, to be processed
            byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] hashBytes = sha.ComputeHash(textBytes);

            // Convert back to a string, removing the '-' that BitConverter adds
            string hash = BitConverter
                .ToString(hashBytes)
                .Replace("-", String.Empty);

            return hash;
        }
    }
}

[Serializable]
public struct AnswerRevealStruct
{
    public float[] playerAnswers;
    public Sprite[] playerAvatars;
    public float correctAnswerIndex;
}


/// <summary>
/// This class serves as an example of how to employ the netcode to create a quiz game.
/// Prior to launching a game the players have gathered inside a lobby.
/// The game is launched with the information provided by the lobby.
/// </summary>
public class Game : MonoBehaviour
{
    /// <summary>
    /// Channel for network updates and calls.
    /// </summary>
    public NetworkingEventsChannel channel;

    [Header("GUI Actions")]
    public UnityAction<float> OnUpdateTimer;
    public UnityAction<AnswerRevealStruct> OnRevealAnswer;

    /// <summary>
    /// Section containing information about the game and the server.
    /// </summary>
    public GameStruct currentGame;
    public ServerStruct server;
    /// <summary>
    /// Only the client hosting the server has this bool set to true.
    /// </summary>
    private bool isHost;

    [Header("Game Logic")]
    private ChallengeStruct currentChallenge;
    private Dictionary<string, float> playerAnswers;
    private Coroutine timer;

    /// <summary>
    /// First, subscribe to the network updates.
    /// </summary>
    private void OnEnable()
    {
        channel.OnGameStarted += StartGame;
        channel.OnServerUpdated += UpdateServer;
        channel.OnChallengeReceived += ReceiveChallenge;
        channel.OnChallengeAnswerReceived += ReceiveChallengeAnswer;
        //Empty call in this setup, can be used to verify game state for each client
        channel.RaiseAction(channel.ServerStatusRequest);
    }

    /// <summary>
    /// Similarly, unsubscribe from the network updates on disabling the game.
    /// </summary>
    private void OnDisable()
    {
        channel.OnGameStarted -= StartGame;
        channel.OnServerUpdated -= UpdateServer;
        channel.OnChallengeReceived -= ReceiveChallenge;
        channel.OnChallengeAnswerReceived -= ReceiveChallengeAnswer;
    }

    /// <summary>
    /// After connecting to the game, the host sends out the first challenge with a delayed action.
    /// </summary>
    /// <param name="game"></param>
    public void StartGame(GameStruct game)
    {
        currentGame = game;
        isHost = ConnectionInterface.isHost;

        if (isHost)
            StartCoroutine(DelaySend(6.0f));
    }

    private void UpdateServer(ServerStruct server)
    {
        this.server = server;
    }

    private void UpdateGame()
    {
        channel.RaiseAction(channel.OnGameUpdated, currentGame);
    }

    public void PlayerLeft()
    {

    }

    private IEnumerator DelaySend(float delay)
    {
        float deltaTime = 0.0f;
        while(deltaTime < delay)
        {
            deltaTime += Time.deltaTime;

            if(OnUpdateTimer != null)
                OnUpdateTimer.Invoke(1 - deltaTime / delay);

            yield return null;
        }

        OnUpdateTimer(0.0f);
        SendChallenge();
    }

    /// <summary>
    /// Here, every client receives the new challenge.
    /// </summary>
    /// <param name="challenge"></param>
    private void ReceiveChallenge(ChallengeMessage challenge)
    {
        currentChallenge.answerOptions = challenge.answerOptions;
        currentChallenge.challengeText = challenge.challengeText;
        currentChallenge.challengeType = challenge.challengeType;
    }

    /// <summary>
    /// Method for the host to send a new challenge to each client.
    /// Checks if the end of the game is reached.
    /// </summary>
    private void SendChallenge()
    {
        if (!isHost)
            return;

        //Increase played rounds and check for end of game
        currentGame.currentRound++;

        if(currentGame.currentRound > currentGame.maxRounds)
        {
            SendGameOver();
            return;
        }

        //Reset game state for answers
        playerAnswers = new Dictionary<string, float>();

        //Retrieve new challenge
        ChallengeStruct challenge = GetNextChallenge();

        currentChallenge = challenge;

        //Construct new challenge message and send it to all clients
        ChallengeMessage challengeMessage = new ChallengeMessage()
        {
            challengeText = challenge.challengeText,
            answerOptions = challenge.answerOptions,
            challengeType = challenge.challengeType,
            mediaType = challenge.mediaType,
            mediaAddress = challenge.mediaAddress
        };
        channel.RaiseAction(channel.SendChallenge, challengeMessage);
        UpdateGame();

        //Restart round timer
        if(currentGame.roundTimer > 0)
        {
            timer = StartCoroutine(Timer(currentGame.roundTimer));
        }
    }

    /// <summary>
    /// Method for the host to send the evaluation of all player answers to the clients.
    /// </summary>
    /// <param name="feedback"></param>
    private void SendChallengeFeedback(AnswerFeedbackMessage feedback)
    {
        if (!isHost)
            return;

        channel.RaiseAction(channel.SendChallengeFeedback, feedback);
    }

    /// <summary>
    /// Method for the host to inform the lobby about the game's end.
    /// </summary>
    private void SendGameOver()
    {
        if (!isHost)
            return;

        //Calculate the player scores
        List<int> winnerIndex = new List<int>();
        int winnerScore = 0;
        for (int i = 0; i < currentGame.playerScores.Length; i++)
        {
            if(currentGame.playerScores[i] > winnerScore)
            {
                winnerIndex = new List<int>();
                winnerScore = currentGame.playerScores[i];
                winnerIndex.Add(i);
            }
            else
            {
                winnerIndex.Add(i);
            }

        }

        //Construct end of game message and send it to all clients
        EndGameMessage endGameMessage = new EndGameMessage
        {
            winnerIndex = winnerIndex.ToArray(),
            playerNames = currentGame.playerNames,
            playerScores = currentGame.playerScores,
            playerColors = currentGame.playerColors,
            playerSpriteIndices = currentGame.playerSpriteIndices
        };
        channel.RaiseAction(channel.SendGameOver, endGameMessage);
    }

    /// <summary>
    /// Method for host to handle a players answer to a challenge and to check for the end of the round.
    /// </summary>
    /// <param name="answer"></param>
    private void ReceiveChallengeAnswer(ChallengeAnswerMessage answer)
    {
        if (!isHost)
            return;

        if (playerAnswers.ContainsKey(answer.ipAddress))
            return;

        playerAnswers.Add(answer.ipAddress, answer.answerIndex);

        if (playerAnswers.Count == currentGame.playerNames.Length)
            EvaluateAnswers();
    }

    /// <summary>
    /// Method for the host to evaluate the players answers at the end of the round and to start a new one.
    /// </summary>
    private void EvaluateAnswers()
    {
        if (!isHost)
            return;

        Dictionary<string, bool> playerFeedback = new Dictionary<string, bool>();

        //Construct the player feedback based on their performance
        if (currentChallenge.challengeType == ChallengeType.MultipleChoice)
        {
            foreach (var player in playerAnswers.Keys)
            {
                bool correctAnswer = playerAnswers[player] == currentChallenge.correctAnswerIndex;
                playerFeedback.Add(player, correctAnswer);
                if (correctAnswer)
                    currentGame.playerScores[FindPlayerIndex(player)]++;
            }
        }
        else if (currentChallenge.challengeType == ChallengeType.GuessingQuestion)
        {
            var sortedDict = from entry in playerAnswers orderby Mathf.Abs(entry.Value - currentChallenge.correctAnswerIndex) ascending select entry;

            var firstPlace = sortedDict.First();
            if (firstPlace.Value == float.MinValue)
                playerFeedback.Add(firstPlace.Key, false);
            else
            {
                playerFeedback.Add(firstPlace.Key, true);
                currentGame.playerScores[FindPlayerIndex(firstPlace.Key)]++;
            }

            foreach (var player in sortedDict)
            {

                if (player.Key != firstPlace.Key)
                {
                    if (firstPlace.Value == float.MinValue)
                        playerFeedback.Add(player.Key, false);
                    else if (player.Value == firstPlace.Value)
                    {
                        playerFeedback.Add(player.Key, true);
                        currentGame.playerScores[FindPlayerIndex(player.Key)]++;
                    }
                    else
                    {
                        playerFeedback.Add(player.Key, false);
                    }
                }
            }
        }

        //Construct feedback message and send it to each client individually
        AnswerFeedbackMessage feedback = new AnswerFeedbackMessage
        {
            playerScores = currentGame.playerScores
        };

        List<float> answers = new List<float>();
        foreach (var player in playerAnswers.Keys)
        {
            feedback.challengeSucceeded = playerFeedback[player];
            feedback.playerAddress = player;
            SendChallengeFeedback(feedback);

            answers.Add(playerAnswers[player]);
        }

        //Construct answer reveal message and publish it to lobby
        AnswerRevealStruct reveal = new AnswerRevealStruct
        {
            correctAnswerIndex = currentChallenge.correctAnswerIndex,
            playerAnswers = answers.ToArray()
        };

        OnRevealAnswer(reveal);
        UpdateGame();

        //Reset timer and start new round
        if(timer != null)
            StopCoroutine(timer);
        StartCoroutine(DelaySend(8.0f));
    }

    private int FindPlayerIndex(string ipAddress)
    {
        List<string> addresses = new List<string>(currentGame.clientAddresses);
        return addresses.IndexOf(ipAddress);
    }

    public int GetIndexOfAnswer(string answer)
    {
        List<string> answers = new List<string>(currentChallenge.answerOptions);

        if (answers.Contains(answer))
            return answers.IndexOf(answer);
        else
            throw new Exception("The answer: " + answer + "is not valid for this challenge!");
    }

    private ChallengeStruct GetNextChallenge()
    {
        return new ChallengeStruct();
    }

    public ChallengeStruct GetCurrentChallenge()
    {
        return currentChallenge;
    }

    private IEnumerator Timer(float time)
    {
        float timeDelta = 0;
        while(timeDelta < time)
        {
            timeDelta += Time.deltaTime;
            if (OnUpdateTimer != null)
                OnUpdateTimer.Invoke(1f - (timeDelta / time));
            yield return null;
        }

        foreach (var address in currentGame.clientAddresses)
        {
            if (!playerAnswers.ContainsKey(address))
                playerAnswers.Add(address, float.MinValue);
        }
        EvaluateAnswers();
    }
}