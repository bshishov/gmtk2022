using Konklav;
using NUnit.Framework;

namespace KonklavTests
{
    public class Tests
    {
        [Test]
        [TestCase("100", 100f)]
        [TestCase("1", 1f)]
        [TestCase("0.01", .01f)]
        [TestCase("-10000", -10000f)]
        [TestCase("+.42", .42f)]
        [TestCase("100a", 100f)]
        [TestCase("1  asdasd", 1f)]
        [TestCase("0.01 \n", .01f)]
        [TestCase("-10000-1", -10000f)]
        [TestCase("+.42asdfvn!", .42f)]
        public void ParseValidFloat(string source, float expected)
        {
            Assert.AreEqual(new Parser(source).ReadFloatLiteral(), expected);
        }
        
        [Test]
        [TestCase("100", 100)]
        [TestCase("1", 1)]
        [TestCase("100a", 100)]
        [TestCase("1  asdasd", 1)]
        [TestCase("-10000-1", -10000)]
        public void ParseValidInteger(string source, int expected)
        {
            Assert.AreEqual(new Parser(source).ReadIntLiteral(), expected);
        }

        [Test]
        [TestCase("true")]
        [TestCase("true asdasd")]
        public void ParseTrueBoolExpression(string source)
        {
            Assert.IsInstanceOf<LiteralTrueExpression>(new Parser(source).ReadBoolExpression());
        }
        
        [Test]
        [TestCase("false")]
        [TestCase("false asdasd")]
        public void ParseFalseBoolExpression(string source)
        {
            Assert.IsInstanceOf<LiteralFalseExpression>(new Parser(source).ReadBoolExpression());
        }
        
        [Test]
        [TestCase("1-1")]
        [TestCase("2-5 kek")]
        public void ParseRangeDiceResult(string source)
        {
            Assert.IsInstanceOf<RangeDiceResultExpression>(new Parser(source).ReadRangeDiceResultExpression());
        }
        
        [Test]
        [TestCase("1")]
        [TestCase("2 kek")]
        public void ParseExactDiceResult(string source)
        {
            Assert.IsInstanceOf<ExactDiceResultExpression>(new Parser(source).ReadExactDiceResultExpression());
        }
        
        [Test]
        [TestCase("1 or 5")]
        public void ParseOr(string source)
        {
            Assert.IsInstanceOf<OrExpression>(new Parser(source).ReadOrExpression());
        }
        
        [Test]
        [TestCase("1 and true")]
        [TestCase("false and 1-2")]
        public void ParseAnd(string source)
        {
            var parser = new Parser(source);
            var expr = parser.ReadAndExpression();
            Assert.IsInstanceOf<AndExpression>(expr);
        }

        [Test]
        [TestCase("kek pek", "kek")]
        [TestCase("this is ok", "this")]
        [TestCase("this_is_ok", "this_is_ok")]
        [TestCase("kek\r\n", "kek")]
        [TestCase("kek\t", "kek")]
        public void ReadNonEmptyString(string source, string expected)
        {
            var parser = new Parser(source);
            var expr = parser.ReadNonEmptyStringUntilWhitespace();
            Assert.AreEqual(expected, expr);
        }
        
        [Test]
        [TestCase("")]
        [TestCase("\r\n")]
        [TestCase(" asdkl")]
        [TestCase("\t")]
        public void ReadNonEmptyThrows(string source)
        {
            var parser = new Parser(source);
            Assert.Throws<ParserError>(() => parser.ReadNonEmptyStringUntilWhitespace());
        }
    }
}