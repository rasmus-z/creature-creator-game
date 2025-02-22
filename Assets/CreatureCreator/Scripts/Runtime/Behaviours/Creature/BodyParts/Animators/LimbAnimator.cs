﻿// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace DanielLochner.Assets.CreatureCreator
{
    [RequireComponent(typeof(LimbConstructor))]
    public class LimbAnimator : BodyPartAnimator
    {
        #region Fields
        [SerializeField] private bool useIK;

        protected Transform target, limb;
        protected ChainIKConstraint limbIK; //protected TwoBoneIKConstraint limbIK;
        
        protected Vector3[] defaultBonePositions;
        protected Quaternion[] defaultBoneRotations;
        protected Quaternion defaultExtremityRotation;
        #endregion

        #region Properties
        public LimbConstructor LimbConstructor => BodyPartConstructor as LimbConstructor;
        public LimbAnimator FlippedLimb => Flipped as LimbAnimator;

        public Transform Target => target;
        public ChainIKConstraint LimbIK => limbIK;
        public Vector3[] DefaultPositions => defaultBonePositions;
        #endregion

        #region Methods
        protected virtual void LateUpdate()
        {
            HandleTarget();
        }
        protected virtual void OnDestroy()
        {
            if (limbIK != null)
            {
                Destroy(limbIK.gameObject);
            }
        }

        public override void Setup(CreatureAnimator creatureAnimator)
        {
            base.Setup(creatureAnimator);

            limb = new GameObject("Limb").transform;
            limb.SetParent(creatureAnimator.Rig.transform.Find("Limbs"), false);

            target = new GameObject("Target").transform;
            target.SetParent(limb, false);

            if (useIK)
            {
                //if (LimbConstructor.Bones.Length == 3)
                //{
                //    limbIK = limb.gameObject.AddComponent<TwoBoneIKConstraint>();
                //    limbIK.Reset();

                //    limbIK.data.root = LimbConstructor.Bones[0];
                //    limbIK.data.mid = LimbConstructor.Bones[1];
                //    limbIK.data.tip = LimbConstructor.Bones[2];
                //    limbIK.data.target = target;
                //    limbIK.data.targetRotationWeight = 0;
                //}
                //else
                {
                    limbIK = limb.gameObject.AddComponent<ChainIKConstraint>();
                    limbIK.Reset();

                    limbIK.data.root = LimbConstructor.Bones[0];
                    limbIK.data.tip = LimbConstructor.Bones[LimbConstructor.Bones.Length - 1];
                    limbIK.data.target = target;
                    limbIK.data.tipRotationWeight = 0;
                }
            }

            LimbConstructor.OnSetAttached += delegate
            {
                limb.name = $"{name} (Constraint)";
                FlippedLimb.limb.name = $"{FlippedLimb.name} (Constraint)";
            };
        }

        public virtual void RestoreDefaults()
        {
            // Limb
            for (int i = 0; i < LimbConstructor.Bones.Length; i++)
            {
                LimbConstructor.Bones[i].position = CreatureAnimator.transform.TransformPoint(LimbConstructor.AttachedLimb.bones[i].position);
                LimbConstructor.Bones[i].rotation = CreatureAnimator.transform.rotation * LimbConstructor.AttachedLimb.bones[i].rotation;
            }

            // Connected Extremity
            if (LimbConstructor.ConnectedExtremity != null)
            {
                LimbConstructor.ConnectedExtremity.transform.rotation = CreatureAnimator.Constructor.Body.rotation * defaultExtremityRotation;
            }

            LimbConstructor.Flip();
        }
        public virtual void Restructure(bool isAnimated)
        {
            // Limb
            for (int i = 1; i < LimbConstructor.Bones.Length; i++)
            {
                LimbConstructor.Bones[i].SetParent(isAnimated ? LimbConstructor.Bones[i - 1] : LimbConstructor.Root);
            }

            // Connected Extremity
            if (LimbConstructor.ConnectedExtremity != null)
            {
                LimbConstructor.ConnectedExtremity.transform.SetParent(isAnimated ? LimbConstructor.Extremity : CreatureAnimator.Constructor.Bones[LimbConstructor.AttachedLimb.boneIndex]);
            }
        }
        public virtual void Reinitialize()
        {
        }

        protected virtual void HandleTarget()
        {
            target.rotation = LimbConstructor.Bones[LimbConstructor.Bones.Length - 1].rotation;
        }
        #endregion
    }
}