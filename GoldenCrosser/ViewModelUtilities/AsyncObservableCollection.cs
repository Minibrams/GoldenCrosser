using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GoldenCrosser
{
    /* The difference between this class and its parent, ObservableCollection,
     * is that elements in the collection can be added and removed from
     * different threads. */
    public class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        private readonly object _collectionLock = new object();

        public AsyncObservableCollection() : this(Enumerable.Empty<T>()) { }

        public AsyncObservableCollection(IEnumerable<T> items) : base(items) {
            BindingOperations.EnableCollectionSynchronization(this, _collectionLock);
        }
    }
}
