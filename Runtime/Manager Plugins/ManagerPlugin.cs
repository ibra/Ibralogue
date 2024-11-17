using Ibralogue.Parser;
using UnityEngine;

public abstract class ManagerPlugin : MonoBehaviour
{
    public abstract void Display(Conversation currentConversation, int lineIndex);
    public abstract void Clear(Conversation currentConversation, int lineIndex);
}
