using AxCrypt.Cryptor.Model;
using System.Collections.ObjectModel;

namespace AxCrypt.App.Components.Password
{
    public class SecretClientCollection : KeyedCollection<Guid, SecretClientModel>
    {
        protected override Guid GetKeyForItem(SecretClientModel item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return item.Id;
        }

        public void AddRange(IEnumerable<SecretClientModel> secrets)
        {
            if (secrets == null)
            {
                throw new ArgumentNullException(nameof(secrets));
            }
            foreach (SecretClientModel secret in secrets)
            {
                Add(secret);
            }
        }

        private int _originalCount;

        /// <summary>
        /// If this collection is the result of a filtering operating, this is the original count.
        /// </summary>
        public int OriginalCount
        {
            get { return _originalCount; }
            set { _originalCount = value; }
        }
    }
}