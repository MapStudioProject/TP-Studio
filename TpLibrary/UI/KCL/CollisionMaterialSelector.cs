using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Numerics;
using ImGuiNET;
using UIFramework;
using MapStudio.UI;
using Toolbox.Core;
using RevoKartLibrary.CollisionEditor;
using Microsoft.VisualBasic;
using TpLibrary;

namespace RevoKartLibrary
{
    public class CollisionMaterialSelector 
    {
        public PLC.sBgPc Attribute = new PLC.sBgPc();

        public bool IsEnabled = true;

        private bool _openPopup = false;

        int IconSize = 17;

        Vector2 WindowPosition = new Vector2(0);
        public CollisionMaterialSelector()
        {
            foreach (var mat in Directory.GetFiles($"{Runtime.ExecutableDir}\\Lib\\Images\\Collision"))
                if (!IconManager.HasIcon(mat))
                    IconManager.TryAddIcon(System.IO.Path.GetFileNameWithoutExtension(mat), File.ReadAllBytes(mat));
        }

        public void OpenPopup()
        {
            _openPopup = true;
        }

        public string GetAttributeName(PLC.sBgPc polyCode)
        {
            return polyCode.Att0Code.ToString();

            return "";
        }

        public string GetAttributeMaterialName(PLC.sBgPc polyCode)
        {
            return polyCode.WallCode.ToString();

            return "";
        }

        public void Update(PLC.sBgPc attribute)
        {
            this.Attribute = attribute;
        }

        public void Render(Dictionary<string, CollisionEntry> materials, List<string> selected)
        {
            var codeData = this.Attribute;

            void EditEnum<T>(string label, string property)
            {
                ImGui.Text(label);
                ImGui.NextColumn();

                if (ImGuiHelper.ComboFromEnum<T>($"##{label}", codeData, property))
                {
                    var p = codeData.GetType().GetProperty(property);
                    var v = p.GetValue(codeData);
                    foreach (var mat in materials)
                        p.SetValue(mat.Value.Attribute, v);
                }

                ImGui.NextColumn();
            }

            void EditUint(string label, string property)
            {
                ImGui.Text(label);
                ImGui.NextColumn();

                var p = codeData.GetType().GetProperty(property);
                var v = (int)(uint)p.GetValue(codeData);

                if (ImGui.InputInt($"##{label}", ref v))
                {
                    foreach (var mat in materials)
                        p.SetValue(mat.Value.Attribute, (uint)v);
                }
                ImGui.NextColumn();
            }

            void EditBool(string label, string property)
            {
                ImGui.Text(label);
                ImGui.NextColumn();

                var p = codeData.GetType().GetProperty(property);
                var v = (bool)p.GetValue(codeData);

                if (ImGui.Checkbox($"##{label}", ref v))
                {
                    foreach (var mat in materials)
                        p.SetValue(mat.Value.Attribute, v);
                }
                ImGui.NextColumn();
            }

            if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                EditEnum<PLC.WallCode>("Wall", "WallCode");
                EditEnum<PLC.GroundCode>("Ground", "GroundCode");
                EditEnum<PLC.SpecialCode>("Special", "Spl");
                EditEnum<PLC.Att0Code>("Effect Attribute 0", "Att0Code");
                EditEnum<PLC.Att1Code>("Effect Attribute 1", "Att1Code");
                EditEnum<PLC.SoundID>("Sound ID", "SoundID");
                EditUint("Exit ID", "Exit");
                EditUint("Room ID", "Room");
                EditUint("Link ID", "LinkNo");
                EditUint("PolyColor", "PolyColor");

                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader("Misc", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                EditBool("Underwater Roof", "UnderwaterRoof");
                EditUint("Monkey Bars", "MonkeyBars");
                EditUint("Magnet", "Magnet");
                //   EditBool("NoHorseEntry", "NoHorseEntry");

                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader("Camera", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                EditUint("RoomInf", "RoomInf");
                EditUint("RoomCam", "RoomCam");
                EditUint("RoomCamPath", "RoomCamPath");
                EditUint("RoomCamPathPnt", "RoomCamPathPnt");

                ImGui.Columns(1);
            }
            if (ImGui.CollapsingHeader("Passthrough", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);

                EditBool("Pass Shadow", "ShadowThrough");
                EditBool("Pass HSStick", "HSStickThrough");
                EditBool("Pass Arrow", "ArrowThrough");
                EditBool("Pass Attack", "AttackThrough");
                EditBool("Pass Bomb", "BombThrough");
                EditBool("Pass Boomerang", "BoomerangThrough");
                EditBool("Pass Camera", "CameraThrough");
                EditBool("Pass IronBall", "IronBallThrough");
                EditBool("Pass Link", "LinkThrough");
                EditBool("Pass Object", "ObjThrough");
                EditBool("Pass Rope", "RopeThrough");

                ImGui.Columns(1);
            }
        }
    }
}
