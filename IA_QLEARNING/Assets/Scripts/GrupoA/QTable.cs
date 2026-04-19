using QMind;
using System;
using System.Collections.Generic;

namespace GrupoA
{
    public class QTable
    {
        private readonly QTableStorage _storage;
        private readonly string[] _actionNames;

        public QTable(QTableStorage storage)
        {
            _storage = storage;
            _actionNames = Enum.GetNames(typeof(QAction));
        }

        private void EnsureState(string stateKey)
        {
            if (!_storage.Data.ContainsKey(stateKey))
            {
                _storage.Data[stateKey] = new float[_actionNames.Length];
            }
        }

        /// <summary>
        /// TODO(alumno):
        /// Devuelve el valor Q(s, a) correspondiente al estado y acción indicados.
        /// 
        /// Pasos recomendados:
        ///  1. Asegúrate de que el estado existe llamando a EnsureState(stateKey).
        ///  2. Convierte la acción en un índice del array:
        ///        int index = (int)action;
        ///  3. Devuelve el valor almacenado en:
        ///        _storage.Data[stateKey][index]
        /// </summary>
        public float GetQ(string stateKey, QAction action)
        {
            EnsureState(stateKey);
            int index = (int)action;
            return _storage.Data[stateKey][index];
        }


        /// <summary>
        /// TODO(alumno):
        /// Asigna el valor Q(s, a) para el estado y acción indicados.
        /// 
        /// Pasos recomendados:
        ///  1. Asegúrate de que el estado existe llamando a EnsureState(stateKey).
        ///  2. Convierte la acción en un índice del array:
        ///        int index = (int)action;
        ///  3. Guarda el valor recibido en:
        ///        _storage.Data[stateKey][index] = value;
        /// </summary>
        public void SetQ(string stateKey, QAction action, float value)
        {
            EnsureState(stateKey);
            int index = (int)action;
            _storage.Data[stateKey][index] = value;
        }


        /// <summary>
        /// TODO(alumno):
        /// Devuelve el valor máximo max_a Q(s, a) para el estado indicado.
        /// 
        /// Este método se usa en la actualización de Q-Learning:
        ///   maxQNext = GetMaxQ(nextStateKey)
        /// 
        /// Pasos recomendados:
        ///  1. Asegúrate de que el estado existe llamando a EnsureState(stateKey).
        ///  2. Obtén el array de Q-values:
        ///        var qValues = _storage.Data[stateKey];
        ///  3. Recorre el array buscando el valor máximo y devuélvelo.
        /// </summary>
        public float GetMaxQ(string stateKey)
        {
            EnsureState(stateKey);
            var qValues = _storage.Data[stateKey];

            float max = qValues[0];
            for (int i = 1; i < qValues.Length; i++)
            {
                if (qValues[i] >= max)
                    max = qValues[i];
            }
            return max;
        }


        /// <summary>
        /// TODO(alumno):
        /// Devuelve la mejor acción para el estado indicado:
        ///    argmax_a Q(s, a)
        /// 
        /// Este método se usa para:
        ///   - Política greedy (explotar lo aprendido).
        ///   - Parte "explotar" de la política ε-greedy.
        /// 
        /// Pasos recomendados:
        ///  1. Asegúrate de que el estado existe llamando a EnsureState(stateKey).
        ///  2. Obtén el array de Q-values:
        ///        var qValues = _storage.Data[stateKey];
        ///  3. Recorre el array buscando el índice del valor máximo.
        ///  4. Convierte ese índice a QAction:
        ///        return (QAction)bestIndex;
        /// </summary>
        /// 

        public QAction GetBestAction(string stateKey)
        {
            EnsureState(stateKey);
            var qValues = _storage.Data[stateKey];

            int bestIndex = 0;
            float bestValue = qValues[0];

            for (int i = 1; i < qValues.Length; i++)
            {
                if (qValues[i] > bestValue)
                {
                    bestValue = qValues[i];
                    bestIndex = i;
                }
            }

            return (QAction)bestIndex;
        }

        //public QAction GetBestAction(string stateKey)
        //{
        //    // Implementa aquí la selección de la mejor acción según la Tabla Q

        //    EnsureState(stateKey); //Se asegura que el estado existe, para ello se llama a EnsureState
        //    var qValues = _storage.Data[stateKey]; //Obtener el array de Q-values
        //    float max = float.MinValue;

        //    List<int> bestIndex = new List<int>(); //En la lista se meteran los indices más altos

        //    for (int i = 0; i < qValues.Length; i++)
        //    {
        //        if (qValues[i] > max) //Si el valor es mayor que el máximo
        //        {
        //            max = qValues[i]; //Se declara el nuevo maximo
        //            bestIndex.Clear(); //Se limpia la lista 
        //            bestIndex.Add(i); //Se introduce el nuevo máximo

        //        }
        //        else if (qValues[i] == max) //Si son iguales
        //        {
        //            bestIndex.Add(i); //Solo se añade a la lista
        //        }
        //    }
        //    return (QAction)bestIndex[UnityEngine.Random.Range(0, bestIndex.Count)];//Si hay varios indices iguales, se devuelve uno aleatorio. Por el contrario, si solo hay uno, pues se devolverá ese


        public QAction GetRandomAction(string stateKey)
        {
            EnsureState(stateKey); //Se asegura que el estado existe, para ello se llama a EnsureState

            var actions = Enum.GetValues(typeof(QAction)); //Se crea un array con las posibles direcciones
            int action = UnityEngine.Random.Range(0, actions.Length); //Se genera un numero aleatorio 0, 1, 2, 3 o 4

            return (QAction)actions.GetValue(action); //Se devuelve la acción aleatoria

        }




        public void SaveToCsv()
        {
            _storage.Save();
        }
    }
}
