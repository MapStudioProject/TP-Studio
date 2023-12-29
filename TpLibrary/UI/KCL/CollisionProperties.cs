using ImGuiNET;
using MapStudio.UI;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenTK.Graphics.OpenGL.GL;

namespace TpLibrary.UI.KCL
{
    public class CollisionProperties
    {
        public static void Render(PLC.sBgPc codeData)
        {
            void EditEnum<T>(string label, string property)
            {
                ImGui.Text(label);
                ImGui.NextColumn();

                ImGuiHelper.ComboFromEnum<T>($"##{label}", codeData, property, ImGuiComboFlags.HeightLargest);

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
                    p.SetValue(codeData, (uint)v);
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
                    p.SetValue(codeData, v);
                }
                ImGui.NextColumn();
            }

            if (ImGui.CollapsingHeader("Raw"))
            {
                ImGui.Columns(2);

                ImGui.Text($"0: {codeData.code0.ToString("X")}");
                ImGui.Text($"1: {codeData.code1.ToString("X")}");
                ImGui.Text($"2: {codeData.code2.ToString("X")}");
                ImGui.Text($"3: {codeData.code3.ToString("X")}");
                ImGui.Text($"4: {codeData.code4.ToString("X")}");

                ImGui.Columns(1);
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
