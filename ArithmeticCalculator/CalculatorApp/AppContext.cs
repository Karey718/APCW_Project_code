using System;
using System.Windows.Forms;
using CalculatorAPP.Forms;

namespace CalculatorAPP
{
    public class AppContext : ApplicationContext
    {
        private static AppContext current;
        public static AppContext Current => current;

        public MainForm MainForm { get; private set; }
        public CoorMainForm CoorMainForm { get; private set; }
        public DebugForm DebugForm { get; private set;}

        public AppContext()
        {
            current = this;
            ShowMainForm();
            ShowDebugForm();
        }

        public void ShowMainForm()
        {
            if (MainForm == null || MainForm.IsDisposed)
            {
                MainForm = new MainForm();
                MainForm.FormClosed += (s, args) => 
                {
                    if (Application.OpenForms.Count == 0)
                        ExitThread();
                };
            }
            
            // CoorMainForm?.Hide();
            MainForm.Show();
        }

        public void ShowCoorMainForm()
        {
            if (CoorMainForm == null || CoorMainForm.IsDisposed)
            {
                CoorMainForm = new CoorMainForm();
                CoorMainForm.FormClosed += (s, args) =>
                {
                    if (Application.OpenForms.Count == 0)
                        ExitThread();
                };
            }

            if (CoorMainForm.debugForm == null)
            {
                CoorMainForm.SetDebugForm(DebugForm);
            } 
            
            // MainForm?.Hide();
            CoorMainForm.Show();
        }

        public void ShowDebugForm()
        {
            if (DebugForm == null || DebugForm.IsDisposed)
            {
                DebugForm = new DebugForm();
                DebugForm.FormClosed += (s, args) => 
                {
                    if (Application.OpenForms.Count == 0)
                        ExitThread();
                };
            }

            DebugForm.Show();
        }
    }
}