using System;
using System.Collections.Generic;
using System.Linq;
using Konklav;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Dragger Dragger;
    [SerializeField] private RealDie AttackDie;
    [SerializeField] private Book Book;
    [SerializeField] private TextMeshProUGUI PageText;
    [SerializeField] private Image PageImage;

    private Player _player;
    private readonly List<Activity> _activities = new List<Activity>();
    
    private Activity _activeActivity;
    private Dice[] _availableDice;

    private RealDie _selectedDie;
    private bool _showGui;

    private void Start()
    {   
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

        BeginStory();
    }

    private void BeginStory()
    {
        _activities.Clear();
        LoadKonklavActivities();
        
        //foreach (var activity in Utils.ConstructAllObjectOfType<Activity>())
        // _activities.Add(activity);
        foreach (var encounter in _activities)
            Debug.Log($"Loaded: {encounter.Name}");

        _player = new Player(ShowTextOnCurrentPage, ShowImageOnCurrentPage);
        StartActivity(_activities.FirstOrDefault(a => a.Name.Equals("start")));
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
    
    private void ShowImageOnCurrentPage(string imageName)
    {
        Book.ChangeImage(() =>
        {
            PageImage.sprite = GetSpriteByName(imageName);
        });
    }

    private Sprite GetSpriteByName(string imageName)
    {
        if (imageName == null)
            imageName = "default";        
        
        var sprite = Resources.Load<Sprite>($"Images/{imageName}");
        if (sprite == null)
        {
            Debug.LogWarning($"Failed to load sprite {imageName}");
        }
        return sprite;
    }

    private void Update()
    {
        // Cheat reload
        if (Input.GetKeyDown(KeyCode.F5))
            BeginStory();

        // Cheats menu
        if (Input.GetKeyDown(KeyCode.F3))
            _showGui = !_showGui;
        
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
        if (_selectedDie != null && _selectedDie.State == RealDie.DieState.Selected)
        {
            var relativeDragDelta = new Vector2(direction.x / Screen.width, direction.y / Screen.height);
            
            _selectedDie.Unselect();
            _selectedDie.Throw(new Vector3(relativeDragDelta.x, 0, relativeDragDelta.y));
        }
    }

    private Activity GetActivityByName(string activityName)
    {
        return _activities.FirstOrDefault(a => a.Name.Equals(activityName));
    }

    private void StartActivity(Activity activity)
    {
        // Finish last
        if (_activeActivity != null)
        {
            _player.Debug("---- End ----");
            _activeActivity.AfterEnd(_player);    
        }

        // Start new
        if(activity == null)
            return;
        
        Debug.Log($"Starting: {activity.Name}");

        _activeActivity = activity;
        _availableDice = _activeActivity.AvailableDice(_player);
        _player.ShouldStartNewEncounter = false;
        _player.NextExpectedActivity = null;
        _player.VisitedHashSet.Add(activity.Name);
        _player.CurrentActivity = activity.Name;
        _player.Debug("---- Start ----");
        _activeActivity.BeforeStart(_player);
        
        PageText.text = _activeActivity.Text(_player);
        PageImage.sprite = GetSpriteByName(_activeActivity.Image(_player));
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
        _player.Debug($"Rolled dice: {rolledDice}");
        _activeActivity.PlayerRoll(_player, rolledDice);

        if (_player.ShouldStartNewEncounter)
        {
            Book.FlipPage(() =>
            {
                Activity nextActivity;
                if (_player.NextExpectedActivity != null)
                {
                    nextActivity = GetActivityByName(_player.NextExpectedActivity);
                }
                else
                {
                    nextActivity = RollActivity();
                }
                
                StartActivity(nextActivity);
            });
        }
    }

    private Vector2 _scrollPosition;
    
    private void OnGUI()
    {
        if (_showGui)
        {
            GUILayout.BeginArea(new Rect(0, 0, 200, Screen.height));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            foreach (var activity in _activities)
            {
                if (GUILayout.Button(activity.Name))
                {
                    StartActivity(GetActivityByName(activity.Name));
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
