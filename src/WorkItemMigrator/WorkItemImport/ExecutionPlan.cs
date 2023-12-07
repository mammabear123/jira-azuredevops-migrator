﻿using Migration.Common;
using Migration.WIContract;
using System.Collections.Generic;

namespace WorkItemImport
{
    public class ExecutionPlan
    {
        public class ExecutionItem
        {
            public string OriginId { get; set; }
            public int WiId { get; set; } = -1;
            public WiRevision Revision { get; set; }
            public string WiType { get; internal set; }
            /// <summary> Is this the final revision for the work item? </summary>
            public bool IsFinal { get; set; } = false;

            public override string ToString()
            {
                return $"{OriginId}/{WiId}, {Revision.Index}";
            }
        }

        private readonly MigrationContext _context;

        public Queue<RevisionReference> ReferenceQueue { get; private set; }

        public ExecutionPlan(IEnumerable<RevisionReference> orderedRevisionReferences, MigrationContext context)
        {
            ReferenceQueue = new Queue<RevisionReference>(orderedRevisionReferences);
            this._context = context;
        }

        private ExecutionItem TransformToExecutionItem(RevisionReference revRef)
        {
            var item = _context.GetItem(revRef.OriginId);
            var rev = item.Revisions[revRef.RevIndex];
            rev.Time = revRef.Time;
            return new ExecutionItem() { OriginId = item.OriginId, WiId = item.WiId, WiType = item.Type, Revision = rev, IsFinal = revRef.IsFinal };
        }

        public bool TryPop(out ExecutionItem nextItem)
        {
            nextItem = null;
            if (ReferenceQueue.Count > 0)
            {
                nextItem = TransformToExecutionItem(ReferenceQueue.Dequeue());
                return true;
            }
            else
                return false;
        }
    }
}
