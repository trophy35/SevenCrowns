using System.Threading.Tasks;
using UnityEngine;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// Scene component that owns a WorldMapSaveService and exposes Save/Load methods
    /// for UI wiring (e.g., Main Menu Save button).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaveGameServiceBehaviour : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField, Tooltip("Default save slot identifier.")]
        private string _defaultSlotId = "slot1";

        private IWorldMapSaveService _service;
        private FileWorldMapPersistence _file;

        private void Awake()
        {
            // Construct service once per scene. Keep it simple: no DI container.
            var reader = new WorldMapStateReader();
            _file = new FileWorldMapPersistence();
            _service = new WorldMapSaveService(reader, _file);
        }

        /// <summary>
        /// Saves to the default slot.
        /// </summary>
        public void Save()
        {
            if (_service == null) Awake();
            // Fire and forget; this work is synchronous in current implementation
            _service.SaveAsync(_defaultSlotId).GetAwaiter().GetResult();
            if (_file != null)
            {
                var path = _file.ResolvePath(_defaultSlotId);
                Debug.Log($"[SaveGame] Saved to: {path}", this);
            }
        }

        /// <summary>
        /// Loads from the default slot.
        /// </summary>
        public void Load()
        {
            if (_service == null) Awake();
            _service.LoadAsync(_defaultSlotId).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Saves to a specific slot id (callable from UI with Event argument binding).
        /// </summary>
        public void SaveToSlot(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId)) slotId = _defaultSlotId;
            if (_service == null) Awake();
            _service.SaveAsync(slotId).GetAwaiter().GetResult();
            if (_file != null)
            {
                var path = _file.ResolvePath(slotId);
                Debug.Log($"[SaveGame] Saved to: {path}", this);
            }
        }

        /// <summary>
        /// Loads from a specific slot id (callable from UI with Event argument binding).
        /// </summary>
        public void LoadFromSlot(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId)) slotId = _defaultSlotId;
            if (_service == null) Awake();
            _service.LoadAsync(slotId).GetAwaiter().GetResult();
        }
    }
}
