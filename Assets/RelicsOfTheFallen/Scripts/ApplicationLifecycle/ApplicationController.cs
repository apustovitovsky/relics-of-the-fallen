using System.Collections;
using Mirror;
using RelicsOfTheFallen.ConnectionManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace RelicsOfTheFallen.ApplicationLifecycle
{
    public sealed class ApplicationController : LifetimeScope
    {
        [SerializeField]
        ConnectionManager m_ConnectionManager;

        [SerializeField]
        NetworkManager m_NetworkManager;

        protected override void Configure(
            IContainerBuilder builder)
        {
            builder.RegisterComponent(m_ConnectionManager);
            builder.RegisterComponent(m_NetworkManager);
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(m_ConnectionManager.gameObject);
            DontDestroyOnLoad(m_NetworkManager.gameObject);

            StartCoroutine(LoadMainMenu());
        }

        IEnumerator LoadMainMenu()
        {
            using (LifetimeScope.EnqueueParent(this))
            {
                yield return SceneManager.LoadSceneAsync(
                    "MainMenu",
                    LoadSceneMode.Additive);
            }

            var mainMenuScene = SceneManager.GetSceneByName(
                "MainMenu");

            SceneManager.SetActiveScene(mainMenuScene);

            yield return SceneManager.UnloadSceneAsync(
                "Startup");
        }
    }
}