using Godot;
using System.Collections.Generic;

namespace LiveCollab
{
    public partial class ChangeBuffer : GodotObject
    {
        private List<object> _pendingChanges = new List<object>();
        private double _timer = 0.0;
        private const double BATCH_INTERVAL = 0.05; // 50 milliseconds

        public void QueueChange(object change)
        {
            _pendingChanges.Add(change);
        }

        public ChangeBatch ProcessBuffer(double delta)
        {
            _timer += delta;

            // Check if we should send a batch
            if (_timer >= BATCH_INTERVAL && _pendingChanges.Count > 0)
            {
                // Create batch from pending changes
                var batch = new ChangeBatch
                {
                    Changes = new List<object>(_pendingChanges)
                };

                // Clear pending changes and reset timer
                _pendingChanges.Clear();
                _timer = 0.0;

                return batch;
            }

            return null;
        }

        public void Clear()
        {
            _pendingChanges.Clear();
            _timer = 0.0;
        }

        public int GetPendingCount()
        {
            return _pendingChanges.Count;
        }
    }
}
