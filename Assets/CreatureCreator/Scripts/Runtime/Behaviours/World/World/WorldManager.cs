// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DanielLochner.Assets.CreatureCreator
{
    public class WorldManager : MonoBehaviourSingleton<WorldManager>
    {
        public World World
        {
            get;
            set;
        }

        private void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }

        private void OnServerStarted()
        {
            NetworkManager.Singleton.SceneManager.LoadScene(World.MapName, LoadSceneMode.Single);
        }
        private void OnClientConnect(ulong clientID)
        {
            NetworkManager.Singleton.SceneManager.OnLoad += OnLoad;
        }
        private void OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation operation)
        {
            LoadingManager.Instance.StartCoroutine(LoadingManager.Instance.LoadRoutine(operation, delegate
            {
                NetworkManager.Singleton.SceneManager.OnLoad -= OnLoad;
            }));
        }
    }
}