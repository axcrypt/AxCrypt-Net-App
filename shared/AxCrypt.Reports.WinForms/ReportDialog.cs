using AxCrypt.International;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AxCrypt.Reports.WinForms
{
    public partial class ReportDialog : Form
    {
        public ReportDialog()
        {
            InitializeComponent();
        }

        private void ReportDialog_Load(object sender, EventArgs e)
        {
            if (DesignMode)
            {
                return;
            }
            InitializeMonthPickers();
        }

        private void InitializeMonthPickers()
        {
            fromMonthPicker.MaxDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            fromMonthPicker.Value = fromMonthPicker.MaxDate;
            toMonthPicker.MinDate = fromMonthPicker.MaxDate;
            toMonthPicker.MaxDate = fromMonthPicker.MaxDate;
            toMonthPicker.Value = fromMonthPicker.MaxDate;
        }

        private void fromMonthPicker_ValueChanged(object sender, EventArgs e)
        {
            DateTimePicker dtp = (DateTimePicker)sender;
            toMonthPicker.MinDate = dtp.Value;
            if (toMonthPicker.Value < toMonthPicker.MinDate)
            {
                toMonthPicker.Value = toMonthPicker.MinDate;
            }
        }

        private void toMonthPicker_ValueChanged(object sender, EventArgs e)
        {
        }

        private void toMonthPicker_Validating(object sender, CancelEventArgs e)
        {
            DateTimePicker dtp = (DateTimePicker)sender;
            if (dtp.Value < toMonthPicker.Value)
            {
                e.Cancel = true;
            }
        }

        private void saveAsButton_Click(object sender, EventArgs e)
        {
            DateMonthPeriod period = new DateMonthPeriod(fromMonthPicker.Value, toMonthPicker.Value);
            byte[] zip = new ZipReports().ZipReport(period, LocaleInfo.SE, LocaleInfo.US);

            string path;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.AddExtension = true;
                sfd.AutoUpgradeEnabled = true;
                sfd.CheckPathExists = true;
                sfd.DefaultExt = ".zip";
                sfd.FileName = $"{period}-AxCrypt-Reports.zip";
                sfd.Filter = "Zip files (*.zip)|*.zip|All Files (*.*)|*.*";
                sfd.FilterIndex = 0;
                sfd.OverwritePrompt = true;
                sfd.SupportMultiDottedExtensions = true;
                sfd.Title = "Save Reports in a Zip as...";
                sfd.ValidateNames = true;

                if (sfd.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                path = sfd.FileName;
            }

            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                stream.Write(zip, 0, zip.Length);
            }

            Close();
        }
    }
}