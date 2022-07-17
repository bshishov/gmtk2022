using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Konklav;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
    
    // Debug
    private bool _showGui;
    private Vector2 _scrollPosition;
    private bool _animationIsInProgress;

    private const string WebSource =
        "https://gist.githubusercontent.com/bshishov/35eefa64b794b2003eb99b51083155ed/raw/main.txt";

    private void Start()
    {   
        Dragger.DragCompleted += DraggerOnDragCompleted;

        AttackDie.MouseEnter += () => { SelectDie(AttackDie); };
        AttackDie.MouseExit += () => { SelectDie(null); };
        AttackDie.RollingFinished += () =>
        {
            StartCoroutine(ApplyRoll(new DiceRoll
            {
                Dice = Dice.Attack,
                Value = AttackDie.Value
            }));
        };

        // SelectDie(AttackDie);
        LoadKonklavActivities();
        BeginStory();
        StartCoroutine(LoadWebContent());
    }

    private void BeginStory()
    {
        _player = new Player(ShowTextOnCurrentPage, ShowImageOnCurrentPage);
        StartActivity(_activities.FirstOrDefault(a => a.Name.Equals("start")));
    }

    private void LoadKonklavActivities()
    {
        var activitySourceFiles = Resources.LoadAll<TextAsset>("Activities");
        foreach (var sourceFile in activitySourceFiles)
        {
            LoadAllActivitiesFromSource(sourceFile.text);
        }
    }

    private void LoadAllActivitiesFromSource(string source)
    {
        var parser = new Parser(source + "\n");
        var activities = parser.ReadActivities();

        foreach (var activityAst in activities)
        {
            var newActivityName = activityAst.Name;
            var formatter = new AstFormatter();
            activityAst.FormatAst(formatter);

            Debug.Log($"Loaded {newActivityName} \n {formatter}");

            try
            {
                var activity = KonklavActivity.FromAst(activityAst);
                _activities.RemoveAll(a => a.Name.Equals(newActivityName));
                _activities.Add(activity); 
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load activity {newActivityName}: {e}");
                throw;
            }
        }
    }

    private IEnumerator LoadWebContent()
    {
        yield return new WaitForSeconds(1f);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(WebSource))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log("Loading web content...");
                    LoadAllActivitiesFromSource(webRequest.downloadHandler.text);
                    break;
            }
        }
        
        BeginStory();
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
        
        if(_animationIsInProgress)
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
        if (CanRoll())
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

    private Activity RollNewActivity()
    {
        var possible = _activities.Where(e => e.CanBeRolled(_player) && !_player.Visited(e.Name)).ToList();
        if (!possible.Any())
        {
            Debug.LogWarning("Failed to roll activity... no activities available");
            return null;
        }

        var rolled = Utils.Choice(possible, e => e.Weight(_player));
        Debug.Log($"Rolled: {rolled}");
        return rolled;
    }
    
    private IEnumerator ApplyRoll(DiceRoll rolledDice)
    {
        _animationIsInProgress = true;
        if(_selectedDie != null)
            _selectedDie.StopAllParticleSystems();
        
        _player.Debug($"Rolled dice: {rolledDice}");
        _activeActivity.PlayerRoll(_player, rolledDice);

        // Execute interaction
        for (var i = 0; i < _player.InteractionQueue.Count; i++)
        {
            var interaction = _player.InteractionQueue[i];
            
            // Wait delay
            if (interaction.Delay > 0f)
                yield return new WaitForSeconds(interaction.Delay);
            
            // Do interaction
            interaction.Action?.Invoke();
        }
        _player.InteractionQueue.Clear();

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
                    nextActivity = RollNewActivity();
                }

                if (nextActivity == null)
                    nextActivity = GetActivityByName("no_activity");

                StartActivity(nextActivity);
            });
        }

        yield return new WaitForSeconds(2f);
        _animationIsInProgress = false;
        
        if(_selectedDie != null)
            _selectedDie.StartAllParticleSystems();
    }

    private bool CanRoll()
    {
        return !_animationIsInProgress && _selectedDie != null && _selectedDie.State == RealDie.DieState.Selected;
    }
    
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
