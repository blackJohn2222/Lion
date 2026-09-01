using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputManager : MonoBehaviour
    {
        public InputActionAsset asset;

        protected InputAction _movement;
        protected InputAction _jump;
        
        private float _jumpPressedAt = float.MinValue;

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
            _jump = asset["Jump"];
        }

        protected virtual void OnEnable()
        {
            asset?.Enable();
        }

        protected virtual void OnDisable()
        {
            asset?.Disable();
        }

        protected virtual void Update()
        {
            if (_jump != null && _jump.WasPressedThisFrame())
            {
                _jumpPressedAt = Time.time;
            }
        }

        public Vector2 GetMovement()
        {
            return _movement != null ? _movement.ReadValue<Vector2>() : Vector2.zero;
        }
        
        public bool GetJumpDown(float jumpBufferWindow)
        {
            if (Time.time <= _jumpPressedAt + jumpBufferWindow)
            {
                _jumpPressedAt = float.MinValue;
                return true;
            }
            return false;
        }
    }
}