using UnityEngine;

namespace FlockSimulation.GPU
{
    public struct BoidGPU
    {
        public Vector3 Position;
        public Vector3 Direction;
        public int IsPredator;
        public int State;
        public float Frame;
        public float NextFrame;
        public float FrameInterpolation;
        public float Padding;
    }

    /*
     *State Table
     *  1 Normal
     *  2 Being Chased by Predator
     *  3 Chasing
     */
}