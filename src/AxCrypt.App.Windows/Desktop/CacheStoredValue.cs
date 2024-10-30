namespace AxCrypt.App.Windows.Desktop
{
    internal class CacheStoredValue
    {
        public CacheStoredValue()
        {
            AbsoluteExpiration = DateTime.MaxValue;
            DependentChildren = new List<string>();
        }

        public object Value { get; set; }

        public DateTime AbsoluteExpiration { get; set; }

        public string DependentParent { get; set; }

        public IList<string> DependentChildren { get; private set; }
    }
}
