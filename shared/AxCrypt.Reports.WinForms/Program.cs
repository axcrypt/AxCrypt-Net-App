using AxCrypt.Abstractions;
using AxCrypt.Api.Implementation;
using AxCrypt.Common;
using AxCrypt.Desktop;
using AxCrypt.International;
using AxCrypt.International.Abstractions;
using AxCrypt.International.WebServices;
using AxCrypt.Reports.Abstractions;
using AxCrypt.Reports.Implementation;
using AxCrypt.Reports.WinForms.Implementation;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AxCrypt.Reports.WinForms
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            RegisterTypes();

            Application.Run(new Main());
        }

        private static void RegisterTypes()
        {
            TypeMap.Register.Singleton<ITextPersistence>(() => new FileTextPersistence());
            TypeMap.Register.Singleton<IRepository>(() => new TextFileRepository());
            TypeMap.Register.Singleton<INow>(() => new Now());
            TypeMap.Register.Singleton<IStringSerializer>(() => new StringSerializer());
            TypeMap.Register.Singleton<IExchangeService>(() => new CachedExchangeService(new UnifiedExchangeService(new RiksbankExchangeService(), new EcbExchangeService())));
            TypeMap.Register.Singleton<ICache>(() => new ItemCache());
        }
    }
}