using System;
using System.Collections.Generic;
using System.IO;
using Frosty.Controls;
using Frosty.Core;
using FrostySdk;

namespace Frosty.ModSupport
{
    /// <summary>
    /// Manages backups and restoration of Dead Space Remake's Data folder,
    /// and cleans up mod-generated CAS files before re-applying mods.
    /// </summary>
    public static class DeadSpaceBackupManager
    {
        // Maximum vanilla CAS index per streaming subfolder (files above these are mod-generated and must be deleted)
        private static readonly Dictionary<string, int> s_casLimits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "beyonddefaultinstallpackage",       62 },
            { "beyonddefaultinstallpackageextra1", 63 },
            { "beyondfinalinstallpackage",         44 },
        };

        /// <summary>
        /// Config key used to look up the backup path. Set to "DS_BackupPath" for the Editor
        /// and "DS_MM_BackupPath" for the Mod Manager before calling any backup operations.
        /// </summary>
        public static string CurrentConfigKey { get; set; } = "DS_BackupPath";

        /// <summary>Returns the default backup folder next to the game's .exe: {gamePath}\DSBackup</summary>
        public static string GetDefaultBackupPath(string gamePath) =>
            Path.Combine(gamePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "DSBackup");

        /// <summary>
        /// Returns the configured backup path, falling back to the default DSBackup folder
        /// next to the game .exe when no path has been set by the user.
        /// </summary>
        public static string GetBackupPath(string gamePath)
        {
            string configured = Config.Get<string>(CurrentConfigKey, "", ConfigScope.Game);
            return string.IsNullOrWhiteSpace(configured) ? GetDefaultBackupPath(gamePath) : configured;
        }

        public static bool BackupExists(string gamePath) =>
            Directory.Exists(Path.Combine(GetBackupPath(gamePath), "Data"));

        /// <summary>
        /// Returns true if any mod-generated CAS files (above vanilla limits) are present
        /// in the streaming install subfolders.
        /// </summary>
        public static bool HasExtraModCasFiles(string gamePath)
        {
            string streamingPath = Path.Combine(gamePath, "Data", "Win32", "streaminginstall");
            if (!Directory.Exists(streamingPath))
                return false;

            foreach (var kvp in s_casLimits)
            {
                string folderPath = Path.Combine(streamingPath, kvp.Key);
                if (!Directory.Exists(folderPath))
                    continue;

                foreach (string casFile in Directory.EnumerateFiles(folderPath, "cas_*.cas"))
                {
                    string stem = Path.GetFileNameWithoutExtension(casFile);
                    int underscoreIdx = stem.LastIndexOf('_');
                    if (underscoreIdx >= 0 &&
                        int.TryParse(stem.Substring(underscoreIdx + 1), out int num) &&
                        num > kvp.Value)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if this file should be included in the backup.
        /// Excludes streaminginstall entirely except for language subfolders
        /// within the three known streaming packages.
        /// </summary>
        private static bool ShouldBackupFile(string srcFile, string srcDataRoot)
        {
            string streamingInstall = Path.Combine(srcDataRoot, "Win32", "streaminginstall");

            if (!srcFile.StartsWith(streamingInstall, StringComparison.OrdinalIgnoreCase))
                return true; // not in streaminginstall at all → always back up

            // In streaminginstall: only back up files that are inside a language subfolder
            // of one of the three known package directories (e.g. beyondfinalinstallpackage\de\...)
            foreach (string pkg in s_casLimits.Keys)
            {
                string pkgPath = Path.Combine(streamingInstall, pkg) + Path.DirectorySeparatorChar;
                if (!srcFile.StartsWith(pkgPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                string afterPkg = srcFile.Substring(pkgPath.Length);
                // If there is at least one directory separator after the package root the file
                // sits inside a language subfolder → include it.
                if (afterPkg.IndexOf(Path.DirectorySeparatorChar) >= 0)
                    return true;
            }

            return false; // bare cas/cat files inside a package folder → skip
        }

        /// <summary>
        /// Creates a backup of the game's Data folder.
        /// The streaminginstall CAS/CAT files are excluded, but the language subfolders
        /// within the three streaming packages are included.
        /// </summary>
        public static void CreateBackup(string gamePath)
        {
            string backupRoot = GetBackupPath(gamePath);

            string srcData = Path.Combine(gamePath, "Data");
            string dstData = Path.Combine(backupRoot, "Data");

            // Warn the user if extra CAS files are already present
            if (HasExtraModCasFiles(gamePath))
            {
                FrostyMessageBox.Show(
                    "Extra mod-generated CAS files were detected in your Dead Space Data folder.\n\n" +
                    "This usually means mods were previously applied directly to the game files.\n\n" +
                    "For the cleanest backup it is strongly recommended to first verify game integrity " +
                    "via Steam (Right-click game → Properties → Installed Files → Verify).\n\n" +
                    "The extra CAS files will be removed automatically before the backup is created.",
                    "Dead Space: Modified Data Detected");

                DeleteModCasFiles(gamePath);
            }

            App.Logger.Log("Dead Space: Creating Data backup...");

            foreach (string srcFile in Directory.EnumerateFiles(srcData, "*", SearchOption.AllDirectories))
            {
                if (!ShouldBackupFile(srcFile, srcData))
                    continue;

                string relative = srcFile.Substring(srcData.Length).TrimStart(Path.DirectorySeparatorChar);
                string dstFile = Path.Combine(dstData, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dstFile));
                File.Copy(srcFile, dstFile, overwrite: true);
            }

            App.Logger.Log("Dead Space: Backup complete.");
        }

        /// <summary>
        /// Restores all backed-up files back into the game's Data folder.
        /// </summary>
        public static void RestoreFromBackup(string gamePath)
        {
            if (!BackupExists(gamePath))
                throw new InvalidOperationException("Dead Space backup does not exist at configured path.");

            string srcData = Path.Combine(GetBackupPath(gamePath), "Data");
            string dstData = Path.Combine(gamePath, "Data");

            App.Logger.Log("Dead Space: Restoring Data from backup...");

            foreach (string srcFile in Directory.EnumerateFiles(srcData, "*", SearchOption.AllDirectories))
            {
                string relative = srcFile.Substring(srcData.Length).TrimStart(Path.DirectorySeparatorChar);
                string dstFile = Path.Combine(dstData, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dstFile));
                File.Copy(srcFile, dstFile, overwrite: true);
            }

            App.Logger.Log("Dead Space: Restore complete.");
        }

        /// <summary>
        /// Deletes mod-generated CAS files from the streaming install subfolders
        /// (files whose index exceeds the known vanilla maximum).
        /// </summary>
        public static void DeleteModCasFiles(string gamePath)
        {
            string streamingInstallPath = Path.Combine(gamePath, "Data", "Win32", "streaminginstall");
            if (!Directory.Exists(streamingInstallPath))
                return;

            App.Logger.Log("Dead Space: Cleaning up mod-generated CAS files...");

            foreach (var kvp in s_casLimits)
            {
                string folderPath = Path.Combine(streamingInstallPath, kvp.Key);
                if (!Directory.Exists(folderPath))
                    continue;

                foreach (string casFile in Directory.EnumerateFiles(folderPath, "cas_*.cas"))
                {
                    string stem = Path.GetFileNameWithoutExtension(casFile);
                    int underscoreIdx = stem.LastIndexOf('_');
                    if (underscoreIdx < 0)
                        continue;

                    if (int.TryParse(stem.Substring(underscoreIdx + 1), out int casNumber) && casNumber > kvp.Value)
                    {
                        try
                        {
                            File.Delete(casFile);
                            App.Logger.Log($"Dead Space: Deleted {Path.GetFileName(casFile)} from {kvp.Key}");
                        }
                        catch (Exception ex)
                        {
                            App.Logger.LogWarning($"Dead Space: Could not delete {casFile}: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Full pre-launch cleanup: restore original files and remove mod CAS files.
        /// If no backup exists yet, creates one first (with a pre-check for dirty Data).
        /// </summary>
        public static void PrepareForModApplication(string gamePath)
        {
            if (!BackupExists(gamePath))
            {
                // First run: create the backup automatically in the default or configured folder
                App.Logger.Log($"Dead Space: No backup found, creating one at: {GetBackupPath(gamePath)}");
                CreateBackup(gamePath);
            }
            else
            {
                RestoreFromBackup(gamePath);
                DeleteModCasFiles(gamePath);
            }
        }

        /// <summary>
        /// Copies all files from the compiled ModData\Data folder into the game's Data folder,
        /// overwriting existing files. Called after mod compilation is complete.
        /// </summary>
        public static void CopyModDataToGameData(string modDataPath, string gamePath)
        {
            string srcData = Path.Combine(modDataPath, "Data");
            string dstData = Path.Combine(gamePath, "Data");

            if (!Directory.Exists(srcData))
            {
                App.Logger.LogWarning("Dead Space: ModData\\Data folder not found, skipping copy.");
                return;
            }

            App.Logger.Log("Dead Space: Copying mod data into game Data folder...");

            foreach (string srcFile in Directory.EnumerateFiles(srcData, "*", SearchOption.AllDirectories))
            {
                string relative = srcFile.Substring(srcData.Length).TrimStart(Path.DirectorySeparatorChar);
                string dstFile = Path.Combine(dstData, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dstFile));
                File.Copy(srcFile, dstFile, overwrite: true);
            }

            App.Logger.Log("Dead Space: Mod data copy complete.");
        }
    }
}
