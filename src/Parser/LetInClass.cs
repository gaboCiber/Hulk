using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hulk.src
{
    internal class LetInClass
    {
        private Dictionary<string, Stack<LinkedListNode<TokenInterface>>> GlobalVariableDictionary;

        private Dictionary<LinkedListNode<TokenInterface>, List<string>> LocalVariableDictionary;

        private Stack<LinkedListNode<TokenInterface>> LetStack;

        public LetInClass()
        {
            GlobalVariableDictionary = new Dictionary<string, Stack<LinkedListNode<TokenInterface>>>();
            LocalVariableDictionary = new Dictionary<LinkedListNode<TokenInterface>, List<string>>();
            LetStack = new Stack<LinkedListNode<TokenInterface>>();
        }

        public void AddLet(LinkedListNode<TokenInterface> let)
        {
            LocalVariableDictionary.Add(let, new List<string>());
            LetStack.Push(let);
        }

        public LinkedListNode<TokenInterface>? PeekLastLet()
        {            
            return (LetStack.Count == 0) ? null : LetStack.Peek();
        }

        public void AddVariableNameToLastLet(string variableName)
        {
            LocalVariableDictionary.Last().Value.Add(variableName);

            if (!GlobalVariableDictionary.ContainsKey(variableName))
                GlobalVariableDictionary.Add(variableName, new Stack<LinkedListNode<TokenInterface>>());

        }

        public void AddVariableValue(string variableName ,  LinkedListNode<TokenInterface> token)
        {
            GlobalVariableDictionary[variableName].Push(token);
        }

        public bool ConstainsVariable(string identifier)
        {
            return GlobalVariableDictionary.ContainsKey(identifier);
        }

        public LinkedListNode<TokenInterface> PeekLastValue(string variableName)
        {
            return new LinkedListNode<TokenInterface>(GlobalVariableDictionary[variableName].Peek().Value);
        }

        public void RemoveLastLet()
        {
            foreach (var item in LocalVariableDictionary[LetStack.Peek()])
            {
                GlobalVariableDictionary[item].Pop();

                if (GlobalVariableDictionary[item].Count == 0)
                    GlobalVariableDictionary.Remove(item);
            }

            LocalVariableDictionary.Remove(LetStack.Pop());
        }
    }
}
