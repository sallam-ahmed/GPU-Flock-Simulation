using UnityEngine;

namespace FlockSimulation.GPU
{
    public struct BoidGPU
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector2 Padding; // Pad to 32!
    }
}