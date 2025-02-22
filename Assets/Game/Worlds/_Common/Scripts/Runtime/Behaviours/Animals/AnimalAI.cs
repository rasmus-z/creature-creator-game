// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Unity.Netcode;

namespace DanielLochner.Assets.CreatureCreator
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AnimalAI : StateMachine
    {
        #region Fields
        [SerializeField] private TriggerRegion ignoreTrigger;

        private List<TriggerRegion> triggerRegions;
        private List<TrackRegion> trackRegions;
        #endregion

        #region Properties
        public AnimatorParams Params => Creature.Animator.Params;
        public Animator Animator => Creature.Animator.Animator;

        public CreatureNonPlayerLocal Creature { get; set; }
        public NavMeshAgent Agent { get; set; }

        public bool PVE
        {
            get;
            set;
        }
        public bool CanFollow
        {
            get => currentState is Idling || currentState is Following;
        }
        public bool IsMovingToPosition
        {
            get
            {
                if (Agent.isPathStale || Agent.isStopped)
                {
                    return false;
                }
                else if (Agent.pathPending)
                {
                    return true;
                }

                return (Agent.remainingDistance > Agent.stoppingDistance);
            }
        }
        #endregion

        #region Methods
        public void Awake()
        {
            Creature = GetComponent<CreatureNonPlayerLocal>();
            Agent = GetComponent<NavMeshAgent>();

            if (ignoreTrigger != null)
            {
                triggerRegions = new List<TriggerRegion>(GetComponentsInChildren<TriggerRegion>(true));
                trackRegions = new List<TrackRegion>(GetComponentsInChildren<TrackRegion>(true));
                triggerRegions.Remove(ignoreTrigger);
            }
        }

        public void Setup()
        {
            Agent.speed *= Creature.Constructor.Statistics.Speed;
        }

        public virtual void Follow(Transform target)
        {
            GetState<Following>("FOL").Target = target;
            ChangeState("FOL");
        }
        public virtual void StopFollowing()
        {
            ChangeState(startStateID);
            GetState<Following>("FOL").Target = null;
        }

        public bool IsAnimationState(string state)
        {
            return Animator.GetCurrentAnimatorStateInfo(0).IsName(state);
        }
        
        public void UpdateIgnored()
        {
            List<string> ignored = new List<string>();
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                CreaturePlayer player = client.Value.PlayerObject.GetComponent<CreaturePlayer>();
                if (Creature.Comparer.CompareTo(player.Constructor))
                {
                    ignored.Add(player.name);
                }
            }

            foreach (TriggerRegion region in triggerRegions)
            {
                region.ignored = ignored;
            }
            foreach (TrackRegion region in trackRegions)
            {
                region.ignored = ignored;
            }
        }

        #region Debug
        [ContextMenu("Debug/Follow/Player")]
        public void FollowPlayer()
        {
            Follow(Player.Instance.transform);
        }
        #endregion
        #endregion

        #region Nested
        [Serializable]
        public class Idling : BaseState
        {
            [SerializeField] private MinMax ambientCooldown;
            [SerializeField] private MinMax actionCooldown;
            [SerializeField] private PlayerEffects.Sound[] ambientSounds;
            [SerializeField] private string[] actions;
            protected float silentTimeLeft, actionTimeLeft;

            public AnimalAI AnimalAI => StateMachine as AnimalAI;

            public override void Enter()
            {
                silentTimeLeft = ambientCooldown.Random;
                actionTimeLeft = actionCooldown.Random;
            }
            public override void UpdateLogic()
            {
                if (ambientSounds.Length > 0)
                {
                    TimerUtility.OnTimer(ref silentTimeLeft, ambientCooldown.Random, Time.deltaTime, MakeSound);
                }
                if (actions.Length > 0 && AnimalAI.IsAnimationState("Idling"))
                {
                    TimerUtility.OnTimer(ref actionTimeLeft, actionCooldown.Random, Time.deltaTime, PerformAction);
                }
            }

            private void MakeSound()
            {
                AnimalAI.StartCoroutine(MakeSoundRoutine());
            }
            private IEnumerator MakeSoundRoutine()
            {
                PlayerEffects.Sound sound = ambientSounds[UnityEngine.Random.Range(0, ambientSounds.Length)];

                // Open
                AnimalAI.Params.SetBool("Mouth_IsOpen", true);
                AnimalAI.Creature.Effects.PlaySound(sound.name, sound.volume);

                // Hold (to make the sound)...
                yield return new WaitForSeconds(AnimalAI.Creature.Effects.SoundFX[sound.name].length);

                // Close
                AnimalAI.Params.SetBool("Mouth_IsOpen", false);
            }

            private void PerformAction()
            {
                string action = actions[UnityEngine.Random.Range(0, actions.Length)];
                AnimalAI.Params.SetTrigger(action);
            }
        }

        [Serializable]
        public class Wandering : Idling
        {
            [SerializeField] private MinMax wanderCooldown;
            [SerializeField] public Bounds wanderBounds;
            private float idleTimeLeft;

            public override void Enter()
            {
                base.Enter();
                idleTimeLeft = wanderCooldown.Random;
                AnimalAI.Agent.SetDestination(AnimalAI.transform.position);
            }
            public override void UpdateLogic()
            {
                base.UpdateLogic();
                if (!AnimalAI.IsMovingToPosition && AnimalAI.IsAnimationState("Idling"))
                {
                    TimerUtility.OnTimer(ref idleTimeLeft, wanderCooldown.Random, Time.deltaTime, delegate
                    {
                        WanderToPosition(wanderBounds?.RandomPointInBounds ?? AnimalAI.transform.position);
                    });
                }
            }
            public override void Exit()
            {
                AnimalAI.Agent.SetDestination(AnimalAI.transform.position);
            }

            private void WanderToPosition(Vector3 position)
            {
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    position = hit.position;
                }
                AnimalAI.Agent.SetDestination(position);
            }
        }

        [Serializable]
        public class Following : BaseState
        {
            [SerializeField] private float baseFollowOffset;
            [SerializeField] private UnityEvent onFollow;
            [SerializeField] private UnityEvent onStopFollowing;

            public AnimalAI AnimalAI => StateMachine as AnimalAI;
            public Transform Target { get; set; }

            private float FollowOffset => AnimalAI.Creature.Constructor.Dimensions.radius + baseFollowOffset;

            public override void Enter()
            {
                onFollow.Invoke();
            }
            public override void UpdateLogic()
            {
                Vector3 displacement = Target.position - AnimalAI.transform.position;
                if (displacement.magnitude > FollowOffset)
                {
                    Vector3 offset = 0.99f * FollowOffset * displacement.normalized; // offset slightly closer to target
                    Vector3 target = AnimalAI.transform.position + (displacement - offset);
                    AnimalAI.Agent.SetDestination(target);
                }
            }
            public override void Exit()
            {
                onStopFollowing.Invoke();
            }
        }

        public class Targeting : BaseState
        {
            [SerializeField] protected TrackRegion trackRegion;
            [SerializeField] protected float lookAtSmoothing;

            protected CreatureBase target;
            protected Vector3 lookDir;

            public AnimalAI AnimalAI => StateMachine as AnimalAI;

            protected void UpdateTarget()
            {
                Transform nearest = trackRegion.Nearest.transform;
                if (target == null || target.transform != nearest)
                {
                    target = nearest.GetComponent<CreatureBase>();
                }
            }
            protected void HandleLookAt()
            {
                StateMachine.transform.rotation = Quaternion.Slerp(StateMachine.transform.rotation, Quaternion.LookRotation(lookDir), lookAtSmoothing * Time.deltaTime);
            }
            protected void UpdateLookDir()
            {
                lookDir = Vector3.ProjectOnPlane(target.transform.position - StateMachine.transform.position, StateMachine.transform.up).normalized;
            }

            public override void Enter()
            {
                base.Enter();
                AnimalAI.Agent.updateRotation = false;
            }

            public override void Exit()
            {
                base.Exit();
                AnimalAI.Agent.updateRotation = true;
            }
        }
        #endregion
    }
}