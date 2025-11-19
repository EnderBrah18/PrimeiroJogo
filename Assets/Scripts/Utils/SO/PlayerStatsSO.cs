using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "RPG/Player Stats")]
public class PlayerStatsSO : ScriptableObject, ISOSavable
{
    [Header("Movement")]
    public MovementStats movement = new MovementStats();

    [Header("Stamina")]
    public StaminaStats stamina = new StaminaStats();

    [Header("Gravity")]
    public float gravity = -9.8f;

    [Header("Air Control")]
    public AirControlStats air = new AirControlStats();

    [Header("Dash")]
    public DashStats dash = new DashStats();

    [Header("Climb")]
    public ClimbStats climb = new ClimbStats();

    [Header("Inventory")]
    public InventoryStats inventory = new InventoryStats();


    // ----------------- Subclasses -----------------

    [Serializable]
    public class MovementStats
    {
        public float baseMoveSpeed = 10f;
        public float baseJumpForce = 8f;
        public float turnSpeed = 10f;
    }

    [Serializable]
    public class StaminaStats
    {
        public float maxStamina = 100f;
        public float staminaRegenRate = 15f;
        public float staminaSprintCost = 20f;
        public float staminaClimbCost = 10f;
        public float staminaJumpCost = 15f;
        public float staminaMinToSprint = 5f;
        public float staminaMinToClimb = 5f;
        public float sprintMultiplier = 1.75f;
    }

    [Serializable]
    public class AirControlStats
    {
        public float airControlMultiplier = 0.65f;
        public float airAcceleration = 5f;
        public float airDrag = 2f;
    }

    [Serializable]
    public class DashStats
    {
        public float dashDistance = 10f;
        public float dashCooldown = 1f;
        public float dashDuration = 0.2f;
    }

    [Serializable]
    public class ClimbStats
    {
        public float climbSpeed = 2f;
        public float climbCheckDistance = 1f;
        public float lateralClimbSpeed = 1f;
    }

    [Serializable]
    public class InventoryStats
    {
        public float maxWeight = 100f;
        public int coins = 0;
    }

    public void ResetToDefaults()
    {
        movement = new MovementStats();
        stamina = new StaminaStats();
        gravity = -9.8f;
        air = new AirControlStats();
        dash = new DashStats();
        climb = new ClimbStats();
        inventory = new InventoryStats();
    }

    // ----------------- Save/Load -----------------

    [Serializable]
    private class StatsSaveData
    {
        public MovementStats movement;
        public StaminaStats stamina;
        public float gravity;
        public AirControlStats air;
        public DashStats dash;
        public ClimbStats climb;
        public InventoryStats inventory;
    }

    public string GetSaveKey() => "player_stats";

    public string SaveData()
    {
        StatsSaveData data = new StatsSaveData
        {
            movement = movement,
            stamina = stamina,
            gravity = gravity,
            air = air,
            dash = dash,
            climb = climb,
            inventory = inventory
        };

        return JsonUtility.ToJson(data);
    }

    public void LoadData(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        StatsSaveData data = JsonUtility.FromJson<StatsSaveData>(json);
        if (data == null) return;

        movement = data.movement ?? new MovementStats();
        stamina = data.stamina ?? new StaminaStats();
        gravity = data.gravity;
        air = data.air ?? new AirControlStats();
        dash = data.dash ?? new DashStats();
        climb = data.climb ?? new ClimbStats();
        inventory = data.inventory ?? new InventoryStats();
    }
}
