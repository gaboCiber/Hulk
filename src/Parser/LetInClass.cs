using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hulk.src
{
    internal class LetInClass
    {
        /* Diccionario que tiene como:
            - Key: el nombre de una variable
            - Value: un Stack que guardara los valores de cada `let` que defina para esa variable. */
        private Dictionary<string, Stack<LinkedListNode<IToken>>> GlobalVariableDictionary;

        /* Diccionario que tiene como:
            - Key: los nodos de los `let` que aparecen en la lista de token
            - Value: una lista con las variables que define ese `let */
        private Dictionary<LinkedListNode<IToken>, List<string>> LocalVariableDictionary;

        // Un stack que guarda los nodos de los `let` que aparecen en la lista de token
        private Stack<LinkedListNode<IToken>> LetStack;

        public LetInClass()
        {
            // Inicializar los campos
            GlobalVariableDictionary = new Dictionary<string, Stack<LinkedListNode<IToken>>>();
            LocalVariableDictionary = new Dictionary<LinkedListNode<IToken>, List<string>>();
            LetStack = new Stack<LinkedListNode<IToken>>();
        }

        public void AddLet(LinkedListNode<IToken> let)
        {
            // Adicionar un let (nodo) en el diccionario y la pila
            LocalVariableDictionary.Add(let, new List<string>());
            LetStack.Push(let);
        }

        public LinkedListNode<IToken>? PeekLastLet()
        {            
            // Devolver el ultimo nodo de let ingresado en la pila
            return (LetStack.Count == 0) ? null : LetStack.Peek();
        }

        public void AddVariableNameToLastLet(string variableName)
        {
            // Adicionar una variable al diccionario de los `let` 
            LocalVariableDictionary.Last().Value.Add(variableName);

            // Adicionar una variable al diccionario de los valores
            if (!GlobalVariableDictionary.ContainsKey(variableName))
                GlobalVariableDictionary.Add(variableName, new Stack<LinkedListNode<IToken>>());

        }

        public void AddVariableValue(string variableName ,  LinkedListNode<IToken> token)
        {
            // Adicionar un valor a una variable
            GlobalVariableDictionary[variableName].Push(token);
        }

        public bool ConstainsVariable(string identifier)
        {
            // Comprobar que una variable se encuentre en el diccionario
            return GlobalVariableDictionary.ContainsKey(identifier);
        }

        public LinkedListNode<IToken> PeekLastValue(string variableName)
        {
            // Devolver el ultimo valor dado a una variable 
            return new LinkedListNode<IToken>(GlobalVariableDictionary[variableName].Peek().Value);
        }

        public void RemoveLastLet()
        {
            // Remover el ultimo `let` y los valores de sus variables de los diccionarios
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
