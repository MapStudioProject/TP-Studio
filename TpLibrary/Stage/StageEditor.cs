using GLFrameworkEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using static TpLibrary.DZR;

namespace TpLibrary.Stage
{
    public class StageEditor
    {
        public StageEditor(StagePlugin plugin, DZR dzr)
        {
            NodeBase stage_folder = new NodeBase("room.dzr");
            plugin.Root.AddChild(stage_folder);

            NodeBase door_folder = new NodeBase("Doors");
            stage_folder.AddChild(door_folder);

            NodeBase actor_folder = new NodeBase("Actors");
            stage_folder.AddChild(actor_folder);

            foreach (var door in dzr.DoorList)
                plugin.AddRender(CreateObject(door, door_folder));

            foreach (var actor in dzr.ActorList)
                plugin.AddRender(CreateObject(actor, actor_folder));

            foreach (var chunk in dzr.Chunks)
            {
                NodeBase node = new NodeBase(chunk.Magic);
                node.Tag = chunk.Data;
                stage_folder.AddChild(node);
            }
        }

        private TransformableObject CreateObject(Actor actor, NodeBase folder)
        {
            TransformableObject obj = new TransformableObject(folder, 5);
            obj.UINode.Header = actor.Name;
            obj.Transform.Position = new OpenTK.Vector3(
                actor.Position.X, actor.Position.Y, actor.Position.Z);
            obj.Transform.RotationEulerDegrees = new OpenTK.Vector3(
                actor.Rotation.X, actor.Rotation.Y, actor.Rotation.Z);
            obj.Transform.Scale = new OpenTK.Vector3(1);

            if (actor is DZR.TgscInfo)
            {
                obj.Transform.Scale = new OpenTK.Vector3(
                    ((DZR.TgscInfo)actor).Scale.X,
                    ((DZR.TgscInfo)actor).Scale.Y,
                    ((DZR.TgscInfo)actor).Scale.Z);
            }

            obj.Transform.UpdateMatrix(true);

            obj.Transform.TransformUpdated += delegate
            {
                actor.Position = new System.Numerics.Vector3(
                     obj.Transform.Position.X,
                     obj.Transform.Position.Y,
                     obj.Transform.Position.Z);
                actor.Rotation = new System.Numerics.Vector3(
                     obj.Transform.RotationEulerDegrees.X,
                     obj.Transform.RotationEulerDegrees.Y,
                     obj.Transform.RotationEulerDegrees.Z);
                if (actor is DZR.TgscInfo)
                {
                    ((DZR.TgscInfo)actor).Scale = new System.Numerics.Vector3(
                       obj.Transform.Scale.X,
                       obj.Transform.Scale.Y,
                       obj.Transform.Scale.Z);
                }
            };

            obj.AddCallback += delegate
            {
                if (!folder.Children.Contains(obj.UINode))
                    folder.AddChild(obj.UINode);
            };
            obj.RemoveCallback += delegate
            {
                if (folder.Children.Contains(obj.UINode))
                    folder.Children.Remove(obj.UINode);
            };

            return obj; 
        }
    }
}
