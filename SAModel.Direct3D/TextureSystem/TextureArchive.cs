﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArchiveLib;
using static ArchiveLib.GenericArchive;

namespace SonicRetro.SAModel.Direct3D.TextureSystem
{
    /// <summary>
    /// This TextureArchive class is the primary interface for retrieving a texture list/array from a container format, such as PVM/GVM, and eventually PAK.
    /// </summary>
    public static class TextureArchive
    {
        public static BMPInfo[] GetTextures(string filename)
        {
            if (!File.Exists(filename))
                return null;
            GenericArchive arc;
            List<BMPInfo> textures = new List<BMPInfo>();
            byte[] file = File.ReadAllBytes(filename);
            string ext = Path.GetExtension(filename).ToLowerInvariant();
            switch (ext)
            {
                // Folder texture pack
                case ".txt":
                    string[] files = File.ReadAllLines(filename);
                    List<BMPInfo> txts = new List<BMPInfo>();
                    for (int s = 0; s < files.Length; s++)
                    {
                        string[] entry = files[s].Split(',');
                        txts.Add(new BMPInfo(Path.GetFileNameWithoutExtension(entry[1]), new System.Drawing.Bitmap(Path.Combine(Path.GetDirectoryName(filename), entry[1]))));
                    }
                    return txts.ToArray();
                case ".pak":
                    arc = new PAKFile(filename);
                    string filenoext = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
                    PAKFile pak = (PAKFile)arc;
                    // Get sorted entires from the INF file if it exists
                    List<PAKFile.PAKEntry> sorted = pak.GetSortedEntries(filenoext);
                    arc.Entries = new List<GenericArchiveEntry>(sorted.Cast<GenericArchiveEntry>());
                    break;
                case ".pvmx":
                    arc = new PVMXFile(file);
                    break;
                case ".pb":
                    arc = new PBFile(file);
                    break;
                case ".pvr":
                case ".gvr":
                    arc = new PuyoFile(ext == ".gvr");
                    PuyoFile parcx = (PuyoFile)arc;
                    if (ext == ".gvr")
                        arc.Entries.Add(new GVMEntry(filename));
                    else
                        arc.Entries.Add(new PVMEntry(filename));
                    if (parcx.PaletteRequired)
                        parcx.AddPalette(Path.GetDirectoryName(filename));
                    break;
                case ".prs":
                    file = FraGag.Compression.Prs.Decompress(file);
                    goto default;
                case ".pvm":
                case ".gvm":
                default:
                    arc = new PuyoFile(file);
                    PuyoFile parc = (PuyoFile)arc;
                    if (parc.PaletteRequired)
                        parc.AddPalette(Path.GetDirectoryName(filename));
                    break;
            }
            foreach (GenericArchiveEntry entry in arc.Entries)
            {
                textures.Add(new BMPInfo(Path.GetFileNameWithoutExtension(entry.Name), entry.GetBitmap()));
            }
            return textures.ToArray();
        }
    }
}