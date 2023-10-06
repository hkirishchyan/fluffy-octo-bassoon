using Unity.Collections;
using UnityEngine;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;



public struct Character : IComponentData { }

public struct CharacterBlueprint : IComponentData
{
    public Entity PrefabEntity;
}

public struct CharacterAnimRef : IComponentData
{
    public Animator Value;
}

public struct CharacterMovement : IComponentData
{
    public float3 Value;
}

[UpdateInGroup(typeof(TransformationSystemGroup), OrderFirst = true)]
public class CharacterSetupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities
            .WithNone<CharacterAnimRef>()
            .WithAll<CharacterBlueprint>()
            .ForEach((Entity entity, in CharacterBlueprint characterBlueprint) =>
            {
                var newCharacterObject = EntityManager.Instantiate(characterBlueprint.PrefabEntity);
                var newAnimReference = new CharacterAnimRef
                {
                    Value = EntityManager.GetComponentObject<Animator>(newCharacterObject)
                };
                ecb.AddComponent(entity, newAnimReference);
            })
            .WithStructuralChanges()
            .Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

[UpdateInGroup(typeof(TransformationSystemGroup))]
public class CharacterAnimationUpdateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities
            .ForEach((ref LocalTransform transform, in CharacterAnimRef animReference, in CharacterMovement moveInput) =>
            {
                animReference.Value.transform.position = transform.Position;
                animReference.Value.transform.rotation = transform.Rotation;
                animReference.Value.SetBool("IsSpeedNotNull", math.length(moveInput.Value) > 0f);
            })
            .ScheduleParallel();
    }
}

[UpdateInGroup(typeof(TransformationSystemGroup))]
public class CharacterCleanupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities
            .WithNone<CharacterBlueprint>()
            .WithAll<CharacterAnimRef, LocalTransform>()
            .ForEach((Entity entity, in CharacterAnimRef animReference) =>
            {
                EntityManager.DestroyEntity(entity);
            })
            .WithStructuralChanges()
            .Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
