using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ExpressionCalculator;
using Microsoft.FSharp.Collections;

namespace CalculatorAPP
{
    public partial class MainForm : Form
    {
        private TextBox inputTextBox;
        private TextBox outputTextBox;
        private Button calculateButton;
        private Label inputLabel;
        private Label outputLabel;
        private Button btnSwitch;
        private ListView symbolTableListView;
        private Label symbolTableLabel;
        private Button btnClearSymbolTable;
        private ListView historyListView;
        private Label historyLabel;
        private Button btnClearHistory;

        // 符号表和历史记录
        private FSharpMap<string, Calculator.VarSym> currentSymbolTable;
        private List<HistoryRecord> calculationHistory;


        public MainForm()
        {
            // 初始化符号表和历史记录
            currentSymbolTable = Calculator.GetEmptyEnv();
            calculationHistory = new List<HistoryRecord>();
            SetupUI();
        }

        private void SetupUI()
        {
            // 设置窗体属性
            this.Text = "Arithmetic Expression Calculator";
            this.Size = new Size(950, 460);
            this.MinimumSize = new Size(800, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // 左侧面板 - 符号表 
            var leftPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(200, this.ClientSize.Height),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            symbolTableLabel = new Label
            {
                Text = "Symbol Table:",
                Location = new Point(10, 10),
                Size = new Size(180, 20),
                Font = new Font("Arial", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            symbolTableListView = new ListView
            {
                Location = new Point(10, 40),
                Size = new Size(180, 350),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            // 清理按钮
            btnClearSymbolTable = new Button
            {
                Text = "Clear Symbol Table",
                Dock = DockStyle.Bottom,
                Height = 30,
                Font = new Font("Arial", 10, FontStyle.Bold),
            };
            btnClearSymbolTable.Click += BtnClearSymbolTable_Click;

            symbolTableListView.Columns.Add("Variable", 80);
            symbolTableListView.Columns.Add("Value", 100);


            leftPanel.Controls.Add(symbolTableLabel);
            leftPanel.Controls.Add(symbolTableListView);
            leftPanel.Controls.Add(btnClearSymbolTable);



            // 中间面板 - 主计算
            var middlePanel = new Panel
            {
                Location = new Point(200, 0),
                Size = new Size(
                    this.ClientSize.Width - 400,
                    this.ClientSize.Height),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 输入文本框
            inputTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Arial", 14),
                Margin = new Padding(10),
                Multiline = true,
                ScrollBars = ScrollBars.None
            };

            // 输出文本框
            outputTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Arial", 14),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.LightGray,
                Margin = new Padding(10)
            };

            // 按钮面板
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 7,
                Padding = new Padding(10)
            };

            // 列均分
            for (int i = 0; i < 4; i++)
                buttonPanel.ColumnStyles.Add(
                    new ColumnStyle(SizeType.Percent, 25f));

            // 行均分
            for (int i = 0; i < 7; i++)
                buttonPanel.RowStyles.Add(
                    new RowStyle(SizeType.Percent, 25f));   

            // 按钮定义
            string[] buttons =
            {
                "sin","cos","tan","log",
                "^","pi","e","ln",
                "7","8","9","/",
                "4","5","6","*",
                "1","2","3","-",
                "0",".","=","+",
                "(",")","C","←",
            };


            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = new Button
                {
                    Text = buttons[i],
                    Dock = DockStyle.Fill,
                    Font = new Font("Arial", 10, FontStyle.Bold)
                };
                btn.Click += CalculatorButton_Click;
                buttonPanel.Controls.Add(btn, i % 4, i / 4);
            }

            // 独立计算按钮（不使用 =）
            calculateButton = new Button
            {
                Text = "Calculate",
                Dock = DockStyle.Bottom,
                Height = 45,
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White
            };
            calculateButton.Click += CalculateButton_Click;

            // 切换按钮
            btnSwitch = new Button
            {
                Text = "Show Coordinate System Window",
                Dock = DockStyle.Bottom,
                Height = 45,
                Font = new Font("Arial", 10, FontStyle.Bold),
            };
            btnSwitch.Click += BtnSwitch;


            // 添加到中间面板
            middlePanel.Controls.Add(buttonPanel);
            middlePanel.Controls.Add(calculateButton);
            middlePanel.Controls.Add(btnSwitch);
            middlePanel.Controls.Add(outputTextBox);
            middlePanel.Controls.Add(inputTextBox);



            // 右侧面板 - 历史记录
            var rightPanel = new Panel
            {
                Location = new Point(this.ClientSize.Width - 200, 0),
                Size = new Size(200, this.ClientSize.Height),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
            };

            historyLabel = new Label
            {
                Text = "Calculation History:",
                Location = new Point(10, 10),
                Size = new Size(180, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            historyListView = new ListView
            {
                Location = new Point(10, 40),
                Size = new Size(180, 350),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            // 清理按钮
            btnClearHistory = new Button
            {
                Text = "Clear History",
                Dock = DockStyle.Bottom,
                Height = 30,
                Font = new Font("Arial", 10, FontStyle.Bold),
            };
            btnClearHistory.Click += BtnClearHistory_Click;

            historyListView.Columns.Add("Expression", 100);
            historyListView.Columns.Add("Result", 80);
            historyListView.DoubleClick += HistoryListView_DoubleClick;

            rightPanel.Controls.Add(historyLabel);
            rightPanel.Controls.Add(historyListView);
            rightPanel.Controls.Add(btnClearHistory);

            // 添加所有面板到窗体
            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);
            this.Controls.Add(middlePanel);

            // 更新符号表显示
            UpdateSymbolTableDisplay();
        }

        private void CalculatorButton_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            switch (btn.Text)
            {
                case "C":
                    inputTextBox.Clear();
                    outputTextBox.Clear();
                    outputTextBox.BackColor = Color.LightGray;
                    break;

                case "←":
                    if (inputTextBox.Text.Length > 0)
                    {
                        inputTextBox.Text =
                            inputTextBox.Text.Substring(0, inputTextBox.Text.Length - 1);
                    }
                    break;

                case "=":
                    // 不触发计算，仅作占位（或你也可以直接忽略）
                    inputTextBox.Text += "=";
                    break;

                default:
                    inputTextBox.Text += btn.Text;
                    break;
            }
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            try
            {
                string expression = inputTextBox.Text.Trim();

                if (string.IsNullOrEmpty(expression))
                {
                    outputTextBox.Text = "Error: Please enter an arithmetic expression.";
                    outputTextBox.BackColor = Color.LightPink;
                    return;
                }

                // 调用F#计算模块
                var (result, newSymbolTable) = Calculator.Calculate_st(expression, currentSymbolTable);
                outputTextBox.Text = result;

                // 更新符号表
                currentSymbolTable = newSymbolTable;
                UpdateSymbolTableDisplay();

                // 根据结果显示不同的颜色
                if (result.StartsWith("Error"))
                {
                    outputTextBox.BackColor = Color.LightPink;
                    // 添加到历史记录
                    AddToHistory(expression, result, true);
                }
                else
                {
                    outputTextBox.BackColor = Color.LightGreen;
                    AddToHistory(expression, result, false);
                }
            }
            catch (Exception ex)
            {
                outputTextBox.Text = $"Program error: {ex.Message}";
                outputTextBox.BackColor = Color.LightPink;
            }
        }

        private void UpdateSymbolTableDisplay()
        {
            symbolTableListView.Items.Clear();
            
            try
            {
                // 调用F#方法来获取符号表内容
                var symbolTableData = Calculator.GetSymbolTableData(currentSymbolTable);
                
                foreach (var item in symbolTableData)
                {
                    string variableName = item.Item1;
                    string valueDisplay = item.Item2;
                    
                    var listItem = new ListViewItem(new[] { variableName, valueDisplay });
                    symbolTableListView.Items.Add(listItem);
                }
            }
            catch (Exception ex)
            {
                // 如果获取符号表数据失败，显示错误信息
                var errorItem = new ListViewItem(new[] { "Error", ex.Message });
                symbolTableListView.Items.Add(errorItem);
            }
        }

        private void AddToHistory(string expression, string result, bool error)
        {
            // 限制历史记录数量
            if (calculationHistory.Count >= 50)
            {
                calculationHistory.RemoveAt(0);
            }

            var historyRecord = new HistoryRecord
            {
                Expression = expression,
                Result = result,
                Error = error,
                Timestamp = DateTime.Now
            };

            calculationHistory.Add(historyRecord);
            UpdateHistoryDisplay();
        }

        private void UpdateHistoryDisplay()
        {
            historyListView.Items.Clear();

            foreach (var record in calculationHistory)
            {
                // 简化表达式显示
                string shortExpression = record.Expression.Length > 20
                    ? record.Expression.Substring(0, 20) + "..."
                    : record.Expression;

                // 简化结果显示
                string shortResult = record.Result.Length > 30
                    ? record.Result.Substring(0, 30) + "..."
                    : record.Result;

                var item = new ListViewItem(new[] { shortExpression, shortResult });
                item.Tag = record; 
                historyListView.Items.Add(item);
            }
        }

        private void HistoryListView_DoubleClick(object sender, EventArgs e)
        {
            if (historyListView.SelectedItems.Count > 0)
            {
                var record = historyListView.SelectedItems[0].Tag as HistoryRecord;
                if (record != null)
                {
                    inputTextBox.Text = record.Expression;
                    outputTextBox.Text = record.Result;
                    if (record.Error)
                    {
                        outputTextBox.BackColor = Color.LightPink;
                    }
                    else
                    {
                        outputTextBox.BackColor = Color.LightGreen;
                    }
                }
            }
        }


        private void BtnClearSymbolTable_Click(object sender, EventArgs e)
        {
            currentSymbolTable = Calculator.GetEmptyEnv();
            symbolTableListView.Items.Clear();
        }

        private void BtnClearHistory_Click(object sender, EventArgs e)
        {
            calculationHistory = new List<HistoryRecord>();
            historyListView.Items.Clear();
        }

        private void BtnSwitch(object sender, EventArgs e)
        {
            AppContext.Current?.ShowCoorMainForm();
        }
    }
    
    // 历史记录类
    public class HistoryRecord
    {
        public string Expression { get; set; }
        public string Result { get; set; }
        public bool Error { get; set; }
        public DateTime Timestamp { get; set; }
    }
}