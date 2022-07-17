using System;
using System.Linq;
using Konklav;
using UnityEngine;

public abstract class Activity
{
    public virtual float Weight(Player player) => 100f;
    public virtual string Name => GetType().Name;
    public abstract string Text(Player player);
    public virtual Dice[] AvailableDice(Player player) => Array.Empty<Dice>();
    public virtual bool CanBeRolled(Player player) => false;
    public abstract void PlayerRoll(Player player, DiceRoll roll);
    public virtual void BeforeStart(Player player) {}
    public virtual void AfterEnd(Player player) {}
}

public class KonklavActivity: Activity 
{
    private readonly TextAction _defaultTextAction;
    private readonly IAction _playerAction;
    private readonly IBoolExpression _rollableExpression;
    private readonly INumberExpression _weightExpression;
    public override string Name { get; }

    public KonklavActivity(string name, TextAction defaultTextAction, IAction playerAction, IBoolExpression rollableExpression, INumberExpression weightExpression)
    {
        Name = name;
        _defaultTextAction = defaultTextAction;
        _playerAction = playerAction;
        _rollableExpression = rollableExpression;
        _weightExpression = weightExpression;
    }
    
    public override bool CanBeRolled(Player player)
    {
        if (_rollableExpression != null)
            return _rollableExpression.Evaluate(player);
        
        return false;
    }

    public override float Weight(Player player)
    {
        if (_weightExpression != null)
            return _weightExpression.EvaluateAsFloat(player);
        return 100f;
    }

    public override string Text(Player player)
    {
        return _defaultTextAction.Text;
    }

    public override void PlayerRoll(Player player, DiceRoll roll)
    {
        player.LastDiceRoll = roll;
        _playerAction?.Execute(player);
    }

    public static Activity FromAst(ActivityAst ast)
    {
        var defaultTextAction = ast.CompositeAction.Actions.OfType<TextAction>().FirstOrDefault();
        if (defaultTextAction == null)
        {
            Debug.LogError($"Missing default text action for '{ast.Name}'");    
        }
        
        
        var weightAttrib = ast.CompositeAction.Actions.OfType<WeighAttribAction>().FirstOrDefault();
        var rollableAttrib = ast.CompositeAction.Actions.OfType<RollableAttribAction>().FirstOrDefault();

        var otherActions =
            ast.CompositeAction.Actions.Where(a => a != defaultTextAction && a != weightAttrib && a != rollableAttrib)
                .ToList();
        
        if (!otherActions.Any())
            Debug.LogWarning($"No player actions defined for '{ast.Name}'");
        
        var playerAction = new CompositeAction(otherActions);
        return new KonklavActivity(ast.Name, defaultTextAction, playerAction, rollableAttrib?.Condition, weightAttrib?.Expression);
    }
}