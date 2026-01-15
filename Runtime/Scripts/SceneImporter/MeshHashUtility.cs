using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityGLTF
{
    /// <summary>
    /// High-performance mesh hashing utility using Jobs and Burst.
    /// Computes hashes directly from vertex buffer streams without copying data.
    /// </summary>
    public static class MeshHashUtility
    {
        /// <summary>
        /// Burst-compiled job to compute xxHash64 from raw byte data.
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        private unsafe struct HashBufferJob : IJob
        {
            [ReadOnly, NativeDisableUnsafePtrRestriction]
            public IntPtr DataPtr;
            public int DataLength;
            public NativeReference<ulong> ResultHash;
            public ulong Seed;

            private const ulong Prime64_1 = 11400714785074694791UL;
            private const ulong Prime64_2 = 14029467366897019727UL;
            private const ulong Prime64_3 = 1609587929392839161UL;
            private const ulong Prime64_4 = 9650029242287828579UL;
            private const ulong Prime64_5 = 2870177450012600261UL;

            public void Execute()
            {
                byte* data = (byte*)DataPtr;
                ulong hash = ComputeXxHash64(data, DataLength, Seed);
                ResultHash.Value = hash;
            }

            private static ulong ComputeXxHash64(byte* data, int length, ulong seed)
            {
                ulong h64;
                int index = 0;

                if (length >= 32)
                {
                    ulong v1 = seed + Prime64_1 + Prime64_2;
                    ulong v2 = seed + Prime64_2;
                    ulong v3 = seed;
                    ulong v4 = seed - Prime64_1;

                    int limit = length - 32;
                    do
                    {
                        v1 = Round(v1, *(ulong*)(data + index));
                        index += 8;
                        v2 = Round(v2, *(ulong*)(data + index));
                        index += 8;
                        v3 = Round(v3, *(ulong*)(data + index));
                        index += 8;
                        v4 = Round(v4, *(ulong*)(data + index));
                        index += 8;
                    } while (index <= limit);

                    h64 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
                    h64 = MergeRound(h64, v1);
                    h64 = MergeRound(h64, v2);
                    h64 = MergeRound(h64, v3);
                    h64 = MergeRound(h64, v4);
                }
                else
                {
                    h64 = seed + Prime64_5;
                }

                h64 += (ulong)length;

                // Process remaining 8-byte blocks
                while (index + 8 <= length)
                {
                    h64 ^= Round(0, *(ulong*)(data + index));
                    h64 = RotateLeft(h64, 27) * Prime64_1 + Prime64_4;
                    index += 8;
                }

                // Process remaining 4-byte block
                if (index + 4 <= length)
                {
                    h64 ^= *(uint*)(data + index) * Prime64_1;
                    h64 = RotateLeft(h64, 23) * Prime64_2 + Prime64_3;
                    index += 4;
                }

                // Process remaining bytes
                while (index < length)
                {
                    h64 ^= data[index] * Prime64_5;
                    h64 = RotateLeft(h64, 11) * Prime64_1;
                    index++;
                }

                // Final avalanche
                h64 ^= h64 >> 33;
                h64 *= Prime64_2;
                h64 ^= h64 >> 29;
                h64 *= Prime64_3;
                h64 ^= h64 >> 32;

                return h64;
            }

            private static ulong Round(ulong acc, ulong input)
            {
                acc += input * Prime64_2;
                acc = RotateLeft(acc, 31);
                acc *= Prime64_1;
                return acc;
            }

            private static ulong MergeRound(ulong acc, ulong val)
            {
                val = Round(0, val);
                acc ^= val;
                acc = acc * Prime64_1 + Prime64_4;
                return acc;
            }

            private static ulong RotateLeft(ulong value, int count)
            {
                return (value << count) | (value >> (64 - count));
            }
        }

        /// <summary>
        /// Combines multiple hash values into a single hash using xxHash-style mixing.
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        private struct CombineHashesJob : IJob
        {
            [ReadOnly] public NativeArray<ulong> Hashes;
            public NativeReference<ulong> ResultHash;

            private const ulong Prime64_1 = 11400714785074694791UL;
            private const ulong Prime64_2 = 14029467366897019727UL;

            public void Execute()
            {
                ulong combined = 0;
                for (int i = 0; i < Hashes.Length; i++)
                {
                    combined ^= Hashes[i] * Prime64_1;
                    combined = ((combined << 31) | (combined >> 33)) * Prime64_2;
                }
                ResultHash.Value = combined;
            }
        }

        // Reusable list for bind poses to avoid GC allocations
        [ThreadStatic] private static List<Matrix4x4> s_bindPoseList;

        /// <summary>
        /// Computes a fast hash for a Unity Mesh using Burst-compiled jobs.
        /// Reads directly from CPU-side vertex data using Mesh.AcquireReadOnlyMeshData.
        /// Includes: all vertex streams, indices, and bind poses.
        /// </summary>
        /// <param name="mesh">The mesh to hash</param>
        /// <returns>A 64-bit hash value, or 0 if the mesh is invalid</returns>
        public static unsafe long ComputeMeshHash(Mesh mesh)
        {
            if (!mesh || !mesh.isReadable)
                return 0;

            // Acquire read-only mesh data (CPU-side access)
            var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            
            try
            {
                var meshData = meshDataArray[0];
                
                // Count how many buffers we'll hash
                int streamCount = mesh.vertexBufferCount;
                bool hasIndices = meshData.indexFormat == IndexFormat.UInt16 ? meshData.GetIndexData<ushort>().Length > 0 : meshData.GetIndexData<uint>().Length > 0;
                bool hasBindPoses = mesh.bindposeCount > 0;
                
                int maxBuffers = streamCount + (hasIndices ? 1 : 0) + (hasBindPoses ? 1 : 0);
                
                if (maxBuffers == 0)
                    return 0;

                var hashResults = new NativeReference<ulong>[maxBuffers];
                var handles = new JobHandle[maxBuffers];
                NativeArray<Matrix4x4> bindPosesNative = default;

                try
                {
                    int bufferIndex = 0;

                    // Hash all vertex streams using CPU-side data
                    for (int stream = 0; stream < streamCount; stream++)
                    {
                        var vertexData = meshData.GetVertexData<byte>(stream);
                        
                        if (vertexData.IsCreated && vertexData.Length > 0)
                        {
                            hashResults[bufferIndex] = new NativeReference<ulong>(Allocator.TempJob);
                            
                            var job = new HashBufferJob
                            {
                                DataPtr = (IntPtr)vertexData.GetUnsafeReadOnlyPtr(),
                                DataLength = vertexData.Length,
                                ResultHash = hashResults[bufferIndex],
                                Seed = (ulong)(stream + 1) * 0x9E3779B97F4A7C15UL // Different seed per stream
                            };
                            handles[bufferIndex] = job.Schedule();
                            bufferIndex++;
                        }
                    }

                    // Hash index buffer using CPU-side data
                    if (hasIndices)
                    {
                        int indexCount = meshData.indexFormat == IndexFormat.UInt16 
                            ? meshData.GetIndexData<ushort>().Length 
                            : meshData.GetIndexData<uint>().Length;
                        
                        if (indexCount > 0)
                        {
                            hashResults[bufferIndex] = new NativeReference<ulong>(Allocator.TempJob);
                            
                            IntPtr indexPtr;
                            int indexDataLength;
                            
                            if (meshData.indexFormat == IndexFormat.UInt16)
                            {
                                var indexData = meshData.GetIndexData<ushort>();
                                indexPtr = (IntPtr)indexData.GetUnsafeReadOnlyPtr();
                                indexDataLength = indexData.Length * 2;
                            }
                            else
                            {
                                var indexData = meshData.GetIndexData<uint>();
                                indexPtr = (IntPtr)indexData.GetUnsafeReadOnlyPtr();
                                indexDataLength = indexData.Length * 4;
                            }
                            
                            var job = new HashBufferJob
                            {
                                DataPtr = indexPtr,
                                DataLength = indexDataLength,
                                ResultHash = hashResults[bufferIndex],
                                Seed = 0xC6A4A7935BD1E995UL // Unique seed for indices
                            };
                            handles[bufferIndex] = job.Schedule();
                            bufferIndex++;
                        }
                    }

                    // Hash bind poses (for skinned meshes)
                    if (hasBindPoses)
                    {
                        // Use thread-static list to avoid GC
                        s_bindPoseList ??= new List<Matrix4x4>(64);
                        s_bindPoseList.Clear();
                        mesh.GetBindposes(s_bindPoseList);
                        
                        if (s_bindPoseList.Count > 0)
                        {
                            bindPosesNative = new NativeArray<Matrix4x4>(s_bindPoseList.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                            for (int i = 0; i < s_bindPoseList.Count; i++)
                            {
                                bindPosesNative[i] = s_bindPoseList[i];
                            }
                            
                            hashResults[bufferIndex] = new NativeReference<ulong>(Allocator.TempJob);
                            
                            var job = new HashBufferJob
                            {
                                DataPtr = (IntPtr)bindPosesNative.GetUnsafeReadOnlyPtr(),
                                DataLength = bindPosesNative.Length * UnsafeUtility.SizeOf<Matrix4x4>(),
                                ResultHash = hashResults[bufferIndex],
                                Seed = 0x853C49E6748FEA9BUL // Unique seed for bind poses
                            };
                            handles[bufferIndex] = job.Schedule();
                            bufferIndex++;
                        }
                    }

                    if (bufferIndex == 0)
                        return 0;

                    // Wait for all hash jobs to complete
                    for (int i = 0; i < bufferIndex; i++)
                    {
                        handles[i].Complete();
                    }

                    // Combine all partial hashes
                    var validHashes = new NativeArray<ulong>(bufferIndex, Allocator.TempJob);
                    var finalResult = new NativeReference<ulong>(Allocator.TempJob);

                    try
                    {
                        for (int i = 0; i < bufferIndex; i++)
                        {
                            validHashes[i] = hashResults[i].Value;
                        }

                        var combineJob = new CombineHashesJob
                        {
                            Hashes = validHashes,
                            ResultHash = finalResult
                        };
                        combineJob.Schedule().Complete();

                        return unchecked((long)finalResult.Value);
                    }
                    finally
                    {
                        validHashes.Dispose();
                        finalResult.Dispose();
                    }
                }
                finally
                {
                    // Cleanup
                    for (int i = 0; i < hashResults.Length; i++)
                    {
                        if (hashResults[i].IsCreated)
                            hashResults[i].Dispose();
                    }

                    if (bindPosesNative.IsCreated)
                        bindPosesNative.Dispose();
                }
            }
            finally
            {
                meshDataArray.Dispose();
            }
        }

        /// <summary>
        /// Computes hashes for multiple meshes.
        /// Returns a dictionary mapping mesh index to its computed hash.
        /// </summary>
        /// <param name="meshes">Array of meshes to hash (can contain nulls)</param>
        /// <returns>Dictionary with mesh as key and hash as value</returns>
        public static Dictionary<Mesh, long> ComputeMeshHashes(Mesh[] meshes)
        {
            var result = new Dictionary<Mesh, long>(meshes.Length);
            
            // Note: We can't easily parallelize the outer loop because GetVertexBuffer/GetIndexBuffer
            // must be called from the main thread. However, each mesh's internal hashing is parallelized.
            for (int i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                if (mesh != null && mesh.isReadable)
                {
                    long hash = ComputeMeshHash(mesh);
                    if (hash != 0)
                    {
                        result[mesh] = hash;
                    }
                }
            }

            return result;
        }
    }
}

