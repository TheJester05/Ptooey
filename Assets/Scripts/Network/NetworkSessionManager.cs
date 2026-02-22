using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
    public class NetworkSessionManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        #region Public Variables
        [SerializeField] private NetworkPrefabRef playerPrefab;

        public static NetworkSessionManager Instance
        {
            get; private set;
        }
        #endregion
    
        #region Private Variables
        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();
        private NetworkRunner _networkRunner;
        private List<PlayerRef> _joinedPlayers = new();
        public IReadOnlyList<PlayerRef> JoinedPlayers => _joinedPlayers;
    
        public event Action<PlayerRef> OnPlayerJoinedEvent;
        public event Action<PlayerRef> OnPlayerLeftEvent;
    
        #endregion

        public async void StartGame(GameMode game)
        {
            _networkRunner = GetComponent<NetworkRunner>();
            if (_networkRunner == null)
            {
                _networkRunner = gameObject.AddComponent<NetworkRunner>();
            }
            _networkRunner.ProvideInput = true;
        
            var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            var sceneInfo = new NetworkSceneInfo();
            if(scene.IsValid)
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);

            var sceneManager = GetComponent<INetworkSceneManager>();
            if (sceneManager == null)
            {
                sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            await _networkRunner.StartGame(new StartGameArgs()
            {
                GameMode = game,
                SessionName = "TestSession",
                Scene = scene,
                SceneManager = sceneManager
            });
        }
    
        #region Unity Callbacks

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if SERVER
            StartGame(GameMode.Server);
#elif CLIENT
            StartGame(GameMode.Client);
#else
            // Fallback for editor or builds without specific defines
            StartGame(Application.isEditor ? GameMode.AutoHostOrClient : GameMode.Client);
#endif
        }

        #endregion
    
        #region Used Fusion Callbacks
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData();
            data.InputVector = new Vector2(Input.GetAxisRaw("Horizontal"), 
                Input.GetAxisRaw("Vertical"));
            data.JumpInput = Input.GetButton("Jump");

            data.MouseX = Input.GetAxis("Mouse X");

            if (Input.GetKey(KeyCode.LeftShift))
            {
                data.SprintInput = true;
            }

            if (Input.GetKey(KeyCode.E))
            {
                data.InteractInput = true;
            }
            
            input.Set(data);
        }
    
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer) return;
            _joinedPlayers.Add(player);
            OnPlayerJoinedEvent?.Invoke(player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            //if (!_spawnedCharacters.TryGetValue(player, out var playerObject)) return;
            _joinedPlayers.Remove(player);
            OnPlayerLeftEvent?.Invoke(player);
        }
    
        #endregion

        #region Unused Fusion Callbacks
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        
        }
    
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
        
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
        
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
        
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
        
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
        
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        
        }

    
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
        
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
        
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
        
        }
    
        #endregion
    }
}
