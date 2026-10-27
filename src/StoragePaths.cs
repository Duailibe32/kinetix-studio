using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KinetixStudio
{
    public static class StoragePaths
    {
        public static string Initialize()
        {
            string custom = Environment.GetEnvironmentVariable("KINETIX_DATA_DIR");
            string root = String.IsNullOrWhiteSpace(custom) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KinetixStudio") : custom;
            if (!Path.IsPathRooted(root))
                throw new InvalidDataException("A pasta de dados deve ser um caminho absoluto.");
            root = Path.GetFullPath(root);
            Directory.CreateDirectory(root);
            Migrate(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"), root);
            return root;
        }

        public static int Migrate(string source, string destination)
        {
            source = Path.GetFullPath(source);
            destination = Path.GetFullPath(destination);
            if (String.Equals(source.TrimEnd('\\'), destination.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) || !Directory.Exists(source))
                return 0;
            Directory.CreateDirectory(destination);
            string identity;
            using (SHA256 hash = SHA256.Create())
                identity = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(source.ToUpperInvariant()))).Replace("-", "");
            string marker = Path.Combine(destination, "migrations", identity + ".done");
            if (File.Exists(marker))
                return 0;
            int copied = 0;
            string profiles = Path.Combine(source, "profiles");
            if (Directory.Exists(profiles))
                foreach (string file in Directory.GetFiles(profiles, "*.json"))
                {
                    if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                        continue;
                    if (new FileInfo(file).Length > 65536)
                        continue;
                    Profile profile;
                    try
                    {
                        profile = Store.Deserialize(File.ReadAllText(file));
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {
                        continue;
                    }
                    catch (InvalidDataException)
                    {
                        continue;
                    }

                    Guid id;
                    if (!Guid.TryParseExact(profile.Id, "N", out id) || !String.Equals(Path.GetFileNameWithoutExtension(file), profile.Id, StringComparison.OrdinalIgnoreCase))
                        continue;
                    copied += CopyPreserving(file, Path.Combine(destination, "profiles", profile.Id + ".json"), destination);
                }

            foreach (string name in new string[]
            {
                "selected.txt",
                "desktop.json",
                "kinetix.log",
                "veylora.log"
            }

            )
            {
                string file = Path.Combine(source, name);
                if (File.Exists(file) && (File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0)
                    copied += CopyPreserving(file, Path.Combine(destination, name), destination);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(marker));
            File.WriteAllText(marker, DateTime.UtcNow.ToString("o") + "\n" + copied + " arquivos importados. Originais preservados.", new UTF8Encoding(false));
            return copied;
        }

        static int CopyPreserving(string source, string target, string destination)
        {
            if (File.Exists(target))
            {
                string sourceHash = Hash(source);
                if (sourceHash == Hash(target))
                    return 0;
                // Conflicts are archived separately; the existing active configuration wins.
                string conflict = Path.Combine(destination, "migration-backups", Path.GetFileName(target) + "." + sourceHash + ".bak");
                if (!File.Exists(conflict))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(conflict));
                    File.Copy(source, conflict, false);
                }

                return 0;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target));
            string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(source, temp, false);
                File.Move(temp, target);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }

            return 1;
        }

        static string Hash(string path)
        {
            using (SHA256 hash = SHA256.Create())
            using (FileStream input = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(input)).Replace("-", "");
        }
    }
}
