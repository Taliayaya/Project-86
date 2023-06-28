using UnityEngine;

namespace Gameplay.Mecha
{
    public class Spring
    {
        public float Strength;
        public float Damper;
        public float Target;
        public float Velocity;
        public float Value;

        public void Update(float deltaTime)
        {
            var direction = Target - Value >= 0 ? 1f : -1f;
            var force = Mathf.Abs(Target - Value) * Strength;
            Velocity += (force * direction - Velocity * Damper) * deltaTime;
            Value += Velocity * deltaTime;
        }
        
        public void Reset()
        {
            Value = 0f;
            Velocity = 0f;
        }
    }
}