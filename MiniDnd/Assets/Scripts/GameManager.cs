using System.Collections.Generic;
using System.Linq;
using Activities;
using Konklav;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Dragger Dragger;
    [SerializeField] private RealDie AttackDie;
    [SerializeField] private Book Book;
    [SerializeField] private TextMeshProUGUI PageText;

    private Player _player;
    private List<Activity> _activities = new List<Activity>();
    
    private Activity _activeActivity;
    private Dice[] _availableDice;

    private RealDie _selectedDie;

    private void Start()
    {
        LoadKonklavActivities();
        
        //foreach (var activity in Utils.ConstructAllObjectOfType<Activity>())
        // _activities.Add(activity);
        foreach (var encounter in _activities)
        {
            Debug.Log($"Loaded: {encounter}");
        }
        
        _player = new Player(ShowTextOnCurrentPage);
        StartActivity(_activities.FirstOrDefault(a => a.Name.Equals("start")));
        
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
        
        //SelectDie(AttackDie);
    }

    private void LoadKonklavActivities()
    {
        var activitySourceFiles = Resources.LoadAll<TextAsset>("Activities");
        foreach (var sourceFile in activitySourceFiles)
        {
            var parser = new Parser(sourceFile.text + "\n");
            var activities = parser.ReadActivities();

            foreach (var activityAst in activities)
            {
                Debug.Log($"Loaded {activityAst.Name}");
                
                var activity = KonklavActivity.FromAst(activityAst);
                _activities.Add(activity); 
            }
        }
    }

    private void ShowTextOnCurrentPage(string text)
    {
        Book.ChangeText(() =>
        {
            PageText.text = text;
        });
    }

    private void Update()
    {
        // Transitions test
        if (Input.GetKeyDown(KeyCode.D)) Book.FlipPage(() =>
        {
            PageText.text = _activeActivity.Text(_player);
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
            _selectedDie.Unselect();
            _selectedDie.Throw(new Vector3(direction.x, 0, direction.y) * 1f);
        }
    }

    private void StartActivity(Activity activity)
    {
        // Finish last
        _activeActivity?.AfterEnd(_player);

        // Start new
        if(activity == null)
            return;
        
        Debug.Log($"Starting: {activity.Name}");

        _activeActivity = activity;
        _availableDice = _activeActivity.AvailableDice(_player);
        _player.ShouldStartNewEncounter = false;
        _player.NextExpectedActivity = null;
        _player.VisitedHashSet.Add(activity.Name);
        _activeActivity.BeforeStart(_player);
        PageText.text = _activeActivity.Text(_player);
    }

    private Activity RollActivity()
    {
        var possible = _activities.Where(e => e.CanBeRolled(_player)).ToList();
        if (!possible.Any())
        {
            Debug.LogWarning("Failed to roll activity... no activities available");
            return null;
        }

        var rolled = Utils.Choice(possible, e => e.Weight(_player));
        Debug.Log($"Rolled: {rolled}");
        return rolled;
    }

    private void ApplyRoll(DiceRoll rolledDice)
    {
        Debug.Log($"Rolled dice: {rolledDice}");
        _activeActivity.PlayerRoll(_player, rolledDice);

        if (_player.ShouldStartNewEncounter)
        {
            Book.FlipPage(() =>
            {
                Activity nextActivity;
                if (_player.NextExpectedActivity != null)
                {
                    nextActivity = _activities.FirstOrDefault(a => a.Name.Equals(_player.NextExpectedActivity));
                }
                else
                {
                    nextActivity = RollActivity();
                }
                
                StartActivity(nextActivity);
            });
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 600));
        GUILayout.BeginVertical();

        if (_activeActivity != null)
        {
            GUILayout.Label("Current encounter:");
            GUILayout.Label(_activeActivity.Name);
            GUILayout.Space(20);
            GUILayout.Label("Text:");
            GUILayout.Label(_activeActivity.Text(_player));
        }
        GUILayout.FlexibleSpace();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
