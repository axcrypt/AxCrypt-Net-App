using AxCrypt.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace AxCrypt.App.Windows.Infrastructure.Dialogs;

//public partial class DebugLogOutputDialog : StyledMessageBase
public partial class DebugLogOutputDialog : Page
{
    StringBuilder _logOutputTextBox = new StringBuilder();
    public DebugLogOutputDialog()
    {
        //InitializeComponent();
    }

    //public DebugLogOutputDialog(Form parent)
    //    : this()
    //{
    //    InitializeStyle(parent);
    //}

    //protected override void InitializeContentResources()
    //{
    //    Text = Texts.DialogDebugLogTitle;
    //}

    //private void DebugLogOutputDialog_Load(object sender, EventArgs e)
    //{
    //    FormClosing += (fsender, fe) => { if (!AllowClose) { Visible = false; fe.Cancel = true; } };
    //}

    public void AppendText(string text)
    {
        _logOutputTextBox.AppendLine(text);
    }

    //public bool AllowClose { get; set; }
}

