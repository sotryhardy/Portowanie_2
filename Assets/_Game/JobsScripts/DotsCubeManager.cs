using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Game.JobsScripts
{
    public class DotsCubeManager : MonoBehaviour
    {
        [FormerlySerializedAs("_prefab")] public Cube prefab;
        public int dotsCubeAmount;


        private NativeArray<float3> _cubesPosition;
        private FindClosestJob _findClosestJob;
        private JobHandle _handler;


        private bool lowMemory;
        private int spawnFramesOffset = 10;
        private int currentOffset;
        private float deltaTime;

        private List<Cube> _spawnedCubes = new List<Cube>();


        private void Awake()
        {
            Spawn();
        }

        private void Update()
        {
            UpdatePositionDots();
        }

        private void LateUpdate()
        {
            _handler.Complete();

            var spawnedDotsCubeCount = _spawnedCubes.Count;
            var result = _findClosestJob.result;

            for (int i = 0; i < spawnedDotsCubeCount; i++)
            {
                _spawnedCubes[i].Closest = new[]
                {
                    _spawnedCubes[result[i].first].transform.position,
                    _spawnedCubes[result[i].second].transform.position,
                    _spawnedCubes[result[i].third].transform.position,
                };
                _spawnedCubes[i].Farthest = _spawnedCubes[result[i].farthest].transform.position;
            }
        }

        [ContextMenu("Spawn")]
        public void Spawn()
        {
            for (int i = 0; i < dotsCubeAmount; i++)
            {
                var cube = Instantiate(prefab, UnityEngine.Random.insideUnitSphere * 100f, Quaternion.identity, transform);
                _spawnedCubes.Add(cube);

            }
        }

        public void UpdatePositionDots()
        {
            var spawnedDotsCubesCount = _spawnedCubes.Count;



            _cubesPosition = new NativeArray<float3>(spawnedDotsCubesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var result1 = new NativeArray<ClosestAndFarthest>(spawnedDotsCubesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);


           
            for (int i = 0; i < spawnedDotsCubesCount; i++)
            {
                _cubesPosition[i] = _spawnedCubes[i].transform.position; 
            }


            _findClosestJob = new FindClosestJob
            {
                length = spawnedDotsCubesCount,
                cubesPosition = _cubesPosition,
                result = result1
            };

            _handler = _findClosestJob.Schedule();

        }

        [BurstCompile]
        private struct FindClosestJob : IJob
        {
            public int length;
            [ReadOnly] public NativeArray<float3> cubesPosition;
            [WriteOnly] public NativeArray<ClosestAndFarthest> result;

            public void Execute()
            {
                for (int i = 0; i < length; i++)
                    result[i] = GetClosestAndFarthest(i, cubesPosition[i]);

            }

            private ClosestAndFarthest GetClosestAndFarthest(int currentIndex, float3 currentPosition)
            {

                var farthestDistance = float.MinValue;
                var farthestIndex = -1;

                var Distances = new NativeArray<float>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var Indices = new NativeArray<int>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);


                for (int i = 0; i < 3; i++)
                {
                    Distances[i] = float.MaxValue;
                    Indices[i] = -1;
                }

                for (int i = 0; i < length; i++)
                {
                    if (i == currentIndex)
                        continue;

                    var distance = math.distance(currentPosition, cubesPosition[i]);

                    for (int j = 0; j < 3; j++)
                    {
                        if (distance < Distances[j])
                        {
                            for (int k = 2; k > j; k--)
                            {
                                Distances[k] = Distances[k - 1];
                                Indices[k] = Indices[k - 1];
                            }

                            Distances[j] = distance;
                            Indices[j] = i;
                            break;
                        }
                    }

                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                        farthestIndex = i;
                    }
                }

                return new ClosestAndFarthest()
                {
                    first = Indices[0],
                    second = Indices[1],
                    third = Indices[2],
                    farthest = farthestIndex
                };
            }
        }

    }
}