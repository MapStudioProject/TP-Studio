using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using GLFrameworkEngine;
using KclLibrary;
using Toolbox.Core.ViewModels;
using MapStudio.UI;
using System.IO;
using TpLibrary;
using OpenTK.Audio.OpenAL;
using ImGuiNET;

namespace RevoKartLibrary
{
    public class CollisionRender : ITransformableObject, IColorPickable, IDrawable
    {
        public GLTransform Transform { get; set; } = new GLTransform();

        public List<CollisionMesh> Meshes = new List<CollisionMesh>();

        public bool IsHovered { get; set; }
        public bool IsSelected { get; set; }
        public bool CanSelect { get; set; } = false;


        public static bool DisplayCollision = false;

        public static float TransparencyOverlay = 1;
        public static Vector4 WireframeColor = new Vector4(0, 0, 0, 1f);

        KCLFile KCLFile;

        public bool EditMode = true;
        bool IsDisposed = false;

        private bool isVisible = true;
        public bool IsVisible
        {
            get
            {
                return DisplayCollision || isVisible;
            }
            set
            {
                isVisible = value;
            }
        }

        public IEnumerable<ITransformableObject> Selectables => this.Meshes;

        public CollisionRender(KCLFile kclFile, PLC plc) 
        {
            Init(kclFile, plc);
        }

        private void Init(KCLFile kclFile, PLC plc)
        {
            KCLFile = kclFile;

            CanSelect = true;

            Meshes.Clear();

            Dictionary<ushort, List<Triangle>> meshDiv = new Dictionary<ushort, List<Triangle>>();
            foreach (var model in kclFile.Models)
            {
                foreach (var prism in model.Prisms)
                {
                    var tri = model.GetTriangle(prism);
                    if (!meshDiv.ContainsKey(prism.CollisionFlags))
                        meshDiv.Add(prism.CollisionFlags, new List<Triangle>());

                    meshDiv [prism.CollisionFlags].Add(tri);
                }
            }

            foreach (var mesh in meshDiv)
                Meshes.Add(new CollisionMesh(mesh.Key, mesh.Value, plc));
        }

        public void Reload(KCLFile kclFile, PLC plc)
        {
            Init(kclFile, plc);
        }

        public void DrawColorPicking(GLContext context)
        {
            if (!IsVisible || IsDisposed || !CanSelect || EditMode)
                return;

            context.CurrentShader = GlobalShaders.GetShader("PICKING");
            foreach (var mesh in Meshes)
            {
                context.ColorPicker.SetPickingColor(mesh, context.CurrentShader);

                GL.Disable(EnableCap.CullFace);
                mesh.Draw(context);
                GL.Enable(EnableCap.CullFace);
            }
        }

        public void OnMouseMove(MouseEventInfo mouseInfo)
        {
            Select(mouseInfo);
        }

        public void OnMouseDown(MouseEventInfo mouseInfo)
        {
            Select(mouseInfo);
        }

        private void Select(MouseEventInfo mouseInfo)
        {
            if (!EditMode)
                return;

            var mouse = mouseInfo.Position;
            var pos = GLContext.ActiveContext.GetPointUnderMouse(100);
            var eye = GLContext.ActiveContext.Camera.GetViewPostion();

            var ray = CameraRay.PointScreenRay((int)mouse.X, (int)mouse.Y, GLContext.ActiveContext.Camera);
            Vector3 hitPos = (ray.Direction * 1);

            var rayPos = new System.Numerics.Vector3(hitPos.X, hitPos.Y, hitPos.Z);
            var cameraEye = new System.Numerics.Vector3(ray.Origin.X, ray.Origin.Y, ray.Origin.Z);

            if (!KeyEventInfo.State.KeyCtrl && !KeyEventInfo.State.KeyShift)
            {
                foreach (var mesh in Meshes)
                {
                    foreach (var face in mesh.Faces)
                        face.IsSelected = false;
                }
            }

            float smallestDist = float.MaxValue;
            CollisionFace closest = null;
            foreach (var mesh in Meshes)
            {
                foreach (var face in mesh.Faces)
                {
                    if (face.Triangle.IsRayInTriangle(rayPos, cameraEye, System.Numerics.Matrix4x4.Identity))
                    {
                        var triCenter = face.Triangle.GetTriangleCenter();
                        float numerator = (System.Numerics.Vector3.Dot(face.Triangle.Normal, System.Numerics.Vector3.Subtract(cameraEye, triCenter)));
                        float denominator = System.Numerics.Vector3.Dot(rayPos, face.Triangle.Normal);
                        float distance = (-(numerator) / denominator);

                        if (distance < smallestDist)
                        {
                            closest = face;
                            smallestDist = distance;
                        }
                    }

                    // if (face.Triangle.Prism.GlobalIndex == hit.Prism.GlobalIndex)
                    //    face.IsSelected = true;
                }
            }

            /*  var hit = KCLFile.Models[0].CheckRayCast(rayPos, cameraEye);
              if (hit != null)
              {


              }*/

            if (closest != null)
            {
                closest.IsSelected = true;
                foreach (var mesh in Meshes)
                    mesh.UpdateIndexBuffer();
            }
        }

        public void OnMouseUp(MouseEventInfo mouseInfo)
        {

        }

        public void DrawModel(GLContext context, Pass pass)
        {
            if (!IsVisible || IsDisposed)
                return;

            foreach (var mesh in Meshes)
            {
                if (EditMode && mesh.Faces.Any(x => x.IsHovered) && pass == Pass.OPAQUE)
                    mesh.UpdateIndexBuffer();
            }

            foreach (var mesh in Meshes)
            {
                if (!mesh.IsVisible)
                    continue;

                if (TransparencyOverlay < 1.0f)
                    mesh.DrawOverlayCollision(context, pass);
                else
                    mesh.DrawSolidCollision(context, pass);
            }
        }

        public void Dispose()
        {
            foreach (var mesh in Meshes)
                mesh.Dispose();
        }
    }
    
    public class CollisionMesh : RenderMesh<CollisionVertex>, ITransformableObject, IRenderNode
    {
        public GLTransform Transform { get; set; } = new GLTransform();

        public bool IsHovered { get; set; }

        public bool IsSelected
        {
            get { return UINode.IsSelected; }
            set { UINode.IsSelected = value; }
        }

        public bool IsVisible
        {
            get { return UINode.IsChecked; }
            set { UINode.IsChecked = value; }
        }

        public bool CanSelect { get; set; } = true;
        public NodeBase UINode { get; set; }

        public CollisionFace[] Faces;

        StandardMaterial SolidMaterial = new StandardMaterial();
        StandardMaterial OverlayMaterial = new StandardMaterial();
        PatternMaterial PatternMaterial = new PatternMaterial();

        public BufferObject IndexBuffer;
        public BufferObject SelectionBuffer;

        private bool usePatternShading = false;

        private ushort AttributeID;

        private Vector4 Color = Vector4.One;

        public CollisionMesh(ushort att, List<Triangle> tris, PLC plc) : base(GetVertices(tris), PrimitiveType.Triangles)
        {
            UINode = new NodeBase(att.ToString("X4")) { HasCheckBox = true };
            UINode.Icon = IconManager.MESH_ICON.ToString();
            AttributeID = att;
            UINode.Tag = att;

            UINode.Header = att.ToString();
            UINode.Icon = "Material";

            UINode.TagUI.UIDrawer += delegate
            {
                var codeData = plc.Codes[att];

                void EditEnum<T>(string label, string property)
                {
                    ImGui.Text(label);
                    ImGui.NextColumn();

                    ImGuiHelper.ComboFromEnum<T>($"##{label}", codeData, property);

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
            };

            string icon = $"{Toolbox.Core.Runtime.ExecutableDir}\\Lib\\Images\\Collision\\Material.png";
            if (!IconManager.HasIcon("Material") && File.Exists(icon)) //Load the icon if found
                IconManager.TryAddIcon("Material", File.ReadAllBytes(icon));

            //Index buffer and a seperate selection buffer for selected indices to redraw a selection material
            IndexBuffer = new BufferObject(BufferTarget.ElementArrayBuffer);
            SelectionBuffer = new BufferObject(BufferTarget.ElementArrayBuffer);

            Faces = new CollisionFace[tris.Count];
            for (int i = 0; i < Faces.Length; i++)
                Faces[i] = new CollisionFace(tris[i]);

            UpdateIndexBuffer();
        }

        private void LoadAttributeIcon(string attribute)
        {
            //Check if there is a valid attribute set and icon not been loaded
            if (!string.IsNullOrEmpty(attribute) && !IconManager.HasIcon(attribute))
            {
                //File path on disk to the icon
                string icon = $"{Toolbox.Core.Runtime.ExecutableDir}\\Lib\\Images\\Collision\\{attribute}.png";
                if (File.Exists(icon)) //Load the icon if found
                    IconManager.TryAddIcon(System.IO.Path.GetFileNameWithoutExtension(attribute), File.ReadAllBytes(icon));
            }
            //Set the icon to the tree node
            if (IconManager.HasIcon(attribute))
                UINode.Icon = attribute;
        }

        //Draw a normal collision view with half lambert shading
        public void DrawSolidCollision(GLContext context, Pass pass)
        {
            if (pass != Pass.OPAQUE)
                return;

            //Draw filled faces
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(-5f, 1f);

            //A square pattern
            if (usePatternShading)
                PatternMaterial.Render(context, this.Transform);
            else //Standard solid shading with lambert shading.
            {
                //Gray default color
             //   SolidMaterial.Color = new Vector4(0.7f, 0.7f, 0.7f, 1);

                SolidMaterial.Color = this.Color;
                SolidMaterial.ModelMatrix = this.Transform.TransformMatrix;
                SolidMaterial.HalfLambertShading = true;
                SolidMaterial.hasVertexColors = true;
                SolidMaterial.Render(context);
            }

            GLMaterialBlendState.TranslucentAlphaOne.RenderBlendState();

            GL.Disable(EnableCap.CullFace);

            if (DebugShaderRender.DebugRendering != DebugShaderRender.DebugRender.Default)
                DrawDebugShading(context, Transform.TransformMatrix, this.IsSelected);
            else
                this.DrawWithSelection(context, IsSelected);

            //DrawLineWireframe(context);

            SolidMaterial.Color = CollisionRender.WireframeColor;
            this.DrawSelection(context);

            GLMaterialBlendState.Opaque.RenderBlendState();

            GL.Enable(EnableCap.CullFace);

            //Reset
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Disable(EnableCap.PolygonOffsetLine);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(0f, 0f);
            GL.Enable(EnableCap.CullFace);
        }

        public void DrawWireframeCollision(GLContext context, Pass pass)
        {
            GL.LineWidth(1);
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-10f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            OverlayMaterial.Render(context);
            this.DrawDefault(context);

            //Reset
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Disable(EnableCap.PolygonOffsetLine);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(0f, 0f);
            GL.Enable(EnableCap.CullFace);
        }

        //Draw using a translucent overlay
        public void DrawOverlayCollision(GLContext context, Pass pass)
        {
            if (pass != Pass.TRANSPARENT)
                return;

            if (CollisionRender.TransparencyOverlay == 0)
            {
                DrawWireframeCollision(context, pass);
                return;
            }

            OverlayMaterial.Color = new Vector4(Color.Xyz, CollisionRender.TransparencyOverlay);
            OverlayMaterial.ModelMatrix = this.Transform.TransformMatrix;
            OverlayMaterial.hasVertexColors = true;

            GLMaterialBlendState.TranslucentAlphaOne.RenderBlendState();

            //Draw filled faces
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(-5f, 1f);

            OverlayMaterial.DisplaySelection = IsSelected;
            OverlayMaterial.Render(context);
            this.DrawDefault(context);

            OverlayMaterial.Color = new Vector4(1, 0, 0, CollisionRender.TransparencyOverlay);
            OverlayMaterial.Render(context);
            this.DrawSelection(context);

            //Draw lines
            OverlayMaterial.Color = CollisionRender.WireframeColor;

            GL.LineWidth(1);
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-10f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            OverlayMaterial.Render(context);
            this.DrawDefault(context);

            //Reset
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Disable(EnableCap.PolygonOffsetLine);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(0f, 0f);
            GL.Enable(EnableCap.CullFace);

            GLMaterialBlendState.Opaque.RenderBlendState();
        }

        private void DrawLineWireframe(GLContext context)
        {
            GL.LineWidth(1);
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-0.08f, 1f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            //Gray default color
            SolidMaterial.Color = new Vector4(1, 1, 1, 1);
            SolidMaterial.Render(context);

            this.Draw(context);

            //Reset
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Disable(EnableCap.PolygonOffsetLine);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(0f, 0f);
            GL.Enable(EnableCap.CullFace);
        }

        //Draw unselected
        public void DrawDefault(GLContext context)
        {
            PrepareAttributes(context.CurrentShader);
            BindVAO();

            IndexBuffer.Bind();
            GL.DrawElements(BeginMode.Triangles, IndexBuffer.DataCount, DrawElementsType.UnsignedInt, 0);
        }

        //Draw Selected
        public void DrawSelection(GLContext context)
        {
            if (SelectionBuffer.DataCount == 0)
                return;

            PrepareAttributes(context.CurrentShader);
            BindVAO();

            SelectionBuffer.Bind();
            GL.DrawElements(BeginMode.Triangles, SelectionBuffer.DataCount, DrawElementsType.UnsignedInt, 0);
        }

        public void UpdateIndexBuffer()
        {
            List<int> indexBuffer = new List<int>();
            List<int> selIndexBuffer = new List<int>();

            //Create a normal index buffer and selection buffer
            int vertexIndex = 0;
            for (int i = 0; i < Faces.Length; i++)
            {
                if (Faces[i].IsSelected || Faces[i].IsHovered)
                {
                    selIndexBuffer.Add(vertexIndex++);
                    selIndexBuffer.Add(vertexIndex++);
                    selIndexBuffer.Add(vertexIndex++);
                }
                else
                {
                    indexBuffer.Add(vertexIndex++);
                    indexBuffer.Add(vertexIndex++);
                    indexBuffer.Add(vertexIndex++);
                }
            }
            IndexBuffer.SetData(indexBuffer.ToArray(), BufferUsageHint.StaticDraw);
            SelectionBuffer.SetData(selIndexBuffer.ToArray(), BufferUsageHint.StaticDraw);
        }

        static CollisionVertex[] GetVertices(List<Triangle> triangles)
        {
            List<CollisionVertex> vertices = new List<CollisionVertex>();
            for (int i = 0; i < triangles.Count; i++)
            {
                var triangle = triangles[i];
                var id = triangle.Attribute;
                var color = Vector4.One;

                for (int j = 0; j < 3; j++)
                {
                    vertices.Add(new CollisionVertex()
                    {
                        Position = new Vector3(
                            triangle.Vertices[j].X,
                            triangle.Vertices[j].Y,
                            triangle.Vertices[j].Z) * GLContext.PreviewScale,
                        Normal = new Vector3(
                            triangle.Normal.X,
                            triangle.Normal.Y,
                            triangle.Normal.Z),
                        Color = color,
                    });
                }
            }
            return vertices.ToArray();
        }
    }

    public class CollisionFace : ITransformableObject
    {
        public int Index;

        public GLTransform Transform { get; set; }

        public bool IsHovered { get; set; }

        public bool IsSelected { get; set; }

      public  bool CanSelect { get; set; }

        public Triangle Triangle;

        public CollisionFace(Triangle tri)
        {
            Triangle = tri;
        }
    }

    public struct CollisionVertex
    {
        [RenderAttribute(GLConstants.VPosition, VertexAttribPointerType.Float, 0)]
        public Vector3 Position;

        [RenderAttribute(GLConstants.VNormal, VertexAttribPointerType.Float, 12)]
        public Vector3 Normal;

        [RenderAttribute(GLConstants.VColor, VertexAttribPointerType.Float, 24)]
        public Vector4 Color;

        public CollisionVertex(Vector3 position, Vector3 normal, Vector4 color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }
    }

    class PatternMaterial
    {
        public void Render(GLContext context, GLTransform transform)
        {
            var shader = GlobalShaders.GetShader("KCL");
            context.CurrentShader = shader;
            shader.SetTransform(GLConstants.ModelMatrix, transform);
        }
    }
}
