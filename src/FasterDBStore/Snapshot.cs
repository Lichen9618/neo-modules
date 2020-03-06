using FASTER.core;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly Store store;
        private Guid checkpoint;
        private long sno = 0;

        public Snapshot(Store store)
        {
            this.store = store;

            store.db.TakeFullCheckpoint(out checkpoint);
            store.db.CompleteCheckpointAsync().GetAwaiter().GetResult();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Commit()
        {
            store.session.CompletePending(true);
            store.db.TakeFullCheckpoint(out checkpoint);
            store.db.CompleteCheckpointAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (sno == 0) return;

            // Recover the checkpoint
            store.db.Recover(checkpoint);
            checkpoint = Guid.Empty;
            sno = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Delete(byte table, byte[] key)
        {
            //store.Delete(table, key);

            var k = new BufferKey(table, key);
            store.session.Delete(ref k, Empty.Default, ++sno);
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte table, byte[] prefix)
        {
            return store.Find(table, prefix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Put(byte table, byte[] key, byte[] value)
        {
            //store.Put(table, key, value);

            var k = new BufferKey(table, key);
            var v = new BufferValue(value);

            store.session.Upsert(ref k, ref v, Empty.Default, ++sno);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] TryGet(byte table, byte[] key)
        {
            return store.TryGet(table, key);
        }
    }
}
