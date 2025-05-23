using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace ShowGenerator
{
    public static class ShowrunnerAudioZipper
    {
        // Zips all .mp3 files in the given folder to the specified zipPath
        public static void ZipAudioFiles(string folder, string zipPath)
        {
            if (!Directory.Exists(folder))
            {
                Debug.LogError($"Audio folder not found: {folder}");
                return;
            }
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(folder, zipPath);
            Debug.Log($"Audio files zipped to: {zipPath}");
        }
    }
} 