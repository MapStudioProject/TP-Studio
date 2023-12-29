using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using MapStudio.UI;
using KclLibrary;
using Toolbox.Core;
using TpLibrary;

namespace RevoKartLibrary.CollisionEditor
{
    public class CollisionImporter
    {
        public KCLFile GetCollisionFile() => CollisionFile;

        CollisionPresetData Preset;
        CollisionMaterialSelector MaterialSelector = new CollisionMaterialSelector();

        private bool displayHex = false;
        //Determines to map attributes by meshes instead of materials
        private bool FilterByMeshes = true;
        //Selected material ID
        private ushort materialID = 0;
        //Selected materials
        List<string> selectedMaterials = new List<string>();

        private ObjModel ImportedModel;
        private KCLFile CollisionFile;

        //Results for the mapped material/mesh and attribute used
        private Dictionary<string, CollisionEntry> Results = new Dictionary<string, CollisionEntry>();

        //Keep track of error logs to render out (0 for normal, 1 for error)
        private List<Tuple<int, string>> logger = new List<Tuple<int, string>>();

        private bool IsBigEndian = true;
        private FileVersion Version = FileVersion.VersionWII;

        public PLC PlcFile = new PLC();

        public CollisionImporter(bool isBigEndian = true, FileVersion version = FileVersion.VersionWII)
        {
            IsBigEndian = isBigEndian;
            Version = version;
            //Get the preset of all the collision material attributes.
            Preset = new CollisionPresetData()
            {
                GameTitle = "Mario Kart Wii",
                Platform = "WII",
                PrismThickness = 300,
                SphereRadius  = 250,
                PaddingMin = -250,
                PaddingMax = 250,
                MaxRootSize = 2048,
                MinRootSize = 128,
                MinCubeSize = 512,
                MaxTrianglesInCube = 60,
            };
        }

        public void OpenObjectFile(IONET.Core.IOScene scene)
        {
            ImportedModel = new ObjModel(scene);
            UpdateMaterialList();
        }

        public void OpenObjectFile(string filePath)
        {
            ImportedModel = new ObjModel(filePath);
            UpdateMaterialList();
        }

        private void UpdateMaterialList()
        {
            Results.Clear();
            if (FilterByMeshes) {
                foreach (var mesh in ImportedModel.GetMeshNameList())
                    Results.Add(mesh, new CollisionEntry(mesh, PlcFile));
            }
            else {
                foreach (var mat in ImportedModel.GetMaterialNameList())
                    Results.Add(mat, new CollisionEntry(mat, PlcFile));
            }
           
            selectedMaterials.Clear();
            selectedMaterials.Add(Results.Keys.FirstOrDefault());
        }

        public void Render()
        {
            if (logger.Count > 0)
            {
                foreach (var log in logger)
                {
                    if (log.Item1 == 1)
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), log.Item2);
                    else
                        ImGui.Text(log.Item2);
                }
                return;
            }

            bool updateList = false;

            ImGui.BeginColumns("col", 2);

            updateList |= ImGui.RadioButton(TranslationSource.GetText("MATERIAL_BY_MATERIALS"), !FilterByMeshes);
            ImGui.SameLine();
            updateList |= ImGui.RadioButton(TranslationSource.GetText("MATERIAL_BY_MESHES"), FilterByMeshes);

            if (updateList) {
                FilterByMeshes = !FilterByMeshes;
                UpdateMaterialList();
            }

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);

            //Child is needed to keep dialog focused??
            var posY = ImGui.GetCursorPosY();
            var sizew = new Vector2(ImGui.GetColumnWidth() + 15, ImGui.GetWindowHeight() - posY - 15);

            ImGui.BeginChild("materialChild", sizew, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

            var col = ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg];
            if (ImGui.BeginTable("listView", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupColumn("Material");

                ImGui.TableSetupColumn("Wall");
                ImGui.TableSetupColumn("Ground");
                ImGui.TableSetupColumn("Attribute");

                ImGui.TableHeadersRow();
                foreach (var material in Results)
                {
                    //enable/disable color
                    var textColor = material.Value.IsEnabled ? ThemeHandler.Theme.Text : new Vector4(0.5F, 0.5F, 0.5F, 1);
                    ImGui.PushStyleColor(ImGuiCol.Text, textColor);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    //Selection and material display column
                    bool selected = ImGui.Selectable(material.Key, selectedMaterials.Contains(material.Key), ImGuiSelectableFlags.SpanAllColumns);
                    bool focused = ImGui.IsItemFocused();
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                    {
                        if (!selectedMaterials.Contains(material.Key))
                        {
                            if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                                selectedMaterials.Clear();

                            selectedMaterials.Add(material.Key);
                        }
                        MaterialSelector.OpenPopup();
                    }
                    ImGui.TableNextColumn();

                    //Attribute column
                    string attributeName = MaterialSelector.GetAttributeName(material.Value.Attribute);
                    string materialName = MaterialSelector.GetAttributeMaterialName(material.Value.Attribute);

                    ImGui.Text(material.Value.Attribute.WallCode.ToString()); ImGui.TableNextColumn();
                    ImGui.Text(material.Value.Attribute.GroundCode.ToString()); ImGui.TableNextColumn();
                    ImGui.Text(material.Value.Attribute.Att0Code.ToString()); 

                    if (selected || (focused && !selectedMaterials.Contains(material.Key)))
                    {
                        if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                            selectedMaterials.Clear();

                        if (ImGui.GetIO().KeyShift)
                        {
                            bool selectRange = false;
                            foreach (var val in Results)
                            {
                                if (selectedMaterials.Contains(val.Key) || val.Key == material.Key)
                                {
                                    if (!selectRange)
                                        selectRange = true;
                                    else
                                        selectRange = false;
                                }
                                if (selectRange && !selectedMaterials.Contains(val.Key))
                                    selectedMaterials.Add(val.Key);
                            }
                        }

                        if (!selectedMaterials.Contains(material.Key))
                            selectedMaterials.Add(material.Key);

                        MaterialSelector.IsEnabled = material.Value.IsEnabled;
                        MaterialSelector.Update(material.Value.Attribute);
                    }


                    ImGui.PopStyleColor();
                }
                ImGui.EndTable();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();

            ImGui.NextColumn();

            ImGui.BeginChild("propertyWindow", new Vector2(ImGui.GetColumnWidth(), ImGui.GetWindowHeight() - 80));
            MaterialSelector.Render(Results, selectedMaterials);
            ImGui.EndChild();

            ImGui.EndColumns();

            ImGui.SetCursorPos(new Vector2(ImGui.GetWindowWidth() - 220, ImGui.GetWindowHeight() - 46));
            var size = new Vector2(100, 40);

            if (ImGui.Button("Cancel", size))
                DialogHandler.ClosePopup(false);

            ImGui.SameLine();
            if (ImGui.Button("Ok", size) && Results.Count > 0)
            {
                CollisionFile = ApplyDialog().Result;
                if (CollisionFile != null)
                    DialogHandler.ClosePopup(true);
            }
        }

        private async Task<KCLFile> ApplyDialog()
        {
            logger.Clear();

            KCLFile kcl = null;

            DebugLogger.OnProgressUpdated += (s, e) =>
            {
                if (DebugLogger.IsCurrentError)
                    logger.Add(Tuple.Create(1, (string)s));
                else
                    logger.Add(Tuple.Create(0, (string)s));
            };

            await Task.Run(() =>
            {
                var settings = new CollisionImportSettings()
                {
                    SphereRadius = Preset.SphereRadius,
                    PrismThickness = Preset.PrismThickness,
                    PaddingMax = new Vector3(Preset.PaddingMax),
                    PaddingMin = new Vector3(Preset.PaddingMin),
                    MaxRootSize = Preset.MaxRootSize,
                    MinRootSize = Preset.MinRootSize,
                    MinCubeSize = Preset.MinCubeSize,
                    MaxTrianglesInCube = Preset.MaxTrianglesInCube,
                };

                var polyCodes = Results.Select(x => x.Value.Attribute).Distinct().ToList();
                this.PlcFile.Codes = polyCodes;

                foreach (var mesh in ImportedModel.Scene.Models[0].Meshes)
                {
                    var removedPolygons = new List<IONET.Core.Model.IOPolygon>();
                    foreach (var poly in mesh.Polygons)
                    {
                        bool enabled = true;

                        if (!this.FilterByMeshes)
                        {
                            var mat = ImportedModel.Scene.Materials.FirstOrDefault(x => x.Name == poly.MaterialName);
                            if (mat != null)
                            {
                                string name = mat.Label == null ? mat.Name : mat.Label;
                                if (Results.ContainsKey(name))
                                {
                                    poly.Attribute = (ushort)polyCodes.IndexOf(Results[name].Attribute);
                                    enabled = Results[name].IsEnabled;
                                }
                            }
                        }
                        else if (Results.ContainsKey(mesh.Name))
                        {
                            poly.Attribute = (ushort)polyCodes.IndexOf(Results[mesh.Name].Attribute);
                            enabled = Results[mesh.Name].IsEnabled;
                        }
                        if (!enabled)
                            removedPolygons.Add(poly);
                    }
                    //Remove any polygons
                    foreach (var poly in removedPolygons)
                        mesh.Polygons.Remove(poly);
                }

                kcl = new KCLFile(ImportedModel.ToTriangles(), Version, IsBigEndian, settings);
            });
            return kcl;
        }

        class MaterialEntry
        {
            public string Name { get; set; }
            public int AttributeID { get; set; }
        }
    }
}
