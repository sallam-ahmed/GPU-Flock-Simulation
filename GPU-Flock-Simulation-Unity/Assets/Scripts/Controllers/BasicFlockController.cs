using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;

namespace FlockSimulation.GPU
{
    public class BasicFlockController : MonoBehaviour
    {
        private const int BoidsGroupSizeX = 512;
        private const int BoidsGroupSizeY = 2;
        private string BufferName = "boidBuffer";
        public ComputeShader FlockingComputeShader;
        public int BoidsCount;
        public float SpawnRadius;

        [Header("Boid Rendering")]
        public Mesh BoidMesh;
        public List<Material> MaterialsList;

        [Header("Boid Behaviour")]
        public Transform Target;
        public float RotationSpeed = 1f;
        public float BoidSpeed = 1f;
        public float NeighbourDistance = 1f;
        [Header("Radius Control")]
        public float FleeRadius;
        public float AlignmentRadius;
        public float CohesionRadius;
        public float SeparationRadius;

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
        private int drawCallsCount;
        private readonly Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        private void Start()
        {
            ConstructBuffer(BoidMesh);
            this.boidsData = new BoidGPU[this.BoidsCount];
            this.kernelHandle = FlockingComputeShader.FindKernel("CSMain");
            for (int i = 0; i < this.BoidsCount; i++)
            {
                this.boidsData[i] = this.CreateBoidData();
            }
            boidComputeBuffer = new ComputeBuffer(BoidsCount, 32);
            boidComputeBuffer.SetData(this.boidsData);

            FlockingComputeShader.SetInt("BoidsCount", BoidsCount);
            FlockingComputeShader.SetBuffer(kernelHandle, BufferName, boidComputeBuffer);
        }

        private void Update()
        {
            //Crucial
            FlockingComputeShader.SetFloat("DeltaTime", Time.deltaTime);
            //Realtime Control!           
            UpdateBufferParams();
            FlockingComputeShader.Dispatch(kernelHandle, BoidsCount / BoidsGroupSizeX + 1, BoidsGroupSizeY, 1);
            for (int i = 0; i < drawCallsCount; i++)
            {
                MaterialsList[i].SetBuffer(BufferName, boidComputeBuffer);
                Graphics.DrawMeshInstancedIndirect(BoidMesh, i, MaterialsList[i], bounds, drawArgsBuffer, i * 5 * sizeof(uint));
            }
        }

        private void OnDestroy()
        {
            if (boidComputeBuffer != null) boidComputeBuffer.Release();
            if (drawArgsBuffer != null) drawArgsBuffer.Release();
        }

        private void OnDrawGizmosSelected() => DrawGizmos(true);

        private void OnDrawGizmos() => DrawGizmos(false);

        private void ConstructBuffer(Mesh meshTemplate)
        {
            List<uint> renderArgsList = new List<uint>(5);
            drawCallsCount = 0;
            for (int i = 0; i < meshTemplate.subMeshCount; i++)
            {
                uint meshIndexStart = meshTemplate.GetIndexStart(i);
                uint meshBaseVertex = meshTemplate.GetBaseVertex(i);
                uint meshIndexCount = meshTemplate.GetIndexCount(i);
                renderArgsList.Add(meshIndexCount);
                renderArgsList.Add((uint)BoidsCount);
                renderArgsList.Add(meshIndexStart);
                renderArgsList.Add(meshBaseVertex);
                renderArgsList.Add(0);
                drawCallsCount += 1;
            }
            drawArgsBuffer = new ComputeBuffer(1, drawCallsCount * 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            drawArgsBuffer.SetData(renderArgsList.ToArray());
        }

        private void DrawGizmos(bool selected)
        {
            Gizmos.color = selected ? new Color(0, 1, 0, 1) : new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(transform.position, SpawnRadius);
            Gizmos.DrawIcon(transform.position + Vector3.up, "BoidController");
        }

        private BoidGPU CreateBoidData()
        {
            BoidGPU boidData = new BoidGPU();
            Vector3 pos = transform.position + Random.insideUnitSphere * SpawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, Random.value);
            boidData.Position = pos;
            boidData.Direction = rot.eulerAngles;
            return boidData;
        }

        private void UpdateBufferParams()
        {
            FlockingComputeShader.SetFloat("RotationSpeed", RotationSpeed);
            FlockingComputeShader.SetFloat("BoidSpeed", BoidSpeed);
            FlockingComputeShader.SetVector("FlockingTargetPosition", Target.transform.position);
            FlockingComputeShader.SetFloat("NeighbourhoodRadius", NeighbourDistance);
            FlockingComputeShader.SetInt("AlignScale", AlignScale);
            FlockingComputeShader.SetInt("CohesionScale", CohesionScale);
            FlockingComputeShader.SetInt("SeparationScale", SeparationScale);

            FlockingComputeShader.SetFloat("FleeRadius", FleeRadius);
            FlockingComputeShader.SetFloat("AlignmentRadius", AlignmentRadius);
            FlockingComputeShader.SetFloat("CohesionRadius", CohesionRadius);
            FlockingComputeShader.SetFloat("SeparationRadius", SeparationRadius);
        }
    }
}