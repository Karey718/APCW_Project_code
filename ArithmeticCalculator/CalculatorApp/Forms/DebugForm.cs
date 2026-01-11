using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CalculatorAPP
{
    public partial class DebugForm : Form
    {
        private TextBox txtDebug; // 新增调试文本框
        private Button btnDebugToggle; // 调试窗口显示/隐藏按钮

        public DebugForm()
        {
            SetupUI();
        }


        private void SetupUI()
        {
            // 设置窗体属性
            this.Text = "Debug";
            this.Size = new Size(450, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // 调试标题
            var lblDebug = new Label
            {
                Text = "Debugging information:",
                Location = new Point(5, 5),
                Size = new Size(150, 20),
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            // 调试文本框
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

            // 调试控制按钮
            btnDebugToggle = new Button
            {
                Text = "Clear test information",
                Location = new Point(5, 155),
                Size = new Size(100, 20),
                Font = new Font("Arial", 8)
            };
            btnDebugToggle.Click += BtnDebugToggle_Click;

            // 添加控件到窗体
            this.Controls.AddRange(new Control[] 
            {
                lblDebug,
                txtDebug,
                btnDebugToggle,


            });

        }

        private void BtnDebugToggle_Click(object sender, EventArgs e)
        {
            // 清空调试信息
            txtDebug.Clear();
            AddDebugInfo("调试信息已清空");
        }

        // 公开方法：添加调试信息
        public void AddDebugInfo(string message)
        {
            if (txtDebug.InvokeRequired)
            {
                // 如果在非UI线程上调用，使用Invoke
                txtDebug.Invoke(new Action<string>(AddDebugInfo), message);
            }
            else
            {
                // 添加时间戳
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string fullMessage = $"[{timestamp}] {message}";
                
                // 添加到文本框
                txtDebug.Text = fullMessage + Environment.NewLine + txtDebug.Text;
                
                // 限制行数，避免内存占用过大
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