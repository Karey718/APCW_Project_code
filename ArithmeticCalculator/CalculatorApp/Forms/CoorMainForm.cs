using System;
using System.Drawing;
using System.Windows.Forms;
using CalculatorAPP.Controls;

namespace CalculatorAPP.Forms
{
    public partial class CoorMainForm : Form
    {
        public DebugForm debugForm;

        private CoorGraphControl coorGraphControl;
        private TextBox txtExpression;
        private ComboBox cmbColor;
        private Button btnAdd;
        private Button btnClear;
        private Button btnReset;
        private ListBox lstFunctions;
        
        public CoorMainForm()
        {

            SetupUI();
            
        }

        private void SetupUI()
        {
            Text = "Mathematical Function Coordinate System View Visualisation Tool";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;

            // 创建分割容器
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 8
            };

            // 左侧控制面板
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Control,
                AutoScroll = true
            };

            // 右侧图形区域
            coorGraphControl = new CoorGraphControl
            {
                Dock = DockStyle.Fill
            };

            coorGraphControl.SetMainForm(this);

            // 控件设置
            SetupControls(panel);

            splitContainer.Panel1.Controls.Add(panel);
            splitContainer.Panel2.Controls.Add(coorGraphControl);

            Controls.Add(splitContainer);
            int requiredWidth = CalculateRequiredPanelWidth(panel);
            splitContainer.SplitterDistance = Math.Min(requiredWidth, 400);

            coorGraphControl.ResetView();

        }

        public void SetDebugForm(DebugForm form)
        {
            debugForm = form;
        }

        public void AddDebugInfo(string message)
        {
            debugForm.AddDebugInfo(message);
        }
        
        private int CalculateRequiredPanelWidth(Panel panel)
        {
            int maxRight = 0;
            foreach (Control control in panel.Controls)
            {
                int right = control.Location.X + control.Width;
                if (right > maxRight)
                {
                    maxRight = right;
                }
            }
            return maxRight + 20;
        }

        private void SetupControls(Panel panel)
        {
            int y = 20;
            int controlWidth = 260;

            // 表达式输入
            var lblExpression = new Label
            {
                Text = "Mathematical Expression:",
                Location = new Point(20, y),
                Size = new Size(controlWidth, 20)
            };
            panel.Controls.Add(lblExpression);

            y += 25;
            txtExpression = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(controlWidth, 25),
            };
            panel.Controls.Add(txtExpression);

            // 颜色选择
            y += 35;
            var lblColor = new Label
            {
                Text = "Curve colour:",
                Location = new Point(20, y),
                Size = new Size(controlWidth, 20)
            };
            panel.Controls.Add(lblColor);

            y += 25;
            cmbColor = new ComboBox
            {
                Location = new Point(20, y),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbColor.Items.AddRange(new string[] { "Red", "Blue", "Green","Yellow", "Purple", "Orange", "Brown" });
            cmbColor.SelectedIndex = 0;
            panel.Controls.Add(cmbColor);

            y += 35;
            btnAdd = new Button
            {
                Text = "Add Expression",
                Location = new Point(20, y),
                Size = new Size(controlWidth / 2 - 5, 30)
            };
            btnAdd.Click += BtnAdd_Click;
            panel.Controls.Add(btnAdd);

            btnClear = new Button
            {
                Text = "Clear all",
                Location = new Point(20 + controlWidth / 2 + 5, y),
                Size = new Size(controlWidth / 2 - 5, 30)
            };
            btnClear.Click += BtnClear_Click;
            panel.Controls.Add(btnClear);

            // 重置视图按钮
            y += 40;
            btnReset = new Button
            {
                Text = "Reset view",
                Location = new Point(20, y),
                Size = new Size(controlWidth, 30)
            };
            btnReset.Click += BtnReset_Click;
            panel.Controls.Add(btnReset);

            // 测试按钮
            y += 40;
            var btnTest = new Button
            {
                Text = "Testing",
                Location = new Point(20, y),
                Size = new Size(controlWidth, 30)
            };
            btnTest.Click += BtnTest_Click;
            panel.Controls.Add(btnTest);

            // 函数列表
            y += 50;
            var lblList = new Label
            {
                Text = "Added Expression:",
                Location = new Point(20, y),
                Size = new Size(controlWidth, 20)
            };
            panel.Controls.Add(lblList);

            y += 25;
            lstFunctions = new ListBox
            {
                Location = new Point(20, y),
                Size = new Size(controlWidth, 200)
            };
            panel.Controls.Add(lstFunctions);


            // 使用说明
            y += 220;
            var lblHelp = new Label
            {
                Text = "User Guide:\n" +
                       "• Mouse drag: Move the view\n" +
                       "• Mouse wheel: Zoom in/out\n" +
                       "• Display reasonable range:\n   1*10^8 ~ 1*10^-8\n" +
                       "• Example: x^3 + x^2 - 2*x + 1",
                Location = new Point(20, y),
                Size = new Size(controlWidth, 150),
                ForeColor = Color.DarkBlue
            };
            panel.Controls.Add(lblHelp);

        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string expression = txtExpression.Text.Trim();
            if (string.IsNullOrEmpty(expression))
            {
                MessageBox.Show("Please enter a mathematical expression.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Color color = GetSelectedColor();
                coorGraphControl.AddFunction(expression, color);
                lstFunctions.Items.Add($"{expression} ({cmbColor.SelectedItem})");
            }
            catch
            {
                 MessageBox.Show($"Error adding expression {expression}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            txtExpression.SelectAll();
            txtExpression.Focus();
        }
        
        private void BtnTest_Click(object sender, EventArgs e)
        {
            coorGraphControl.ClearFunctions();
            lstFunctions.Items.Clear();
            
            string[] testFunctions = {
                "x^2",         // 抛物线
                "x+1",         // 直线
                "sin(x)",      // 正弦波
                "cos(x)",      // 余弦波
                "tan(x)",      // 正切函数（注意不连续点）
                "1/x",          // 反比例函数
                "log(x)",      // 对数函数
                "x^3 + x^2 - 2*x + 1",
                "error"
            };
            
            Color[] colors = {
                Color.Red,
                Color.Blue,
                Color.Green,
                Color.Yellow,
                Color.Purple,
                Color.Orange,
                Color.Brown,
                Color.Red,
                Color.Red
            };
            
            for (int i = 0; i < testFunctions.Length; i++)
            {
                try
                {
                    coorGraphControl.AddFunction(testFunctions[i], colors[i]);
                    lstFunctions.Items.Add($"{testFunctions[i]} ({colors[i].Name})");
                }
                catch (Exception ex)
                {
                    AddDebugInfo($"Error adding expression {testFunctions[i]}: {ex.Message}");
                }
            }
            
            coorGraphControl.ResetView();
            
            MessageBox.Show("Test function added! Please check the graphical display.", "Testing", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSwitch_Click(object sender, EventArgs e)
        {
            AppContext.Current?.ShowMainForm();
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            coorGraphControl.ClearFunctions();
            lstFunctions.Items.Clear();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            coorGraphControl.ResetView();
        }

        private Color GetSelectedColor()
        {
            return cmbColor.SelectedIndex switch
            {
                0 => Color.Red,
                1 => Color.Blue,
                2 => Color.Green,
                3 => Color.Yellow,
                4 => Color.Purple,
                5 => Color.Orange,
                6 => Color.Brown,
                _ => Color.Red
            };
        }
    }
}