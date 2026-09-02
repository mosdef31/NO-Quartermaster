using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Quartermaster
{
    internal static class IconLoader
    {

        internal static string ModFolder = "";

        private static readonly Dictionary<string, Sprite?> _cache =
            new Dictionary<string, Sprite?>(StringComparer.OrdinalIgnoreCase);

        internal static Sprite? Load(string relativePath, string entryName)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            if (_cache.TryGetValue(relativePath, out Sprite? cached)) return cached;

            Sprite? sprite = LoadUncached(relativePath, entryName);
            _cache[relativePath] = sprite;
            return sprite;
        }

        private static Sprite? LoadUncached(string relativePath, string entryName)
        {
            try
            {
                if (ModFolder.Length == 0)
                {
                    Warn(entryName, relativePath, "the mod's own folder is not known");
                    return null;
                }

                foreach (char c in relativePath)
                {
                    if (!char.IsControl(c)) continue;

                    Warn(entryName, relativePath,
                         "a backslash in it was read as an escape rather than as a folder "
                         + @"separator. Double them - ""icons\\battery.png"" - or write the path "
                         + @"with forward slashes - ""icons/battery.png""");
                    return null;
                }

                if (Path.IsPathRooted(relativePath))
                {
                    Warn(entryName, relativePath,
                         "it is a full path from the root of a drive. Put the image in the mod's "
                         + "folder and name it from there, so the list still works for anyone you "
                         + "send it to");
                    return null;
                }

                string full = Path.GetFullPath(Path.Combine(ModFolder, relativePath));
                string root = Path.GetFullPath(ModFolder);

                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    Warn(entryName, relativePath,
                         "it points outside the mod's own folder. Put the image beside the mod "
                         + "and name it from there");
                    return null;
                }

                if (!File.Exists(full))
                {
                    Warn(entryName, relativePath, $"there is no file at {full}");
                    return null;
                }

                byte[] bytes = File.ReadAllBytes(full);

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
                if (!texture.LoadImage(bytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    Warn(entryName, relativePath,
                         "the file is not an image this game can read. PNG and JPG work");
                    return null;
                }

                texture.name = $"Quartermaster_{Path.GetFileNameWithoutExtension(relativePath)}";

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                sprite.name = texture.name;

                QuartermasterPlugin.Diag(
                    $"\"{entryName}\": icon \"{relativePath}\" loaded, "
                    + $"{texture.width} x {texture.height} pixels.");

                return sprite;
            }
            catch (Exception e)
            {
                Warn(entryName, relativePath, e.Message);
                return null;
            }
        }

        private static void Warn(string entryName, string path, string why) =>
            QuartermasterPlugin.Log.LogWarning(
                $"\"{entryName}\": the icon \"{path}\" was not used because {why}. "
                + "The button falls back to the icon of the first unit in the list.");
    }
}
