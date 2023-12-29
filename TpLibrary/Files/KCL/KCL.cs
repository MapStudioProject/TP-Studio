using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using MapStudio.UI;
using Toolbox.Core;
using System.IO;
using KclLibrary;
using GLFrameworkEngine;
using Toolbox.Core.ViewModels;
using ImGuiNET;
using UIFramework;
using TpLibrary;
using RevoKartLibrary.CollisionEditor;

namespace RevoKartLibrary
{
    public class KCL : FileEditor
    {
        public string[] Description => new string[] { "Gcn Collision" };
        public string[] Extension => new string[] { "*.kcl" };

        /// <summary>
        /// Wether or not the file can be saved.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// Information of the loaded file.
        /// </summary>
        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, Stream stream)
        {
            using (var reader = new Toolbox.Core.IO.FileReader(stream, true))
            {
                reader.ByteOrder = Syroot.BinaryData.ByteOrder.BigEndian;
                return fileInfo.Extension == ".kcl";
            }
        }

        public KCLFile KclFile { get; set; }

        /// <summary>
        /// The collision debug renerer.
        /// </summary>
        public CollisionRender CollisionRender = null;

        private PLC PlcFile { get; set; } = new PLC();

        public KCL() { FileInfo = new File_Info() { FileName = "course.kcl" }; }

        public KCL(Stream stream, PLC plc)
        {
            FileInfo = new File_Info() { FileName = "course.kcl" };
            Load(stream, plc);
        }

        public bool UpdateTransformedVertices = false;

        public override bool CreateNew()
        {
            Root.Header = "course.kcl";
            Root.Tag = this;

            FileInfo.FileName = "course.kcl";

            //Empty file
            KclFile = new KCLFile(new List<Triangle>(), FileVersion.VersionGC, true);
            UpdateCollisionFile(KclFile, new PLC());

            return true;
        }

        public void Load(Stream stream, PLC plc)
        {
            PlcFile = plc;
            KclFile = new KCLFile(stream);
            UpdateCollisionFile(KclFile, plc);

            CollisionRender.CanSelect = true;
        }

        public void Save(Stream stream)
        {
            KclFile.Save(stream);
        }

        public void UpdateCollisionFile(KCLFile collision, PLC plc)
        {
            KclFile = collision;

            Scene.Init();
            Scene.Objects.Clear();

            //Setup tree node
            Root.Header = FileInfo.FileName;
            Root.Tag = KclFile;
            Root.ContextMenus.Clear();
            Root.ContextMenus.Add(new MenuItemModel("Export", ExportCollision));
            Root.ContextMenus.Add(new MenuItemModel("Replace", ImportCollision));

            Console.WriteLine("Loading collision render");

            //Add or update the existing renderer of the collision
            if (CollisionRender == null)
            {
                CollisionRender = new CollisionRender(collision, plc);
                this.AddRender(CollisionRender);
            }
            else
                CollisionRender.Reload(collision, plc);

            Console.WriteLine("Loading collision tree");

            //Prepare displayed children in tree
            ReloadTree();

            Scene.Objects.Add(CollisionRender);

            OpenTK.Vector3 ToVec3(System.Numerics.Vector3 v)
            {
                return new OpenTK.Vector3((float)v.X, (float)v.Y, (float)v.Z);
            }

            Console.WriteLine("Loading collision ray caster");

            //Generate a ray caster to automate collision detection from moving objects
            CollisionRayCaster collisionRayCaster = new CollisionRayCaster();
            foreach (var model in KclFile.Models)
            {
                foreach (var prism in model.Prisms)
                {
                    var tri = model.GetTriangle(prism);
                    collisionRayCaster.AddTri(
                        ToVec3(tri.Vertices[2]) * GLContext.PreviewScale,
                        ToVec3(tri.Vertices[1]) * GLContext.PreviewScale,
                        ToVec3(tri.Vertices[0]) * GLContext.PreviewScale);
                }
            }
            collisionRayCaster.UpdateCache();
            GLContext.ActiveContext.CollisionCaster = collisionRayCaster;

            Console.WriteLine("finished collision");
        }

        string CurrentPreset = "";

        private void ReloadTree()
        {
            Dictionary<ushort, KclPrism> materials = new Dictionary<ushort, KclPrism>();
            foreach (var model in KclFile.Models)
            {
                foreach (var prism in model.Prisms)
                {
                    if (!materials.ContainsKey(prism.CollisionFlags))
                        materials.Add(prism.CollisionFlags, prism);
                }
            }

            Root.Children.Clear();
            foreach (var mesh in CollisionRender.Meshes)
                Root.AddChild(mesh.UINode);
        }

        public override List<DockWindow> PrepareDocks()
        {
            var docks = base.PrepareDocks();
            //   if (CollisionPainter == null)
            //    CollisionPainter = new CollisionPainterUI(this.Workspace);
            // docks.Add(CollisionPainter);
            return docks;
        }

        public override List<MenuItemModel> GetEditMenuItems()
        {
            var items = new List<MenuItemModel>();
            items.Add(new MenuItemModel($"        Import Collision", ImportCollision));
            return items;
        }

        public override bool OnFileDrop(string filePath)
        {
            if (filePath.EndsWith(".obj"))
            {
                return true;
            }
            return false;
        }

        public void ExportCollision()
        {
            ImguiFileDialog sfd = new ImguiFileDialog();
            sfd.FileName = System.IO.Path.GetFileNameWithoutExtension(FileInfo.FileName);
            sfd.SaveDialog = true;
            sfd.AddFilter(".obj", "Object File");

            if (sfd.ShowDialog())
            {
                KclFile.CreateGenericModel().Save(sfd.FilePath);
            }
        }


        public void ImportCollision()
        {
            ImguiFileDialog ofd = new ImguiFileDialog();
            ofd.AddFilter(".obj", "Object File");
            ofd.AddFilter(".dae", "Collada File");
            ofd.AddFilter(".fbx", "Fbx File");

            if (ofd.ShowDialog())
            {
                ImportCollision(ofd.FilePath);
            }
        }
        public void ImportCollision(string filePath)
        {
            var importer = PrepareDialog();
            importer.OpenObjectFile(filePath);
        }

        public void ImportCollision(IONET.Core.IOScene scene)
        {
            var importer = PrepareDialog();
            importer.OpenObjectFile(scene);
        }

        private CollisionImporter PrepareDialog()
        {
            var importer = new CollisionImporter();
            importer.PlcFile = PlcFile;

            DialogHandler.Show("Collision Importer", 600, 500, () =>
            {
                importer.Render();
            }, (e) =>
            {
                if (e)
                {
                    this.UpdateCollisionFile(importer.GetCollisionFile(), PlcFile);
                    this.CanSave = true;
                }
            });
            return importer;
        }
    }
}
