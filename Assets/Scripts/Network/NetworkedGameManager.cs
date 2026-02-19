using System.Collections.Generic;
using System.Linq;
using Core;
using Fusion;
using TMPro;
using UnityEngine;

namespace Network
{
    public class NetworkedGameManager : NetworkBehaviour
    {
        public static NetworkedGameManager Instance { get; private set; }

        #region Public Variables
        [SerializeField] private NetworkPrefabRef playerPrefab;
        [SerializeField] private TextMeshProUGUI _playerCountText;
        [SerializeField] private TextMeshProUGUI _timerCountText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] public TextMeshProUGUI _currentRecipeText;
        [SerializeField] public Core.Pot pot;
        [SerializeField] private Transform[] spawnPoints;
        #endregion

        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();
        [Networked, Capacity(4)] private NetworkDictionary<PlayerRef, int> _playerScores => default;

        [SerializeField] private int maxPlayers = 2;
        private const int timerBeforeStart = 3;
        private const float gameDuration = 120f;
        private bool hasGameStarted = false;

        #region Networked Properties
        [Networked] public TickTimer RoundStartTimer { get; set; }
        [Networked] public TickTimer GameTimer { get; set; }
        #endregion

        public override void Spawned()
        {
            base.Spawned();
            Instance = this;
            NetworkSessionManager.Instance.OnPlayerJoinedEvent += OnPlayerJoined;
            NetworkSessionManager.Instance.OnPlayerLeftEvent += OnPlayerLeft;

            if (HasStateAuthority)
            {
                int playerCount = Object.Runner.ActivePlayers.Count();
                Debug.Log($"[Host] GameManager Spawned. Players connected: {playerCount}");
                if (playerCount >= maxPlayers)
                    RoundStartTimer = TickTimer.CreateFromSeconds(Object.Runner, timerBeforeStart);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            NetworkSessionManager.Instance.OnPlayerJoinedEvent -= OnPlayerJoined;
            NetworkSessionManager.Instance.OnPlayerLeftEvent -= OnPlayerLeft;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (RoundStartTimer.IsRunning && RoundStartTimer.Expired(Object.Runner))
            {
                RoundStartTimer = default;
                OnGameStarted();
            }

            if (GameTimer.IsRunning && GameTimer.Expired(Object.Runner))
            {
                GameTimer = default;
                OnGameOver();
            }
        }

        public override void Render()
        {
            // Player count
            _playerCountText.text = $"Players: {Object.Runner.ActivePlayers.Count()}/{maxPlayers}";

            // Timer
            if (RoundStartTimer.IsRunning)
                _timerCountText.text = $"Starting in: {Mathf.CeilToInt(RoundStartTimer.RemainingTime(Object.Runner).Value)}";
            else if (GameTimer.IsRunning)
                _timerCountText.text = $"Time: {Mathf.CeilToInt(GameTimer.RemainingTime(Object.Runner).Value)}";
            else
                _timerCountText.text = "Game Over!";

            // Scores
            string scores = "Scores:\n";
            foreach (var kvp in _playerScores)
            {
                scores += $"Player {kvp.Key.PlayerId}: {kvp.Value}\n";
            }
            _scoreText.text = scores;

            // Current recipe UI
            if (pot != null && pot.CurrentRecipe != null)
                _currentRecipeText.text = $"Current Recipe: {pot.CurrentRecipe.RecipeName}";
            else
                _currentRecipeText.text = "Current Recipe: None";
        }

        // Award points safely
        public void AwardPoints(PlayerRef player, int points)
        {
            if (!HasStateAuthority) return;

            if (_playerScores.ContainsKey(player))
            {
                int current = _playerScores.Get(player);
                _playerScores.Set(player, current + points);
                Debug.Log($"Player {player.PlayerId} awarded {points} points. Total: {_playerScores.Get(player)}");
            }
        }

        private void OnPlayerJoined(PlayerRef player)
        {
            if (!HasStateAuthority) return;

            if (Object.Runner.ActivePlayers.Count() >= maxPlayers)
                RoundStartTimer = TickTimer.CreateFromSeconds(Object.Runner, timerBeforeStart);
        }

        private void OnPlayerLeft(PlayerRef player)
        {
            if (!HasStateAuthority) return;
            if (!_spawnedCharacters.TryGetValue(player, out var networkObject)) return;
            Object.Runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }

        private void OnGameStarted()
        {
            if (!HasStateAuthority || hasGameStarted) return;

            hasGameStarted = true;
            GameTimer = TickTimer.CreateFromSeconds(Object.Runner, gameDuration);

            int index = 0;
            foreach (var playerRef in Object.Runner.ActivePlayers)
            {
                Vector3 spawnPosition = spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null
                    ? spawnPoints[index].position : Vector3.zero;

                Quaternion spawnRotation = spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null
                    ? spawnPoints[index].rotation : Quaternion.identity;

                var networkObject = Object.Runner.Spawn(playerPrefab, spawnPosition, spawnRotation, playerRef);
                _spawnedCharacters.Add(playerRef, networkObject);
                _playerScores.Set(playerRef, 0);
                index++;
            }
        }

        private void OnGameOver()
        {
            Debug.Log("Game Over!");
        }
    }
}