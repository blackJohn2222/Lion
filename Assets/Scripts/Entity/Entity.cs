using UnityEngine;

namespace Entity
{
    public abstract class Entity<T> : EntityBase where T : Entity<T>
    {
        public EntityStateManager<T> states { get; protected set; }

        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
            states = GetComponent<EntityStateManager<T>>();
            if (states == null)
            {
                throw new MissingComponentException(
                    $"{GetType().Name} requires an EntityStateManager<{typeof(T).Name}> component.");
            }
            
            isGrounded = controller.isGrounded;
        }

        protected virtual void Update()
        {
            states?.Step();
            controller.Move(velocity * Time.deltaTime);
            UpdateGround();
        }

        public virtual void UpdateGround()
        {
            var grounded = controller.isGrounded;
            if (grounded != isGrounded)
            {
                isGrounded = grounded;
                if (isGrounded)
                {
                    entityEvents?.OnGroundEnter?.Invoke();
                }
                else
                {
                    entityEvents?.OnGroundExit?.Invoke();
                }
            }
        }
        
        public void Accelerate(Vector3 direction, float maxAcceleration, float topSpeed)
        {
            if (direction.sqrMagnitude > 0)
            {
                var dir = direction.normalized;
                var dotSpeed = Vector3.Dot(dir, lateralVelocity);
                var addSpeed = topSpeed - dotSpeed;
                if (addSpeed > 0)
                {
                    var maxAccele = maxAcceleration * topSpeed * Time.deltaTime;
                    lateralVelocity += dir * Mathf.Min(maxAccele, addSpeed);
                }
            }
        }

        public void Decelerate(float friction, float stopSpeed)
        {
            var speed = lateralVelocity.magnitude;
            if (speed <= 0)
            {
                return;
            }
            var control = Mathf.Max(speed, stopSpeed);
            var drop = control * friction * Time.deltaTime;
            var newspeed = Mathf.Max(0f, speed - drop);
            lateralVelocity *= newspeed / speed;
        }

        public void SnapToGround(float snapForce)
        {
            if (isGrounded && verticalVelocity.y <= 0)
            {
                verticalVelocity = Vector3.down * snapForce;
            }
        }

        public void Gravity(float gravity)
        {
            if (!isGrounded)
            {
                verticalVelocity += Vector3.down * gravity * Time.deltaTime;
            }
        }

        public void Jump(float jumpHeight, float gravity)
        {
            var speed = Mathf.Sqrt(2f * jumpHeight * gravity);
            verticalVelocity = Vector3.up * speed;
        }
    }
}
