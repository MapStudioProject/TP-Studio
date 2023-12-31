﻿using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;

namespace TpLibrary
{
    public class RARC : IFileFormat, IArchiveFile, IDisposable
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "RARC" };
        public string[] Extension { get; set; } = new string[] { "*.rarc", "*.arc", "*.yaz0" };

        public File_Info FileInfo { get; set; }

        public bool CanAddFiles { get; set; } = false;
        public bool CanRenameFiles { get; set; } = false;
        public bool CanReplaceFiles { get; set; } = false;
        public bool CanDeleteFiles { get; set; } = false;

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            return false;

            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "RARC") || reader.CheckSignature(4, "CRAR"); ;
            }
        }

        public RarcFile Header;

        public IEnumerable<ArchiveFileInfo> Files => Header.Files;
        public void ClearFiles() { Header.Files.Clear(); }

        public void Load(Stream stream) {
            Header = new RarcFile(stream);
        }

        public void Save(Stream stream) {
            Header.Save(stream);
        }

        public bool AddFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }

        public bool DeleteFile(ArchiveFileInfo archiveFileInfo)
        {
            return false;
        }

        public void Dispose()
        {
         /*   _stream?.Dispose();
            foreach (var file in Files)
                file.FileData?.Dispose();
            _stream = null;*/
        }

        public class FileEntry : ArchiveFileInfo
        {
        }
    }
}
