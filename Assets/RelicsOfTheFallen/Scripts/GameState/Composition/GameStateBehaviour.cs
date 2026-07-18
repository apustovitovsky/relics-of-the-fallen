using GameStateId = RelicsOfTheFallen.GameState.GameState;
using UnityEngine;
using VContainer.Unity;

namespace RelicsOfTheFallen.GameState.Composition
{
    public abstract class GameStateBehaviour : LifetimeScope
    {
        static GameObject s_ActiveStateGameObject;

        public virtual bool Persists => false;

        public abstract GameStateId ActiveState { get; }

        protected override void Awake()
        {
            base.Awake();

            if (Parent != null)
            {
                Parent.Container.Inject(this);
            }
        }

        protected virtual void Start()
        {
            if (s_ActiveStateGameObject != null)
            {
                if (s_ActiveStateGameObject == gameObject)
                {
                    return;
                }

                var previousState =
                    s_ActiveStateGameObject.GetComponent<GameStateBehaviour>();

                if (previousState.Persists &&
                    previousState.ActiveState == ActiveState)
                {
                    Destroy(gameObject);
                    return;
                }

                Destroy(s_ActiveStateGameObject);
            }

            s_ActiveStateGameObject = gameObject;

            if (Persists)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected override void OnDestroy()
        {
            if (!Persists)
            {
                s_ActiveStateGameObject = null;
            }

            base.OnDestroy();
        }
    }
}
