using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace GravityEmitter
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public class GravityFactorSystem : SystemBase
    {
        private EntityQuery _triggerGravityQuery;
        private EntityQuery _dynamicEntityQuery;

        protected override void OnCreate()
        {
            _triggerGravityQuery = GetEntityQuery(typeof(TriggerGravityComponent));
            _dynamicEntityQuery = GetEntityQuery(typeof(PhysicsVelocity));

            RequireForUpdate(_triggerGravityQuery);
            RequireForUpdate(_dynamicEntityQuery);
        }

        protected override void OnUpdate()
        {
            var triggerGravityComponents = _triggerGravityQuery.ToComponentDataArray<TriggerGravityComponent>(Allocator.TempJob);

            Dependency = new UpdateGravityFactorJob
            {
                TriggerGravityComponents = triggerGravityComponents,
                DynamicEntityQuery = _dynamicEntityQuery,
            }.ScheduleParallel(Dependency);

            triggerGravityComponents.Dispose(Dependency);
        }
    }
}


namespace GravityEmitter
{
    [BurstCompile]
    public struct UpdateGravityFactorJob : IJobEntityBatchWithIndex
    {
        [field: SerializeField,ReadOnly] public NativeArray<TriggerGravityComponent> TriggerGravityComponents{ get; set; }
        [field: SerializeField, ReadOnly] public EntityQuery DynamicEntityQuery{ get; set; }
        public ComponentDataFromEntity<PhysicsGravityFactor> PhysicsGravityFactors{ get; set; }
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocities { get; set; }

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var triggerGravityComponent = TriggerGravityComponents[batchIndex];
            var dynamicEntities = batchInChunk.GetNativeArray(EntityType);

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var dynamicEntity = dynamicEntities[i];

                if (PhysicsGravityFactors.Exists(dynamicEntity) && PhysicsVelocities.Exists(dynamicEntity))
                {
                    var physicsGravityFactor = PhysicsGravityFactors[dynamicEntity];
                    var physicsVelocity = PhysicsVelocities[dynamicEntity];

                    physicsGravityFactor.Value = triggerGravityComponent.GravityFactor;
                    physicsVelocity.Linear *= triggerGravityComponent.DampingFactor;

                    PhysicsGravityFactors[dynamicEntity] = physicsGravityFactor;
                    PhysicsVelocities[dynamicEntity] = physicsVelocity;
                }
            }
        }
    }
}

namespace GravityEmitter
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(GravityFactorSystem))]
    public class UpdatePhysicsVelocitySystem : SystemBase
    {
        private EntityQuery _dynamicEntityQuery;

        protected override void OnCreate()
        {
            _dynamicEntityQuery = GetEntityQuery(typeof(PhysicsVelocity));
        }

        protected override void OnUpdate()
        {
            float3 gravityAcceleration = new float3(0.0f, -9.81f, 0.0f);
            float deltaTime = Time.DeltaTime;

            Entities
                .WithStoreEntityQueryInField(ref _dynamicEntityQuery)
                .ForEach((ref PhysicsVelocity physicsVelocity) =>
                {
                    physicsVelocity.Linear += gravityAcceleration * deltaTime;
                })
                .ScheduleParallel();
        }
    }
}


namespace GravityEmitter
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(GravityFactorSystem))]
    public class QueryGravityFactorSystem : SystemBase
    {
        private EntityQuery _triggerGravityQuery;

        protected override void OnCreate()
        {
            _triggerGravityQuery = GetEntityQuery(typeof(TriggerGravityComponent));
        }

        protected override void OnUpdate()
        {
            var triggerGravityComponents = _triggerGravityQuery.ToComponentDataArray<TriggerGravityComponent>(Allocator.TempJob);

            if (triggerGravityComponents.Length > 0)
            {
                float averageGravityFactor = 0;
                foreach (var factor in triggerGravityComponents)
                {
                    averageGravityFactor += factor.GravityFactor;
                }
                /*averageGravityFactor /= triggerGravityComponents.Length;

                Debug.Log($"Average GravityFactor: {averageGravityFactor}");*/
            }

            triggerGravityComponents.Dispose();
        }
    }
}