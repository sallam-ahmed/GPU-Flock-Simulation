using System.Collections.Generic;
using UnityEngine;

namespace FlockSimulation.GPU
{
    public static class MeshExtensions
    {
        public static ComputeBuffer CreateDrawComputeBuffer(this Mesh meshTemplate,int instancesCount, out int drawCallsCount)
        {
            List<uint> renderArgsList = new List<uint>(5);
            drawCallsCount = 0;
            for (int i = 0; i < meshTemplate.subMeshCount; i++)
            {
                uint meshIndexStart = meshTemplate.GetIndexStart(i);
                uint meshBaseVertex = meshTemplate.GetBaseVertex(i);
                uint meshIndexCount = meshTemplate.GetIndexCount(i);
                renderArgsList.Add(meshIndexCount);
                renderArgsList.Add((uint)instancesCount);
                renderArgsList.Add(meshIndexStart);
                renderArgsList.Add(meshBaseVertex);
                renderArgsList.Add(0);
                drawCallsCount += 1;
            }
            ComputeBuffer drawArgsBuffer = new ComputeBuffer(1, drawCallsCount * 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            drawArgsBuffer.SetData(renderArgsList.ToArray());
            return drawArgsBuffer;
        }

        public static ComputeBuffer CreateSkinnedAnimationComputeBuffer(this SkinnedMeshRenderer skinnedMeshRenderer, Animator animator, AnimationClip animationClip, out int framesCount)
        {
            AnimatorStateInfo aniStateInfo = animator.GetCurrentAnimatorStateInfo(0);

            Mesh bakedMesh = new Mesh();
            float sampleTime = 0;
            float perFrameTime = 0;

            framesCount = Mathf.ClosestPowerOfTwo((int)(animationClip.frameRate * animationClip.length));
            perFrameTime = animationClip.length / framesCount;

            int vertexCount = skinnedMeshRenderer.sharedMesh.vertexCount;
            
            Vector4[] vertexAnimationData = new Vector4[vertexCount * framesCount];
            for (int i = 0; i < framesCount; i++)
            {
                animator.Play(aniStateInfo.shortNameHash, 0, sampleTime);
                animator.Update(0f);

                skinnedMeshRenderer.BakeMesh(bakedMesh);

                for (int j = 0; j < vertexCount; j++)
                {
                    Vector3 vertex = bakedMesh.vertices[j];
                    vertexAnimationData[(j * framesCount) + i] = vertex;
                }

                sampleTime += perFrameTime;
            }

            ComputeBuffer vertexAnimationBuffer = new ComputeBuffer(vertexCount * framesCount, sizeof(float) * 4);
            vertexAnimationBuffer.SetData(vertexAnimationData.Clone() as Vector4[]);
            return vertexAnimationBuffer;
        }
    }
}