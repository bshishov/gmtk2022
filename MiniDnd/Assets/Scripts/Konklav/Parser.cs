using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Konklav
{
    public class AstFormatter
    {
        private int _indent;
        private readonly StringBuilder _sb;

        public AstFormatter()
        {
            _indent = 0;
            _sb = new StringBuilder();
        }

        private void PrependIndent()
        {
            for (var i = 0; i < _indent; i++)
                _sb.Append("    ");
        }

        private void Write(string text)
        {
            _sb.Append(text);
        }

        public void BeginNode(string text)
        {
            PrependIndent();
            Write(text);
            Write(" {\n");
            _indent++;
        }

        public void Node(string text, string arg = null)
        {
            PrependIndent();
            Write(text);

            if (!string.IsNullOrEmpty(arg))
            {
                Write(" : ");
                Write(arg);
            }

            Write("\n");
        }

        public void EndNode()
        {
            _indent--;
            PrependIndent();
            Write("}\n");
        }

        public override string ToString() => _sb.ToString();
    }

    public interface IAst
    {
        void FormatAst(AstFormatter formatter);
    }
    
    public interface IContext
    {
        int LastDiceRollValue { get; }
        void ShowText(string text);
        void Goto(string activityName);
        bool Visited(string activityName);
        void End();
        void Debug(string message);
        void ShowImage(string imageName);
        void SetTag(string tag);
        bool HasTag(string tag);
    }


    public interface IBoolExpression : IAst
    {
        bool Evaluate(IContext player);
    }

    public readonly struct AndExpression : IBoolExpression
    {
        public readonly IBoolExpression[] Expressions;

        public AndExpression(params IBoolExpression[] expressions)
        {
            Expressions = expressions;
        }

        public bool Evaluate(IContext player) => Expressions.All(e => e.Evaluate(player));
        
        public void FormatAst(AstFormatter formatter)
        {
            formatter.BeginNode("And");
            foreach (var expression in Expressions) expression.FormatAst(formatter);
            formatter.EndNode();
        }
    }

    public readonly struct OrExpression : IBoolExpression
    {
        public readonly IBoolExpression[] Expressions;

        public OrExpression(params IBoolExpression[] expressions) => Expressions = expressions;

        public bool Evaluate(IContext player) => Expressions.Any(e => e.Evaluate(player));
        
        public void FormatAst(AstFormatter formatter)
        {
            formatter.BeginNode("Or");
            foreach (var expression in Expressions) expression.FormatAst(formatter);
            formatter.EndNode();
        }
    }

    public struct LiteralTrueExpression : IBoolExpression
    {
        public bool Evaluate(IContext player) => true; 
        
        public void FormatAst(AstFormatter formatter) => formatter.Node("True");
    }

    public struct LiteralFalseExpression : IBoolExpression
    {
        public bool Evaluate(IContext player) => true; 
        
        public void FormatAst(AstFormatter formatter) => formatter.Node("False");
    }

    public readonly struct ExactDiceResultExpression : IBoolExpression
    {
        public readonly int Value;

        public ExactDiceResultExpression(int value)
        {
            Value = value;
        }

        public bool Evaluate(IContext player) => player.LastDiceRollValue == Value;
        
        public void FormatAst(AstFormatter formatter) => formatter.Node("ExactDice", Value.ToString());
    }

    public readonly struct RangeDiceResultExpression : IBoolExpression
    {
        private readonly int _rangeFrom;
        private readonly int _rangeTo;

        public RangeDiceResultExpression(int rangeFrom, int rangeTo)
        {
            _rangeFrom = rangeFrom;
            _rangeTo = rangeTo;
        }

        public bool Evaluate(IContext player) => player.LastDiceRollValue >= _rangeFrom && player.LastDiceRollValue <= _rangeTo;
        
        public void FormatAst(AstFormatter formatter) => formatter.Node("RangeDice", $"{_rangeFrom}-{_rangeTo}");
    }

    public readonly struct VisitedExpression : IBoolExpression
    {
        public readonly string ActivityName;
        
        public VisitedExpression(string activityName)
        {
            ActivityName = activityName;
        }
        
        public bool Evaluate(IContext player) => player.Visited(ActivityName);
        
        public void FormatAst(AstFormatter formatter) => formatter.Node("Visited", ActivityName);
    }

    public class HasTagExpression : IBoolExpression
    {
        public readonly string Tag;
        
        public HasTagExpression(string tag)
        {
            Tag = tag;
        }

        public bool Evaluate(IContext player)
        {
            return player.HasTag(Tag);
        }
        
        public void FormatAst(AstFormatter formatter) => formatter.Node("HasTag", Tag);
    }

    public interface INumberExpression : IAst
    {
        float EvaluateAsFloat(IContext player);
    }

    public class ExactFloatValue : INumberExpression
    {
        private readonly float _value;

        public ExactFloatValue(float value)
        {
            _value = value;
        }

        public float EvaluateAsFloat(IContext player) => _value;
        
        public void FormatAst(AstFormatter formatter) => formatter.Node("Float", _value.ToString(CultureInfo.InvariantCulture));
    }

    public interface IAction : IAst
    {
        void Execute(IContext player);
    }

    public class TextAction : IAction
    {
        public readonly string Text;

        public TextAction(string text)
        {
            Text = text;
        }

        public void Execute(IContext player)
        {
            player.Debug($"Text: {Text}");
            player.ShowText(Text);
        }

        public void FormatAst(AstFormatter formatter)
        {
            formatter.Node("Text", Text);
        }
    }

    public class GotoAction : IAction
    {
        private readonly string _activityName;

        public GotoAction(string activityName)
        {
            _activityName = activityName;
        }

        public void Execute(IContext player)
        {
            player.Debug($"/goto {_activityName}");
            player.Goto(_activityName);
        }

        public void FormatAst(AstFormatter formatter)
        {
            formatter.Node("Goto", _activityName);
        }
    }
    
    public class TagAction : IAction
    {
        public readonly string TagName;

        public TagAction(string tagName)
        {
            TagName = tagName;
        }

        public void Execute(IContext player)
        {
            player.Debug($"/tag {TagName}");
            player.SetTag(TagName);
        }

        public void FormatAst(AstFormatter formatter)
        {
            formatter.Node("Tag", TagName);
        }
    }
    
    public class DebugAction : IAction
    {
        private readonly string _string;

        public DebugAction(string s)
        {
            _string = s;
        }

        public void Execute(IContext player) => player.Debug(_string);
        public void FormatAst(AstFormatter formatter)
        {
            formatter.Node("Debug", _string);
        }
    }
    
    public class EndAction : IAction
    {
        public void Execute(IContext player)
        {
            player.Debug($"/end");
            player.End();
        }

        public void FormatAst(AstFormatter formatter)
        {
            formatter.Node("End");
        }
    }

    public class CompositeAction : IAction
    {
        public readonly IEnumerable<IAction> Actions;

        public CompositeAction(params IAction[] actions) => Actions = actions;
        public CompositeAction(IEnumerable<IAction> actions) => Actions = actions;

        public void Execute(IContext player)
        {
            foreach (var action in Actions) action.Execute(player);
        }

        public void FormatAst(AstFormatter formatter)
        {
            formatter.BeginNode("Composite");
            foreach (var action in Actions) 
                action.FormatAst(formatter);
            formatter.EndNode();
        }
    }

    public class ConditionalAction : IAction
    {
        private readonly IBoolExpression _condition;
        private readonly IAction _action;

        public ConditionalAction(IBoolExpression condition, IAction action)
        {
            _condition = condition;
            _action = action;
        }
    
        public void Execute(IContext player)
        {
            if (_condition.Evaluate(player))
            {
                _action.Execute(player);
            }
        }

        public void FormatAst(AstFormatter formatter)
        {
            formatter.BeginNode("ConditionalAction");
            formatter.BeginNode("Condition");
            _condition.FormatAst(formatter);
            formatter.EndNode();
            formatter.BeginNode("Action");
            _action.FormatAst(formatter);
            formatter.EndNode();
            formatter.EndNode();
        }
    }

    public class RollableAttribAction : IAction
    {
        public readonly IBoolExpression Condition;

        public RollableAttribAction(IBoolExpression condition)
        {
            Condition = condition;
        }
    
        public void Execute(IContext player) { }

        public void FormatAst(AstFormatter formatter)
        {
            formatter.BeginNode("RollableAttrib");
            Condition.FormatAst(formatter);
            formatter.EndNode();
        }
    }

    public class WeighAttribAction : IAction
    {
        public readonly INumberExpression Expression;

        public WeighAttribAction(INumberExpression expression)
        {
            Expression = expression;
        }
    
        public void Execute(IContext player) { }
        
        public void FormatAst(AstFormatter formatter)
        {
            formatter.BeginNode("WeighAttribAction");
            Expression.FormatAst(formatter);
            formatter.EndNode();
        }
    }

    public class ImageAction : IAction
    {
        public readonly string ImageName;

        public ImageAction(string imageName)
        {
            ImageName = imageName;
        }

        public void Execute(IContext player)
        {
            player.Debug($"Image: {ImageName}");
            player.ShowImage(ImageName);
        }
        
        public void FormatAst(AstFormatter formatter)
        {
            formatter.Node("Image", ImageName);
        }
    }

    public class ActivityAst : IAst
    {
        public string Name;
        public CompositeAction CompositeAction;
        
        public void FormatAst(AstFormatter formatter)
        {
            formatter.BeginNode("Activity");
            formatter.Node("Name", Name);
            CompositeAction.FormatAst(formatter);
            formatter.EndNode();
        }
    }

    public class ParserError : Exception
    {
        public readonly int Position;
        private readonly string _source;

        public ParserError(string source, int position, string message)
            : base(message)
        {
            _source = source;
            Position = position;
        }
    }

    public class Parser
    {
        private readonly string _source;
        private int _position;
        private const string AnyWhitespace = " \t\n\r";
        private const string AnyNonBreakingWhitespace = " \t";
        private const string LineBreak = "\r\n";
        private const string Digits = "0123456789";

        public Parser(string source)
        {
            _position = 0;
            _source = source;
        }

        private ParserError Error(string message)
        {
            var line = 1;
            var to = Math.Min(_position, _source.Length);
            for (var i = 0; i < to; i++)
            {
                if (_source[i] == '\n')
                    line++;
            }
            
            return new ParserError(_source, _position, $"Error at line {line}: {message}");
        }

        private char TakeChar()
        {
            if (_position >= _source.Length)
                throw Error("EOF");
            return _source[_position++];
        }

        private char CurrentChar() => _source[_position];

        private bool IsEnd() => _position >= _source.Length;

        public string ReadNonEmptyStringUntilWhitespace()
        {
            var sb = new StringBuilder();
            while (!IsEnd())
            {
                if (AnyWhitespace.Contains(CurrentChar()))
                    break;

                sb.Append(TakeChar());
            }

            var value = sb.ToString();
            if (string.IsNullOrWhiteSpace(value))
                throw Error("Non empty string expected");
            return value;
        }
        
        public string ReadNonEmptyStringUntilLineBreak()
        {
            var sb = new StringBuilder();
            while (!IsEnd())
            {
                if (LineBreak.Contains(CurrentChar()))
                    break;

                sb.Append(TakeChar());
            }

            var value = sb.ToString();
            if (string.IsNullOrWhiteSpace(value))
                throw Error("Non empty string expected");
            return value;
        }

        private T ReadOr<T>(params Func<T>[] readers)
        {
            var startPosition = _position;

            foreach (var readerFunction in readers)
            {
                _position = startPosition;
                try
                {
                    return readerFunction();
                }
                catch (ParserError) { }   
            }
        
            throw Error("Or failed");
        }

        private List<T> ReadOneOrMore<T>(Func<T> reader)
        {
            var result = new List<T>();
            var firstElement = reader();
            result.Add(firstElement);
            
            while (true)
            {
                var backtrack = _position;
                try
                {
                    result.Add(reader());
                }
                catch (ParserError)
                {
                    _position = backtrack;
                    break;
                }
            }

            return result;
        }

        private void ReadCharsMaybe(string charset, StringBuilder to, int atMost = 10000000)
        {
            var collected = 0;
            while (!IsEnd())
            {
                if (charset.Contains(CurrentChar()))
                {
                    to.Append(TakeChar());
                    if (collected++ >= atMost)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void ReadMoreThanOneChar(string charset, StringBuilder to)
        {
            var ch = TakeChar();
            if (!charset.Contains(ch))
                throw Error($"Expected something containing these symbols: {charset}, got {ch}");

            to.Append(ch);
            ReadCharsMaybe(charset, to, 10000);
        }

        private void ReadExact(string value)
        {
            var collected = 0;
            while (!IsEnd() && collected < value.Length)
            {
                var ch = TakeChar();
                if (ch != value[collected++])
                    throw Error($"Unexpected symbol '{ch}' while parsing '{value}'");
            }
        }

        private void ReadExact(char ch)
        {
            var taken = TakeChar(); 
            if (taken != ch)
                throw Error($"Unexpected char `{taken}`, `{ch}` expected");
        }

        private void ReadSingleLineBreak()
        {
            var sb = new StringBuilder();
            ReadCharsMaybe(AnyNonBreakingWhitespace, sb);
            ReadCharsMaybe("\r", sb, 1);
            ReadExact('\n');
        }

        private void ReadLineBreaks()
        {
            var sb = new StringBuilder();
            ReadCharsMaybe(AnyNonBreakingWhitespace, sb);
            ReadCharsMaybe("\r", sb, 1);
            ReadExact('\n');
            ReadCharsMaybe(AnyWhitespace, sb);
        }
    
        public void ReadNonBreakingWhitespace()
        {
            var sb = new StringBuilder();
            ReadMoreThanOneChar(AnyNonBreakingWhitespace, sb);
        }

        public int ReadIntLiteral()
        {
            var sb = new StringBuilder();
            ReadCharsMaybe("+-", sb, 1);
            ReadMoreThanOneChar(Digits, sb);
            
            var rawValue = sb.ToString();
            if (int.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }

            throw Error($"Invalid int literal '{rawValue}'");
        }

        public float ReadFloatLiteral()
        {
            var sb = new StringBuilder();
            ReadCharsMaybe("+-", sb, 1);
            ReadCharsMaybe(Digits, sb, 10);
            ReadCharsMaybe(".", sb, 1);
            ReadCharsMaybe(Digits, sb, 10);

            var rawValue = sb.ToString();
            if (float.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }

            throw Error($"Invalid float literal '{rawValue}'");
        }

        public IBoolExpression ReadBoolExpression() => ReadOr(
            ReadAndExpression,
            ReadOrExpression,
            ReadOneBoolExpressionElement
        );
        
        public IBoolExpression ReadOneBoolExpressionElement() => ReadOr(
            ReadVisitedExpression,
            ReadHasTag,
            ReadRangeDiceResultExpression,
            ReadExactDiceResultExpression,
            ReadLiteralTrue, 
            ReadLiteralFalse
        );
        
        public HasTagExpression ReadHasTag()
        {
            ReadExact("tagged");
            ReadNonBreakingWhitespace();
            return new HasTagExpression(ReadNonEmptyStringUntilWhitespace());   
        }

        public IBoolExpression ReadVisitedExpression()
        {
            ReadExact("visited");
            ReadNonBreakingWhitespace();
            var activityName = ReadNonEmptyStringUntilWhitespace();
            return new VisitedExpression(activityName);   
        }

        public IBoolExpression ReadLiteralTrue()
        {
            ReadExact("true");
            return new LiteralTrueExpression();
        }
    
        public IBoolExpression ReadLiteralFalse()
        {
            ReadExact("false");
            return new LiteralFalseExpression();
        }

        public IBoolExpression ReadExactDiceResultExpression() => new ExactDiceResultExpression(ReadIntLiteral());
    
        public IBoolExpression ReadRangeDiceResultExpression()
        {
            var v1 = ReadIntLiteral();
            ReadExact("-");
            var v2 = ReadIntLiteral(); 
            return new RangeDiceResultExpression(v1, v2);
        }

        public IBoolExpression ReadAndExpression()
        {
            var first = ReadOneBoolExpressionElement();
            ReadNonBreakingWhitespace();
            ReadExact("and");
            ReadNonBreakingWhitespace();
            var second = ReadBoolExpression();
            return new AndExpression(first, second);
        }
    
        public IBoolExpression ReadOrExpression()
        {
            var first = ReadOneBoolExpressionElement();
            ReadNonBreakingWhitespace();
            ReadExact("or");
            ReadNonBreakingWhitespace();
            var second = ReadBoolExpression();
            return new OrExpression(first, second);
        }

        private INumberExpression ReadNumberExpression()
        {
            var value = ReadFloatLiteral();
            return new ExactFloatValue(value);
        }

        private IAction ReadCommand()
        {
            ReadExact('/');
            var commandName = ReadNonEmptyStringUntilWhitespace();
            switch (commandName)
            {
                case "rollable":
                    ReadNonBreakingWhitespace();
                    return new RollableAttribAction(ReadBoolExpression());
                case "image":
                    ReadNonBreakingWhitespace();
                    return new ImageAction(ReadNonEmptyStringUntilWhitespace());
                case "weight":
                    ReadNonBreakingWhitespace();
                    return new WeighAttribAction(ReadNumberExpression());
                case "goto":
                    ReadNonBreakingWhitespace();
                    return new GotoAction(ReadNonEmptyStringUntilWhitespace());
                case "tag":
                    ReadNonBreakingWhitespace();
                    return new TagAction(ReadNonEmptyStringUntilWhitespace());
                case "end":
                    return new EndAction();
                default:
                    throw Error($"Unexpected command {commandName}");
            }
        }

        private IAction ReadDebug()
        {
            ReadExact("//");
            var line = ReadNonEmptyStringUntilLineBreak();
            return new DebugAction(line.Trim());
        }

        private TextAction ReadTextAction()
        {
            ReadExact('-');
            ReadCharsMaybe(AnyWhitespace, new StringBuilder());
            var result = new StringBuilder();

            // First line
            result.Append(ReadNonEmptyStringUntilLineBreak());

            while (true)
            {
                var backtrackTo = _position;
                try
                {
                    //ReadSingleLineBreak();
                    ReadCharsMaybe(AnyWhitespace, new StringBuilder());
                    ReadExact('-');
                    ReadCharsMaybe(AnyNonBreakingWhitespace, new StringBuilder());
                    var line = ReadNonEmptyStringUntilLineBreak();
                    result.Append("\n");
                    result.Append(line);
                }
                catch (ParserError)
                {
                    _position = backtrackTo;
                    break;
                }
            }

            return new TextAction(result.ToString());
        }

        private IAction ReadAction() => ReadOr(
            ReadCompositeAction,
            ReadOneAction
        );

        private IAction ReadOneAction() => ReadOr(
            ReadCommand,
            ReadDebug,
            ReadConditional,
            ReadTextAction
        );

        private IAction ReadConditional()
        {
            ReadExact("if");
            ReadNonBreakingWhitespace();
            var condition = ReadBoolExpression();
            ReadSingleLineBreak();
            var action = ReadCompositeAction();
            // ReadLineBreaks();
            ReadCharsMaybe(AnyWhitespace, new StringBuilder());
            ReadExact("end");
            return new ConditionalAction(condition, action);
        }

        private CompositeAction ReadCompositeAction()
        {
            ReadCharsMaybe(AnyNonBreakingWhitespace, new StringBuilder());
            var actions = new List<IAction> { ReadOneAction() };

            while (true)
            {
                var backtrackTo = _position;
                try
                {
                    //ReadLineBreaks();
                    //ReadCharsMaybe(AnyNonBreakingWhitespace, new StringBuilder());
                    ReadCharsMaybe(AnyWhitespace, new StringBuilder());
                    actions.Add(ReadOneAction());
                }
                catch (ParserError)
                {
                    _position = backtrackTo;
                    break;
                }
            }

            return new CompositeAction(actions);
        }

        public ActivityAst ReadActivity()
        {
            ReadExact('#');
            ReadNonBreakingWhitespace();
            var name = ReadNonEmptyStringUntilWhitespace();
            ReadLineBreaks();
            var action = ReadCompositeAction();

            return new ActivityAst
            {
                Name = name,
                CompositeAction = action
            };
        }

        public List<ActivityAst> ReadActivities()
        {
            var result = new List<ActivityAst>();
            
            // Trim start
            ReadCharsMaybe(AnyWhitespace, new StringBuilder());

            var first = ReadActivity();
            result.Add(first);

            while (true)
            {
                var backtrackTo = _position;
                try
                {
                    ReadLineBreaks();
                    var activity = ReadActivity();
                    result.Add(activity);
                }
                catch (ParserError e)
                {
                    if (e.Message.Contains("EOF"))
                    {
                        return result;
                    }

                    throw;
                }
            }

            return result;
        }
    }
}