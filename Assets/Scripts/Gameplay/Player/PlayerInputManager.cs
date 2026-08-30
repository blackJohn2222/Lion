using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputManager : MonoBehaviour
    {
        public InputActionAsset asset;

        protected InputAction _movement;

        protected virtual void Awake()
        {
            if (asset != null)
            {
                CacheActions();
            }
        }

        private void CacheActions()
        {
            _movement = asset["Movement"];
        }

        protected virtual void OnEnable()
        {
            asset?.Enable();
        }

        protected virtual void OnDisable()
        {
            asset?.Disable();
        }

        public Vector2 GetMovement()
        {
            return _movement != null ? _movement.ReadValue<Vector2>() : Vector2.zero;
        }
    }
}