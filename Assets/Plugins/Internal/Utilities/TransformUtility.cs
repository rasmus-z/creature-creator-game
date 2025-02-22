﻿using UnityEngine;

namespace DanielLochner.Assets
{
    public static class TransformUtility
    {
        public static void DestroyChildren(this Transform transform)
        {
            while (transform.childCount > 0)
            {
                Object.DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }

        public static void Set(this Transform transform, SerializableTransform serializableTransform, Transform world)
        {
            transform.position = world.TransformPoint(serializableTransform.position);
            transform.localScale = serializableTransform.scale;
            transform.rotation = world.rotation * serializableTransform.rotation;
        }
        
        public static void SetWorldScale(this Transform transform, Vector3 scale)
        {
            if (transform.parent == null)
            {
                transform.localScale = scale;
            }
            else
            {
                Vector3 parentWorldScale = transform.parent.lossyScale;
                float x = scale.x / parentWorldScale.x;
                float y = scale.y / parentWorldScale.y;
                float z = scale.z / parentWorldScale.z;

                transform.localScale = new Vector3(x, y, z);
            }
        }
		
		public static void SetZeroParent(this Transform transform, Transform parent)
		{
			transform.parent = parent;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}

        public static void LerpTo(this Transform transform, Vector3 position, float smoothing)
        {
            transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * smoothing);
        }
        public static void SlerpTo(this Transform transform, Quaternion rotation, float smoothing)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * smoothing);
        }

        public static Vector3 L2WSpace(this Transform transform, Vector3 position)
        {
            return transform.TransformPoint(position);
        }
        public static Vector3 W2LSpace(this Transform transform, Vector3 position)
        {
            return transform.InverseTransformPoint(position);
        }
        public static Quaternion L2WSpace(this Transform transform, Quaternion rotation)
        {
            return transform.rotation * rotation;
        }
        public static Quaternion W2LSpace(this Transform transform, Quaternion rotation)
        {
            return Quaternion.Inverse(transform.rotation) * rotation;
        }
    }
}