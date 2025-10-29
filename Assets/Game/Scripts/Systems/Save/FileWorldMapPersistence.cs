using System;
using System.IO;
using UnityEngine;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// File-based persistence under Application.persistentDataPath.
    /// Not used by unit tests (prefer InMemory), but available for runtime.
    /// </summary>
    public sealed class FileWorldMapPersistence : IWorldMapPersistence
    {
        private const string DefaultFolderName = "Saves";
        private readonly string _rootFolder;

        public FileWorldMapPersistence(string subFolder = DefaultFolderName)
        {
            if (string.IsNullOrWhiteSpace(subFolder)) subFolder = DefaultFolderName;
            _rootFolder = Path.Combine(Application.persistentDataPath, subFolder);
            if (!Directory.Exists(_rootFolder))
            {
                Directory.CreateDirectory(_rootFolder);
            }
        }

        public void Save(string slotId, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("slotId is required", nameof(slotId));
            string path = GetPath(slotId);
            File.WriteAllBytes(path, data ?? Array.Empty<byte>());
        }

        public bool TryLoad(string slotId, out byte[] data)
        {
            if (string.IsNullOrWhiteSpace(slotId)) { data = null; return false; }
            string path = GetPath(slotId);
            if (!File.Exists(path)) { data = null; return false; }
            data = File.ReadAllBytes(path);
            return true;
        }

        /// <summary>
        /// Resolves the absolute file path that would be used for a given slot id.
        /// </summary>
        public string ResolvePath(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentException("slotId is required", nameof(slotId));
            return GetPath(slotId);
        }

        private string GetPath(string slotId)
        {
            string safe = slotId.Replace('/', '_').Replace('\\', '_');
            return Path.Combine(_rootFolder, safe + ".json");
        }
    }
}
