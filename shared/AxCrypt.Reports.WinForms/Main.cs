using AxCrypt.Reports.Abstractions;
using AxCrypt.Reports.Model.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Reports.WinForms
{
    public partial class Main : Form
    {
        private static readonly PersistentName REPOSITORY_NAME = new PersistentName("AxReports");

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (DesignMode)
            {
                return;
            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IEnumerable<string> files;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.DefaultExt = ".csv";
                ofd.Filter = "Comma separated files (*.csv)|*.csv|Text files (*.txt)|*.txt|All Files (*.*)|*.*";
                ofd.Multiselect = true;
                ofd.ReadOnlyChecked = true;
                ofd.ShowReadOnly = false;
                ofd.SupportMultiDottedExtensions = true;
                ofd.Title = "Select files with data to import.";
                ofd.ValidateNames = true;

                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                files = ofd.FileNames;
            }

            foreach (string file in files)
            {
                try
                {
                    using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        CsvImport.Import(stream, REPOSITORY_NAME);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Can't import {file} due to {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void createReportsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ReportDialog dlg = new ReportDialog())
            {
                dlg.ShowDialog(this);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void clearAllDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            New<IRepository>().ClearAll(REPOSITORY_NAME);
        }
    }
}