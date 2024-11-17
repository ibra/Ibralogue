using Ibralogue.Parser;
using Ibralogue.Plugins;
using Ibralogue.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue
{
    public static class DialogueGlobals
    {
        public static readonly Dictionary<string, string> GlobalVariables = new Dictionary<string, string>();
    }

    public abstract class DialogueManagerBase : MonoBehaviour
    {
        protected ManagerPlugin[] managerPlugins;

        public UnityEvent PersistentOnConversationStart = new UnityEvent();
        public UnityEvent PersistentOnConversationEnd = new UnityEvent();

        [HideInInspector] public UnityEvent OnConversationStart = new UnityEvent();
        [HideInInspector] public UnityEvent OnConversationEnd = new UnityEvent();

        public List<Conversation> ParsedConversations { get; protected set; }
        protected Conversation _currentConversation;

        protected int _lineIndex;
        protected bool _linePlaying;

        [Header("Dialogue UI")]
        [SerializeField]
        protected float scrollSpeed = 25f;

        [SerializeField] protected TextMeshProUGUI nameText;
        [SerializeField] protected TextMeshProUGUI sentenceText;

        [Header("Choice UI")][SerializeField] protected Transform choiceButtonHolder;
        [SerializeField] protected GameObject choiceButton;
        protected List<ChoiceButton> _choiceButtonInstances = new List<ChoiceButton>();

        [Header("Function Invocations")]
        [SerializeField]
        private bool searchAllAssemblies;

        [SerializeField] private List<string> includedAssemblies = new List<string>();


        /// <summary>
        /// Starts a dialogue by parsing all the text in a file, clearing the dialogue box and starting the <see cref="DisplayDialogue"/> function.
        /// </summary>
        /// <param name="interactionDialogue">The dialogue file that we want to use in the conversation</param>
        /// <param name="startIndex">The index of the conversation you want to start.</param>
        public void StartConversation(DialogueAsset interactionDialogue, int startIndex = 0)
        {
            if (interactionDialogue == null)
                throw new ArgumentNullException(nameof(interactionDialogue));

            ParsedConversations = DialogueParser.ParseDialogue(interactionDialogue);

            if (startIndex < 0 || startIndex > ParsedConversations.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex),
                    "Expected value is between 0 and conversations count (exclusive)");

            managerPlugins = GetComponents<ManagerPlugin>();
            StartConversation(ParsedConversations[startIndex]);
        }

        /// <summary>
        /// <remarks>
        /// Should only be used inside the DialogueManager, as files should ALWAYS be parsed before any conversations
        /// are started (use the other overload method for this purpose). This function assumes that you have already parsed the dialogue file, and is to be
        /// used to avoid parsing the whole file again.
        /// </remarks>
        /// </summary>
        /// <param name="conversation"></param>
        private void StartConversation(Conversation conversation)
        {
            StopConversation();
            _currentConversation = conversation;

            OnConversationStart.AddListener(PersistentOnConversationStart.Invoke);
            OnConversationEnd.AddListener(PersistentOnConversationEnd.Invoke);

            OnConversationStart.Invoke();
            StartCoroutine(DisplayDialogue());
        }

        /// <summary>
        /// Stops the currently playing conversation and clears the dialogue box.
        /// </summary>
        public void StopConversation()
        {
            StopCoroutine(DisplayDialogue());
            ClearDialogueBox();

            _lineIndex = 0;
            _currentConversation = null;

            OnConversationEnd.Invoke();

            OnConversationStart.RemoveAllListeners();
            OnConversationEnd.RemoveAllListeners();
        }

        /// <summary>
        /// Jumps to a  given conversation in the dialogue by using its name.
        /// </summary>
        /// <param name="conversationName">Name as seen in the DialogueAsset</param>
        public void JumpTo(string conversationName)
        {
            if (ParsedConversations == null || ParsedConversations.Count == 0)
                throw new InvalidOperationException(
                    "There is no ongoing conversation, therefore the jump cannot be executed");

            Conversation conversation = ParsedConversations.Find(c => c.Name == conversationName);

            if (conversation.Name == null)
                throw new ArgumentException($"There is no {nameof(conversation)} matching the given argument",
                    nameof(conversationName));

            StartConversation(conversation);
        }

        /// <summary>
        // Displays the entire dialogue and displays choices if present.
        /// </summary>
        protected virtual IEnumerator DisplayDialogue()
        {
            _linePlaying = true;

            if (_currentConversation.Choices != null && _currentConversation.Choices.Count > 0)
            {
                KeyValuePair<Choice, int> foundChoice =
                    _currentConversation.Choices.FirstOrDefault(x => x.Value == _lineIndex);
                if (foundChoice.Key != null && _lineIndex == foundChoice.Value) DisplayChoices();
            }

            nameText.text = _currentConversation.Lines[_lineIndex].Speaker;
            sentenceText.text = _currentConversation.Lines[_lineIndex].LineContent.Text;

            foreach(ManagerPlugin plugin in managerPlugins)
            {
                plugin.Display(_currentConversation,_lineIndex);
            }

            InvokeFunctions(_currentConversation.Lines[_lineIndex].LineContent.Invocations);

            _linePlaying = false;
            yield return null;
        }

        /// <summary>
        /// Looks for functions and invokes them in a given line. The function also handles multiple return types and the parameters passed in.
        /// </summary>
        /// <param name="index">The index of the current visible character.</param>
        /// <param name="functionInvocations">The invocations inside the current line being displayed.</param>
        protected virtual void InvokeFunctions(Dictionary<int, string> functionInvocations)
        {
            IEnumerable<MethodInfo> dialogueMethods = GetDialogueMethods();

            if (functionInvocations != null)
            {
                foreach (KeyValuePair<int, string> function in functionInvocations)
                {
                    foreach (MethodInfo methodInfo in dialogueMethods)
                    {
                        if (methodInfo.Name != function.Value)
                            continue;

                        if (methodInfo.ReturnType == typeof(string))
                        {
                            string replacedText = methodInfo.GetParameters().Length > 0 ? (string)methodInfo.Invoke(null, new object[] { this }) : (string)methodInfo.Invoke(null, null);
                            string processedSentence = _currentConversation.Lines[_lineIndex].LineContent.Text
                                .Insert(function.Key, replacedText);
                            sentenceText.text = processedSentence;
                        }
                        else
                        {
                            if (methodInfo.GetParameters().Length > 0)
                            {
                                methodInfo.Invoke(null, new object[] { this });
                            }
                            else
                            {
                                methodInfo.Invoke(null, null);
                            }
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Clears the dialogue box and displays the next line if no sentences are left in the
        /// current one.
        /// </summary>
        public void TryDisplayNextLine()
        {
            if (_linePlaying) return;
            if (_currentConversation == null) return;
            if (_choiceButtonInstances.Count > 0) return;

            ClearDialogueBox();

            if (_lineIndex < _currentConversation.Lines.Count - 1)
            {
                _lineIndex++;
                StartCoroutine(DisplayDialogue());
            }
            else
            {
                StopConversation();
            }
        }


        /// <summary>
        /// Uses the Unity UI system and TextMeshPro to render choice buttons.
        /// </summary>
        protected void DisplayChoices()
        {
            _choiceButtonInstances.Clear();
            if (_currentConversation.Choices == null || !_currentConversation.Choices.Any()) return;
            foreach (Choice choice in _currentConversation.Choices.Keys)
            {
                ChoiceButton choiceButtonInstance = Instantiate(choiceButton, choiceButtonHolder).GetComponent<ChoiceButton>();
                if (choiceButtonInstance == null)
                {
                    DialogueLogger.LogError(2, "ChoiceButton is null. Make sure you have the ChoiceButton component added to your Button object!");
                }

                UnityAction onClickAction = null;
                int conversationIndex = -1;

                switch (choice.LeadingConversationName)
                {
                    case ">>":
                        DialogueLogger.LogError(2,
                            "The embedded choice is not yet implemented, '>>' keyword is reserved for future use");
                        break;
                    default:
                        conversationIndex =
                            ParsedConversations.FindIndex(c => c.Name == choice.LeadingConversationName);
                        if (conversationIndex == -1)
                            DialogueLogger.LogError(2,
                                $"No conversation called \"{choice.LeadingConversationName}\" found for choice \"{choice.ChoiceName}\" in \"{_currentConversation.Name}\".",
                                this);
                        onClickAction = () => StartConversation(ParsedConversations[conversationIndex]);
                        break;
                }

                choiceButtonInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;
                choiceButtonInstance.OnChoiceClick.AddListener(onClickAction);

                _choiceButtonInstances.Add(choiceButtonInstance);
            }
        }

        /// <summary>
        /// Gets all methods for the current assembly, other specified assemblies, or all assemblies, and checks them against the
        /// DialogueFunction attribute.
        /// </summary>
        protected IEnumerable<MethodInfo> GetDialogueMethods()
        {
            List<Assembly> assemblies = new List<Assembly>();
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (searchAllAssemblies) assemblies.AddRange(allAssemblies);
            else
                foreach (Assembly assembly in allAssemblies)
                {
                    string name = assembly.GetName().Name;
                    if (name == "Assembly-CSharp" || includedAssemblies.Contains(name) ||
                        assembly == Assembly.GetExecutingAssembly()) assemblies.Add(assembly);
                }

            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (Assembly assembly in assemblies)
            {
                IEnumerable<MethodInfo> allMethods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(DialogueFunctionAttribute), false).Length > 0);
                methods.AddRange(allMethods);
            }

            return methods;
        }

        /// <summary>
        /// Clears all text and Images in the dialogue box.
        /// </summary>
        protected virtual void ClearDialogueBox()
        {
            _linePlaying = false;
            nameText.text = string.Empty;
            sentenceText.text = string.Empty;

            foreach (ManagerPlugin plugin in managerPlugins)
            {
                plugin.Clear(_currentConversation, _lineIndex);
            }

            if (GetComponent<PortraitImagePlugin>() != null)
            {

            }

            if (_choiceButtonInstances == null)
                return;

            foreach (ChoiceButton choiceButton in _choiceButtonInstances)
            {
                choiceButton.OnChoiceClick.RemoveAllListeners();
                Destroy(choiceButton.gameObject);
            }

            _choiceButtonInstances.Clear();
        }
    }
}