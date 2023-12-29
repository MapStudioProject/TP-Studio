using GLFrameworkEngine;
using KclLibrary;
using MapStudio.UI;
using RevoKartLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using Toolbox.Core.ViewModels;
using TpLibrary.Stage;
using UIFramework;
using static TpLibrary.DZR;

namespace TpLibrary
{
    public class StagePlugin : FileEditor, IFileFormat, IDisposable
    {
        public string[] Description => new string[0];

        public string[] Extension => new string[0];

        public bool CanSave {  get; set; } = true;
        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "RARC");
            }
        }

        public RarcFile ArchiveFile;

        public KCL CollisionFile;
        public PLC CollisionPolyAttributes;
        public DZR StageFile;

        private StageEditor StageEditor;

        public void Load(Stream stream)
        {
            ArchiveFile = new RarcFile(stream);

            var colFile = ArchiveFile.Files.FirstOrDefault(x => x.FileName.EndsWith(".kcl"));
            var polyAttrFile = ArchiveFile.Files.FirstOrDefault(x => x.FileName.EndsWith(".plc"));
            var stageFile = ArchiveFile.Files.FirstOrDefault(x => x.FileName.EndsWith(".dzr"));

            if (colFile != null)
            {
                CollisionPolyAttributes = new PLC(polyAttrFile.FileData);
                CollisionFile = new KCL(colFile.FileData, CollisionPolyAttributes);
                CollisionFile.Root.Header = colFile.Name;

                this.AddRender(CollisionFile.CollisionRender);
                this.Root.AddChild(CollisionFile.Root);
            }

          //  StageFile = new DZR(stageFile.FileData);
          //  StageEditor = new StageEditor(this, StageFile);
        }

        public void Save(Stream stream)
        {
         //   SaveStage();
            SaveCollision();
            SaveCollisionAttributes();

            ArchiveFile.Save(stream);
        }

        private void SaveStage()
        {
            var stageFile = ArchiveFile.Files.FirstOrDefault(x => x.FileName.EndsWith(".dzr"));

            var mem = new MemoryStream();
            StageFile.Save(mem);
            stageFile.FileData = new MemoryStream(mem.ToArray());
        }

        private void SaveCollision()
        {
            var colFile = ArchiveFile.Files.FirstOrDefault(x => x.FileName.EndsWith(".kcl"));

            var mem = new MemoryStream();
            CollisionFile.Save(mem);
            colFile.FileData = new MemoryStream(mem.ToArray());
        }

        private void SaveCollisionAttributes()
        {
            var polyAttrFile = ArchiveFile.Files.FirstOrDefault(x => x.FileName.EndsWith(".plc"));

            var mem = new MemoryStream();
            CollisionPolyAttributes.Save(mem);
            polyAttrFile.FileData = new MemoryStream(mem.ToArray());
        }

        public void Dispose()
        {
            CollisionFile?.CollisionRender.Dispose();
        }

        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            windows.Add(Workspace.ConsoleWindow);
            windows.Add(Workspace.AssetViewWindow);
            windows.Add(Workspace.HelpWindow);
            windows.Add(Workspace.ViewportWindow);
            return windows;
        }
    }
}
