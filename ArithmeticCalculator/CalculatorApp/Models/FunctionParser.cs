using System;
using ExpressionCalculator; 

namespace CalculatorAPP.Models
{
    public class FunctionParser
    {
        private readonly Action<string> _addDebugInfo;

        public FunctionParser(Action<string> addDebugInfo = null)
        {
            _addDebugInfo = addDebugInfo;
        }

        public Func<double, double> ParseFunction(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return x => double.NaN;

            try
            {
                EvaluateExpression(expression,0);
                return x => EvaluateExpression(expression, x);
            }
            catch
            {
                throw new Exception($"Expression {expression} parsing error");
            }
        }

        private double EvaluateExpression(string expression, double x)
        {
            try
            {
                // 替换变量 x
                string xString = x.ToString("0.####################");
                string finalExpr = expression.Replace("x", xString);
                
                // 使用自定义表达式求值器
                return EvaluateSimpleExpression(finalExpr);
            }
            catch (Exception ex)
            {
                throw new Exception($"The expression {expression} failed to calculate when X={x}: {ex.Message}");
            }
        }

        private double EvaluateSimpleExpression(string expression)
        {
            try
            {
                string result = Calculator.Calculate(expression);
                // 自定义报错捕捉处理
                if (result.Contains("Error"))
                {
                    if (result.Contains("zero"))
                    {
                        return double.NaN;
                    }
                    
                    throw new Exception(result); 
                }
                
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }

}