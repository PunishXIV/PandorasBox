using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.UI
{
    public unsafe class MergeStacks : Feature
    {
        public override string Name => "Automatically merge stacks of same items";

        public override string Description => "When you open your inventory, the plugin will try and pull all stacks of the same item together.";
    }
}
