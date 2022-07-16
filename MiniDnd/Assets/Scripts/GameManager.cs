using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Dragger Dragger;
    [SerializeField] private RealDie AttackDie;
    [SerializeField] private RealDie DefenceDie;
    [SerializeField] private Book Book;
    [SerializeField] private TextMeshProUGUI PageText;

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
        AttackDie.RollingFinished += () =>
        {
            ApplyRoll(new DiceRoll
            {
                Dice = Dice.Attack,
                Value = AttackDie.Value
            });
        };
        
        //DefenceDie.MouseEnter += () => { SelectDie(DefenceDie); };
        //DefenceDie.MouseExit += () => { SelectDie(null); };
        
        SelectDie(AttackDie);
    }

    private void Update()
    {
        // Transitions test
        if (Input.GetKeyDown(KeyCode.D)) Book.FlipPage(() =>
        {
            PageText.text = _activeEncounter.Text(_player);
        });

        // Die test controls
        if (_selectedDie != null)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var direction = new Vector3(Random.Range(0.2f, 1f), .1f, Random.Range(0.2f, 1f));
                _selectedDie.ResetToOriginalPosition();
                _selectedDie.Throw(direction * 100);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                _selectedDie.ResetToOriginalPosition();
            }
        }
    }

    private void SelectDie(RealDie die)
    {
        if(Dragger.IsDragging)
            return;

        if (_selectedDie != null)
            _selectedDie.Unselect();

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

        PageText.text = _activeEncounter.Text(_player);
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

    private void ApplyRoll(DiceRoll rolledDice)
    {
        Debug.Log($"Rolled dice: {rolledDice}");
        _activeEncounter.Act(_player, rolledDice);

        if (_player.ShouldStartNewEncounter)
        {
            Book.FlipPage(() =>
            {
                StartNewEncounter(RollEncounter());
            });
        }
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
            if (GUILayout.Button(die.ToString())) ApplyRoll(DiceRoll.Random(die));
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
