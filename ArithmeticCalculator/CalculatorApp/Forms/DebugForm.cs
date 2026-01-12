using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CalculatorAPP
{
    public partial class DebugForm : Form
    {
        private TextBox txtDebug; 
        private Button btnDebugToggle; 

        public DebugForm()
        {
            SetupUI();
        }


        private void SetupUI()
        {
            this.Text = "Debug";
            this.Size = new Size(450, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            var lblDebug = new Label
            {
                Text = "Debugging information:",
                Location = new Point(5, 5),
                Size = new Size(150, 20),
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            txtDebug = new TextBox
            {
                Location = new Point(5, 30),
                Size = new Size(400, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 8),
                WordWrap = false
            };

            btnDebugToggle = new Button
            {
                Text = "Clear test information",
                Location = new Point(5, 155),
                Size = new Size(100, 20),
                Font = new Font("Arial", 8)
            };
            btnDebugToggle.Click += BtnDebugToggle_Click;

            this.Controls.AddRange(new Control[] 
            {
                lblDebug,
                txtDebug,
                btnDebugToggle,


            });

        }

        private void BtnDebugToggle_Click(object sender, EventArgs e)
        {
            txtDebug.Clear();
            AddDebugInfo("调试信息已清空");
        }

        // Public method: Add debug information
        public void AddDebugInfo(string message)
        {
            if (txtDebug.InvokeRequired)
            {
                // If invoked on a non-UI thread, use Invoke.
                txtDebug.Invoke(new Action<string>(AddDebugInfo), message);
            }
            else
            {
                // Add timestamp
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string fullMessage = $"[{timestamp}] {message}";
                
                // Add to text box
                txtDebug.Text = fullMessage + Environment.NewLine + txtDebug.Text;
                
                // Limit the number of lines to prevent excessive memory usage.
                var lines = txtDebug.Lines;
                if (lines.Length > 100)
                {
                    txtDebug.Lines = lines.Skip(lines.Length - 100).ToArray();
                }
                
                txtDebug.SelectionStart = 0;
                txtDebug.ScrollToCaret();
            }
        }
    }
}