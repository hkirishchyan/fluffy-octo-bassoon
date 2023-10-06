using UnityEngine;
using Unity.Entities;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
public partial class CustomInputSystem : SystemBase
{
    private CustomMovementActions _customMovementActions;

    protected override void OnCreate()
    {
        RequireSingletonForUpdate<CustomPlayerTag>();
        RequireSingletonForUpdate<CustomPlayerMoveInput>();

        _customMovementActions = new CustomMovementActions();
    }

    protected override void OnStartRunning()
    {
        _customMovementActions.Enable();
        _customMovementActions.CustomMap.PlayerJump.performed += OnPlayerJump;

        EntityManager entityManager = World.EntityManager;
        Entity playerEntity = entityManager.CreateEntityQuery(typeof(CustomPlayerTag)).GetSingletonEntity();
        entityManager.AddComponent<CustomInputTag>(playerEntity);
    }

    protected override void OnUpdate()
    {
        var currentMoveInput = _customMovementActions.CustomMap.PlayerMovement.ReadValue<Vector2>();

        EntityManager entityManager = World.EntityManager;
        Entity playerEntity = entityManager.CreateEntityQuery(typeof(CustomPlayerTag)).GetSingletonEntity();
        entityManager.SetComponentData(playerEntity, new CustomPlayerMoveInput { InputValue = currentMoveInput });
    }

    protected override void OnStopRunning()
    {
        _customMovementActions.CustomMap.PlayerJump.performed -= OnPlayerJump;
        _customMovementActions.Disable();

        EntityManager entityManager = World.EntityManager;
        Entity playerEntity = entityManager.CreateEntityQuery(typeof(CustomPlayerTag)).GetSingletonEntity();
        entityManager.RemoveComponent<CustomInputTag>(playerEntity);
    }

    private void OnPlayerJump(InputAction.CallbackContext context)
    {
        EntityManager entityManager = World.EntityManager;
        Entity playerEntity = entityManager.CreateEntityQuery(typeof(CustomPlayerTag)).GetSingletonEntity();

        if (entityManager.HasComponent<CustomFireTag>(playerEntity))
        {
            entityManager.SetComponentData(playerEntity, new CustomFireTag { IsFiring = true });
        }
    }
}

public struct CustomPlayerMoveInput : IComponentData
{
    public Vector2 InputValue;
}

public struct CustomInputTag : IComponentData { }
