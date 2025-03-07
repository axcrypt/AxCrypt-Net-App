using System.ComponentModel;

namespace AxCrypt.Core.UI
{
    public enum SecureMsgrSearchFilters
    {
        [Category("IgnoreSelectList")]
        None,

        OneWeek,
        OneMonth,
        ThreeMonth,
        SixMonth,
        OneYear
    }
}