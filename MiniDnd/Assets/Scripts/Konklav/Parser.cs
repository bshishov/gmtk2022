using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Konklav
{
    public interface IContext
    {
        int LastDiceRollValue { get; }
        void ShowText(string text);
        void Goto(string activityName);
        bool Visited(string activityName);
        void End();
    }


    public interface IBoolExpression
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
    }

    public readonly struct OrExpression : IBoolExpression
    {
        public readonly IBoolExpression[] Expressions;

        public OrExpression(params IBoolExpression[] expressions) => Expressions = expressions;

        public bool Evaluate(IContext player) => Expressions.Any(e => e.Evaluate(player));
    }

    public struct LiteralTrueExpression : IBoolExpression
    {
        public bool Evaluate(IContext player) => true; 
    }

    public struct LiteralFalseExpression : IBoolExpression
    {
        public bool Evaluate(IContext player) => true; 
    }

    public readonly struct ExactDiceResultExpression : IBoolExpression
    {
        public readonly int Value;

        public ExactDiceResultExpression(int value)
        {
            Value = value;
        }

        public bool Evaluate(IContext player) => player.LastDiceRollValue == Value;
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
    }

    public readonly struct VisitedExpression : IBoolExpression
    {
        public readonly string ActivityName;
        
        public VisitedExpression(string activityName)
        {
            ActivityName = activityName;
        }
        
        public bool Evaluate(IContext player) => player.Visited(ActivityName);
    }

    public interface INumberExpression
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
    }

    public interface IAction
    {
        void Execute(IContext player);
    }

    public class TextAction : IAction
    {
        private readonly string _text;

        public TextAction(string text)
        {
            _text = text;
        }

        public void Execute(IContext player) => player.ShowText(_text);
    }

    public class GotoAction : IAction
    {
        private readonly string _activityName;

        public GotoAction(string activityName)
        {
            _activityName = activityName;
        }

        public void Execute(IContext player) => player.Goto(_activityName);
    }
    
    public class EndAction : IAction
    {
        public void Execute(IContext player) => player.End();
    }

    public class CompositeAction : IAction
    {
        private readonly IEnumerable<IAction> _actions;

        public CompositeAction(params IAction[] actions) => _actions = actions;
        public CompositeAction(IEnumerable<IAction> actions) => _actions = actions;

        public void Execute(IContext player)
        {
            foreach (var action in _actions) action.Execute(player);
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
    }

    public class RollableAttribAction : IAction
    {
        public readonly IBoolExpression Condition;

        public RollableAttribAction(IBoolExpression condition)
        {
            Condition = condition;
        }
    
        public void Execute(IContext player) { }
    }

    public class WeighAttribAction : IAction
    {
        public readonly INumberExpression Expression;

        public WeighAttribAction(INumberExpression expression)
        {
            Expression = expression;
        }
    
        public void Execute(IContext player) { }
    }

    public class ActivityAst
    {
        public string Name;
        public IBoolExpression IsRollable;
        public INumberExpression Weight;
        public IAction Action;
    }

    public class ParserError : Exception
    {
        public readonly int Position;

        public ParserError(int position, string message)
            : base(message)
        {
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
            return new ParserError(_position, message);
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
            ReadRangeDiceResultExpression,
            ReadExactDiceResultExpression,
            ReadLiteralTrue, 
            ReadLiteralFalse
        );

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
                case "weight":
                    ReadNonBreakingWhitespace();
                    return new WeighAttribAction(ReadNumberExpression());
                case "goto":
                    ReadNonBreakingWhitespace();
                    return new GotoAction(ReadNonEmptyStringUntilWhitespace());
                case "end":
                    return new EndAction();
                default:
                    throw Error($"Unexpected command {commandName}");
            }
        }

        private TextAction ReadTextAction()
        {
            ReadExact('-');
            ReadCharsMaybe(AnyWhitespace, new StringBuilder());
            var result = new StringBuilder();

            // First line
            result.Append(ReadNonEmptyStringUntilLineBreak());
            ReadSingleLineBreak();

            while (true)
            {
                var backtrackTo = _position;
                try
                {
                    var line = ReadNonEmptyStringUntilLineBreak();
                    ReadSingleLineBreak();
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
            ReadConditional,
            ReadTextAction
        );

        private IAction ReadConditional()
        {
            ReadExact('?');
            ReadNonBreakingWhitespace();
            var condition = ReadBoolExpression();
            ReadSingleLineBreak();
            var action = ReadOneAction();
            return new ConditionalAction(condition, action);
        }

        private IAction ReadCompositeAction()
        {
            var actions = new List<IAction> { ReadOneAction() };

            while (true)
            {
                var backtrackTo = _position;
                try
                {
                    ReadLineBreaks();
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
            var action = ReadAction();

            return new ActivityAst
            {
                Name = name,
                Action = action
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
                catch (ParserError)
                {
                    _position = backtrackTo;
                    break;
                }
            }

            return result;
        }
    }
}