using System;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;

namespace FlockSimulation.GPU
{
    public class BasicFlockController : MonoBehaviour
    {
        private const int BoidsGroupSizeX = 256;
        private const string BufferName = "boidBuffer";
        private const string AnimationBufferName = "vertexAnimationBuffer";

        [Header("Setup")]
        public ComputeShader FlockingComputeShader;
        public int BoidsCount;
        public int PredatorsCount;
        public float SpawnRadius;

        [Header("Boid Rendering")]
        public Mesh BoidMesh;
        public List<Material> MaterialsList;
        public Bounds RenderBounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        [Header("Animation")] public GameObject SampleBoid;
        public AnimationClip BoidAnimationClip;
        public float AnimationFrameSpeed;
        public bool FrameInterpolation;


        [Header("Boid Behaviour")]
        public Transform Target;
        public float RotationSpeed = 1f;
        public float BoidSpeed = 1f;
        [Range(1f, 3f)]
        public float PredatorSpeedMultiplier = 2f;
        public float NeighbourDistance = 1f;

        [Header("Radius Control")]
        public float FleeRadius;
        public float AlignmentRadius;
        public float CohesionRadius;
        public float SeparationRadius;
        public float PredatorHuntRadius;

        [Header("Behaviours Control")]
        [Range(1, 5)]
        public int AlignScale;
        [Range(1, 5)]
        public int CohesionScale;
        [Range(1, 5)]
        public int SeparationScale;

        
        private BoidGPU[] boidsData;
        private int kernelHandle;
        private ComputeBuffer boidComputeBuffer;
        private ComputeBuffer drawArgsBuffer;
        private ComputeBuffer animationBuffer;
        private int drawCallsCount;

        private SkinnedMeshRenderer boidSkinnedMeshRenderer;
        private Animator boidAnimator;
        private int framesCount;
        private bool isFrameInterpolationEnabled;

        private void Start()
        {
            Setup();
            UpdateBufferParams();
        }

        private void Update()
        {
            //Crucial
            FlockingComputeShader.SetFloat("DeltaTime", Time.deltaTime);
            //Realtime Control!           
            UpdateBufferParams();
            FlockingComputeShader.Dispatch(kernelHandle, BoidsCount / BoidsGroupSizeX + 1, 1, 1);
            for (int i = 0; i < drawCallsCount; i++)
            {
                MaterialsList[i].SetBuffer(BufferName, boidComputeBuffer);
                MaterialsList[i].SetInt("FramesCount", framesCount);
                Graphics.DrawMeshInstancedIndirect(BoidMesh, i, MaterialsList[i], RenderBounds, drawArgsBuffer, i * 5 * sizeof(uint));
            }
        }

        private void OnDestroy()
        {
            if (boidComputeBuffer != null) boidComputeBuffer.Release();
            if (drawArgsBuffer != null) drawArgsBuffer.Release();
            if (animationBuffer != null) animationBuffer.Release();
        }

        private void OnDrawGizmosSelected() => DrawGizmos(true);

        private void OnDrawGizmos() => DrawGizmos(false);

        private void DrawGizmos(bool selected)
        {
            Gizmos.color = selected ? new Color(0, 1, 0, 1) : new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position, SpawnRadius);
            Gizmos.DrawIcon(transform.position + Vector3.up, "BoidController");
            if (selected)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(RenderBounds.center, RenderBounds.size);
            }
        }

        private void Setup()
        {
            
            drawArgsBuffer = BoidMesh.CreateDrawComputeBuffer(BoidsCount, out drawCallsCount);

            this.boidsData = new BoidGPU[this.BoidsCount];
            this.kernelHandle = FlockingComputeShader.FindKernel("CSMain");
            if (PredatorsCount > BoidsCount)
            {
                throw new Exception("Unable to Start Simulation, Predators count higher than Boids Total Count");
            }

            for (int i = BoidsCount - 1; i >= BoidsCount - PredatorsCount; i--)
            {
                boidsData[i] = CreatePredator();
            }

            for (int i = 0; i < this.BoidsCount - PredatorsCount; i++)
            {
                this.boidsData[i] = this.CreateBoidData();
            }

            boidComputeBuffer = new ComputeBuffer(BoidsCount, 48);
            boidComputeBuffer.SetData(this.boidsData);

            FlockingComputeShader.SetInt("BoidsCount", BoidsCount);
            FlockingComputeShader.SetBuffer(kernelHandle, BufferName, boidComputeBuffer);

            SetupAnimationProperties();
        }

        private void SetupAnimationProperties()
        {
            SampleBoid.SetActive(true);
            boidSkinnedMeshRenderer = SampleBoid.GetComponentInChildren<SkinnedMeshRenderer>();
            boidAnimator = SampleBoid.GetComponentInChildren<Animator>();
            boidAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            animationBuffer = boidSkinnedMeshRenderer.CreateSkinnedAnimationComputeBuffer(boidAnimator, BoidAnimationClip, out framesCount);

            MaterialsList.ForEach(material =>
            {
                material.SetBuffer(AnimationBufferName, animationBuffer);
                material.SetInt("FramesCount", framesCount);
                if (FrameInterpolation)
                {
                    material.EnableKeyword("FRAME_INTERPOLATION");
                }
                else
                {
                    material.DisableKeyword("FRAME_INTERPOLATION");
                }
            });
            isFrameInterpolationEnabled = FrameInterpolation;
            SampleBoid.SetActive(false);
        }

        private BoidGPU CreateBoidData()
        {
            BoidGPU boidData = new BoidGPU();
            Vector3 pos = transform.position + Random.insideUnitSphere * SpawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, Random.value);
            boidData.Position = pos;
            boidData.Direction = rot.eulerAngles;
            boidData.IsPredator = 0;
            return boidData;
        }

        private BoidGPU CreatePredator()
        {
            BoidGPU boidData = new BoidGPU();
            Vector3 pos = transform.position + Random.insideUnitSphere * SpawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, Random.value);
            boidData.Position = pos;
            boidData.Direction = rot.eulerAngles;
            boidData.IsPredator = 1;
            return boidData;
        }

        private void UpdateBufferParams()
        {
            FlockingComputeShader.SetFloat("RotationSpeed", RotationSpeed);
            FlockingComputeShader.SetFloat("BoidSpeed", BoidSpeed);
            FlockingComputeShader.SetFloat("PredatorSpeedMultiplier", PredatorSpeedMultiplier);
            FlockingComputeShader.SetVector("FlockingTargetPosition", Target.transform.position);
            FlockingComputeShader.SetFloat("NeighbourhoodRadius", NeighbourDistance);
            FlockingComputeShader.SetInt("AlignScale", AlignScale);
            FlockingComputeShader.SetInt("CohesionScale", CohesionScale);
            FlockingComputeShader.SetInt("SeparationScale", SeparationScale);

            FlockingComputeShader.SetFloat("PredatorHuntRadius", PredatorHuntRadius);
            FlockingComputeShader.SetFloat("FleeRadius", FleeRadius);
            FlockingComputeShader.SetFloat("AlignmentRadius", AlignmentRadius);
            FlockingComputeShader.SetFloat("CohesionRadius", CohesionRadius);
            FlockingComputeShader.SetFloat("SeparationRadius", SeparationRadius);

            FlockingComputeShader.SetFloat("AnimationFrameSpeed", AnimationFrameSpeed);
            FlockingComputeShader.SetInt("FramesCount", framesCount);

            if (FrameInterpolation && !isFrameInterpolationEnabled)
            {
                MaterialsList.ForEach(material => material.EnableKeyword("FRAME_INTERPOLATION"));
                isFrameInterpolationEnabled = true;
            }

            if (!FrameInterpolation && isFrameInterpolationEnabled)
            {
                MaterialsList.ForEach(material => material.DisableKeyword("FRAME_INTERPOLATION"));
                isFrameInterpolationEnabled = false;
            }
        }
    }
}