using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GAS
{
    [Serializable]
    public class PlayerController : MonoBehaviour
    {
        [Header("Gameplay")]
        public AbilitySystemComponent asc;
        public bool selfCastIfNoTarget = true;
        public float moveSpeed = 10f;
        public float rotateSpeed = 100f;
        public List<AbilitySystemComponent> targets = new List<AbilitySystemComponent>();
        public Collider targetChecker;
        public Material enemyMaterial;
        public Material targetedMaterial;

        [Header("Attributes")]
        public AttributeName movementSpeed;
        public GameplayTag jumpTag;

        [Header("Input")]
        [SerializeField] InputActionAsset m_InputActions;

        public int selectedAbilityIndex;
        public Action OnSelectAbility;

        InputActionMap m_GameplayActions;
        InputAction m_MoveAction;
        InputAction m_TurnAction;
        InputAction m_StrafeAction;
        InputAction m_ScrollAbilityAction;
        InputAction m_ActivateSelectedAction;
        InputAction m_JumpAction;
        InputAction m_DashAction;
        readonly InputAction[] m_AbilityActions = new InputAction[10];

        void Awake()
        {
            asc = GetComponent<AbilitySystemComponent>();

            asc.OnAttributeChanged += OnAttributeChanged;
            asc.OnTagsInstant += OnTagsInstant;

            if (m_InputActions == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerController)} on '{name}' needs an Input Actions asset.",
                    this);

                enabled = false;
                return;
            }

            m_GameplayActions = m_InputActions.FindActionMap("Gameplay", true);
            m_MoveAction = m_GameplayActions.FindAction("Move", true);
            m_TurnAction = m_GameplayActions.FindAction("Turn", true);
            m_StrafeAction = m_GameplayActions.FindAction("Strafe", true);
            m_ScrollAbilityAction = m_GameplayActions.FindAction("ScrollAbility", true);
            m_ActivateSelectedAction = m_GameplayActions.FindAction("ActivateSelected", true);
            m_JumpAction = m_GameplayActions.FindAction("Jump", true);
            m_DashAction = m_GameplayActions.FindAction("Dash", true);

            for (var abilityIndex = 0; abilityIndex < m_AbilityActions.Length; abilityIndex++)
            {
                m_AbilityActions[abilityIndex] =
                    m_GameplayActions.FindAction($"Ability{abilityIndex}", true);
            }
        }

        void OnEnable()
        {
            m_GameplayActions?.Enable();
        }

        void OnDisable()
        {
            m_GameplayActions?.Disable();
        }

        void Update()
        {
            if (m_GameplayActions == null)
            {
                return;
            }

            float move = m_MoveAction.ReadValue<float>();
            float turn = m_TurnAction.ReadValue<float>();
            float strafe = m_StrafeAction.ReadValue<float>();
            float scroll = m_ScrollAbilityAction.ReadValue<Vector2>().y;

            if (scroll > 0f)
            {
                ScrollAbility(1);
            }
            else if (scroll < 0f)
            {
                ScrollAbility(-1);
            }

            if (m_ActivateSelectedAction.WasPressedThisFrame())
            {
                TryActivateAbilityCommand(selectedAbilityIndex);
            }

            if (move != 0f)
            {
                transform.position += transform.forward * move * moveSpeed * Time.deltaTime;
            }

            if (turn != 0f)
            {
                transform.Rotate(Vector3.up, turn * rotateSpeed * Time.deltaTime);
            }

            if (strafe != 0f)
            {
                transform.position += transform.right * strafe * moveSpeed * Time.deltaTime;
            }

            if (m_JumpAction.WasPressedThisFrame())
            {
                Jump();
            }

            if (m_DashAction.WasPressedThisFrame())
            {
                Dash();
            }

            for (var abilityIndex = 0; abilityIndex < m_AbilityActions.Length; abilityIndex++)
            {
                if (m_AbilityActions[abilityIndex].WasPressedThisFrame())
                {
                    TryActivateAbilityCommand(abilityIndex);
                }
            }
        }

        void FixedUpdate()
        {
            targets.ForEach(target =>
            {
                if (target != null)
                {
                    target.GetComponentInChildren<Renderer>().material = enemyMaterial;
                }
            });

            targets.Clear();

            var colliders = Physics
                .OverlapBox(targetChecker.bounds.center, targetChecker.bounds.extents)
                .ToList();

            colliders.Remove(targetChecker);
            colliders.RemoveAll(collider =>
                collider.GetComponent<AbilitySystemComponent>() == null);
            colliders.RemoveAll(collider =>
                collider.GetComponent<AbilitySystemComponent>() == asc);

            targets = colliders
                .Select(collider => collider.GetComponent<AbilitySystemComponent>())
                .ToList();

            targets.ForEach(target =>
                target.GetComponentInChildren<Renderer>().material = targetedMaterial);
        }

        public void TryActivateAbilityCommand(int abilityIndex)
        {
            if (abilityIndex < 0 ||
                abilityIndex >= asc.grantedGameplayAbilities.Count)
            {
                return;
            }

            if (selfCastIfNoTarget && targets.Count == 0)
            {
                targets.Add(asc);
            }

            if (asc.grantedGameplayAbilities[abilityIndex] is TargetedProjectileAbility)
            {
                targets = FindObjectsByType<AbilitySystemComponent>().ToList();
                targets.Remove(asc);
            }

            foreach (var target in targets)
            {
                asc.TryActivateAbility(abilityIndex, target);
            }

            if (targets.Contains(asc))
            {
                targets.Remove(asc);
            }
        }

        public void ScrollAbility(int scrollQuantity)
        {
            if ((selectedAbilityIndex == 0 && scrollQuantity < 0) ||
                (selectedAbilityIndex > asc.grantedGameplayAbilities.Count - 2 &&
                 scrollQuantity > 0))
            {
                return;
            }

            selectedAbilityIndex += scrollQuantity;
            OnSelectAbility?.Invoke();
        }

        public void Jump()
        {
            asc.TryActivateAbility("Jump", asc);
        }

        public void Dash()
        {
            asc.TryActivateAbility("Dash", asc);
        }

        void OnAttributeChanged(
            AttributeName attributeName,
            float oldValue,
            float newValue,
            GameplayEffect gameplayEffect)
        {
            if (attributeName == movementSpeed)
            {
                moveSpeed = newValue;
            }
        }

        void OnTagsInstant(
            List<GameplayTag> tags,
            AbilitySystemComponent source,
            AbilitySystemComponent target,
            string applicationGuid)
        {
            if (tags.Contains(jumpTag))
            {
                GetComponent<Rigidbody>().AddForce(
                    Vector3.up * 10f,
                    ForceMode.VelocityChange);
            }
        }
    }
}