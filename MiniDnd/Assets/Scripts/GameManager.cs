using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Dragger Dragger;
    [SerializeField] private RealDie AttackDie;
    [SerializeField] private RealDie DefenceDie;
    [SerializeField] private BookPage BookPage;

    private Player _player;
    private List<Encounter> _encounters;
    
    private Encounter _activeEncounter;
    private Dice[] _availableDice;

    private RealDie _selectedDie;

    private void Start()
    {
        _encounters = Utils.ConstructAllObjectOfType<Encounter>().ToList();
        foreach (var encounter in _encounters)
        {
            Debug.Log($"Loaded: {encounter}");
        }

        _player = new Player();
        StartNewEncounter(RollEncounter());
        
        Dragger.DragCompleted += DraggerOnDragCompleted;
        AttackDie.MouseEnter += () => { SelectDie(AttackDie); };
        AttackDie.MouseExit += () => { SelectDie(null); };
        DefenceDie.MouseEnter += () => { SelectDie(DefenceDie); };
        DefenceDie.MouseExit += () => { SelectDie(null); };
    }

    private void Update()
    {
        // Transitions test
        if (Input.GetKeyDown(KeyCode.D)) BookPage.TransitionIn();
        if (Input.GetKeyDown(KeyCode.S)) BookPage.TransitionOut();
    }

    private void SelectDie(RealDie die)
    {
        if(Dragger.IsDragging)
            return;

        if (die != null && die.State != RealDie.DieState.Idle)
            return;

        if (_selectedDie != null)
        {
            _selectedDie.Unselect();
        }

        if (die != null)
        {
            die.Select();
            //Dragger.gameObject.SetActive(die != null);
        }

        Debug.Log($"ActiveDie = {die}");
        _selectedDie = die;
    }

    private void DraggerOnDragCompleted(Vector2 direction)
    {
        if (_selectedDie != null)
        {
            _selectedDie.Throw(new Vector3(direction.x, 0, direction.y) * 1f);
        }
    }

    private void StartNewEncounter(Encounter encounter)
    {
        // Finish last
        _activeEncounter?.AfterEncounterEnd(_player);

        // Start new
        if(encounter == null)
            return;
        
        _activeEncounter = encounter;
        _availableDice = _activeEncounter.AvailableDice(_player);
        _player.ShouldStartNewEncounter = false;
        _activeEncounter.BeforeEncounterStart(_player);
    }

    private Encounter RollEncounter()
    {
        var possible = _encounters.Where(e => e.Check(_player)).ToList();
        if (!possible.Any())
        {
            Debug.LogWarning("No encounters available");
            return null;
        }

        var rolled = Utils.Choice(possible, e => e.Weight);
        Debug.Log($"Rolled: {rolled}");
        return rolled;
    }

    private void Roll(Dice dice)
    {
        var rolledDice = DiceRoll.Random(dice);
        Debug.Log($"Rolled dice: {rolledDice}");
        _activeEncounter.Act(_player, rolledDice);

        if (_player.ShouldStartNewEncounter)
            StartNewEncounter(RollEncounter());
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 600));
        GUILayout.BeginVertical();

        if (_activeEncounter != null)
        {
            GUILayout.Label("Current encounter:");
            GUILayout.Label(_activeEncounter.Name);
            GUILayout.Space(20);
            GUILayout.Label("Text:");
            GUILayout.Label(_activeEncounter.Text(_player));
            
            GUILayout.Space(20);
            foreach (var die in _availableDice)
            {
                GUILayout.Label(die.ToString());    
            }
        }
        GUILayout.FlexibleSpace();
        
        foreach (var die in _availableDice)
        {
            if (GUILayout.Button(die.ToString())) Roll(die);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
