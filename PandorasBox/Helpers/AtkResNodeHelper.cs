using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Numerics;

namespace PandorasBox.Helpers
{
    public static unsafe class AtkResNodeHelper
    {
        public static unsafe bool GetAtkUnitBase(this nint ptr, out AtkUnitBase* atkUnitBase)
        {
            if (ptr == IntPtr.Zero) { atkUnitBase = null;  return false; }

            atkUnitBase = (AtkUnitBase*) ptr;
            return true;
        }
        
        public static unsafe Vector2 GetNodePosition(AtkResNode* node)
        {
            var pos = new Vector2(node->X, node->Y);
            var par = node->ParentNode;
            while (par != null)
            {
                pos *= new Vector2(par->ScaleX, par->ScaleY);
                pos += new Vector2(par->X, par->Y);
                par = par->ParentNode;
            }

            return pos;
        }

        public static unsafe Vector2 GetNodeScale(AtkResNode* node)
        {
            if (node == null) return new Vector2(1, 1);
            var scale = new Vector2(node->ScaleX, node->ScaleY);
            while (node->ParentNode != null)
            {
                node = node->ParentNode;
                scale *= new Vector2(node->ScaleX, node->ScaleY);
            }

            return scale;
        }

        public static T* SearchNodeById<T>(this AtkUldManager atkUldManager, uint nodeId) where T : unmanaged
        {
            foreach (var node in atkUldManager.Nodes)
            {
                if (node.Value is not null)
                {
                    if (node.Value->NodeId == nodeId)
                        return (T*)node.Value;
                }
            }

            return null;
        }
    }
}
